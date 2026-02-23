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
 * This file is part of Zongsoft.Data.SQLite library.
 *
 * The Zongsoft.Data.SQLite is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.SQLite is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.SQLite library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Data;

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.SQLite;

public class SQLiteExpressionVisitor : ExpressionVisitorBase
{
	#region 构造函数
	public SQLiteExpressionVisitor() { }
	#endregion

	#region 公共属性
	public override IExpressionDialect Dialect => SQLiteExpressionDialect.Instance;
	#endregion

	#region 重写方法
	protected override void VisitStatement(ExpressionVisitorContext context, IStatementBase statement)
	{
		switch(statement)
		{
			case TableDefinition table:
				SQLiteTableDefinitionVisitor.Instance.Visit(context, table);
				break;
			case SelectStatement select:
				SQLiteSelectStatementVisitor.Instance.Visit(context, select);
				break;
			case DeleteStatement delete:
				SQLiteDeleteStatementVisitor.Instance.Visit(context, delete);
				break;
			case InsertStatement insert:
				SQLiteInsertStatementVisitor.Instance.Visit(context, insert);
				break;
			case UpdateStatement update:
				SQLiteUpdateStatementVisitor.Instance.Visit(context, update);
				break;
			case UpsertStatement upsert:
				SQLiteUpsertStatementVisitor.Instance.Visit(context, upsert);
				break;
			case AggregateStatement aggregate:
				SQLiteAggregateStatementVisitor.Instance.Visit(context, aggregate);
				break;
			case ExistStatement exist:
				SQLiteExistStatementVisitor.Instance.Visit(context, exist);
				break;
			case ExecutionStatement execution:
				SQLiteExecutionStatementVisitor.Instance.Visit(context, execution);
				break;
			default:
				throw new DataException($"Not supported '{statement}' statement.");
		}
	}

	protected override void VisitFunction(ExpressionVisitorContext context, MethodExpression expression)
	{
		if(expression is CastFunctionExpression casting)
		{
			context.Write("CONVERT(");
			this.OnVisit(context, casting.Value);
			context.Write(",");
			context.Write(this.Dialect.GetDataType(casting.Type, casting.Length, casting.Precision, casting.Scale));
			context.Write(")");

			return;
		}

		base.VisitFunction(context, expression);
	}
	#endregion

	#region 嵌套子类
	private class SQLiteExpressionDialect : IExpressionDialect
	{
		#region 单例字段
		public static readonly SQLiteExpressionDialect Instance = new();
		#endregion

		#region 私有构造
		private SQLiteExpressionDialect() { }
		#endregion

		#region 公共属性
		public string Name => SQLiteDriver.NAME;
		#endregion

		#region 公共方法
		public string GetAlias(string alias) => $"'{alias}'";
		public string GetSymbol(Operator @operator) => null;
		public string GetIdentifier(string name) => $"\"{name}\"";
		public string GetIdentifier(IIdentifier identifier) => this.GetIdentifier(identifier.Name);
		public string GetIdentifier(ReturningKind kind) => null;

		public string GetDataType(DataType type, int length, byte precision, byte scale) => type.DbType switch
		{
			DbType.AnsiString => length > 0 ? $"varchar({length})" : "text",
			DbType.AnsiStringFixedLength => length > 0 ? $"char({length})" : "text",
			DbType.String => length > 0 ? $"nvarchar({length})" : "text",
			DbType.StringFixedLength => length > 0 ? $"nchar({length})" : "text",
			DbType.Binary => length > 0 ? $"varbinary({length})" : "blob",
			DbType.Boolean => "tinyint(1)",
			DbType.Byte => "unsigned tinyint",
			DbType.SByte => "tinyint",
			DbType.Date => "date",
			DbType.DateTime => "datetime",
			DbType.DateTime2 => "datetime",
			DbType.DateTimeOffset => "datetime",
			DbType.Guid => "binary(16)",
			DbType.Int16 => "smallint",
			DbType.Int32 => "int",
			DbType.Int64 => "bigint",
			DbType.Time => "time",
			DbType.UInt16 => "unsigned smallint",
			DbType.UInt32 => "unsigned int",
			DbType.UInt64 => "unsigned bigint",
			DbType.Currency => precision > 0 ? $"decimal({precision},{scale})" : "decimal(12,2)",
			DbType.Decimal => $"decimal({precision},{scale})",
			DbType.Double => $"double({precision},{scale})",
			DbType.Single => $"float({precision},{scale})",
			DbType.VarNumeric => $"numeric({precision},{scale})",
			DbType.Xml => "text",
			DbType.Object => type.ToString(),
			_ => throw new DataException($"Unsupported '{type}' data type."),
		};

		public string GetMethodName(MethodExpression method)
		{
			if(method.Name.Equals(Functions.IsNull, StringComparison.OrdinalIgnoreCase))
				return "IFNULL";
			if(method.Name.Equals(Functions.Stuff, StringComparison.OrdinalIgnoreCase))
				return "INSERT";
			if(method.Name.Equals(Functions.Replicate, StringComparison.OrdinalIgnoreCase))
				return "REPEAT";
			if(method.Name.Equals(Functions.Substring, StringComparison.OrdinalIgnoreCase))
				return "SUBSTR";
			if(method.Name.Equals(Functions.TrimEnd, StringComparison.OrdinalIgnoreCase))
				return "RTRIM";
			if(method.Name.Equals(Functions.TrimStart, StringComparison.OrdinalIgnoreCase))
				return "LTRIM";
			if(method.Name.Equals(Functions.Random, StringComparison.OrdinalIgnoreCase))
				return "RAND";
			if(method.Name.Equals(Functions.Guid, StringComparison.OrdinalIgnoreCase))
				return "UUID";

			return method switch
			{
				AggregateExpression aggregate => GetAggregateName(aggregate.Function),
				SequenceExpression sequence => GetSequenceName(sequence),
				_ => method.Name,
			};
		}
		#endregion

		#region 私有方法
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static string GetAggregateName(DataAggregateFunction function) => function switch
		{
			DataAggregateFunction.Count => "COUNT",
			DataAggregateFunction.Sum => "SUM",
			DataAggregateFunction.Average => "AVG",
			DataAggregateFunction.Maximum => "MAX",
			DataAggregateFunction.Minimum => "MIN",
			DataAggregateFunction.Deviation => "STDEV",
			DataAggregateFunction.DeviationPopulation => "STDEV_POP",
			DataAggregateFunction.Variance => "VARIANCE",
			DataAggregateFunction.VariancePopulation => "VAR_POP",
			_ => throw new NotSupportedException($"Invalid '{function}' aggregate method."),
		};

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static string GetSequenceName(SequenceExpression sequence)
		{
			if(sequence.Method != SequenceMethod.Current)
				throw new DataException($"The SQLite driver does not support the '{sequence.Method.ToString()}' sequence function.");

			return "LAST_INSERT_ID";
		}
		#endregion
	}

	private class SQLiteTableDefinitionVisitor : TableDefinitionVisitor
	{
		#region 单例字段
		public static readonly SQLiteTableDefinitionVisitor Instance = new();
		#endregion

		#region 私有构造
		private SQLiteTableDefinitionVisitor() { }
		#endregion
	}
	#endregion
}
