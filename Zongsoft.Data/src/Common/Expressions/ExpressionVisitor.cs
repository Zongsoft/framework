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
using System.Text;
using System.Threading;

namespace Zongsoft.Data.Common.Expressions
{
	public class ExpressionVisitor : IExpressionVisitor
	{
		#region 事件定义
		public event EventHandler<ExpressionEventArgs> Unrecognized;
		#endregion

		#region 私有变量
		private int _conditionDepth;
		#endregion

		#region 成员字段
		private int _depth;
		private readonly StringBuilder _output;
		#endregion

		#region 构造函数
		protected ExpressionVisitor(StringBuilder output = null)
		{
			_depth = -1;
			_output = output ?? new StringBuilder(256);
		}
		#endregion

		#region 公共属性
		public int Depth
		{
			get
			{
				return _depth;
			}
		}

		public virtual IExpressionDialect Dialect
		{
			get
			{
				return NormalDialect.Instance;
			}
		}

		public StringBuilder Output
		{
			get
			{
				return _output;
			}
		}
		#endregion

		#region 公共方法
		public IExpression Visit(IExpression expression)
		{
			if(expression == null)
				return null;

			try
			{
				//递增深度变量
				Interlocked.Increment(ref _depth);

				//执行具体的访问操作
				var result = this.OnVisit(expression);

				//递减深度变量
				Interlocked.Decrement(ref _depth);

				//返回访问后的表达式
				return result;
			}
			catch
			{
				//重置深度值
				_depth = -1;

				throw;
			}
		}
		#endregion

		#region 虚拟方法
		protected virtual IExpression OnVisit(IExpression expression)
		{
			switch(expression)
			{
				case BlockExpression block:
					return this.VisitBlock(block);
				case TableIdentifier table:
					return this.VisitTable(table);
				case FieldIdentifier field:
					return this.VisitField(field);
				case FieldDefinition field:
					return this.VisitField(field);
				case VariableIdentifier variable:
					return this.VisitVariable(variable);
				case ParameterExpression parameter:
					return this.VisitParameter(parameter);
				case LiteralExpression literal:
					return this.VisitLiteral(literal);
				case CommentExpression comment:
					return this.VisitComment(comment);
				case ConstantExpression constant:
					return this.VisitConstant(constant);
				case UnaryExpression unary:
					return this.VisitUnary(unary);
				case BinaryExpression binary:
					return this.VisitBinary(binary);
				case RangeExpression range:
					return this.VisitRange(range);
				case MethodExpression method:
					return this.VisitMethod(method);
				case ConditionExpression condition:
					return this.VisitCondition(condition);
				case ExpressionCollection collection:
					return this.VisitCollection(collection);
				case IStatementBase statement:
					return this.VisitStatement(statement);
				default:
					return this.OnUnrecognized(expression);
			}
		}

		protected virtual IExpression VisitBlock(BlockExpression block)
		{
			if(block.Count == 0)
				return block;

			int index = 0;

			foreach(var item in block)
			{
				//添加分割符以区隔各块元素
				if(index++ > 0 && block.Delimiter != BlockExpressionDelimiter.None)
					_output.Append(this.GetDelimiter(block.Delimiter));

				this.Visit(item);
			}

			return block;
		}

		protected virtual IExpression VisitTable(TableIdentifier table)
		{
			_output.Append(this.GetIdentifier(table));

			if(!string.IsNullOrEmpty(table.Alias) && !string.Equals(table.Name, table.Alias))
				_output.Append(" AS " + table.Alias);

			return table;
		}

		protected virtual IExpression VisitField(FieldIdentifier field)
		{
			if(field.Table == null || string.IsNullOrEmpty(field.Table.Alias))
				_output.Append(this.GetIdentifier(field));
			else
				_output.Append(field.Table.Alias + "." + this.GetIdentifier(field));

			if(!string.IsNullOrEmpty(field.Alias))
				_output.Append(" AS " + this.GetAlias(field.Alias));

			return field;
		}

		protected virtual IExpression VisitField(FieldDefinition field)
		{
			_output.Append($"{field.Name} {this.Dialect.GetDbType(field.DbType, field.Length, field.Precision, field.Scale)}");

			if(field.Nullable)
				_output.Append(" NULL");
			else
				_output.Append(" NOT NULL");

			return field;
		}

		protected virtual IExpression VisitVariable(VariableIdentifier variable)
		{
			if(variable.IsGlobal)
				_output.Append("@@" + variable.Name);
			else
				_output.Append("@" + variable.Name);

			return variable;
		}

		protected virtual IExpression VisitParameter(ParameterExpression parameter)
		{
			_output.Append("@" + parameter.Name);
			return parameter;
		}

		protected virtual IExpression VisitLiteral(LiteralExpression literal)
		{
			if(string.IsNullOrEmpty(literal.Text))
				return null;

			//输出字面量文本
			_output.Append(literal.Text);

			return literal;
		}

