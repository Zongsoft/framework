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
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Data.Transactions;

namespace Zongsoft.Data;

/// <summary>表示事务的环境上下文和生命周期。</summary>
public sealed partial class TransactionContext
{
	#region 公共事件
	/// <summary>当事务上下文完成时发生。</summary>
	public event Func<object, EventArgs, CancellationToken, ValueTask> Completed;
	#endregion

	#region 静态字段
	private static readonly AsyncLocal<TransactionContext> _current = new();
	#endregion

	#region 成员字段
	private TaskCompletionSource _completion;
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
		this.OnInitialized();
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
	/// <summary>获取创建此上下文的事务。</summary>
	internal Transaction Transaction { get; }
	#endregion

	#region 公共方法
	/// <summary>向根事务登记一个事务处理过程的回调。</summary>
	/// <param name="enlistment">要登记的非空事务回调。</param>
	/// <returns>登记成功返回真；事务已开始终结或回调已登记时返回假。</returns>
	public bool Enlist(IEnlistment enlistment)
	{
		if(enlistment == null)
			throw new ArgumentNullException(nameof(enlistment));

		if(object.ReferenceEquals(this, this.Root))
			return this.EnlistCore(enlistment);

		lock(this.Root._enlistments)
		{
			return _completion == null && this.Root.EnlistCore(enlistment);
		}
	}
	#endregion

	#region 内部方法
	/// <summary>将指定上下文设为当前调用流的环境事务。</summary>
	/// <param name="context">进入的事务上下文。</param>
	internal static void Enter(TransactionContext context) => _current.Value = context;
	/// <summary>退出当前上下文并恢复其父上下文。</summary>
	/// <param name="context">要退出的事务上下文；非当前上下文时不执行。</param>
	internal static void Exit(TransactionContext context)
	{
		if(object.ReferenceEquals(_current.Value, context))
			_current.Value = context.Parent;
	}

	/// <summary>提交事务并等待登记的异步回调全部结束。</summary>
	/// <param name="cancellation">仅在提交决议被接受前响应的取消令牌。</param>
	/// <returns>事务提交及完成通知结束的任务。</returns>
	internal Task CommitAsync(CancellationToken cancellation = default) => this.CompleteAsync(EnlistmentPhase.Commit, null, cancellation);
	/// <summary>回滚事务并等待登记的异步回调全部结束。</summary>
	/// <param name="cancellation">调用方的取消令牌；事务终结不受取消影响。</param>
	/// <returns>事务回滚及完成通知结束的任务。</returns>
	internal Task RollbackAsync(CancellationToken cancellation = default) => this.CompleteAsync(EnlistmentPhase.Rollback, null, CancellationToken.None);
	/// <summary>回滚事务，并在终结失败时合并引发回滚的业务异常。</summary>
	/// <param name="exception">仅在首次接受回滚决议时采用的业务异常。</param>
	/// <param name="cancellation">调用方的取消令牌；事务终结不受取消影响。</param>
	/// <returns>事务回滚及完成通知结束的共享任务。</returns>
	internal Task RollbackAsync(Exception exception, CancellationToken cancellation = default) => this.CompleteAsync(EnlistmentPhase.Rollback, exception, CancellationToken.None);
	#endregion

	#region 重写方法
	/// <summary>获取包含标识、状态与隔离级别的事务描述。</summary>
	/// <returns>当前事务上下文的文本表示。</returns>
	public override string ToString() => $"{this.Identifier}({this.Status}/{this.IsolationLevel})";
	#endregion

	#region 分部方法
	/// <summary>在事务上下文进入调用环境前初始化内部扩展。</summary>
	partial void OnInitialized();

	/// <summary>在用户完成通知前发布内部条件特性的最终结果。</summary>
	partial void OnFinalized();
	#endregion

