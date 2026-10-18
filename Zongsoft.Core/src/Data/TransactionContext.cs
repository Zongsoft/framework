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

		lock(_enlistments)
		{
			return _completion == 0 && this.Root.EnlistCore(enlistment);
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

	/// <summary>提交事务并等待登记的同步回调全部结束。</summary>
	internal void Commit() => this.Complete(EnlistmentPhase.Commit);
	/// <summary>提交事务并等待登记的异步回调全部结束。</summary>
	/// <param name="cancellation">调用方的取消令牌；事务终结不受取消影响。</param>
	/// <returns>事务提交及完成通知结束的任务。</returns>
	internal Task CommitAsync(CancellationToken cancellation = default) => this.CompleteAsync(EnlistmentPhase.Commit, cancellation);
	/// <summary>回滚事务并等待登记的同步回调全部结束。</summary>
	internal void Rollback() => this.Complete(EnlistmentPhase.Rollback);
	/// <summary>回滚事务并等待登记的异步回调全部结束。</summary>
	/// <param name="cancellation">调用方的取消令牌；事务终结不受取消影响。</param>
	/// <returns>事务回滚及完成通知结束的任务。</returns>
	internal Task RollbackAsync(CancellationToken cancellation = default) => this.CompleteAsync(EnlistmentPhase.Rollback, cancellation);
	#endregion

	#region 重写方法
	/// <summary>获取包含标识、状态与隔离级别的事务描述。</summary>
	/// <returns>当前事务上下文的文本表示。</returns>
	public override string ToString() => $"{this.Identifier}({this.Status}/{this.IsolationLevel})";
	#endregion

	#region 分部方法
	/// <summary>在事务上下文进入调用环境前初始化内部扩展。</summary>
	partial void OnInitialized();
	#endregion

	#region 私有方法
	/// <summary>在事务尚未终结时登记不重复的回调。</summary>
	/// <param name="enlistment">待登记的事务回调。</param>
	/// <returns>登记成功返回 <see langword="true"/>，否则返回 <see langword="false"/>。</returns>
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

	/// <summary>同步终结事务，并汇总登记回调的执行结果。</summary>
	/// <param name="phase">请求执行的事务终结阶段。</param>
	private void Complete(EnlistmentPhase phase)
	{
		if(!this.TryComplete(ref phase, out var enlistments, out var nested))
			return;

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

		this.FinishCompletion(phase, exceptions);
	}

	/// <summary>异步终结事务，并等待登记回调实际完成。</summary>
	/// <param name="phase">请求执行的事务终结阶段。</param>
	/// <param name="cancellation">调用方的取消令牌；事务终结不受取消影响。</param>
	/// <returns>所有登记回调及完成通知结束的任务。</returns>
	private async Task CompleteAsync(EnlistmentPhase phase, CancellationToken cancellation)
	{
		//事务终结不可取消：保留取消参数只为维持调用契约，不将其传入登记回调或底层资源终结
		_ = cancellation;

		if(!this.TryComplete(ref phase, out var enlistments, out var nested))
			return;

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
				//异步执行事务登记回调，确保其真实提交/回滚完成后再继续后续流程
				await enlistment.OnEnlistAsync(new EnlistmentContext(this.Transaction, phase), CancellationToken.None).ConfigureAwait(false);
			}
			catch(Exception exception)
			{
				(exceptions ??= []).Add(exception);
			}
		}

		this.FinishCompletion(phase, exceptions);
	}

	/// <summary>以互斥方式开始终结，并确定实际阶段及待执行的登记回调。</summary>
	/// <param name="phase">请求的终结阶段；仅允许回滚时将提交改为回滚。</param>
	/// <param name="enlistments">输出根事务的登记回调快照。</param>
	/// <param name="nested">输出当前上下文是否属于嵌套事务。</param>
	/// <returns>首次开始终结返回 <see langword="true"/>，已开始终结返回 <see langword="false"/>。</returns>
	private bool TryComplete(ref EnlistmentPhase phase, out IEnlistment[] enlistments, out bool nested)
	{
		lock(_enlistments)
		{
			if(_completion != 0)
			{
				enlistments = null;
				nested = false;
				return false;
			}

			_completion = 1;

			if(this.Parent != null)
			{
				if(phase is EnlistmentPhase.Abort or EnlistmentPhase.Rollback)
					this.Root._rollbackOnly = 1;

				this.Status = ToStatus(phase);
				enlistments = null;
				nested = true;
			}
			else
			{
				if(phase == EnlistmentPhase.Commit && _rollbackOnly != 0)
					phase = EnlistmentPhase.Rollback;

				enlistments = [.. _enlistments];
				_enlistments.Clear();
				nested = false;
			}

			return true;
		}
	}

	/// <summary>保存最终状态并通知订阅者，随后传播登记回调的异常。</summary>
	/// <param name="phase">实际执行的事务终结阶段。</param>
	/// <param name="exceptions">登记回调的异常集合；<see langword="null"/> 表示全部成功。</param>
	private void FinishCompletion(EnlistmentPhase phase, List<Exception> exceptions)
	{
		this.Status = exceptions == null ? ToStatus(phase) : TransactionStatus.Undetermined;
		this.OnCompleted();

		if(exceptions?.Count == 1)
			System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exceptions[0]).Throw();
		if(exceptions?.Count > 1)
			throw new AggregateException(exceptions);
	}

	/// <summary>依次通知完成事件的订阅者，并记录订阅者异常。</summary>
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
