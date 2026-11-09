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
using System.Collections.Generic;

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.ClickHouse;

public class ClickHouseInsertStatementVisitor : InsertStatementVisitor
{
	#region 单例字段
	public static readonly ClickHouseInsertStatementVisitor Instance = new();
	#endregion

	#region 构造函数
	private ClickHouseInsertStatementVisitor() { }
	#endregion

	#region 重写方法
	protected override void VisitInsert(ExpressionVisitorContext context, InsertStatement statement)
	{
		if(statement.Options.ConstraintIgnored)
			context.Write("INSERT IGNORE INTO ");
		else
			context.Write("INSERT INTO ");
	}

	protected override void VisitValues(ExpressionVisitorContext context, InsertStatement statement, ICollection<IExpression> values, int rounds)
	{
		context.WriteLine(" VALUES");

		for(int i = 0; i < statement.Values.Count; i++)
		{
			var field = statement.Fields[i % rounds];
			var value = statement.Values[i];

			if(i > 0)
				context.Write(",");

			if(i % rounds == 0)
				context.Write("(");

			var parenthesisRequired = value is IStatementBase;

			if(parenthesisRequired)
				context.Write("(");

			context.Write("{");
			context.Visit(value);
			context.Write(":");
			context.Write(ClickHouseUtility.GetDataType(field.Token.Property));
			context.Write("}");

			if(parenthesisRequired)
				context.Write(")");

			if((i + 1) % rounds == 0)
				context.Write(")");
		}
	}
	#endregion
}
