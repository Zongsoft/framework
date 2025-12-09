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
 * Copyright (C) 2010-2022 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data.ClickHouse library.
 *
 * The Zongsoft.Data.ClickHouse is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.ClickHouse is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.ClickHouse library. If not, see <http://www.gnu.org/licenses/>.
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

		for(int i = 0; i < statement.Fields.Count; i++)
		{
			var field = statement.Fields[i];
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
