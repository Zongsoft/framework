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
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq.Expressions;
using System.Collections.Concurrent;

namespace Zongsoft.Data
{
	public static class Range
	{
		#region 私有变量
		private static readonly ConcurrentDictionary<Type, RangeToken> _tokens = new ConcurrentDictionary<Type, RangeToken>();
		#endregion

		#region 公共方法
		public static object Create(Type type, object value)
		{
			if(type == null)
				throw new ArgumentNullException(nameof(type));

			if(!type.IsValueType)
				throw new ArgumentException($"The specified '{nameof(type)}' parameter must be a value type.");

			if(type.IsGenericType)
			{
				var prototype = type.GetGenericTypeDefinition();

				if(prototype == typeof(Nullable<>) || prototype == typeof(Range<>))
					type = type.GenericTypeArguments[0];
			}

			return Activator.CreateInstance(typeof(Range<>).MakeGenericType(type), new object[] { Common.Convert.ConvertValue(value, type) });
		}

		public static object Create(Type type, object minimum, object maximum)
		{
			if(type == null)
				throw new ArgumentNullException(nameof(type));

			if(!type.IsValueType)
				throw new ArgumentException($"The specified '{nameof(type)}' parameter must be a value type.");

			if(type.IsGenericType)
			{
				var prototype = type.GetGenericTypeDefinition();

				if(prototype == typeof(Nullable<>) || prototype == typeof(Range<>))
					type = type.GenericTypeArguments[0];
			}

			if(minimum == null || (minimum is string min && (string.IsNullOrWhiteSpace(min) || min == "?" || min == "*")))
				minimum = null;
			else
				minimum = Common.Convert.ConvertValue(minimum, type);

			if(maximum == null || (maximum is string max && (string.IsNullOrWhiteSpace(max) || max == "?" || max == "*")))
				maximum = null;
			else
				maximum = Common.Convert.ConvertValue(maximum, type);

			return Activator.CreateInstance(typeof(Range<>).MakeGenericType(type), new object[] { minimum, maximum });
		}

		public static Range<uint> Create(Zongsoft.Common.HierarchyVector32 vector)
		{
			return vector.IsZero ? default : new Range<uint>(vector.Minimum, vector.Maximum);
		}

		public static Range<T> Create<T>(T minimum, T maximum) where T : struct, IComparable<T> => new Range<T>(minimum, maximum);
		public static Range<T> Create<T>(T? minimum, T? maximum) where T : struct, IComparable<T> => new Range<T>(minimum, maximum);

		public static Range<T> Empty<T>() where T : struct, IComparable<T> => EmptyRange<T>.Value;
		public static Range<T> Minimum<T>(T minimum) where T : struct, IComparable<T> => new Range<T>(minimum, null);
		public static Range<T> Maximum<T>(T maximum) where T : struct, IComparable<T> => new Range<T>(null, maximum);

		public static Range<T> Parse<T>(string text) where T : struct, IComparable<T> => Range<T>.Parse(text);
		public static bool TryParse<T>(string text, out Range<T> value) where T : struct, IComparable<T> => Range<T>.TryParse(text, out value);

		public static bool In<T>(T value, T? minimum, T? maximum) where T : struct, IComparable<T>
		{
			var range = new Range<T>(minimum, maximum);
			return range.Contains(value);
		}

		public static bool IsRange(object target)
		{
			return target == null ? false : IsRange(target.GetType());
		}

		public static bool IsRange(object target, out Type underlyingType)
		{
			if(target != null)
				return IsRange(target.GetType(), out underlyingType);

			underlyingType = null;
			return false;
		}

		public static bool IsRange(object target, out object minimum, out object maximum)
		{
			minimum = null;
			maximum = null;

			if(target == null)
				return false;

			var type = target.GetType();

			if(!IsRange(type, out var underlyingType))
				return false;

			if(type.IsArray)
			{
				Array array = (Array)target;

				if(array.Length >= 2)
				{
					minimum = array.GetValue(0);
					maximum = array.GetValue(1);

					return true;
				}

				return false;
			}

			_tokens.GetOrAdd(underlyingType, t => new RangeToken(t)).GetRange(target, out minimum, out maximum);

			return minimum != null || maximum != null;
		}

