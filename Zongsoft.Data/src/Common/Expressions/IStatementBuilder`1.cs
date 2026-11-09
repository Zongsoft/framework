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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Zongsoft.Data.Common.Expressions;

/// <summary>提供生成 <see cref="IStatementBase"/> 数据操作语句的接口。</summary>
public interface IStatementBuilder<TContext> where TContext : IDataAccessContext
{
	/// <summary>根据指定的数据访问上下文生成对应的数据操作语句。</summary>
	/// <param name="context">指定的数据访问上下文。</param>
	/// <param name="cancellation">验证与准备语句的取消令牌。</param>
	/// <returns>返回生成的数据操作语句。</returns>
	IAsyncEnumerable<IStatementBase> BuildAsync(TContext context, CancellationToken cancellation = default);
}
