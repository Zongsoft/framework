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

namespace Zongsoft.Data.Common.Expressions;

/// <summary>表示写入语句（即新增、更新、删除等）的接口。</summary>
public interface IMutateStatement : IStatementBase
{
	/// <summary>获取或设置写入语句对应的模式成员。</summary>
	SchemaMember Schema { get; set; }

	/// <summary>获取或设置写入语句的输出子句。</summary>
	ReturningClause Returning { get; set; }
}