		public static bool IsRange(Type type)
		{
			if(type == null)
				throw new ArgumentNullException(nameof(type));

			if(type.IsArray)
			{
				var elementType = type.GetElementType();

				if(typeof(IComparable).IsAssignableFrom(elementType) ||
				   typeof(IComparable<>).MakeGenericType(elementType).IsAssignableFrom(elementType))
					return true;

				return false;
			}

			if(Zongsoft.Common.TypeExtension.IsNullable(type, out var underlyingType))
				type = underlyingType;

			return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Range<>);
		}

		public static bool IsRange(Type type, out Type underlyingType)
		{
			if(type == null)
				throw new ArgumentNullException(nameof(type));

			if(type.IsArray)
			{
				underlyingType = type.GetElementType();

				if(typeof(IComparable).IsAssignableFrom(underlyingType) ||
				   typeof(IComparable<>).MakeGenericType(underlyingType).IsAssignableFrom(underlyingType))
					return true;

				return false;
			}

			if(Zongsoft.Common.TypeExtension.IsNullable(type, out var nullableType))
				type = nullableType;

			if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Range<>))
			{
				underlyingType = type.GetGenericArguments()[0];
				return true;
			}

			underlyingType = null;
			return false;
		}

		public static bool IsEmpty(object target)
		{
			if(target == null)
				return true;

			if(IsRange(target.GetType(), out var underlyingType))
				return _tokens.GetOrAdd(underlyingType, type => new RangeToken(type)).IsEmpty(target);

			throw new InvalidOperationException($"The specified '{nameof(target)}' parameter of type '{target.GetType()}' is not a Rang type and this operation is not supported.");
		}

		public static bool HasValue(object target)
		{
			return !IsEmpty(target);
		}

		public static bool HasValue(object target, Action<Func<string, Condition>> fallback)
		{
			if(target == null)
				return false;

			if(IsRange(target.GetType(), out var underlyingType))
			{
				var token = _tokens.GetOrAdd(underlyingType, type => new RangeToken(type));

				if(!token.IsEmpty(target))
				{
					var proxy = new HasValueProxy(target, token);
					fallback?.Invoke(proxy.Call);
					return true;
				}
			}

			return false;
		}
		#endregion

		#region 时间范围
		public static class Timing
		{
			public static Range<DateTime> Day(DateTime date) => new Range<DateTime>(date.Date, new DateTime(date.Year, date.Month, date.Day, 23, 59, 59, 999));
			public static Range<DateTime> Day(int year, int month, int day) => new Range<DateTime>(new DateTime(year, month, day), new DateTime(year, month, day, 23, 59, 59, 999));
			public static Range<DateTime> Today() => Day(DateTime.Today);
			public static Range<DateTime> Yesterday() => Day(DateTime.Today.AddDays(-1));

			public static Range<DateTime> ThisWeek()
			{
				var today = DateTime.Today;
				var days = (int)today.DayOfWeek;
				var firstday = today.AddDays(-(days == 0 ? 6 : days - 1));
				return new Range<DateTime>(firstday, firstday.AddSeconds((60 * 60 * 24 * 7) - 1));
			}

			public static Range<DateTime> ThisMonth()
			{
				var today = DateTime.Today;
				return Month(today.Year, today.Month);
			}

			public static Range<DateTime> ThisYear() => Year(DateTime.Today.Year);
			public static Range<DateTime> LastYear() => Year(DateTime.Today.Year - 1);

			public static Range<DateTime> Year(int year) => new Range<DateTime>(new DateTime(year, 1, 1), new DateTime(year, 12, 31, 23, 59, 59, 999));
			public static Range<DateTime> Month(int year, int month) => new Range<DateTime>(new DateTime(year, month, 1), new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 59, 59, 999));

			public static Range<DateTime> Ago(int number, char unit)
			{
				var now = DateTime.Now;

				if(number == 0)
					return new Range<DateTime>(null, now);

				return unit switch
				{
					'Y' or 'y' => new Range<DateTime>(null, now.AddYears(-number)),
					'M' => new Range<DateTime>(null, now.AddMonths(-number)),
					'D' or 'd' => new Range<DateTime>(null, now.AddDays(-number)),
					'H' or 'h' => new Range<DateTime>(null, now.AddHours(-number)),
					'm' => new Range<DateTime>(null, now.AddMinutes(-number)),
					'S' or 's' => new Range<DateTime>(null, now.AddSeconds(-number)),
					_ => throw new ArgumentException(unit == '\0' ?
						$"Missing the parameter unit of the ago datetime range function." :
						$"Invalid parameter unit({unit}) of the ago datetime range function."),
				};
			}

			public static Range<DateTime> Last(int number, char unit)
			{
				var now = DateTime.Now;

				return unit switch
				{
					'Y' or 'y' => number == 0 ?
						new Range<DateTime>(new DateTime(now.Year, 1, 1), now) :
						new Range<DateTime>(now.AddYears(-number), now),
					'M' => number == 0 ?
						new Range<DateTime>(new DateTime(now.Year, now.Month, 1), now) :
						new Range<DateTime>(now.AddMonths(-number), now),
					'D' or 'd' => number == 0 ?
						new Range<DateTime>(now.Date, now) :
						new Range<DateTime>(now.AddDays(-number), now),
					'H' or 'h' => number == 0 ?
						new Range<DateTime>(new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0), now) :
						new Range<DateTime>(now.AddHours(-number), now),
					'm' => number == 0 ?
						new Range<DateTime>(new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0), now) :
						new Range<DateTime>(now.AddMinutes(-number), now),
					'S' or 's' => new Range<DateTime>(now.AddSeconds(-number), now),
					_ => throw new ArgumentException(unit == '\0' ?
						$"Missing the parameter unit of the ago datetime range function." :
						$"Invalid parameter unit({unit}) of the ago datetime range function."),
				};
			}
		}
		#endregion

		#region 嵌套子类
		private static class EmptyRange<T> where T : struct, IComparable<T>
		{
			internal static readonly Range<T> Value = new Range<T>();
		}

		private struct HasValueProxy
		{
			private readonly object _target;
			private readonly RangeToken _token;

			public HasValueProxy(object target, RangeToken token)
			{
				_target = target;
				_token = token;
			}

			public Condition Call(string name)
			{
				return _token.GetCondition(_target, name);
			}
		}

		private class RangeToken
		{
			#region 委托定义
			public delegate void GetRangeDelegate(object target, out object minimum, out object maximum);
			#endregion

			#region 成员字段
			private readonly Type _underlyingType;
			private GetRangeDelegate _getRange;
			private Func<object, bool> _isEmpty;
			private Func<object, string, Condition> _getCondition;
			#endregion

			#region 构造函数
			public RangeToken(Type underlyingType)
			{
				if(underlyingType == null)
					throw new ArgumentNullException(nameof(underlyingType));

				if(!underlyingType.IsValueType)
					throw new ArgumentException($"The specified 'underlyingType({underlyingType.FullName})' parameter must be a value type.");

				_underlyingType = underlyingType.IsEnum ? Enum.GetUnderlyingType(underlyingType) : underlyingType;
			}
			#endregion

			#region 公共方法
			public bool IsEmpty(object target)
			{
				if(_isEmpty == null)
				{
					lock(this)
					{
						if(_isEmpty == null)
							_isEmpty = CompileIsEmpty();
					}
				}

				return _isEmpty.Invoke(target);
			}

			public Condition GetCondition(object target, string name)
			{
				if(_getCondition == null)
				{
					lock(this)
					{
						if(_getCondition == null)
							_getCondition = CompileGetCondition();
					}
				}

				return _getCondition.Invoke(target, name);
			}

			public void GetRange(object target, out object minimum, out object maximum)
			{
				if(_getRange == null)
				{
					lock(this)
					{
						if(_getRange == null)
							_getRange = CompileGetRange();
					}
				}

				_getRange.Invoke(target, out minimum, out maximum);
			}
			#endregion

			#region 动态编译

			/*
			 * 示例：动态编译后的代码。
			 * 
			private static void GetRange_XXX(object target, out object minimum, out object maximum)
			{
				var range = (Range<XXX>)target;

				if(range.Minimum == null)
					minimum = null;
				else
					minimum = range.Minimum.Value;

				if(range.Maximum == null)
					maximum = null;
				else
					maximum = range.Maximum.Value;
			}

			private static bool HasValue_XXX(object target)
			{
				var range = (Range<XXX>)target;
				return range.HasValue;
			}

			private static Condition GetCondition_XXX(object target, string name)
			{
				var range = (Range<XXX>)target;
				return range.ToCondition(name);
			}
			*/

			private GetRangeDelegate CompileGetRange()
			{
				var rangeType = typeof(Range<>).MakeGenericType(_underlyingType);
				var nullableType = typeof(Nullable<>).MakeGenericType(_underlyingType);

				var method = new DynamicMethod(
								"GetRange$" + _underlyingType.FullName.Replace('.', '-'),
								null,
								new Type[] { typeof(object), typeof(object).MakeByRefType(), typeof(object).MakeByRefType() },
								typeof(Range),
								true);

				method.DefineParameter(1, ParameterAttributes.None, "target");
				method.DefineParameter(2, ParameterAttributes.Out, "minimum");
				method.DefineParameter(3, ParameterAttributes.Out, "maximum");

				var generator = method.GetILGenerator();

				//local_0 : Range<T>
				generator.DeclareLocal(rangeType);
				//local_1 : Nullable<T>
				generator.DeclareLocal(nullableType);

				var minimumElseLabel = generator.DefineLabel();
				var maximumIfLabel = generator.DefineLabel();
				var maximumElseLabel = generator.DefineLabel();

				//local_0 = (Range<T>)target;
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Unbox_Any, rangeType);
				generator.Emit(OpCodes.Stloc_0);

				//if(!range.Minimum.HasValue)
				generator.Emit(OpCodes.Ldloca_S, 0);
				generator.Emit(OpCodes.Call, rangeType.GetProperty("Minimum").GetMethod);
				generator.Emit(OpCodes.Stloc_1);
				generator.Emit(OpCodes.Ldloca_S, 1);
				generator.Emit(OpCodes.Call, nullableType.GetProperty("HasValue").GetMethod);
				generator.Emit(OpCodes.Brtrue_S, minimumElseLabel);

				//minimum = null;
				generator.Emit(OpCodes.Ldarg_1);
				generator.Emit(OpCodes.Ldnull);
				generator.Emit(OpCodes.Stind_Ref);
				generator.Emit(OpCodes.Br_S, maximumIfLabel);

				generator.MarkLabel(minimumElseLabel);

				//minimum = range.Minimum.Value
				generator.Emit(OpCodes.Ldarg_1);

				generator.Emit(OpCodes.Ldloca_S, 1);
				generator.Emit(OpCodes.Call, nullableType.GetProperty("Value").GetMethod);
				generator.Emit(OpCodes.Box, _underlyingType);
				generator.Emit(OpCodes.Stind_Ref);

				generator.MarkLabel(maximumIfLabel);

				//if(!range.Maximum.HasValue)
				generator.Emit(OpCodes.Ldloca_S, 0);
				generator.Emit(OpCodes.Call, rangeType.GetProperty("Maximum").GetMethod);
				generator.Emit(OpCodes.Stloc_1);
				generator.Emit(OpCodes.Ldloca_S, 1);
				generator.Emit(OpCodes.Call, nullableType.GetProperty("HasValue").GetMethod);
				generator.Emit(OpCodes.Brtrue_S, maximumElseLabel);

				//maximum = null;
				generator.Emit(OpCodes.Ldarg_2);
				generator.Emit(OpCodes.Ldnull);
				generator.Emit(OpCodes.Stind_Ref);
				generator.Emit(OpCodes.Ret);

				generator.MarkLabel(maximumElseLabel);

				//maximum = range.Maximum.Value
				generator.Emit(OpCodes.Ldarg_2);

				generator.Emit(OpCodes.Ldloca_S, 1);
				generator.Emit(OpCodes.Call, nullableType.GetProperty("Value").GetMethod);
				generator.Emit(OpCodes.Box, _underlyingType);
				generator.Emit(OpCodes.Stind_Ref);
				generator.Emit(OpCodes.Ret);

				return (GetRangeDelegate)method.CreateDelegate(typeof(GetRangeDelegate));
			}

			private GetRangeDelegate CompileGetRangeWithExpression(Type type)
			{
				var target = Expression.Parameter(typeof(object), "target");
				var minimum = Expression.Parameter(typeof(object).MakeByRefType(), "minimum");
				var maximum = Expression.Parameter(typeof(object).MakeByRefType(), "maximum");

				var variables = new[]
				{
					Expression.Variable(typeof(Range<>).MakeGenericType(type), "range"),
				};

				var minimumProperty = Expression.Property(variables[0], variables[0].Type.GetProperty("Minimum"));
				var maximumProperty = Expression.Property(variables[0], variables[0].Type.GetProperty("Maximum"));

				var statements = new Expression[]
				{
					Expression.Assign(variables[0], Expression.Convert(target, variables[0].Type)),

					Expression.IfThenElse(
						Expression.Equal(minimumProperty, Expression.Constant(null)),
						Expression.Assign(minimum, Expression.Constant(null)),
						Expression.Assign(minimum, Expression.Convert(Expression.Property(minimumProperty, typeof(Nullable<>).MakeGenericType(type).GetProperty("Value")), typeof(object)))),

					Expression.IfThenElse(
						Expression.Equal(maximumProperty, Expression.Constant(null)),
						Expression.Assign(maximum, Expression.Constant(null)),
						Expression.Assign(maximum, Expression.Convert(Expression.Property(maximumProperty, typeof(Nullable<>).MakeGenericType(type).GetProperty("Value")), typeof(object))))
				};

				return Expression.Lambda<GetRangeDelegate>(Expression.Block(variables, statements), target, minimum, maximum).Compile();
			}

			private Func<object, bool> CompileIsEmpty()
			{
				var rangeType = typeof(Range<>).MakeGenericType(_underlyingType);

				var method = new DynamicMethod(
								"IsEmpty$" + _underlyingType.FullName.Replace('.', '-'),
								typeof(bool),
								new Type[] { typeof(object) },
								typeof(Range),
								true);

				method.DefineParameter(1, ParameterAttributes.None, "target");

				var generator = method.GetILGenerator();

				//local_0 : Range<T>
				generator.DeclareLocal(rangeType);

				//local_0 = (Range<T>)target;
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Unbox_Any, rangeType);
				generator.Emit(OpCodes.Stloc_0);

				//return local_0.IsEmpty;
				generator.Emit(OpCodes.Ldloca_S, 0);
				generator.Emit(OpCodes.Call, rangeType.GetProperty("IsEmpty").GetMethod);
				generator.Emit(OpCodes.Ret);

				return (Func<object, bool>)method.CreateDelegate(typeof(Func<object, bool>));
			}

			private Func<object, string, Condition> CompileGetCondition()
			{
				var rangeType = typeof(Range<>).MakeGenericType(_underlyingType);

				var method = new DynamicMethod(
								"GetCondition" + _underlyingType.FullName.Replace('.', '-'),
								typeof(Condition),
								new Type[] { typeof(object), typeof(string) },
								typeof(Range),
								true);

				method.DefineParameter(1, ParameterAttributes.None, "target");
				method.DefineParameter(2, ParameterAttributes.None, "name");

				var generator = method.GetILGenerator();

				//local_0 : Range<T>
				generator.DeclareLocal(rangeType);

				//local_0 = (Range<T>)target;
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Unbox_Any, rangeType);
				generator.Emit(OpCodes.Stloc_0);

				//return local_0.ToCondition(name);
				generator.Emit(OpCodes.Ldloca_S, 0);
				generator.Emit(OpCodes.Ldarg_1);
				generator.Emit(OpCodes.Call, rangeType.GetMethod("ToCondition"));
				generator.Emit(OpCodes.Ret);

				return (Func<object, string, Condition>)method.CreateDelegate(typeof(Func<object, string, Condition>));
			}
			#endregion
		}
		#endregion
	}
}
