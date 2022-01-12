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

using Zongsoft.Common;

namespace Zongsoft.Data
{
	/// <summary>
	/// 提供条件转换的静态类。
	/// </summary>
	public static class Criteria
	{
		#region 静态变量
		private static readonly ConcurrentDictionary<Type, CriteriaDescriptor> _cache = new ConcurrentDictionary<Type, CriteriaDescriptor>();
		#endregion

		#region 公共方法
		public static ICondition Transform(this IModel criteria, string path = null)
		{
			if(criteria == null)
				return null;

			var changes = criteria.GetChanges();

			if(changes == null || changes.Count == 0)
				return null;

			var descriptor = _cache.GetOrAdd(criteria.GetType(), type => new CriteriaDescriptor(type));

			return changes.Count == 1 ?
				GetCondition(criteria, descriptor.Properties[changes.First().Key], path) :
				ConditionCollection.And(changes.Select(p => GetCondition(criteria, descriptor.Properties[p.Key], path)));
		}

		public static ICondition Transform<TCriteria>(string expression, bool strict) => Transform(typeof(TCriteria), expression, 0, -1, strict);
		public static ICondition Transform(Type criteriaType, string expression, bool strict) => Transform(criteriaType, expression, 0, -1, strict);

		public static ICondition Transform<TCriteria>(string expression, int start = 0, int count = -1, bool strict = true) => Transform(typeof(TCriteria), expression, start, count, strict);
		public static ICondition Transform(Type criteriaType, string expression, int start = 0, int count = -1, bool strict = true)
		{
			if(criteriaType == null)
				throw new ArgumentNullException(nameof(criteriaType));

			KeyValuePair<string, string>[] members;

			if(strict)
				members = CriteriaParser.Parse(expression, start, count);
			else if(!CriteriaParser.TryParse(expression, start, count, out members))
				return null;

			return Transform(criteriaType, members, strict);
		}

		public static ICondition Transform<TCriteria>(IEnumerable<KeyValuePair<string, string>> members, bool strict = true) => Transform(typeof(TCriteria), members, strict);
		public static ICondition Transform(Type criteriaType, IEnumerable<KeyValuePair<string, string>> members, bool strict = true)
		{
			if(criteriaType == null)
				throw new ArgumentNullException(nameof(criteriaType));

			if(members == null || !members.Any())
				return null;

			if(!typeof(IModel).IsAssignableFrom(criteriaType))
				throw new ArgumentException($"The specified ‘{criteriaType.FullName}’ type is not a valid criteria type.");

			var instance = (IModel)(criteriaType.IsAbstract ? Model.Build(criteriaType) : Activator.CreateInstance(criteriaType));
			var descriptor = _cache.GetOrAdd(criteriaType, type => new CriteriaDescriptor(type));
			var properties = new List<CriteriaPropertyDescripor>();

			foreach(var member in members)
			{
				if(descriptor.Properties.TryGetValue(member.Key, out var property))
				{
					properties.Add(property);

					object propertyValue;

					if(property.PropertyType.IsArray || property.PropertyType.IsCollection())
					{
						var elementType = property.PropertyType.GetElementType();
						var parts = member.Value.Trim('(', ')', '[', ']').Slice(',').Select(p => Common.Convert.ConvertValue(p, elementType)).ToArray();

						if(property.PropertyType.IsArray)
						{
							propertyValue = Array.CreateInstance(elementType, parts.Length);
							Array.Copy(parts, (Array)propertyValue, parts.Length);
						}
						else
						{
							propertyValue = Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));

							for(int i = 0; i < parts.Length; i++)
								((System.Collections.IList)propertyValue).Add(parts[i]);
						}
					}
					else
					{
						if((property.PropertyType == typeof(bool) || property.PropertyType == typeof(bool?)) && string.IsNullOrEmpty(member.Value))
							propertyValue = true;
						else
							propertyValue = Common.Convert.ConvertValue(member.Value, property.PropertyType);
					}

					Reflection.Reflector.SetValue(property.PropertyInfo, ref instance, propertyValue);
				}
				else
				{
					if(strict)
						throw new DataArgumentException(member.Key, $"The specified ‘{member.Key}’ condition is undefined in the '{criteriaType.FullName}' type.");
				}
			}

			if(properties.Count == 0)
				return null;

			return properties.Count == 1 ?
				GetCondition(instance, properties[0]) :
				ConditionCollection.And(properties.Select(p => GetCondition(instance, p)));
		}
		#endregion

		#region 私有方法
		private static ICondition GetCondition(IModel criteria, CriteriaPropertyDescripor property, string path = null)
		{
			//如果当前属性值为默认值，则忽略它
			if(property == null)
				return null;

			//获取当前属性对应的条件名数组
			var names = property.Attribute == null || property.Attribute.Names == null || property.Attribute.Names.Length == 0 ?
				new[] { property.Name } :
				property.Attribute.Names;

			//创建转换器上下文
			var context = new ConditionConverterContext(criteria,
				property.Attribute == null ? ConditionBehaviors.IgnoreNullOrEmpty : property.Attribute.Behaviors,
				path,
				names,
				property.PropertyType,
				property.GetValue(criteria),
				property.Operator);

			//如果当前属性指定了特定的转换器，则使用该转换器来处理
			if(property.Converter != null)
				return property.Converter.Convert(context);

			//使用默认转换器进行转换处理
			return ConditionConverter.Default.Convert(context);
		}
		#endregion

		#region 嵌套子类
		private class CriteriaDescriptor
		{
			public readonly Type Type;
			public readonly IDictionary<string, CriteriaPropertyDescripor> Properties;

			public CriteriaDescriptor(Type type)
			{
				this.Type = type;

				var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
				this.Properties = new Dictionary<string, CriteriaPropertyDescripor>(StringComparer.OrdinalIgnoreCase);

				foreach(var property in properties)
				{
					if(!property.CanRead)
						continue;

					var attribute = property.GetCustomAttribute<ConditionAttribute>(true);

					if(attribute != null && attribute.Ignored)
						continue;

					this.Properties.Add(property.Name, new CriteriaPropertyDescripor(property, attribute));

					foreach(var alias in property.GetCustomAttributes<Zongsoft.ComponentModel.AliasAttribute>(true))
						this.Properties.Add(alias.Alias, new CriteriaPropertyDescripor(property, attribute));
				}
			}
		}

		private class CriteriaPropertyDescripor
		{
			public readonly string Name;
			public readonly Type PropertyType;
			public readonly PropertyInfo PropertyInfo;
			public readonly ConditionAttribute Attribute;
			public readonly IConditionConverter Converter;

			public CriteriaPropertyDescripor(PropertyInfo property, ConditionAttribute attribute)
			{
				this.PropertyInfo = property;
				this.Attribute = attribute;
				this.Name = property.Name;
				this.PropertyType = property.PropertyType;

				if(attribute != null && attribute.ConverterType != null)
					this.Converter = Activator.CreateInstance(attribute.ConverterType) as IConditionConverter;
				else
				{
					var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

					if(propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Complex<>))
						this.Converter = (IConditionConverter)Activator.CreateInstance(typeof(ComplexConverter<>).MakeGenericType(propertyType.GenericTypeArguments[0]));
				}
			}

			public ConditionOperator? Operator { get => this.Attribute != null ? this.Attribute.Operator : null; }

			public object GetValue(object target) => Zongsoft.Reflection.Reflector.GetValue(this.PropertyInfo, ref target);
		}
		#endregion
	}
}
