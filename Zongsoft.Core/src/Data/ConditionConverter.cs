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
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Data
{
	public class ConditionConverter : IConditionConverter
	{
		#region 私有变量
		private static IConditionConverter _default;
		#endregion

		#region 单例属性
		public static IConditionConverter Default
		{
			get
			{
				if(_default == null)
					System.Threading.Interlocked.CompareExchange(ref _default, new ConditionConverter(), null);

				return _default;
			}
			set
			{
				_default = value ?? throw new ArgumentNullException();
			}
		}
		#endregion

		#region 成员字段
		private char _wildcard;
		#endregion

		#region 构造函数
		public ConditionConverter()
		{
			_wildcard = '%';
		}
		#endregion

		#region 公共属性
		public char Wildcard
		{
			get => _wildcard;
			set => _wildcard = value;
		}
		#endregion

		#region 公共方法
		public virtual ICondition Convert(ConditionConverterContext context)
		{
			//如果当前属性类型是模型接口(条件)，则进行嵌套转换
			if(typeof(IModel).IsAssignableFrom(context.Type))
			{
				if(context.Operator == ConditionOperator.Exists || context.Operator == ConditionOperator.NotExists)
					return new Condition(context.GetFullName(), Criteria.Transform((IModel)context.Value), context.Operator.Value);
				else
					return Criteria.Transform((IModel)context.Value, context.GetFullName());
			}

			//判断当前属性是否可以忽略
			if(this.IsIgnorable(context))
				return null;

			ICondition result = null;

			void Fallback(Func<string, Condition> factory)
			{
				if(context.Names.Length == 1)
				{
					result = factory(context.GetFullName(0));
					return;
				}

				var criteria = ConditionCollection.Or();

				for(int i = 0; i < context.Names.Length; i++)
				{
					criteria.Add(factory(context.GetFullName(i)));
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

				if(typeof(ICollection).IsAssignableFrom(context.Type) || Zongsoft.Common.TypeExtension.IsAssignableFrom(typeof(ICollection<>), context.Type))
					optor = ConditionOperator.In;
			}

			//如果当前属性只对应一个条件
			if(context.Names.Length == 1)
				return new Condition(context.GetFullName(), this.GetValue(optor, context.Value), optor.Value);

			//当一个属性对应多个条件，则这些条件之间以“或”关系进行组合
			var conditions = ConditionCollection.Or();

			for(int i = 0; i < context.Names.Length; i++)
			{
				conditions.Add(new Condition(context.GetFullName(i), this.GetValue(optor, context.Value), optor.Value));
			}

			return conditions;
		}
		#endregion

		#region 保护方法
		protected bool IsIgnorable(ConditionConverterContext context)
		{
			if((context.Behaviors & ConditionBehaviors.IgnoreNull) == ConditionBehaviors.IgnoreNull && context.Value == null)
				return true;

			if((context.Behaviors & ConditionBehaviors.IgnoreEmpty) == ConditionBehaviors.IgnoreEmpty && context.Type == typeof(string) && string.IsNullOrWhiteSpace((string)context.Value))
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
				return value.ToString().TrimEnd(_wildcard) + _wildcard;

			return value;
		}
		#endregion

		#region 嵌套子类
		public class StringSplitter : ConditionConverter
		{
			#region 公共字段
			public readonly char[] Separators;
			#endregion

			#region 构造函数
			public StringSplitter() => this.Separators = new[] { ',', ';', '|' };
			public StringSplitter(params char[] separators) => this.Separators = separators;
			#endregion

			#region 重写方法
			public override ICondition Convert(ConditionConverterContext context)
			{
				if(context.Value is string text && !string.IsNullOrEmpty(text))
				{
					var values = Common.StringExtension.Slice(text, this.Separators).ToArray();

					if(context.Names.Length == 1)
						return values.Length == 1 ? Condition.Equal(context.GetFullName(), values[0]) : Condition.In(context.GetFullName(), values);

					var criteria = ConditionCollection.Or();

					for(int i = 0; i < context.Names.Length; i++)
					{
						criteria.Add(
							values.Length == 1 ? Condition.Equal(context.GetFullName(i), values[0]) : Condition.In(context.GetFullName(i), values)
						);
					}

					return criteria;
				}

				return base.Convert(context);
			}
			#endregion
		}
		#endregion
	}
}