	#region 私有方法
	/// <summary>在事务尚未终结时登记不重复的回调。</summary>
	/// <param name="enlistment">待登记的事务回调。</param>
	/// <returns>登记成功返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
	private bool EnlistCore(IEnlistment enlistment)
	{
		lock(this.Root._enlistments)
		{
			if(_completion != null || _enlistments.Contains(enlistment))
				return false;

			_enlistments.Enqueue(enlistment);
			return true;
		}
	}

	/// <summary>接受一次终结决议，所有调用方等待同一完成结果。</summary>
	/// <param name="phase">请求的终结阶段。</param>
	/// <param name="cause">首次终结失败时需要保留的业务异常。</param>
	/// <param name="cancellation">只在提交决议被接受前响应的取消令牌。</param>
	/// <returns>所有登记回调及完成通知结束的共享任务。</returns>
	private Task CompleteAsync(EnlistmentPhase phase, Exception cause, CancellationToken cancellation = default)
	{
		IEnlistment[] enlistments;
		TaskCompletionSource completion;

		lock(this.Root._enlistments)
		{
			if(_completion != null)
				return _completion.Task;

			if(cancellation.IsCancellationRequested)
				return Task.FromCanceled(cancellation);

			_completion = completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

			if(this.Parent != null)
			{
				if(phase is EnlistmentPhase.Abort or EnlistmentPhase.Rollback)
					this.Root._rollbackOnly = 1;

				enlistments = [];
			}
			else
			{
				if(phase == EnlistmentPhase.Commit && _rollbackOnly != 0)
					phase = EnlistmentPhase.Rollback;

				enlistments = [.. _enlistments];
				_enlistments.Clear();
			}
		}

		//先发布共享任务，再调用外部回调；所有失败由该任务传递给等待者
		_ = this.CompleteAsyncCore(phase, cause, enlistments, completion);

		//返回共享任务给所有等待者，避免重复调用外部回调
		return completion.Task;
	}

	private async Task CompleteAsyncCore(EnlistmentPhase phase, Exception cause, IEnlistment[] enlistments, TaskCompletionSource completion)
	{
		try
		{
			List<Exception> exceptions = null;

			foreach(var enlistment in enlistments)
			{
				try
				{
					await enlistment.OnEnlistAsync(new EnlistmentContext(this.Transaction, phase), CancellationToken.None).ConfigureAwait(false);
				}
				catch(Exception exception)
				{
					(exceptions ??= []).Add(exception);
				}
			}

			this.Status = exceptions == null ? ToStatus(phase) : TransactionStatus.Undetermined;
			this.OnFinalized();

			var handlers = this.Completed;
			if(handlers != null)
			{
				foreach(Func<object, EventArgs, CancellationToken, ValueTask> handler in handlers.GetInvocationList())
				{
					try
					{
						await handler(this, EventArgs.Empty, CancellationToken.None).ConfigureAwait(false);
					}
					catch(Exception exception)
					{
						(exceptions ??= []).Add(exception);
					}
				}
			}

			if(exceptions == null)
				completion.TrySetResult();
			else
				completion.TrySetException(GetException(cause, exceptions));
		}
		catch(Exception exception)
		{
			completion.TrySetException(GetException(cause, [exception]));
		}

		static Exception GetException(Exception cause, List<Exception> exceptions)
		{
			if(cause == null)
				return exceptions.Count == 1 ? exceptions[0] : new AggregateException(exceptions);

			//业务异常只在终结失败时参与聚合，不参与事务状态判定
			exceptions.RemoveAll(exception => object.ReferenceEquals(exception, cause));
			exceptions.Insert(0, cause);

			return new AggregateException(exceptions);
		}
	}

	/// <summary>将事务终结阶段转换为最终状态。</summary>
	/// <param name="phase">事务终结阶段。</param>
	/// <returns>对应的提交、终止或未决状态。</returns>
	private static TransactionStatus ToStatus(EnlistmentPhase phase) => phase switch
	{
		EnlistmentPhase.Abort or EnlistmentPhase.Rollback => TransactionStatus.Aborted,
		EnlistmentPhase.Commit => TransactionStatus.Committed,
		_ => TransactionStatus.Undetermined,
	};
	#endregion
}
