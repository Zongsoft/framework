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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.Features;

/// <summary>提供新增、更新、增改和删除操作的返回值能力。</summary>
/// <remarks>实例可被多个操作并发共享，不得保存单次操作的可变状态。</remarks>
public interface IReturningFeature
{
	/// <summary>判断指定操作是否支持指定种类的返回值。</summary>
	/// <param name="method">实际语句对应的操作类型。</param>
	/// <param name="kind">返回值的种类。</param>
	/// <returns>支持指定操作的返回值种类则返回真，否则返回假。</returns>
	bool Supports(DataAccessMethod method, ReturningKind kind);

	/// <summary>执行当前语句的写入及返回值读取。</summary>
	/// <param name="context">当前写入上下文。</param>
	/// <param name="statement">当前写入语句。</param>
	/// <param name="execute">绕过特性分派的普通写入回调，至多调用一次；原生返回值由此回调处理。</param>
	/// <param name="populate">消费模拟返回值读取器的回调，不负责释放读取器，不得保留到本次执行结束之后。</param>
	/// <param name="cancellation">操作的取消令牌。</param>
	/// <returns>返回是否继续执行从属语句；假不表示需要回退到其它执行策略。</returns>
	ValueTask<bool> ExecuteAsync(IDataMutateContext context, IMutateStatement statement,
		Func<CancellationToken, ValueTask<bool>> execute,
		Func<DbDataReader, CancellationToken, ValueTask<bool>> populate,
		CancellationToken cancellation = default);
}

/// <summary>提供通过原生语句读取写入返回值的默认实现。</summary>
public class ReturningFeature : IReturningFeature
{
	/// <summary>表示支持新增新值、更新及增改新旧值和删除旧值的默认特性。</summary>
	public static readonly ReturningFeature Instance = new();

	private readonly bool _older;

	/// <summary>初始化原生返回值特性。</summary>
	/// <param name="older">指示更新和增改是否支持返回旧值；新增、更新及增改的新值和删除的旧值始终支持。</param>
	public ReturningFeature(bool older = true) => _older = older;

	/// <summary>判断指定操作是否支持指定种类的返回值。</summary>
	/// <param name="method">实际语句对应的操作类型。</param>
	/// <param name="kind">返回值的种类。</param>
	/// <returns>支持指定操作的返回值种类则返回真，否则返回假。</returns>
	public virtual bool Supports(DataAccessMethod method, ReturningKind kind) => method switch
	{
		DataAccessMethod.Insert => kind == ReturningKind.Newer,
		DataAccessMethod.Update or DataAccessMethod.Upsert => kind == ReturningKind.Newer || _older && kind == ReturningKind.Older,
		DataAccessMethod.Delete => kind == ReturningKind.Older,
		_ => false,
	};

	/// <summary>执行当前语句的写入及返回值读取。</summary>
	/// <param name="context">当前写入上下文。</param>
	/// <param name="statement">当前写入语句。</param>
	/// <param name="execute">绕过特性分派的普通写入回调，至多调用一次；原生返回值由此回调处理。</param>
	/// <param name="populate">消费模拟返回值读取器的回调，不负责释放读取器。</param>
	/// <param name="cancellation">操作的取消令牌。</param>
	/// <returns>返回是否继续执行从属语句。</returns>
	public ValueTask<bool> ExecuteAsync(IDataMutateContext context, IMutateStatement statement,
		Func<CancellationToken, ValueTask<bool>> execute,
		Func<DbDataReader, CancellationToken, ValueTask<bool>> populate, CancellationToken cancellation = default)
	{
		ArgumentNullException.ThrowIfNull(context);
		ArgumentNullException.ThrowIfNull(statement);
		ArgumentNullException.ThrowIfNull(execute);
		ArgumentNullException.ThrowIfNull(populate);
		cancellation.ThrowIfCancellationRequested();

		return this.OnExecuteAsync(context, statement, execute, populate, cancellation);
	}

	/// <summary>执行返回值策略，默认委托普通写入回调完成。</summary>
	/// <param name="context">当前写入上下文。</param>
	/// <param name="statement">当前写入语句。</param>
	/// <param name="execute">绕过特性分派的普通写入回调。</param>
	/// <param name="populate">消费模拟返回值读取器的回调。</param>
	/// <param name="cancellation">操作的取消令牌。</param>
	/// <returns>返回是否继续执行从属语句。</returns>
	protected virtual ValueTask<bool> OnExecuteAsync(IDataMutateContext context, IMutateStatement statement,
		Func<CancellationToken, ValueTask<bool>> execute,
		Func<DbDataReader, CancellationToken, ValueTask<bool>> populate, CancellationToken cancellation) => execute(cancellation);
}
