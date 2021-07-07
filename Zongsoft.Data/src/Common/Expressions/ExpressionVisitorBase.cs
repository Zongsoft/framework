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
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Data;

namespace Zongsoft.Data.Common.Expressions
{
	public abstract class ExpressionVisitorBase : IExpressionVisitor
	{
		#region 事件定义
		public event EventHandler<ExpressionEventArgs> Unrecognized;
		#endregion

		#region 构造函数
		protected ExpressionVisitorBase() { }
		#endregion

		#region 公共属性
		public abstract IExpressionDialect Dialect { get; }
		#endregion

		#region 公共方法
		public string Visit(IExpression expression)
		{
			if(expression == null)
				return null;

			//构建访问上下文对象
			var context = this.CreateContext();

			//执行具体的访问操作
			this.OnVisit(context, expression);

			//返回访问的输出内容
			return context.Output.ToString();
		}
		#endregion

		#region 虚拟方法
		protected virtual ExpressionVisitorContext CreateContext()
		{
			return new ExpressionVisitorContext(this);
		}

		internal protected virtual void OnVisit(ExpressionVisitorContext context, IExpression expression)
		{
			if(expression == null)
				return;

			try
			{
				context.Stack.Push(expression);

				switch(expression)
				{
					case BlockExpression block:
						this.VisitBlock(context, block);
						break;
					case TableIdentifier table:
						this.VisitTable(context, table);
						break;
					case FieldIdentifier field:
						this.VisitField(context, field);
						break;
					case FieldDefinition field:
						this.VisitField(context, field);
						break;
					case VariableIdentifier variable:
						this.VisitVariable(context, variable);
						break;
					case ParameterExpression parameter:
						this.VisitParameter(context, parameter);
						break;
					case LiteralExpression literal:
						this.VisitLiteral(context, literal);
						break;
					case CommentExpression comment:
						this.VisitComment(context, comment);
						break;
					case ConstantExpression constant:
						this.VisitConstant(context, constant);
						break;
					case UnaryExpression unary:
						this.VisitUnary(context, unary);
						break;
					case BinaryExpression binary:
						this.VisitBinary(context, binary);
						break;
					case RangeExpression range:
						this.VisitRange(context, range);
						break;
					case MethodExpression method:
						this.VisitMethod(context, method);
						break;
					case ConditionExpression condition:
						this.VisitCondition(context, condition);
						break;
					case ExpressionCollection collection:
						this.VisitCollection(context, collection);
						break;
					case IStatementBase statement:
						this.VisitStatement(context, statement);
						break;
					default:
						this.OnUnrecognized(context);
						break;
				}
			}
			finally
			{
				context.Stack.Pop();
			}
		}

		protected virtual void VisitBlock(ExpressionVisitorContext context, BlockExpression block)
		{
			if(block.Count == 0)
				return;

			int index = 0;

			foreach(var item in block)
			{
				//添加分割符以区隔各块元素
				if(index++ > 0 && block.Delimiter != BlockExpressionDelimiter.None)
					context.Write(this.GetDelimiter(block.Delimiter));

				this.OnVisit(context, item);
			}
		}

		protected virtual void VisitTable(ExpressionVisitorContext context, TableIdentifier table)
		{
			context.Write(this.GetIdentifier(table));

			if(!string.IsNullOrEmpty(table.Alias) && !string.Equals(table.Name, table.Alias))
				context.Write(" AS " + table.Alias);
		}

		protected virtual void VisitField(ExpressionVisitorContext context, FieldIdentifier field)
		{
			var alias = field.Table?.Alias;

			if(field.Table is IStatementBase statement && !string.IsNullOrEmpty(statement.Table.Alias))
				alias = statement.Table.Alias;

			if(field.Table == null || string.IsNullOrEmpty(alias))
				context.Write(this.GetIdentifier(field));
			else
				context.Write(alias + "." + this.GetIdentifier(field));

			if(!string.IsNullOrEmpty(field.Alias))
				context.Write(" AS " + this.GetAlias(field.Alias));
		}

