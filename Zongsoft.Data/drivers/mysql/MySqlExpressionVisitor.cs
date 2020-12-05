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
 * This file is part of Zongsoft.Data.MySql library.
 *
 * The Zongsoft.Data.MySql is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.MySql is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.MySql library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Data;

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.MySql
{
	public class MySqlExpressionVisitor : ExpressionVisitorBase
	{
		#region 构造函数
		public MySqlExpressionVisitor() { }
		#endregion

		#region 公共属性
		public override IExpressionDialect Dialect => MySqlExpressionDialect.Instance;
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
					MySqlTableDefinitionVisitor.Instance.Visit(context, table);
					break;
				case SelectStatement select:
					MySqlSelectStatementVisitor.Instance.Visit(context, select);
					break;
				case DeleteStatement delete:
					MySqlDeleteStatementVisitor.Instance.Visit(context, delete);
					break;
				case InsertStatement insert:
					MySqlInsertStatementVisitor.Instance.Visit(context, insert);
					break;
				case UpdateStatement update:
					MySqlUpdateStatementVisitor.Instance.Visit(context, update);
					break;
				case UpsertStatement upsert:
					MySqlUpsertStatementVisitor.Instance.Visit(context, upsert);
					break;
				case AggregateStatement aggregate:
					MySqlAggregateStatementVisitor.Instance.Visit(context, aggregate);
					break;
				case ExistStatement exist:
					MySqlExistStatementVisitor.Instance.Visit(context, exist);
					break;
				case ExecutionStatement execution:
					MySqlExecutionStatementVisitor.Instance.Visit(context, execution);
					break;
				default:
					throw new DataException($"Not supported '{statement}' statement.");
			}
		}
		#endregion

		#region 嵌套子类
		private class MySqlExpressionDialect : IExpressionDialect
		{
			#region 单例字段
			public static readonly MySqlExpressionDialect Instance = new MySqlExpressionDialect();
			#endregion

			#region 私有构造
			private MySqlExpressionDialect() { }
			#endregion

			#region 公共方法
			public string GetAlias(string alias)
			{
				return $"'{alias}'";
			}

			public string GetIdentifier(string name)
			{
				return $"`{name}`";
			}

			public string GetIdentifier(IIdentifier identifier)
			{
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
						return length > 0 ? "varchar(" + length.ToString() + ")" : "text";
					case DbType.AnsiStringFixedLength:
						return length > 0 ? "char(" + length.ToString() + ")" : "text";
					case DbType.String:
						return length > 0 ? "nvarchar(" + length.ToString() + ")" : "text";
					case DbType.StringFixedLength:
						return length > 0 ? "nchar(" + length.ToString() + ")" : "text";
					case DbType.Binary:
						return length > 0 ? "varbinary(" + length.ToString() + ")" : "blob";
					case DbType.Boolean:
						return "tinyint(1)";
					case DbType.Byte:
						return "unsigned tinyint";
					case DbType.SByte:
						return "tinyint";
					case DbType.Date:
						return "date";
					case DbType.DateTime:
						return "datetime";
					case DbType.DateTime2:
						return "datetime";
					case DbType.DateTimeOffset:
						return "datetime";
					case DbType.Guid:
						return "binary(16)";
					case DbType.Int16:
						return "smallint";
					case DbType.Int32:
						return "int";
					case DbType.Int64:
						return "bigint";
					case DbType.Object:
						return "json";
					case DbType.Time:
						return "time";
					case DbType.UInt16:
						return "unsigned smallint";
					case DbType.UInt32:
						return "unsigned int";
					case DbType.UInt64:
						return "unsigned bigint";
					case DbType.Currency:
						return "decimal(12,2)";
					case DbType.Decimal:
						return "decimal(" + precision.ToString() + "," + scale.ToString() + ")";
					case DbType.Double:
						return "double(" + precision.ToString() + "," + scale.ToString() + ")";
					case DbType.Single:
						return "float(" + precision.ToString() + "," + scale.ToString() + ")";
					case DbType.VarNumeric:
						return "numeric(" + precision.ToString() + "," + scale.ToString() + ")";
					case DbType.Xml:
						return "text";
				}

				throw new DataException($"Unsupported '{dbType.ToString()}' data type.");
			}

			public string GetMethodName(MethodExpression method)
			{
				return method switch
				{
					AggregateExpression aggregate => this.GetAggregateName(aggregate.Function),
					SequenceExpression sequence => this.GetSequenceName(sequence),
					_ => method.Name,
				};
			}
			#endregion

			#region 私有方法
			[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
			private string GetAggregateName(DataAggregateFunction function)
			{
				switch(function)
				{
					case DataAggregateFunction.Count:
						return "COUNT";
					case DataAggregateFunction.Sum:
						return "SUM";
					case DataAggregateFunction.Average:
						return "AVG";
					case DataAggregateFunction.Maximum:
						return "MAX";
					case DataAggregateFunction.Minimum:
						return "MIN";
					case DataAggregateFunction.Deviation:
						return "STDEV";
					case DataAggregateFunction.DeviationPopulation:
						return "STDEV_POP";
					case DataAggregateFunction.Variance:
						return "VARIANCE";
					case DataAggregateFunction.VariancePopulation:
						return "VAR_POP";
					default:
						throw new NotSupportedException($"Invalid '{function}' aggregate method.");
				}
			}

			[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
			private string GetSequenceName(SequenceExpression sequence)
			{
				if(sequence.Method != SequenceMethod.Current)
					throw new DataException($"The MySQL driver does not support the '{sequence.Method.ToString()}' sequence function.");

				return "LAST_INSERT_ID";
			}
			#endregion
		}

		private class MySqlTableDefinitionVisitor : TableDefinitionVisitor
		{
			#region 单例字段
			public static readonly MySqlTableDefinitionVisitor Instance = new MySqlTableDefinitionVisitor();
			#endregion

			#region 私有构造
			private MySqlTableDefinitionVisitor() { }
			#endregion
		}
		#endregion
	}
}
