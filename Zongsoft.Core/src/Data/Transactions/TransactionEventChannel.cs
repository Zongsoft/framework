/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.com>
 *
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Components;
using Zongsoft.Communication;

namespace Zongsoft.Data.Transactions;

/// <summary>将事务中的事件延迟到事务提交后投递的事件通道。</summary>
/// <remarks>事务内的事件会被立即序列化为快照；提交完成后异步投递，回滚或结果不确定时丢弃。</remarks>
public sealed class TransactionEventChannel : ChannelBase, IEventChannel
{
	#region 成员字段
	private int _closed;
	private Task _dispatching;
	private readonly IEventChannel _channel;
	private readonly System.Threading.Channels.Channel<Snapshot> _dispatcher;
	#endregion

	#region 构造函数
	public TransactionEventChannel(IEventChannel channel)
	{
		_channel = channel ?? throw new ArgumentNullException(nameof(channel));
		_dispatcher = System.Threading.Channels.Channel.CreateUnbounded<Snapshot>(new()
		{
			SingleReader = true,
			SingleWriter = false,
		});
	}
	#endregion

	#region 公共属性
	/// <summary>获取被装饰的事件通道。</summary>
	public IEventChannel Channel => _channel;
	#endregion

	#region 公共方法
	public async ValueTask OpenAsync(EventExchanger exchanger, CancellationToken cancellation = default)
	{
		await _channel.OpenAsync(exchanger, cancellation);
		_dispatching ??= this.DispatchAsync();
	}

	public ValueTask SendAsync(EventContext context, CancellationToken cancellation = default)
	{
		ArgumentNullException.ThrowIfNull(context);

		var transaction = TransactionContext.Current?.Root;
		if(transaction == null || transaction.Status != TransactionStatus.Active)
			return _channel.SendAsync(context, cancellation);

		var snapshot = Snapshot.Create(this, context);
		transaction.Parameters.GetOrAdd(() => new TransactionEventQueue(transaction)).Enqueue(snapshot);
		return ValueTask.CompletedTask;
	}
	#endregion

	#region 重写方法
	protected override async ValueTask OnCloseAsync(CancellationToken cancellation)
	{
		if(Interlocked.Exchange(ref _closed, 1) != 0)
			return;

		_dispatcher.Writer.TryComplete();

		if(_dispatching != null)
			await _dispatching.WaitAsync(cancellation);

		await _channel.CloseAsync(cancellation);
	}

	protected override async ValueTask DisposeAsync(bool disposing)
	{
		await base.DisposeAsync(disposing);

		if(disposing)
			await _channel.DisposeAsync();
	}

	public override string ToString() => $"{nameof(TransactionEventChannel)}:{_channel}";
	#endregion

	#region 私有方法
	private void Dispatch(Snapshot snapshot)
	{
		if(!_dispatcher.Writer.TryWrite(snapshot))
			Diagnostics.Logging.GetLogging(this).Warn($"The committed event '{snapshot.QualifiedName}' cannot be dispatched because the transaction event channel is closed.");
	}

	private async Task DispatchAsync()
	{
		await foreach(var snapshot in _dispatcher.Reader.ReadAllAsync())
		{
			try
			{
				await _channel.SendAsync(snapshot.Restore());
			}
			catch(Exception exception)
			{
				Diagnostics.Logging.GetLogging(this).Error(exception);
			}
		}
	}
	#endregion

	#region 嵌套子类
	private sealed class TransactionEventQueue
	{
		private bool _completed;
		private TransactionStatus _status;
		private readonly object _syncRoot = new();
		private readonly TransactionContext _transaction;
		private readonly List<Snapshot> _snapshots = [];

		public TransactionEventQueue(TransactionContext transaction)
		{
			_transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
			_transaction.Completed += this.OnTransactionCompleted;

			if(_transaction.Status != TransactionStatus.Active)
				this.OnTransactionCompleted(_transaction, EventArgs.Empty);
		}

		public void Enqueue(Snapshot snapshot)
		{
			TransactionStatus status;

			lock(_syncRoot)
			{
				if(!_completed)
				{
					_snapshots.Add(snapshot);
					return;
				}

				status = _status;
			}

			if(status == TransactionStatus.Committed)
				snapshot.Owner.Dispatch(snapshot);
		}

		private void OnTransactionCompleted(object sender, EventArgs args)
		{
			Snapshot[] snapshots;

			lock(_syncRoot)
			{
				if(_completed)
					return;

				_completed = true;
				_status = _transaction.Status;
				snapshots = [.. _snapshots];
				_snapshots.Clear();
			}

			_transaction.Completed -= this.OnTransactionCompleted;

			if(_status != TransactionStatus.Committed)
				return;

			foreach(var snapshot in snapshots)
				snapshot.Owner.Dispatch(snapshot);
		}
	}

	private readonly struct Snapshot(TransactionEventChannel owner, EventRegistryBase registry, string name, string qualifiedName, byte[] data)
	{
		public TransactionEventChannel Owner { get; } = owner;
		public EventRegistryBase Registry { get; } = registry;
		public string Name { get; } = name;
		public string QualifiedName { get; } = qualifiedName;
		public byte[] Data { get; } = data;

		public static Snapshot Create(TransactionEventChannel owner, EventContext context) =>
			new(owner, context.Registry, context.Name, context.QualifiedName, Events.Marshaler.Marshal(context));

		public EventContext Restore()
		{
			var (argument, parameters) = Events.Marshaler.Unmarshal(this.Registry[this.Name], this.Data);
			return this.Registry.GetContext(this.Name, argument, parameters);
		}
	}
	#endregion
}