		protected virtual void VisitField(ExpressionVisitorContext context, FieldDefinition field)
		{
			context.Write($"{field.Name} {this.Dialect.GetDbType(field.DbType, field.Length, field.Precision, field.Scale)}");

			if(field.Nullable)
				context.Write(" NULL");
			else
				context.Write(" NOT NULL");
		}

		protected virtual void VisitVariable(ExpressionVisitorContext context, VariableIdentifier variable)
		{
			if(variable.IsGlobal)
				context.Write("@@" + variable.Name);
			else
				context.Write("@" + variable.Name);
		}

		protected virtual void VisitParameter(ExpressionVisitorContext context, ParameterExpression parameter)
		{
			context.Write("@" + parameter.Name);
		}

		protected virtual void VisitLiteral(ExpressionVisitorContext context, LiteralExpression literal)
		{
			//输出字面量文本
			if(!string.IsNullOrEmpty(literal.Text))
				context.Write(literal.Text);
		}

		protected virtual void VisitComment(ExpressionVisitorContext context, CommentExpression comment)
		{
			//输出注释文本
			if(!string.IsNullOrEmpty(comment.Text))
				context.Write("/* " + comment.Text + " */");
		}

		protected virtual void VisitConstant(ExpressionVisitorContext context, ConstantExpression constant)
		{
			if(constant.Value == null)
			{
				context.Write("NULL");
				return;
			}

			if(constant.ValueType == typeof(bool) || Zongsoft.Common.TypeExtension.IsNumeric(constant.ValueType))
				context.Write(constant.Value.ToString());
			else
				context.Write("'" + constant.Value.ToString() + "'");
		}

		protected virtual void VisitUnary(ExpressionVisitorContext context, UnaryExpression unary)
		{
			switch(unary.Operator)
			{
				case Operator.Not:
				case Operator.Negate:
				case Operator.Exists:
				case Operator.NotExists:
					context.Write(this.GetSymbol(unary.Operator));
					break;
			}

			//只有常量和标记表达式才不需要加小括号
			bool parenthesisRequired = !(unary.Operand is ConstantExpression || unary.Operand is IIdentifier);

			if(parenthesisRequired)
				context.Write("(");

			if(unary.Operator == Operator.Exists || unary.Operator == Operator.NotExists)
				this.VisitExists(context, unary.Operand);
			else
				this.OnVisit(context, unary.Operand);

			if(parenthesisRequired)
				context.Write(")");
		}

		protected virtual void VisitExists(ExpressionVisitorContext context, IExpression expression)
		{
			this.OnVisit(context, expression);
		}

		protected virtual void VisitBinary(ExpressionVisitorContext context, BinaryExpression expression)
		{
			//值表达式(右元)是否需要括号包裹
			var parenthesisRequired = expression.Right is IStatementBase;

			switch(expression.Operator)
			{
				case Operator.Equal:
					if(Expression.IsNull(expression.Right))
						expression.Operator = Operator.Is;
					break;
				case Operator.NotEqual:
					if(Expression.IsNull(expression.Right))
						expression.Operator = Operator.NotIs;
					break;

				case Operator.All:
				case Operator.Any:
				case Operator.In:
				case Operator.NotIn:
				case Operator.Exists:
				case Operator.NotExists:
					parenthesisRequired = true;
					break;
			}

			//获取当前双目表达式的运算优先级
			var precedence = expression.Operator.GetPrecedence();

			//如果左元的运算优先级低于当前双目表达式则表示左元需要采用括号进行组合(提级)
			var precedenceCompound = expression.Left is BinaryExpression left && left.Operator.GetPrecedence() < precedence;

			//如果左元是一个语句也需要使用括号
			if(precedenceCompound || expression.Left is IStatementBase)
				context.Write("(");

			this.OnVisit(context, expression.Left);

			if(precedenceCompound || expression.Left is IStatementBase)
				context.Write(")");

			context.Write(this.GetSymbol(expression.Operator));

			//如果不是必须带括号的计算符，则计算左元的运算优先级以确认是否需要生成括号
			if(!parenthesisRequired)
				parenthesisRequired = expression.Right is BinaryExpression right && right.Operator.GetPrecedence() < precedence;

			if(parenthesisRequired)
				context.Write("(");

			this.OnVisit(context, expression.Right);

			if(parenthesisRequired)
				context.Write(")");
		}