		protected virtual IExpression VisitComment(CommentExpression comment)
		{
			if(string.IsNullOrEmpty(comment.Text))
				return null;

			//输出注释文本
			_output.Append("/* " + comment.Text + " */");

			return comment;
		}

		protected virtual IExpression VisitConstant(ConstantExpression constant)
		{
			if(constant.Value == null)
			{
				_output.Append("NULL");
				return constant;
			}

			if(constant.ValueType == typeof(bool) || Zongsoft.Common.TypeExtension.IsNumeric(constant.ValueType))
				_output.Append(constant.Value.ToString());
			else
				_output.Append("'" + constant.Value.ToString() + "'");

			return constant;
		}

		protected virtual IExpression VisitUnary(UnaryExpression unary)
		{
			switch(unary.Operator)
			{
				case Operator.Minus:
					_output.Append("-");
					break;
				case Operator.Not:
					_output.Append("NOT ");
					break;
				case Operator.Exists:
					_output.Append("EXISTS ");
					break;
				case Operator.NotExists:
					_output.Append("NOT EXISTS ");
					break;
			}

			//只有常量和标记表达式才不需要加小括号
			bool parenthesisRequired = !(unary.Operand is ConstantExpression || unary.Operand is IIdentifier);

			if(parenthesisRequired)
				_output.Append("(");

			this.Visit(unary.Operand);

			if(parenthesisRequired)
				_output.Append(")");

			return unary;
		}

		protected virtual IExpression VisitBinary(BinaryExpression expression)
		{
			//是否需要括号包裹
			var parenthesisRequired = false;

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

			this.Visit(expression.Left);
			_output.Append(this.GetSymbol(expression.Operator));

			if(parenthesisRequired)
				_output.Append("(");

			this.Visit(expression.Right);

			if(parenthesisRequired)
				_output.Append(")");

			return expression;
		}

		protected virtual IExpression VisitRange(RangeExpression expression)
		{
			this.Visit(expression.Minimum);
			_output.Append(" AND ");
			this.Visit(expression.Maximum);

			return expression;
		}

		protected virtual IExpression VisitMethod(MethodExpression expression)
		{
			var methodName = this.Dialect.GetMethodName(expression);
			var isFunction = char.IsLetter(methodName[0]) || methodName[0] == '_';

			//先输出方法或变量名
			_output.Append(methodName);

			if(isFunction)
			{
				_output.Append("(");

				if(expression.Arguments != null)
				{
					var index = 0;

					foreach(var argument in expression.Arguments)
					{
						if(index++ > 0)
							_output.Append(",");

						this.Visit(argument);
					}
				}

				_output.Append(")");
			}

			if(!string.IsNullOrEmpty(expression.Alias))
				_output.Append(" AS " + this.GetAlias(expression.Alias));

			return expression;
		}

		protected virtual IExpression VisitCondition(ConditionExpression condition)
		{
			if(condition == null || condition.Count == 0)
				return null;

			int index = 0;
			var combination = condition.Combination == ConditionCombination.And ? Operator.AndAlso : Operator.OrElse;

			if(condition.Count > 1 && (_conditionDepth++ > 0 || _depth > 1))
				_output.Append("(");

			foreach(var item in condition)
			{
				if(index++ > 0)
					_output.Append(this.GetSymbol(combination));

				this.Visit(item);
			}

			if(condition.Count > 1 && (--_conditionDepth > 0 || _depth > 1))
				_output.Append(")");

			return condition;
		}

		protected virtual IExpression VisitCollection(ExpressionCollection collection)
		{
			if(collection.Count == 0)
				return collection;

			int index = 0;

			foreach(var item in collection)
			{
				if(index++ > 0)
					_output.Append(",");

				this.Visit(item);
			}

			return collection;
		}

		protected virtual IExpression VisitStatement(IStatementBase statement)
		{
			return statement;
		}

		protected virtual string GetDelimiter(BlockExpressionDelimiter delimiter)
		{
			switch(delimiter)
			{
				case BlockExpressionDelimiter.Space:
					return " ";
				case BlockExpressionDelimiter.Break:
					return "\n";
				case BlockExpressionDelimiter.Terminator:
					return ";";
			}

			return string.Empty;
		}
		#endregion

		#region 事件激发
		protected virtual IExpression OnUnrecognized(IExpression expression)
		{
			var unrecognized = this.Unrecognized;

			if(unrecognized == null)
				return expression;

			var args = new ExpressionEventArgs(_output, expression);
			unrecognized(this, args);
			return args.Result;
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
				case Operator.Minus:
				case Operator.Plus:
				case Operator.Modulo:
				case Operator.Divide:
				case Operator.Multiply:
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
			private NormalDialect()
			{
			}
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
					case Operator.Minus:
						return "-";
					case Operator.Multiply:
						return "*";
					case Operator.Divide:
						return "/";
					case Operator.Modulo:
						return "%";
					case Operator.Assign:
						return "=";
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
