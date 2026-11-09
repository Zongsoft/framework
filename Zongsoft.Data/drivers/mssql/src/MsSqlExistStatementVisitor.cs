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

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.MsSql;

public class MsSqlExistStatementVisitor : ExistStatementVisitor
{
	#region 单例字段
	public static readonly MsSqlExistStatementVisitor Instance = new();
	#endregion

	#region 构造函数
	private MsSqlExistStatementVisitor() { }
	#endregion

	#region 重写方法
	protected override void VisitSelectOption(ExpressionVisitorContext context, SelectClause clause)
	{
		//调用基类同名方法
		base.VisitSelectOption(context, clause);

		//限制只返回一条记录
		context.Write("TOP(1) ");
	}
	#endregion
}