		protected virtual void VisitRange(ExpressionVisitorContext context, RangeExpression expression)
		{
			this.OnVisit(context, expression.Minimum);
			context.Write(" AND ");
			this.OnVisit(context, expression.Maximum);
		}

		protected virtual void VisitMethod(ExpressionVisitorContext context, MethodExpression expression)
		{
			var methodName = this.Dialect.GetMethodName(expression);

			//先输出方法或变量名
			context.Write(methodName);

			if(expression.MethodType == MethodType.Function)
			{
				context.Write("(");

				if(expression is AggregateExpression aggregate && aggregate.Distinct)
					context.Write("DISTINCT ");

				if(expression.Arguments != null)
				{
					var index = 0;

					foreach(var argument in expression.Arguments)
					{
						if(index++ > 0)
							context.Write(",");

						var parenthesized = argument != null && typeof(IStatementBase).IsAssignableFrom(argument.GetType());

						if(parenthesized)
							context.Write("(");

						this.OnVisit(context, argument);

						if(parenthesized)
							context.Write(")");
					}
				}

				context.Write(")");
			}

			if(!string.IsNullOrEmpty(expression.Alias))
				context.Write(" AS " + this.GetAlias(expression.Alias));
		}

		protected virtual void VisitCondition(ExpressionVisitorContext context, ConditionExpression condition)
		{
			if(condition == null || condition.Count == 0)
				return;

			int index = 0;
			var combination = condition.Combination == ConditionCombination.And ? Operator.AndAlso : Operator.OrElse;

			if(condition.Count > 1 && context.Counter.Indent())
				context.Write("(");

			foreach(var item in condition)
			{
				if(index++ > 0)
					context.Write(this.GetSymbol(combination));

				this.OnVisit(context, item);
			}

			if(condition.Count > 1 && context.Counter.Dedent())
				context.Write(")");
		}

		protected virtual void VisitCollection(ExpressionVisitorContext context, ExpressionCollection collection)
		{
			if(collection.Count == 0)
				return;

			int index = 0;

			foreach(var item in collection)
			{
				if(index++ > 0)
					context.Write(",");

				this.OnVisit(context, item);
			}
		}

		protected abstract void VisitStatement(ExpressionVisitorContext context, IStatementBase statement);

		protected virtual string GetDelimiter(BlockExpressionDelimiter delimiter)
		{
			return delimiter switch
			{
				BlockExpressionDelimiter.Space => " ",
				BlockExpressionDelimiter.Break => "\n",
				BlockExpressionDelimiter.Terminator => ";",
				_ => string.Empty,
			};
		}
		#endregion

		#region 事件激发
		protected virtual void OnUnrecognized(ExpressionVisitorContext context)
		{
			var unrecognized = this.Unrecognized;

			if(unrecognized == null)
				return;

			var args = new ExpressionEventArgs(context.Output, context.Expression);
			unrecognized(this, args);
		}
		#endregion

