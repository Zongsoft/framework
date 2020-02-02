/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
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
	public class MsSqlExpressionVisitor : ExpressionVisitor
	{
		#region 构造函数
		public MsSqlExpressionVisitor()
		{
		}
		#endregion

		#region 公共属性
		public override IExpressionDialect Dialect
		{
			get
			{
				return MsSqlExpressionDialect.Instance;
			}
		}
		#endregion

		#region 重写方法
		protected override IExpression VisitStatement(IStatementBase statement)
		{
			switch(statement)
			{
				case TableDefinition table:
					MsSqlTableDefinitionVisitor.Instance.Visit(this, table);
					break;
				case SelectStatement select:
					MsSqlSelectStatementVisitor.Instance.Visit(this, select);
					break;
				case DeleteStatement delete:
					MsSqlDeleteStatementVisitor.Instance.Visit(this, delete);
					break;
				case InsertStatement insert:
					MsSqlInsertStatementVisitor.Instance.Visit(this, insert);
					break;
				case UpdateStatement update:
					MsSqlUpdateStatementVisitor.Instance.Visit(this, update);
					break;
				case UpsertStatement upsert:
					MsSqlUpsertStatementVisitor.Instance.Visit(this, upsert);
					break;
				case CountStatement count:
					MsSqlCountStatementVisitor.Instance.Visit(this, count);
					break;
				case ExistStatement exist:
					MsSqlExistStatementVisitor.Instance.Visit(this, exist);
					break;
				case ExecutionStatement execution:
					MsSqlExecutionStatementVisitor.Instance.Visit(this, execution);
					break;
			}

			return statement;
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

			#region 公共方法
			public string GetAlias(string alias)
			{
				return $"'{alias}'";
			}

			public string GetIdentifier(string name)
			{
				return $"[{name}]";
			}

			public string GetIdentifier(IIdentifier identifier)
			{
				if(identifier is TableDefinition tableDefinition && tableDefinition.IsTemporary)
					return "#" + tableDefinition.Name;
				if(identifier is TableIdentifier tableIdentifier && tableIdentifier.IsTemporary)
					return "#" + tableIdentifier.Name;

				return this.GetIdentifier(identifier.Name);
			}

			public string GetSymbol(Operator @operator)
			{
				return null;
			}

			public string GetDbType(DbType dbType, int length, byte precision, byte scale)
			{
				switch(dbType)
				{
					case DbType.AnsiString:
						return length > 0 ? "varchar(" + length.ToString() + ")" : "varchar(MAX)";
					case DbType.AnsiStringFixedLength:
						return length > 0 ? "char(" + length.ToString() + ")" : "char(50)";
					case DbType.String:
						return length > 0 ? "nvarchar(" + length.ToString() + ")" : "nvarchar(MAX)";
					case DbType.StringFixedLength:
						return length > 0 ? "nchar(" + length.ToString() + ")" : "nchar(50)";
					case DbType.Binary:
						return length > 0 ? "varbinary(" + length.ToString() + ")" : "varbinary(MAX)";
					case DbType.Boolean:
						return "bit";
					case DbType.Byte:
						return "tinyint";
					case DbType.SByte:
						return "smallint";
					case DbType.Date:
						return "date";
					case DbType.Time:
						return "time";
					case DbType.DateTime:
						return "datetime";
					case DbType.DateTime2:
						return "datetime2";
					case DbType.DateTimeOffset:
						return "datetimeoffset";
					case DbType.Guid:
						return "uniqueidentifier";
					case DbType.Int16:
						return "smallint";
					case DbType.Int32:
						return "int";
					case DbType.Int64:
						return "bigint";
					case DbType.Object:
						return "sql_variant";
					case DbType.UInt16:
						return "int";
					case DbType.UInt32:
						return "bigint";
					case DbType.UInt64:
						return "bigint";
					case DbType.Currency:
						return "money";
					case DbType.Decimal:
						return "decimal(" + precision.ToString() + "," + scale.ToString() + ")";
					case DbType.Double:
						return "double(" + precision.ToString() + "," + scale.ToString() + ")";
					case DbType.Single:
						return "float(" + precision.ToString() + "," + scale.ToString() + ")";
					case DbType.VarNumeric:
						return "numeric(" + precision.ToString() + "," + scale.ToString() + ")";
					case DbType.Xml:
						return "xml";
				}

				throw new DataException($"Unsupported '{dbType.ToString()}' data type.");
			}

			public string GetMethodName(MethodExpression method)
			{
				switch(method)
				{
					case AggregateExpression aggregate:
						return this.GetAggregateName(aggregate.Method);
					case SequenceExpression sequence:
						return this.GetSequenceName(sequence);
					default:
						return method.Name;
				}
			}
			#endregion

			#region 私有方法
			[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
			private string GetAggregateName(Grouping.AggregateMethod method)
			{
				switch(method)
				{
					case Grouping.AggregateMethod.Count:
						return "COUNT";
					case Grouping.AggregateMethod.Sum:
						return "SUM";
					case Grouping.AggregateMethod.Average:
						return "AVG";
					case Grouping.AggregateMethod.Maximum:
						return "MAX";
					case Grouping.AggregateMethod.Minimum:
						return "MIN";
					case Grouping.AggregateMethod.Deviation:
						return "STDEV";
					case Grouping.AggregateMethod.DeviationPopulation:
						return "STDEVP";
					case Grouping.AggregateMethod.Variance:
						return "VAR";
					case Grouping.AggregateMethod.VariancePopulation:
						return "VARP";
					default:
						throw new NotSupportedException($"Invalid '{method}' aggregate method.");
				}
			}

			[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
			private string GetSequenceName(SequenceExpression sequence)
			{
				if(string.IsNullOrEmpty(sequence.Name))
				{
					if(sequence.Method == SequenceMethod.Current)
						return "SCOPE_IDENTITY()";

					throw new DataException($"The SQL Server driver does not support the '{sequence.Method.ToString()}' sequence function without a name argument.");
				}

				switch(sequence.Method)
				{
					case SequenceMethod.Next:
						return "NEXT VALUE FOR " + sequence.Name;
					default:
						throw new DataException($"The SQL Server driver does not support the '{sequence.Method.ToString()}' sequence function with a name argument.");
				}
			}
			#endregion
		}

		private class MsSqlTableDefinitionVisitor : TableDefinitionVisitor
		{
			#region 单例字段
			public static readonly MsSqlTableDefinitionVisitor Instance = new MsSqlTableDefinitionVisitor();
			#endregion

			#region 私有构造
			private MsSqlTableDefinitionVisitor()
			{
			}
			#endregion

			#region 重写方法
			protected override void OnVisit(IExpressionVisitor visitor, TableDefinition statement)
			{
				if(statement.IsTemporary)
					visitor.Output.AppendLine($"CREATE TABLE #{statement.Name} (");
				else
					visitor.Output.AppendLine($"CREATE TABLE {statement.Name} (");

				int index = 0;

				foreach(var field in statement.Fields)
				{
					if(index++ > 0)
						visitor.Output.AppendLine(",");

					visitor.Visit(field);
				}

				visitor.Output.AppendLine(");");
			}
			#endregion
		}
		#endregion
	}
}
