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

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.TDengine;

public class TDengineExpressionVisitor : ExpressionVisitorBase
{
	#region 构造函数
	public TDengineExpressionVisitor() { }
	#endregion

	#region 公共属性
	public override IExpressionDialect Dialect => TDengineExpressionDialect.Instance;
	#endregion

	#region 重写方法
	protected override void VisitParameter(ExpressionVisitorContext context, ParameterExpression parameter)
	{
		/*
		 * 注意：TDengine 对参数名的首字符有不同的含义：
		 *    $ 打头表示标签字段对应的参数；
		 *    @ 打头表示数据字段对应的参数。
		 */
		if(parameter.Field.IsTagField())
			parameter.Name = '$' + parameter.Name;
		else
			parameter.Name = '@' + parameter.Name;

		context.Write('?');
	}

	protected override void VisitStatement(ExpressionVisitorContext context, IStatementBase statement)
	{
		switch(statement)
		{
			case TableDefinition table:
				TDengineTableDefinitionVisitor.Instance.Visit(context, table);
				break;
			case SelectStatement select:
				TDengineSelectStatementVisitor.Instance.Visit(context, select);
				break;
			case DeleteStatement delete:
				TDengineDeleteStatementVisitor.Instance.Visit(context, delete);
				break;
			case InsertStatement insert:
				TDengineInsertStatementVisitor.Instance.Visit(context, insert);
				break;
			case UpdateStatement:
				throw new NotSupportedException();
			case UpsertStatement:
				throw new NotSupportedException();
			case AggregateStatement aggregate:
				TDengineAggregateStatementVisitor.Instance.Visit(context, aggregate);
				break;
			case ExistStatement exist:
				TDengineExistStatementVisitor.Instance.Visit(context, exist);
				break;
			case ExecutionStatement execution:
				TDengineExecutionStatementVisitor.Instance.Visit(context, execution);
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
	private class TDengineExpressionDialect : IExpressionDialect
	{
		#region 单例字段
		public static readonly TDengineExpressionDialect Instance = new();
		#endregion

		#region 私有构造
		private TDengineExpressionDialect() { }
		#endregion

		#region 公共属性
		public string Name => TDengineDriver.NAME;
		#endregion

		#region 公共方法
		public string GetAlias(string alias) => $"`{alias}`";
		public string GetSymbol(Operator @operator) => null;
		public string GetIdentifier(string name) => $"`{name}`";
		public string GetIdentifier(IIdentifier identifier) => this.GetIdentifier(identifier.Name);

		public string GetDataType(DataType type, int length, byte precision, byte scale) => type.DbType switch
		{
			DbType.AnsiString => length > 0 ? $"varchar({length})" : "varchar(16382)",
			DbType.AnsiStringFixedLength => length > 0 ? $"varchar({length})" : "varchar(16382)",
			DbType.String => length > 0 ? $"nchar({length})" : "nchar(4096)",
			DbType.StringFixedLength => length > 0 ? $"nchar({length})" : "nchar(4096)",
			DbType.Binary => length > 0 ? $"varbinary({length})" : "varbinary(16382)",
			DbType.Boolean => "bool",
			DbType.Byte => "unsigned tinyint",
			DbType.SByte => "tinyint",
			DbType.Date => "timestamp",
			DbType.DateTime => "timestamp",
			DbType.DateTime2 => "timestamp",
			DbType.DateTimeOffset => "timestamp",
			DbType.Guid => "binary(16)",
			DbType.Int16 => "smallint",
			DbType.Int32 => "int",
			DbType.Int64 => "bigint",
			DbType.Object => "json",
			DbType.Time => "timestamp",
			DbType.UInt16 => "unsigned smallint",
			DbType.UInt32 => "unsigned int",
			DbType.UInt64 => "unsigned bigint",
			DbType.Currency => "double",
			DbType.Decimal => "double",
			DbType.Double => "double",
			DbType.Single => "float",
			DbType.VarNumeric => "double",
			DbType.Xml => "nchar(4096)",
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
		private static string GetSequenceName(SequenceExpression sequence) => throw new DataException($"The TDengine driver does not support the '{sequence.Method}' sequence function.");
		#endregion
	}

	private class TDengineTableDefinitionVisitor : TableDefinitionVisitor
	{
		#region 单例字段
		public static readonly TDengineTableDefinitionVisitor Instance = new();
		#endregion

		#region 私有构造
		private TDengineTableDefinitionVisitor() { }
		#endregion
	}
	#endregion
}
