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
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Zongsoft.Data
{
	/// <summary>
	/// 提供条件转换的静态类。
	/// </summary>
	public static class Conditional
	{
		#region 静态变量
		private static readonly ConcurrentDictionary<Type, ConditionalDescriptor> _cache = new ConcurrentDictionary<Type, ConditionalDescriptor>();
		#endregion

		#region 公共方法
		public static ICondition ToCondition(this IModel conditional)
		{
			if(conditional == null)
				return null;

			var changes = conditional.GetChanges();

			if(changes == null || changes.Count == 0)
				return null;

			if(changes.Count > 1)
				return ToConditions(conditional, changes);

			var descriptor = _cache.GetOrAdd(conditional.GetType(), type => new ConditionalDescriptor(type));
			return GenerateCondition(conditional, descriptor.Properties[changes.First().Key]);
		}

		public static ConditionCollection ToConditions(this IModel conditional)
		{
			if(conditional == null)
				return null;

			return ToConditions(conditional, conditional.GetChanges());
		}
		#endregion

		#region 私有方法
		private static ConditionCollection ToConditions(IModel conditional, IDictionary<string, object> changes)
		{
			if(changes == null || changes.Count == 0)
				return null;

			ConditionCollection conditions = null;
			var descriptor = _cache.GetOrAdd(conditional.GetType(), type => new ConditionalDescriptor(type));

			//处理已经被更改过的属性
			foreach(var change in changes)
			{
				var condition = GenerateCondition(conditional, descriptor.Properties[change.Key]);

				if(condition != null)
				{
					if(conditions == null)
						conditions = ConditionCollection.And();

					conditions.Add(condition);
				}
			}

			return conditions;
		}

		private static ICondition GenerateCondition(IModel conditional, ConditionalPropertyDescripor property)
		{
			//如果当前属性值为默认值，则忽略它
			if(property == null)
				return null;

			//获取当前属性对应的条件命列表
			var names = GetConditionNames(property);

			//创建转换器上下文
			var context = new ConditionalConverterContext(conditional,
				property.Attribute == null ? ConditionalBehaviors.None : property.Attribute.Behaviors,
				names,
				property.PropertyType,
				property.GetValue(conditional),
				property.Operator);

			//如果当前属性指定了特定的转换器，则使用该转换器来处理
			if(property.Converter != null)
				return property.Converter.Convert(context);

			//使用默认转换器进行转换处理
			return ConditionalConverter.Default.Convert(context);
		}

		private static string[] GetConditionNames(ConditionalPropertyDescripor property)
		{
			if(property.Attribute != null && property.Attribute.Names != null && property.Attribute.Names.Length > 0)
				return property.Attribute.Names;

			return new string[] { property.Name };
		}
		#endregion

		#region 嵌套子类
		private class ConditionalDescriptor
		{
			public readonly Type Type;
			public readonly IDictionary<string, ConditionalPropertyDescripor> Properties;

			public ConditionalDescriptor(Type type)
			{
				this.Type = type;

				var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
				this.Properties = new Dictionary<string, ConditionalPropertyDescripor>();

				foreach(var property in properties)
				{
					if(!property.CanRead)
						continue;

					var attribute = property.GetCustomAttribute<ConditionalAttribute>(true);

					if(attribute != null && attribute.Ignored)
						continue;

					this.Properties.Add(property.Name, new ConditionalPropertyDescripor(property, attribute));
				}
			}
		}

		private class ConditionalPropertyDescripor
		{
			public readonly string Name;
			public readonly Type PropertyType;
			public readonly PropertyInfo PropertyInfo;
			public readonly ConditionalAttribute Attribute;
			public readonly IConditionalConverter Converter;

			public ConditionalPropertyDescripor(PropertyInfo property, ConditionalAttribute attribute)
			{
				this.PropertyInfo = property;
				this.Attribute = attribute;
				this.Name = property.Name;
				this.PropertyType = property.PropertyType;

				if(attribute != null && attribute.ConverterType != null)
					this.Converter = Activator.CreateInstance(attribute.ConverterType) as IConditionalConverter;
			}

			public ConditionOperator? Operator
			{
				get
				{
					return this.Attribute != null ? this.Attribute.Operator : null;
				}
			}

			public object GetValue(object target)
			{
				return Zongsoft.Reflection.Reflector.GetValue(this.PropertyInfo, ref target);
			}
		}
		#endregion
	}
}
