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

public class Transaction : IDisposable, IEquatable<Transaction>
{
	#region 静态字段
	private static readonly AsyncLocal<Transaction> _current = new();
	#endregion

	#region 状态变量
	/*
	 * 表示事务是否已进入完成流程：零表示尚未完成，非零表示已开始提交或回滚。
	 * 该标记用于阻止后续登记并确保完成流程只执行一次，不表示事务的最终结果。
	 */
	private volatile int _isCompleted;

	/*
	 * 表示事务是否只允许回滚：零表示仍可提交，非零表示必须回滚。
	 * 子事务回滚会将该标记传播给所有父事务；被标记的父事务仍可能尚未完成。
	 */
	private volatile int _isRollbackOnly;
	#endregion

	#region 成员字段
	private readonly Transaction _parent;
	private readonly TransactionInformation _information;
	private readonly Queue<IEnlistment> _enlistments;
	#endregion

	#region 构造函数
	public Transaction() : this(IsolationLevel.ReadCommitted) { }
	public Transaction(IsolationLevel isolationLevel)
	{
		this.Status = TransactionStatus.Active;
		this.IsolationLevel = isolationLevel;

		_parent = _current.Value;
		_current.Value = this;

		//首先设置当前事务的父事务
		_information = new TransactionInformation(this);

		//创建本事务的登记集合
		_enlistments = new Queue<IEnlistment>();
	}
	#endregion

	#region 静态属性
	/// <summary>获取当前环境事务。</summary>
	public static Transaction Current => _current.Value;
	#endregion

	#region 公共属性
	/// <summary>获取当前事务的隔离级别。</summary>
	public IsolationLevel IsolationLevel { get; }

	/// <summary>获取当前事务是否已终结。</summary>
	public bool IsCompleted => _isCompleted != 0;

	/// <summary>获取当前事务的附加信息。</summary>
	public TransactionInformation Information => _information;
	#endregion

	#region 内部属性
	internal Transaction Parent => _parent;
	internal TransactionStatus Status { get; private set; }
	#endregion

	#region 静态方法
	public static Transaction ReadUncommitted() => new(IsolationLevel.ReadUncommitted);
	public static Transaction ReadCommitted() => new(IsolationLevel.ReadCommitted);
	public static Transaction RepeatableRead() => new(IsolationLevel.RepeatableRead);
	public static Transaction Serializable() => new(IsolationLevel.Serializable);
	public static Transaction Snapshot() => new(IsolationLevel.Snapshot);
	public static Transaction Chaos() => new(IsolationLevel.Chaos);
	#endregion

	#region 公共方法
	/// <summary>向当前事务登记一个事务处理过程的回调。</summary>
	/// <param name="enlistment">指定的事务处理过程的回调接口。</param>
	/// <returns>如果注册成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	public bool Enlist(IEnlistment enlistment)
	{
		if(enlistment == null)
			throw new ArgumentNullException(nameof(enlistment));

		lock(_enlistments)
		{
			//如果当前事务已经结束则无法再登记事务处理程序
			if(_isCompleted != 0)
				return false;

			//如果指定的事务处理程序已经被登记过则返回
			if(_enlistments.Contains(enlistment))
				return false;

			//将指定的事务处理程序加入到列表中
			_enlistments.Enqueue(enlistment);

			return true;
		}
	}

	/// <summary>提交事务。</summary>
	public void Commit() => this.DoEnlistment(EnlistmentPhase.Commit);
	/// <summary>回滚事务。</summary>
	public void Rollback() => this.DoEnlistment(EnlistmentPhase.Rollback);
	#endregion

	#region 重写方法
	public bool Equals(Transaction other) => other is not null && object.ReferenceEquals(this, other);
	public override bool Equals(object obj) => this.Equals(obj as Transaction);
	public override int GetHashCode() => _information.Identifier.GetHashCode();
	#endregion

	#region 私有方法
	private void DoEnlistment(EnlistmentPhase phase)
	{
		IEnlistment[] enlistments;

		lock(_enlistments)
		{
			if(_isCompleted != 0)
				return;

			_isCompleted = 1;

			//如果具有父事务则当前事务不用通知投票者(订阅者)，而是交由父事务处理
			if(_parent != null)
			{
				//子事务的回滚会使整个事务成为只能回滚的事务
				if(phase is EnlistmentPhase.Abort or EnlistmentPhase.Rollback)
					this.RequireRollback();

				//更新当前事务的状态
				this.Status = ToStatus(phase);

				//退出当前子事务
				return;
			}

			//根事务一旦被任何子事务中止，就不能再提交
			if(phase == EnlistmentPhase.Commit && _isRollbackOnly != 0)
				phase = EnlistmentPhase.Rollback;

			enlistments = [.. _enlistments];
			_enlistments.Clear();
		}

		List<Exception> exceptions = null;

		foreach(var enlistment in enlistments)
		{
			try
			{
				enlistment.OnEnlist(new EnlistmentContext(this, phase));
			}
			catch(Exception exception)
			{
				(exceptions ??= []).Add(exception);
			}
		}

		//更新当前事务的状态
		this.Status = exceptions == null ? ToStatus(phase) : TransactionStatus.Undetermined;

		if(exceptions?.Count == 1)
			System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exceptions[0]).Throw();
		if(exceptions?.Count > 1)
			throw new AggregateException(exceptions);

		static TransactionStatus ToStatus(EnlistmentPhase phase) => phase switch
		{
			EnlistmentPhase.Abort or EnlistmentPhase.Rollback => TransactionStatus.Aborted,
			EnlistmentPhase.Commit => TransactionStatus.Committed,
			_ => TransactionStatus.Undetermined,
		};
	}

	private void RequireRollback()
	{
		_isRollbackOnly = 1;
		_parent?.RequireRollback();
	}
	#endregion

	#region 处置方法
	public void Dispose()
	{
		this.Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		this.Rollback();

		//如果结束的是环境事务则恢复到父级环境事务
		if(object.ReferenceEquals(_current?.Value, this))
			_current.Value = _parent;
	}
	#endregion
}
