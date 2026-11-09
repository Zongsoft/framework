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

using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.Common.Expressions;

/// <summary>表示写入语句（包括更新、删除等语句）的基类。</summary>
public class MutateStatement : Statement, IMutateStatement
{
	#region 构造函数
	protected MutateStatement(IDataEntity entity, SchemaMember schema = null, string alias = "T", ParameterExpressionCollection parameters = null) : base(entity, alias, parameters)
	{
		this.Schema = schema;
	}
	#endregion

	#region 公共属性
	/// <summary>获取写入语句对应的模式成员。</summary>
	public SchemaMember Schema { get; set; }

	/// <summary>获取或设置 With 子句。</summary>
	public CommonTableExpressionCollection With { get; set; }

	/// <summary>获取或设置写入语句的输出子句。</summary>
	public ReturningClause Returning { get; set; }
	#endregion
}
