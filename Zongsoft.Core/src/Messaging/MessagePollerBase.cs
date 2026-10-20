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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Messaging;

/// <summary>提供消息队列轮询功能的类。</summary>
public abstract class MessagePollerBase : IMessagePoller
{
	#region 成员字段
	private readonly SemaphoreSlim _lifecycle = new(1, 1);
	private CancellationTokenSource _cancellation;
	private Task _polling = Task.CompletedTask;
	private TaskCompletionSource _disposal;
	#endregion

	#region 公共属性
	public bool IsDisposed => Volatile.Read(ref _disposal)?.Task.IsCompleted == true;
	public bool IsPolling => !Volatile.Read(ref _polling).IsCompleted;
	#endregion

	#region 生命周期
	public ValueTask StartAsync(CancellationToken cancellation = default) => this.StartAsync(null, 1000, cancellation);
	public async ValueTask StartAsync(MessageDequeueOptions options, int interval = 1000, CancellationToken cancellation = default)
	{
		await _lifecycle.WaitAsync(cancellation).ConfigureAwait(false);

		try
		{
			ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposal) != null, this);
			if(!_polling.IsCompleted)
				return;

			await _polling.ConfigureAwait(false);
			_cancellation?.Dispose();
			_cancellation = new CancellationTokenSource();

			var token = _cancellation.Token;
			_polling = Task.Run(() => this.PollAsync(options ?? MessageDequeueOptions.Default, Math.Clamp(interval, 100, 60000), token));
		}
		finally { _lifecycle.Release(); }
	}

	public async ValueTask StopAsync(CancellationToken cancellation = default)
	{
		await _lifecycle.WaitAsync(cancellation).ConfigureAwait(false);

		try
		{
			if(_cancellation != null)
				await _cancellation.CancelAsync().ConfigureAwait(false);

			await _polling.ConfigureAwait(false);
		}
		finally
		{
			_cancellation?.Dispose();
			_cancellation = null;
			_lifecycle.Release();
		}
	}
	#endregion

	#region 轮询方法
	private async Task PollAsync(MessageDequeueOptions options, int interval, CancellationToken cancellation)
	{
		try
		{
			while(!cancellation.IsCancellationRequested)
			{
				try
				{
					var message = await this.ReceiveAsync(options, cancellation).ConfigureAwait(false);
					cancellation.ThrowIfCancellationRequested();

					if(message.IsEmpty)
						await Task.Delay(interval, cancellation).ConfigureAwait(false);
					else
						await this.OnHandleAsync(message, cancellation).ConfigureAwait(false);
				}
				catch(OperationCanceledException) when(cancellation.IsCancellationRequested) { break; }
				catch(Exception exception)
				{
					await Diagnostics.Logging.GetLogging(this).ErrorAsync(exception, cancellation).ConfigureAwait(false);
					await Task.Delay(interval, cancellation).ConfigureAwait(false);
				}
			}
		}
		catch(OperationCanceledException) when(cancellation.IsCancellationRequested) { }
	}

	protected abstract ValueTask<Message> ReceiveAsync(MessageDequeueOptions options, CancellationToken cancellation);
	protected abstract ValueTask OnHandleAsync(Message message, CancellationToken cancellation);
	protected virtual ValueTask DisposeAsync(bool disposing) => ValueTask.CompletedTask;
	#endregion

	#region 资源释放
	public ValueTask DisposeAsync()
	{
		var disposal = Volatile.Read(ref _disposal);
		if(disposal == null)
		{
			var candidate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			disposal = Interlocked.CompareExchange(ref _disposal, candidate, null) ?? candidate;
			if(ReferenceEquals(disposal, candidate))
				_ = this.DisposeAsyncCore(candidate);
		}

		return new ValueTask(disposal.Task);
	}

	private async Task DisposeAsyncCore(TaskCompletionSource completion)
	{
		Exception failure = null;

		try { await this.StopAsync().ConfigureAwait(false); }
		catch(Exception exception) { failure = exception; }

		try { await this.DisposeAsync(true).ConfigureAwait(false); }
		catch(Exception exception) { failure = failure == null ? exception : new AggregateException(failure, exception); }

		GC.SuppressFinalize(this);

		if(failure == null)
			completion.TrySetResult();
		else
			completion.TrySetException(failure);
	}
	#endregion
}
