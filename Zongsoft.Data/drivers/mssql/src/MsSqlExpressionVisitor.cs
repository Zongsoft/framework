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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data.MsSql library.
 *
 * The Zongsoft.Data.MsSql is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.MsSql is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.MsSql library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Data;

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.MsSql
{
	public class MsSqlExpressionVisitor : ExpressionVisitorBase
	{
		#region 构造函数
		public MsSqlExpressionVisitor() { }
		#endregion

		#region 公共属性
		public override IExpressionDialect Dialect => MsSqlExpressionDialect.Instance;
		#endregion

		#region 重写方法
		protected override void VisitStatement(ExpressionVisitorContext context, IStatementBase statement)
		{
			switch(statement)
			{
				case TableDefinition table:
					MsSqlTableDefinitionVisitor.Instance.Visit(context, table);
					break;
				case SelectStatement select:
					MsSqlSelectStatementVisitor.Instance.Visit(context, select);
					break;
				case DeleteStatement delete:
					MsSqlDeleteStatementVisitor.Instance.Visit(context, delete);
					break;
				case InsertStatement insert:
					MsSqlInsertStatementVisitor.Instance.Visit(context, insert);
					break;
				case UpdateStatement update:
					MsSqlUpdateStatementVisitor.Instance.Visit(context, update);
					break;
				case UpsertStatement upsert:
					MsSqlUpsertStatementVisitor.Instance.Visit(context, upsert);
					break;
				case AggregateStatement aggregate:
					MsSqlAggregateStatementVisitor.Instance.Visit(context, aggregate);
					break;
				case ExistStatement exist:
					MsSqlExistStatementVisitor.Instance.Visit(context, exist);
					break;
				case ExecutionStatement execution:
					MsSqlExecutionStatementVisitor.Instance.Visit(context, execution);
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
					context.Write(this.Dialect.GetDataType(casting.Type, casting.Length, casting.Precision, casting.Scale));
					context.Write(",");
					this.OnVisit(context, casting.Value);

					if(!string.IsNullOrWhiteSpace(casting.Style))
						context.Write("," + casting.Style);

					context.Write(")");
					return;
				case SequenceExpression sequence:
					if(string.IsNullOrEmpty(sequence.Name))
					{
						if(sequence.Method == SequenceMethod.Current)
							context.Write("SCOPE_IDENTITY()");
						else
							throw new DataException($"The SQL Server driver does not support the '{sequence.Method}' sequence function without a name argument.");

						return;
					}

					var text = sequence.Method switch
					{
						SequenceMethod.Next => "NEXT VALUE FOR " + sequence.Name,
						_ => throw new DataException($"The SQL Server driver does not support the '{sequence.Method}' sequence function with a name argument."),
					};

					context.Write(text);

					if(!string.IsNullOrEmpty(expression.Alias))
						context.Write(" AS " + this.Dialect.GetAlias(expression.Alias));

					return;
			}

			base.VisitFunction(context, expression);
		}
		#endregion

		#region 嵌套子类
		private class MsSqlExpressionDialect : IExpressionDialect
		{
			#region 单例字段
			public static readonly MsSqlExpressionDialect Instance = new MsSqlExpressionDialect();
			#endregion

			#region 私有构造
			private MsSqlExpressionDialect()
			{
			}
			#endregion

			#region 公共属性
			public string Name => MsSqlDriver.NAME;
			#endregion

			#region 公共方法
			public string GetAlias(string alias) => $"'{alias}'";
			public string GetSymbol(Operator @operator) => null;
			public string GetIdentifier(string name) => $"[{name}]";
			public string GetIdentifier(IIdentifier identifier)
			{
				if(identifier is TableDefinition tableDefinition && tableDefinition.IsTemporary)
					return "#" + tableDefinition.Name;
				if(identifier is TableIdentifier tableIdentifier && tableIdentifier.IsTemporary)
					return "#" + tableIdentifier.Name;

				return this.GetIdentifier(identifier.Name);
			}

			public string GetDataType(DataType type, int length, byte precision, byte scale) => type.DbType switch
			{
				DbType.AnsiString => length > 0 ? $"varchar({length})" : "varchar(MAX)",
				DbType.AnsiStringFixedLength => length > 0 ? $"char({length})" : "char(50)",
				DbType.String => length > 0 ? $"nvarchar({length})" : "nvarchar(MAX)",
				DbType.StringFixedLength => length > 0 ? $"nchar({length})" : "nchar(50)",
				DbType.Binary => length > 0 ? $"varbinary({length})" : "varbinary(MAX)",
				DbType.Boolean => "bit",
				DbType.Byte => "tinyint",
				DbType.SByte => "smallint",
				DbType.Date => "date",
				DbType.Time => "time",
				DbType.DateTime => "datetime",
				DbType.DateTime2 => "datetime2",
				DbType.DateTimeOffset => "datetimeoffset",
				DbType.Guid => "uniqueidentifier",
				DbType.Int16 => "smallint",
				DbType.Int32 => "int",
				DbType.Int64 => "bigint",
				DbType.UInt16 => "int",
				DbType.UInt32 => "bigint",
				DbType.UInt64 => "bigint",
				DbType.Currency => "money",
				DbType.Decimal => $"decimal({precision},{scale})",
				DbType.Double => $"double({precision},{scale})",
				DbType.Single => $"float({precision},{scale})",
				DbType.VarNumeric => $"numeric({precision},{scale})",
				DbType.Xml => "xml",
				DbType.Object => "sql_variant",
				_ => throw new DataException($"Unsupported '{type}' data type."),
			};

			public string GetMethodName(MethodExpression method)
			{
				if(method.Name.Equals(Functions.TrimEnd, StringComparison.OrdinalIgnoreCase))
					return "RTRIM";
				if(method.Name.Equals(Functions.TrimStart, StringComparison.OrdinalIgnoreCase))
					return "LTRIM";
				if(method.Name.Equals(Functions.Random, StringComparison.OrdinalIgnoreCase))
					return "RAND";
				if(method.Name.Equals(Functions.Guid, StringComparison.OrdinalIgnoreCase))
					return "NEWID";

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
				DataAggregateFunction.DeviationPopulation => "STDEVP",
				DataAggregateFunction.Variance => "VAR",
				DataAggregateFunction.VariancePopulation => "VARP",
				_ => throw new NotSupportedException($"Invalid '{function}' aggregate method."),
			};
			#endregion
		}

		private class MsSqlTableDefinitionVisitor : TableDefinitionVisitor
		{
			#region 单例字段
			public static readonly MsSqlTableDefinitionVisitor Instance = new MsSqlTableDefinitionVisitor();
			#endregion

			#region 私有构造
			private MsSqlTableDefinitionVisitor() { }
			#endregion

			#region 重写方法
			protected override void OnVisit(ExpressionVisitorContext context, TableDefinition statement)
			{
				if(statement.IsTemporary)
					context.WriteLine($"CREATE TABLE #{statement.Name} (");
				else
					context.WriteLine($"CREATE TABLE {statement.Name} (");

				int index = 0;

				foreach(var field in statement.Fields)
				{
					if(index++ > 0)
						context.WriteLine(",");

					context.Visit(field);
				}

				context.WriteLine(");");
			}
			#endregion
		}
		#endregion
	}
}
