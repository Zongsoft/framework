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

using Zongsoft.Data.Transactions;

namespace Zongsoft.Data;

/// <summary>表示事务的控制范围。</summary>
public class Transaction : IDisposable, IAsyncDisposable, IEquatable<Transaction>
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

	/// <summary>提交事务。</summary>
	/// <remarks>等待所有登记回调（含数据会话的真实提交）完成后再返回，支持取消。</remarks>
	public Task CommitAsync(CancellationToken cancellation = default) => _context.CommitAsync(cancellation);

	/// <summary>回滚事务。</summary>
	public void Rollback() => _context.Rollback();

	/// <summary>回滚事务。</summary>
	/// <remarks>等待所有登记回调（含数据会话的真实回滚）完成后再返回，支持取消。</remarks>
	public Task RollbackAsync(CancellationToken cancellation = default) => _context.RollbackAsync(cancellation);
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
		//先退出环境上下文再回滚，与 DisposeAsync 的顺序保持一致；
		//避免回滚抛异常时 Exit 未执行而导致环境事务上下文残留。
		TransactionContext.Exit(_context);
		this.Rollback();
	}

	public ValueTask DisposeAsync()
	{
		//注意：AsyncLocal 写入无法穿越 async 方法的上下文边界传回调用方，
		//因此退出环境上下文的操作必须在调用方上下文中同步执行，然后再异步回滚。
		TransactionContext.Exit(_context);
		GC.SuppressFinalize(this);
		return new ValueTask(this.RollbackAsync(CancellationToken.None));
	}
	#endregion
}
