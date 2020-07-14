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
using System.Collections;
using System.Collections.Generic;
using System.Net.Security;
using System.Reflection.Metadata.Ecma335;

namespace Zongsoft.Data
{
	public class ConditionalConverter : IConditionalConverter
	{
		#region 私有变量
		private static IConditionalConverter _default;
		#endregion

		#region 单例属性
		public static IConditionalConverter Default
		{
			get
			{
				if(_default == null)
					System.Threading.Interlocked.CompareExchange(ref _default, new ConditionalConverter(), null);

				return _default;
			}
			set
			{
				if(value == null)
					throw new ArgumentNullException();

				_default = value;
			}
		}
		#endregion

		#region 成员字段
		private char _wildcard;
		#endregion

		#region 构造函数
		public ConditionalConverter()
		{
			_wildcard = '%';
		}
		#endregion

		#region 公共属性
		public char Wildcard
		{
			get
			{
				return _wildcard;
			}
			set
			{
				_wildcard = value;
			}
		}
		#endregion

		#region 公共方法
		public virtual ICondition Convert(ConditionalConverterContext context)
		{
			//判断当前属性是否可以忽略
			if(this.IsIgnorable(context))
				return null;

			ICondition result = null;

			void Fallback(Func<string, Condition> factory)
			{
				if(context.Names.Length == 1)
				{
					result = factory(context.Names[0]);
					return;
				}

				var criteria = ConditionCollection.Or();

				foreach(var name in context.Names)
				{
					criteria.Add(factory(name));
				}

				result = criteria;
			}

			//确定是否为区间值，如果是则返回区间条件
			if(IsRange(context.Type) && Range.HasValue(context.Value, Fallback))
				return result;

			var optor = context.Operator;

			//只有当属性没有指定运算符需要确定运算符
			if(optor == null)
			{
				optor = ConditionOperator.Equal;

				if(context.Type == typeof(string) && context.Value != null)
					optor = ConditionOperator.Like;
				else if(typeof(IEnumerable).IsAssignableFrom(context.Type) || Zongsoft.Common.TypeExtension.IsAssignableFrom(typeof(IEnumerable<>), context.Type))
					optor = ConditionOperator.In;
			}

			//如果当前属性只对应一个条件
			if(context.Names.Length == 1)
				return new Condition(context.Names[0], this.GetValue(optor, context.Value), optor.Value);

			//当一个属性对应多个条件，则这些条件之间以“或”关系进行组合
			var conditions = ConditionCollection.Or();

			foreach(var name in context.Names)
			{
				conditions.Add(new Condition(name, this.GetValue(optor, context.Value), optor.Value));
			}

			return conditions;
		}
		#endregion

		#region 保护方法
		protected bool IsIgnorable(ConditionalConverterContext context)
		{
			if((context.Behaviors & ConditionalBehaviors.IgnoreNull) == ConditionalBehaviors.IgnoreNull && context.Value == null)
				return true;

			if((context.Behaviors & ConditionalBehaviors.IgnoreEmpty) == ConditionalBehaviors.IgnoreEmpty && context.Type == typeof(string) && string.IsNullOrWhiteSpace((string)context.Value))
				return true;

			if(IsRange(context.Type))
				return Range.IsEmpty(context.Value);

			return false;
		}
		#endregion

		#region 私有方法
		private static bool IsRange(Type type)
		{
			if(type.IsGenericType && type.IsValueType)
			{
				var prototype = type.GetGenericTypeDefinition();

				if(prototype == typeof(Nullable<>))
					return IsRange(Nullable.GetUnderlyingType(type));
				else if(prototype == typeof(Range<>))
					return true;
			}

			return false;
		}

		private object GetValue(ConditionOperator? @operator, object value)
		{
			if(value == null || System.Convert.IsDBNull(value))
				return value;

			if(@operator == ConditionOperator.Like && _wildcard != '\0')
				return _wildcard + value.ToString().Trim(_wildcard) + _wildcard;

			return value;
		}
		#endregion
	}
}
