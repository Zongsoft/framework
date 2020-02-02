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
using System.Collections;

namespace Zongsoft.Data.Common.Expressions
{
	public static class ConditionExtension
	{
		#region 公共方法
		public static IExpression ToExpression(this Condition condition,
		                                            Func<Condition, FieldIdentifier> fieldThunk,
		                                            Action<ParameterExpression> append = null)
		{
			if(condition == null)
				throw new ArgumentNullException(nameof(condition));

			if(fieldThunk == null)
				throw new ArgumentNullException(nameof(fieldThunk));

			var field = fieldThunk(condition);
			var value = GetConditionValue(condition, field, append);

			if(value == null)
				return null;

			switch(condition.Operator)
			{
				case ConditionOperator.Between:
					if(value is RangeExpression range)
						return Expression.Between(field, range);
					else
						return value as BinaryExpression;
				case ConditionOperator.Like:
					if(Expression.IsNull(value))
						return Expression.Equal(field, value);
					else
						return Expression.Like(field, value);
				case ConditionOperator.Exists:
					return Expression.Exists(value);
				case ConditionOperator.NotExists:
					return Expression.NotExists(value);
				case ConditionOperator.In:
					return Expression.In(field, value);
				case ConditionOperator.NotIn:
					return Expression.NotIn(field, value);
				case ConditionOperator.Equal:
					return Expression.Equal(field, value);
				case ConditionOperator.NotEqual:
					return Expression.NotEqual(field, value);
				case ConditionOperator.GreaterThan:
					return Expression.GreaterThan(field, value);
				case ConditionOperator.GreaterThanEqual:
					return Expression.GreaterThanOrEqual(field, value);
				case ConditionOperator.LessThan:
					return Expression.LessThan(field, value);
				case ConditionOperator.LessThanEqual:
					return Expression.LessThanOrEqual(field, value);
				default:
					throw new NotSupportedException($"Invalid '{condition.Operator}' condition operator.");
			}
		}

		public static ConditionExpression ToExpression(this ConditionCollection conditions,
		                                               Func<Condition, FieldIdentifier> map,
		                                               Action<ParameterExpression> append = null)
		{
			if(conditions == null)
				throw new ArgumentNullException(nameof(conditions));

			ConditionExpression expressions = new ConditionExpression(conditions.Combination);

			foreach(var condition in conditions)
			{
				switch(condition)
				{
					case Condition c:
						var item = ToExpression(c, map, append);

						if(item != null)
							expressions.Add(item);

						break;
					case ConditionCollection cc:
						var items = ToExpression(cc, map, append);

						if(items != null && items.Count > 0)
							expressions.Add(items);

						break;
				}
			}

			return expressions.Count > 0 ? expressions : null;
		}
		#endregion

		#region 私有方法
		private static IExpression GetConditionValue(Condition condition, FieldIdentifier field, Action<ParameterExpression> append = null)
		{
			if(append == null)
				throw new ArgumentNullException(nameof(append));

			if(condition.Value == null)
			{
				if(condition.Operator == ConditionOperator.Equal || condition.Operator == ConditionOperator.NotEqual || condition.Operator == ConditionOperator.Like)
					return ConstantExpression.Null;

				throw new DataException($"The specified '{condition.Name}' parameter value of the type {condition.Operator.ToString()} condition is null.");
			}

			if(condition.Operator == ConditionOperator.Equal && Range.IsRange(condition.Value))
				condition.Operator = ConditionOperator.Between;

			switch(condition.Operator)
			{
				case ConditionOperator.Between:
					if(Range.IsRange(condition.Value, out var minimum, out var maximum))
					{
						ParameterExpression minimumParameter = null;
						ParameterExpression maximumParameter = null;

						if(object.Equals(minimum, maximum))
						{
							condition.Operator = ConditionOperator.Equal;
							append(minimumParameter = Expression.Parameter(field, minimum));
							return minimumParameter;
						}

						if(minimum == null)
						{
							if(maximum == null)
								return null;

							condition.Operator = ConditionOperator.LessThanEqual;
							append(maximumParameter = Expression.Parameter(field, maximum));
							return maximumParameter;
						}
						else
						{
							if(maximum == null)
							{
								condition.Operator = ConditionOperator.GreaterThanEqual;
								append(minimumParameter = Expression.Parameter(field, minimum));
								return minimumParameter;
							}
							else
							{
								append(minimumParameter = Expression.Parameter(field, minimum));
								append(maximumParameter = Expression.Parameter(field, maximum));

								return new RangeExpression(minimumParameter, maximumParameter);
							}
						}
					}

					throw new DataException($"The specified '{condition.Name}' parameter value of the type Between condition is invalid.");
				case ConditionOperator.Exists:
				case ConditionOperator.NotExists:
					return condition.Value as IExpression ??
					       throw new DataException($"Unable to build a subquery corresponding to the specified '{condition.Name}' parameter({condition.Operator}).");
				case ConditionOperator.In:
				case ConditionOperator.NotIn:
					if(condition.Value != null && condition.Value is IEnumerable iterator)
					{
						var collection = new ExpressionCollection();

						foreach(var item in iterator)
						{
							if(item != null)
								collection.Add(Expression.Constant(item));
						}

						if(collection.Count > 0)
							return collection;
					}

					throw new DataException($"The specified '{condition.Name}' parameter value of the type In condition is null or empty set.");
			}

			var parameter = Expression.Parameter(field, condition.Value);
			append(parameter);
			return parameter;
		}
		#endregion
	}
}
