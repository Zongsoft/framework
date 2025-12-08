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
 * This file is part of Zongsoft.Data.PostgreSql library.
 *
 * The Zongsoft.Data.PostgreSql is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.PostgreSql is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.PostgreSql library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Data;

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.PostgreSql;

public class PostgreSqlExpressionVisitor : ExpressionVisitorBase
{
	#region 构造函数
	public PostgreSqlExpressionVisitor() { }
	#endregion

	#region 公共属性
	public override IExpressionDialect Dialect => PostgreSqlExpressionDialect.Instance;
	#endregion

	#region 重写方法
	protected override void VisitExists(ExpressionVisitorContext context, IExpression expression)
	{
		//查找当前表达式所属的语句
		var statement = context.Find<IStatement>();

		//如果当前 Exists/NotExists 表达式位于查询语句，则不需要添加额外的包裹层
		if(statement is SelectStatementBase)
		{
			base.VisitExists(context, expression);
			return;
		}

		/*
		 * 注意：由于 MySQL 不支持在 Update/Delete 等写语句中的 Exists/NotExists 子句中包含上层表别名
		 * 解决该缺陷的小窍门是对其内部的子查询语句再包裹一个查询语句，可参考：
		 * https://dev.mysql.com/doc/mysql-errors/8.0/en/server-error-reference.html
		 * https://stackoverflow.com/questions/5816840/delete-i-cant-specify-target-table
		 * https://www.codeproject.com/Tips/831164/MySQL-can-t-specify-target-table-for-update-in-FRO
		 */

		context.Write("SELECT * FROM (");
		base.VisitExists(context, expression);
		context.WriteLine(") AS t_" + Zongsoft.Common.Randomizer.GenerateString());
	}

	protected override void VisitStatement(ExpressionVisitorContext context, IStatementBase statement)
	{
		switch(statement)
		{
			case TableDefinition table:
				PostgreSqlTableDefinitionVisitor.Instance.Visit(context, table);
				break;
			case SelectStatement select:
				PostgreSqlSelectStatementVisitor.Instance.Visit(context, select);
				break;
			case DeleteStatement delete:
				PostgreSqlDeleteStatementVisitor.Instance.Visit(context, delete);
				break;
			case InsertStatement insert:
				PostgreSqlInsertStatementVisitor.Instance.Visit(context, insert);
				break;
			case UpdateStatement update:
				PostgreSqlUpdateStatementVisitor.Instance.Visit(context, update);
				break;
			case UpsertStatement upsert:
				PostgreSqlUpsertStatementVisitor.Instance.Visit(context, upsert);
				break;
			case AggregateStatement aggregate:
				PostgreSqlAggregateStatementVisitor.Instance.Visit(context, aggregate);
				break;
			case ExistStatement exist:
				PostgreSqlExistStatementVisitor.Instance.Visit(context, exist);
				break;
			case ExecutionStatement execution:
				PostgreSqlExecutionStatementVisitor.Instance.Visit(context, execution);
				break;
			default:
				throw new DataException($"Not supported '{statement}' statement.");
		}
	}

	protected override void VisitFunction(ExpressionVisitorContext context, MethodExpression expression)
	{
		switch(expression)
		{
			case CastFunctionExpression casting:
				context.Write("CONVERT(");
				this.OnVisit(context, casting.Value);
				context.Write(",");
				context.Write(this.Dialect.GetDataType(casting.Type, casting.Length, casting.Precision, casting.Scale));
				context.Write(")");
				return;
			case SequenceExpression sequence:
				var serial = sequence.Arguments != null && sequence.Arguments.Count > 0 ? sequence.Arguments[0]?.ToString() : null;

				var text = sequence.Method switch
				{
					SequenceMethod.Current => string.IsNullOrEmpty(serial) ? "lastval()" : $"currval('{serial}')",
					SequenceMethod.Next => $"nextval('{serial ?? sequence.Name}')",
					_ => throw new NotSupportedException($"Invalid '{sequence.Method}' sequence method."),
				};

				context.Write(text);

				if(!string.IsNullOrEmpty(expression.Alias))
					context.Write(" AS " + this.Dialect.GetAlias(expression.Alias));

				return;
		}

		base.VisitFunction(context, expression);
	}

