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
using System.Data;
using System.Threading;
using System.Collections.Generic;

using Zongsoft.Data.Transactions;

namespace Zongsoft.Data;

/// <summary>表示事务的环境上下文和生命周期。</summary>
public sealed class TransactionContext
{
	#region 公共事件
	/// <summary>当事务上下文完成时发生。</summary>
	public event EventHandler Completed;
	#endregion

	#region 静态字段
	private static readonly AsyncLocal<TransactionContext> _current = new();
	#endregion

	#region 成员字段
	private volatile int _completion;
	private volatile int _rollbackOnly;
	private readonly Queue<IEnlistment> _enlistments;
	#endregion

	#region 构造函数
	internal TransactionContext(Transaction transaction, TransactionContext parent, IsolationLevel isolationLevel)
	{
		this.Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
		this.Parent = parent;
		this.Root = parent?.Root ?? this;
		this.Identifier = $"{Guid.NewGuid():N}";
		this.IsolationLevel = isolationLevel;
		this.Status = TransactionStatus.Active;
		this.Parameters = new();
		_enlistments = new();
	}
	#endregion

	#region 静态属性
	/// <summary>获取当前环境事务上下文。</summary>
	public static TransactionContext Current => _current.Value;
	#endregion

	#region 公共属性
	/// <summary>获取事务上下文的标识。</summary>
	public string Identifier { get; }

	/// <summary>获取事务隔离级别。</summary>
	public IsolationLevel IsolationLevel { get; }

	/// <summary>获取父事务上下文。</summary>
	public TransactionContext Parent { get; }

	/// <summary>获取根事务上下文。</summary>
	public TransactionContext Root { get; }

	/// <summary>获取事务的最终状态。</summary>
	public TransactionStatus Status { get; private set; }

	/// <summary>获取当前事务是否已经完成。</summary>
	public bool IsCompleted => this.Status != TransactionStatus.Active;

	/// <summary>获取事务范围的共享参数。</summary>
	public Collections.Parameters Parameters { get; }
	#endregion

	#region 内部属性
	internal Transaction Transaction { get; }
	#endregion

	#region 公共方法
	/// <summary>向根事务登记一个事务处理过程的回调。</summary>
	public bool Enlist(IEnlistment enlistment)
	{
		if(enlistment == null)
			throw new ArgumentNullException(nameof(enlistment));

		if(object.ReferenceEquals(this, this.Root))
			return this.EnlistCore(enlistment);

		lock(_enlistments)
		{
			return _completion == 0 && this.Root.EnlistCore(enlistment);
		}
	}
	#endregion

	#region 内部方法
	internal static void Enter(TransactionContext context) => _current.Value = context;
	internal static void Exit(TransactionContext context)
	{
		if(object.ReferenceEquals(_current.Value, context))
			_current.Value = context.Parent;
	}

	internal void Commit() => this.Complete(EnlistmentPhase.Commit);
	internal void Rollback() => this.Complete(EnlistmentPhase.Rollback);
	#endregion

	#region 重写方法
	public override string ToString() => $"{this.Identifier}({this.Status}/{this.IsolationLevel})";
	#endregion

	#region 私有方法
	private bool EnlistCore(IEnlistment enlistment)
	{
		lock(_enlistments)
		{
			if(_completion != 0 || _enlistments.Contains(enlistment))
				return false;

			_enlistments.Enqueue(enlistment);
			return true;
		}
	}

	private void Complete(EnlistmentPhase phase)
	{
		IEnlistment[] enlistments = null;
		var nested = false;

		lock(_enlistments)
		{
			if(_completion != 0)
				return;

			_completion = 1;

			if(this.Parent != null)
			{
				if(phase is EnlistmentPhase.Abort or EnlistmentPhase.Rollback)
					this.Root._rollbackOnly = 1;

				this.Status = ToStatus(phase);
				nested = true;
			}
			else
			{
				if(phase == EnlistmentPhase.Commit && _rollbackOnly != 0)
					phase = EnlistmentPhase.Rollback;

				enlistments = [.. _enlistments];
				_enlistments.Clear();
			}
		}

		if(nested)
		{
			this.OnCompleted();
			return;
		}

		List<Exception> exceptions = null;

		foreach(var enlistment in enlistments)
		{
			try
			{
				enlistment.OnEnlist(new EnlistmentContext(this.Transaction, phase));
			}
			catch(Exception exception)
			{
				(exceptions ??= []).Add(exception);
			}
		}

		this.Status = exceptions == null ? ToStatus(phase) : TransactionStatus.Undetermined;
		this.OnCompleted();

		if(exceptions?.Count == 1)
			System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exceptions[0]).Throw();
		if(exceptions?.Count > 1)
			throw new AggregateException(exceptions);
	}

	private void OnCompleted()
	{
		var handlers = this.Completed;
		if(handlers == null)
			return;

		foreach(EventHandler handler in handlers.GetInvocationList())
		{
			try
			{
				handler(this, EventArgs.Empty);
			}
			catch(Exception exception)
			{
				Diagnostics.Logging.GetLogging(this).Error(exception);
			}
		}
	}

	private static TransactionStatus ToStatus(EnlistmentPhase phase) => phase switch
	{
		EnlistmentPhase.Abort or EnlistmentPhase.Rollback => TransactionStatus.Aborted,
		EnlistmentPhase.Commit => TransactionStatus.Committed,
		_ => TransactionStatus.Undetermined,
	};
	#endregion
}