		#region 私有方法
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private string GetSymbol(Operator @operator)
		{
			var space = " ";

			switch(@operator)
			{
				case Operator.Assign:
				case Operator.Negate:
				case Operator.Plus:
				case Operator.Add:
				case Operator.Subtract:
				case Operator.Multiply:
				case Operator.Divide:
				case Operator.Modulo:
				case Operator.Equal:
				case Operator.NotEqual:
				case Operator.GreaterThan:
				case Operator.GreaterThanOrEqual:
				case Operator.LessThan:
				case Operator.LessThanOrEqual:
					space = string.Empty;
					break;
			}

			return space + (this.Dialect.GetSymbol(@operator) ?? NormalDialect.Instance.GetSymbol(@operator)) + space;
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private string GetIdentifier(IIdentifier identifier)
		{
			return this.Dialect.GetIdentifier(identifier);
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private string GetAlias(string alias)
		{
			return this.Dialect.GetAlias(alias);
		}
		#endregion

		#region 嵌套子类
		private class NormalDialect : IExpressionDialect
		{
			#region 单例字段
			public static readonly NormalDialect Instance = new NormalDialect();
			#endregion

			#region 私有构造
			private NormalDialect() { }
			#endregion

			#region 公共方法
			public string GetDbType(DbType dbType, int length, byte precision, byte scale)
			{
				switch(dbType)
				{
					case DbType.AnsiString:
						return length > 0 ? "varchar(" + length.ToString() + ")" : "text";
					case DbType.AnsiStringFixedLength:
						return length > 0 ? "char(" + length.ToString() + ")" : "char(MAX)";
					case DbType.String:
						return length > 0 ? "nvarchar(" + length.ToString() + ")" : "text";
					case DbType.StringFixedLength:
						return length > 0 ? "nchar(" + length.ToString() + ")" : "nchar(MAX)";
					case DbType.Binary:
						return length > 0 ? "varbinary(" + length.ToString() + ")" : "blob";
					case DbType.Boolean:
						return "bool";
					case DbType.Byte:
						return "unsigned tinyint";
					case DbType.SByte:
						return "tinyint";
					case DbType.Date:
						return "date";
					case DbType.DateTime:
						return "datetime";
					case DbType.DateTime2:
						return "datetime2";
					case DbType.DateTimeOffset:
						return "datetime";
					case DbType.Guid:
						return "guid";
					case DbType.Int16:
						return "smallint";
					case DbType.Int32:
						return "int";
					case DbType.Int64:
						return "bigint";
					case DbType.Object:
						return "object";
					case DbType.Time:
						return "time";
					case DbType.UInt16:
						return "unsigned smallint";
					case DbType.UInt32:
						return "unsigned int";
					case DbType.UInt64:
						return "unsigned bigint";
					case DbType.Currency:
						return "currency";
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

				return dbType.ToString();
			}

			public string GetSymbol(Operator @operator)
			{
				switch(@operator)
				{
					case Operator.Plus:
						return "+";
					case Operator.Negate:
						return "-";
					case Operator.Add:
						return "+";
					case Operator.Subtract:
						return "-";
					case Operator.Multiply:
						return "*";
					case Operator.Divide:
						return "/";
					case Operator.Modulo:
						return "%";
					case Operator.Assign:
						return "=";
					case Operator.And:
						return "&";
					case Operator.Or:
						return "|";
					case Operator.Xor:
						return "^";
					case Operator.Not:
						return "NOT";
					case Operator.AndAlso:
						return "AND";
					case Operator.OrElse:
						return "OR";
					case Operator.All:
						return "ALL";
					case Operator.Any:
						return "ANY";
					case Operator.Between:
						return "BETWEEN";
					case Operator.Exists:
						return "EXISTS";
					case Operator.NotExists:
						return "NOT EXISTS";
					case Operator.In:
						return "IN";
					case Operator.NotIn:
						return "NOT IN";
					case Operator.Like:
						return "LIKE";
					case Operator.Is:
						return "IS";
					case Operator.NotIs:
						return "IS NOT";
					case Operator.Equal:
						return "=";
					case Operator.NotEqual:
						return "!=";
					case Operator.LessThan:
						return "<";
					case Operator.LessThanOrEqual:
						return "<=";
					case Operator.GreaterThan:
						return ">";
					case Operator.GreaterThanOrEqual:
						return ">=";
					default:
						throw new DataException($"Unsupported '{@operator}' operator.");
				}
			}

			public string GetIdentifier(string name)
			{
				return name;
			}

			public string GetIdentifier(IIdentifier identifier)
			{
				return this.GetIdentifier(identifier.Name);
			}

			public string GetAlias(string alias)
			{
				return "'" + alias + "'";
			}

			public string GetMethodName(MethodExpression method)
			{
				switch(method)
				{
					case AggregateExpression aggregate:
						return aggregate.Function.ToString();
					case SequenceExpression sequence:
						return sequence.Method.ToString();
					default:
						return method.Name;
				}
			}
			#endregion
		}
		#endregion
	}
}
