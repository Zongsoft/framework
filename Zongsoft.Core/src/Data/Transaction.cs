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

using Zongsoft.Data.Transactions;

namespace Zongsoft.Data;

/// <summary>表示事务的控制范围。</summary>
public class Transaction : IDisposable, IEquatable<Transaction>
{
	#region 成员字段
	private readonly TransactionContext _context;
	#endregion

	#region 构造函数
	public Transaction() : this(IsolationLevel.ReadCommitted) { }
	public Transaction(IsolationLevel isolationLevel)
	{
		_context = new TransactionContext(this, TransactionContext.Current, isolationLevel);
		TransactionContext.Enter(_context);
	}
	#endregion

	#region 静态属性
	/// <summary>获取当前环境事务。</summary>
	public static Transaction Current => TransactionContext.Current?.Transaction;
	#endregion

	#region 公共属性
	/// <summary>获取当前事务的上下文。</summary>
	public TransactionContext Context => _context;

	/// <summary>获取当前事务的隔离级别。</summary>
	public IsolationLevel IsolationLevel => _context.IsolationLevel;

	/// <summary>获取当前事务是否已终结。</summary>
	public bool IsCompleted => _context.IsCompleted;
	#endregion

	#region 内部属性
	internal Transaction Parent => _context.Parent?.Transaction;
	internal TransactionStatus Status => _context.Status;
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
	public bool Enlist(IEnlistment enlistment) => _context.Enlist(enlistment);

	/// <summary>提交事务。</summary>
	public void Commit() => _context.Commit();

	/// <summary>回滚事务。</summary>
	public void Rollback() => _context.Rollback();
	#endregion

	#region 重写方法
	public bool Equals(Transaction other) => other is not null && object.ReferenceEquals(this, other);
	public override bool Equals(object obj) => this.Equals(obj as Transaction);
	public override int GetHashCode() => _context.Identifier.GetHashCode();
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
		TransactionContext.Exit(_context);
	}
	#endregion
}
