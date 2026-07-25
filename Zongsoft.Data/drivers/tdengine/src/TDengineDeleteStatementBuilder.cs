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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data.TDengine library.
 *
 * The Zongsoft.Data.TDengine is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.TDengine is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.TDengine library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Data;
using System.Collections.Generic;

using Zongsoft.Data.Common;
using Zongsoft.Data.Metadata;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.TDengine;

public class TDengineDeleteStatementBuilder : DeleteStatementBuilder
{
	#region 重写方法
	protected override IEnumerable<IStatementBase> BuildComplexity(DataDeleteContext context)
	{
		foreach(var statement in base.BuildComplexity(context))
		{
			if(statement is DeleteStatement deletion)
				Validate(deletion);

			yield return statement;
		}
	}
	#endregion

	#region 私有方法
	private static void Validate(DeleteStatement statement)
	{
		if(statement.Entity == null || statement.Entity.Key == null || statement.Entity.Key.Length == 0 ||
		   statement.Entity.Key[0] is not IDataEntitySimplexProperty property ||
		   !IsTimestamp(property.Type.DbType))
			throw new NotSupportedException("TDengine DELETE requires an entity whose first column is the primary timestamp.");

		if(statement.Where != null)
			Validate(statement.Where, property.GetFieldName());
	}

	private static bool IsTimestamp(DbType type) => type is
		DbType.Date or
		DbType.DateTime or
		DbType.DateTime2 or
		DbType.DateTimeOffset or
		DbType.Time;

	private static void Validate(IExpression expression, string timestamp)
	{
		switch(expression)
		{
			case FieldIdentifier field when !string.Equals(field.Name, timestamp, StringComparison.OrdinalIgnoreCase):
				throw new NotSupportedException($"TDengine DELETE conditions may only reference the primary timestamp field '{timestamp}'.");
			case BinaryExpression binary:
				Validate(binary.Left, timestamp);
				Validate(binary.Right, timestamp);
				break;
			case UnaryExpression unary:
				Validate(unary.Operand, timestamp);
				break;
			case CastFunctionExpression casting:
				Validate(casting.Value, timestamp);
				break;
			case MethodExpression method when method.Arguments != null:
				foreach(var argument in method.Arguments)
					Validate(argument, timestamp);
				break;
			case IEnumerable<IExpression> expressions:
				foreach(var item in expressions)
					Validate(item, timestamp);
				break;
		}
	}
	#endregion
}
