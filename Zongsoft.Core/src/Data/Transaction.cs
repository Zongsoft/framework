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

using Zongsoft.Components;

namespace Zongsoft.Data;

/// <summary>表示事务的控制范围。</summary>
public class Transaction : IAsyncDisposable, IEquatable<Transaction>
{
	#region 静态字段
	/// <summary>表示捕获当前事务环境特性的共享提供者。</summary>
	/// <remarks>每次捕获时读取当前根事务；返回的特性绑定到该事务，不随之后的环境变化。</remarks>
	public static readonly IFeatureProvider Features = new FeatureProvider();
	#endregion

	#region 成员字段
	/// <summary>当前事务对应的环境上下文。</summary>
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
	/// <summary>提交事务。</summary>
	/// <param name="cancellation">仅在接受提交决议前响应取消；已经开始的终结不受取消影响。</param>
	/// <returns>表示事务提交及全部登记回调完成的任务。</returns>
	/// <remarks>等待所有登记回调（含数据会话的真实提交）完成后再返回；提交决议一经接受便不可取消。</remarks>
	public Task CommitAsync(CancellationToken cancellation = default) => _context.CommitAsync(cancellation);

	/// <summary>回滚事务。</summary>
	/// <param name="cancellation">保留用于兼容现有调用；事务终结不响应此取消标记。</param>
	/// <returns>表示事务回滚及全部登记回调完成的任务。</returns>
	/// <remarks>等待所有登记回调（含数据会话的真实回滚）完成后再返回；事务终结一旦调用便不会被取消。</remarks>
	public Task RollbackAsync(CancellationToken cancellation = default) => _context.RollbackAsync(cancellation);

	/// <summary>回滚事务，并在回滚失败时保留引发回滚的业务异常。</summary>
	/// <param name="exception">引发回滚的业务异常；为空时只传播事务终结异常。</param>
	/// <param name="cancellation">保留用于兼容现有调用；事务终结不响应此取消标记。</param>
	/// <returns>表示事务回滚及全部登记回调和完成通知结束的任务。</returns>
	/// <remarks>
	///		<para>回滚及完成通知成功时正常返回，由调用方决定是否重新抛出业务异常；失败时将非空业务异常与终结异常合并为聚合异常。</para>
	///		<para>事务终结不响应取消。已经开始终结时复用已有结果，不重复回滚，也不追加异常；后续释放复用同一结果。</para>
	/// </remarks>
	public Task RollbackAsync(Exception exception, CancellationToken cancellation = default) => _context.RollbackAsync(exception, cancellation);
	#endregion

	#region 重写方法
	public bool Equals(Transaction other) => other is not null && object.ReferenceEquals(this, other);
	public override bool Equals(object obj) => this.Equals(obj as Transaction);
	public override int GetHashCode() => _context.Identifier.GetHashCode();
	#endregion

	#region 处置方法
	public ValueTask DisposeAsync()
	{
		//注意：AsyncLocal 写入无法穿越 async 方法的上下文边界传回调用方，
		//因此退出环境上下文的操作必须在调用方上下文中同步执行，然后再异步回滚。
		TransactionContext.Exit(_context);

		GC.SuppressFinalize(this);
		return new ValueTask(_context.RollbackAsync(CancellationToken.None));
	}
	#endregion

	#region 嵌套子类
	/// <summary>提供当前根事务绑定的条件特性。</summary>
	private sealed class FeatureProvider : IFeatureProvider
	{
		/// <summary>捕获当前根事务共享的条件特性。</summary>
		/// <returns>根事务的条件特性；没有当前事务时为空集合。</returns>
		public IEnumerable<IFeature> GetFeatures()
		{
			var context = TransactionContext.Current?.Root;
			return context?.GetFeatures() ?? [];
		}
	}
	#endregion
}