	protected override void VisitParameter(ExpressionVisitorContext context, ParameterExpression parameter)
	{
		base.VisitParameter(context, parameter);

		/*
		 * 注意：如果
		 *   参数类型是 非Unicode 字符，且
		 *   参数值包含 Unicode 字符，且
		 *   参数位于条件内，则需要为参数添加 COLLATE 字符集说明，以免 MySQL 执行错误！
		 */
		if(IsAnsiString(parameter.Type) && IsNonAnsiString(parameter.Value) && context.Find<ConditionExpression>() != null)
			context.Write($" COLLATE utf8mb4_0900_ai_ci");

		static bool IsAnsiString(DbType type) => type == DbType.AnsiString || type == DbType.AnsiStringFixedLength;
		static bool IsNonAnsiString(object value)
		{
			if(value == null || Convert.IsDBNull(value))
				return false;

			if(value is string text)
			{
				var span = text.AsSpan();

				for(int i = 0; i < span.Length; i++)
				{
					if(!char.IsAscii(span[i]))
						return true;
				}
			}

			return false;
		}
	}
	#endregion

	#region 嵌套子类
	private class PostgreSqlExpressionDialect : IExpressionDialect
	{
		#region 单例字段
		public static readonly PostgreSqlExpressionDialect Instance = new PostgreSqlExpressionDialect();
		#endregion

		#region 私有构造
		private PostgreSqlExpressionDialect() { }
		#endregion

		#region 公共属性
		public string Name => PostgreSqlDriver.NAME;
		#endregion

		#region 公共方法
		public string GetAlias(string alias) => $"\"{alias}\"";
		public string GetSymbol(Operator @operator) => null;
		public string GetIdentifier(string name) => $"\"{name}\"";
		public string GetIdentifier(IIdentifier identifier) => this.GetIdentifier(identifier.Name);

		public string GetDataType(DataType type, int length, byte precision, byte scale) => type.DbType switch
		{
			DbType.AnsiString => length > 0 ? $"varchar({length})" : "text",
			DbType.AnsiStringFixedLength => length > 0 ? $"char({length})" : "text",
			DbType.String => length > 0 ? $"varchar({length})" : "text",
			DbType.StringFixedLength => length > 0 ? $"char({length})" : "text",
			DbType.Binary => length > 0 ? $"bytea" : "bytea",
			DbType.Boolean => "boolean",
			DbType.Byte => "smallint",
			DbType.SByte => "smallint",
			DbType.Date => "date",
			DbType.DateTime => "timestamp",
			DbType.DateTime2 => "timestamp",
			DbType.DateTimeOffset => "timestamp with time zone",
			DbType.Guid => "uuid",
			DbType.Int16 => "smallint",
			DbType.Int32 => "int",
			DbType.Int64 => "bigint",
			DbType.Time => "interval",
			DbType.UInt16 => "smallint",
			DbType.UInt32 => "int",
			DbType.UInt64 => "bigint",
			DbType.Currency => precision > 0 ? $"numeric({precision},{scale})" : "numeric(12,2)",
			DbType.Decimal => $"numeric({precision},{scale})",
			DbType.Double => $"double({precision},{scale})",
			DbType.Single => $"real({precision},{scale})",
			DbType.VarNumeric => $"numeric({precision},{scale})",
			DbType.Xml => "xml",
			DbType.Object => type.Name,
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
		#endregion
	}

	private class PostgreSqlTableDefinitionVisitor : TableDefinitionVisitor
	{
		#region 单例字段
		public static readonly PostgreSqlTableDefinitionVisitor Instance = new PostgreSqlTableDefinitionVisitor();
		#endregion

		#region 私有构造
		private PostgreSqlTableDefinitionVisitor() { }
		#endregion
	}
	#endregion
}
