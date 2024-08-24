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
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace Zongsoft.Data.Metadata
{
	/// <summary>
	/// 表示实体属性及成员信息的标记类。
	/// </summary>
	public struct DataEntityPropertyToken
	{
		#region 公共字段
		/// <summary>
		/// 获取属性的元数据。
		/// </summary>
		public readonly IDataEntityProperty Property;

		/// <summary>
		/// 获取属性的绑定到目标类型的成员信息，如果该字段为空(null)则表示绑定的目标类型为字典。
		/// </summary>
		public readonly MemberInfo Member;

		/// <summary>
		/// 获取目标成员的类型转换器。
		/// </summary>
		public readonly TypeConverter Converter;
		#endregion

		#region 构造函数
		public DataEntityPropertyToken(IDataEntityProperty property, MemberInfo member = null)
		{
			this.Property = property;
			this.Member = member;
			this.Converter = Common.Utility.GetConverter(member);
		}
		#endregion

		#region 公共属性
		public bool IsMultiple
		{
			get
			{
				return this.Property.IsComplex &&
				       ((IDataEntityComplexProperty)this.Property).Multiplicity == DataAssociationMultiplicity.Many;
			}
		}

		public Type MemberType
		{
			get
			{
				if(this.Member == null)
					return null;

				switch(this.Member.MemberType)
				{
					case MemberTypes.Field:
						return ((FieldInfo)this.Member).FieldType;
					case MemberTypes.Property:
						return ((PropertyInfo)this.Member).PropertyType;
					case MemberTypes.Method:
						return ((MethodInfo)this.Member).ReturnType;
				}

				return null;
			}
		}
		#endregion

		#region 公共方法
		public bool TryGetValue(object target, Type conversionType, out object value)
		{
			value = null;

			if(target == null)
				return false;

			if(target is IDictionary<string, object> generic)
			{
				var result = generic.TryGetValue(this.Property.Name, out var propertyValue);

				if(result)
					value = this.ConvertValue(propertyValue, conversionType);

				return result;
			}

			if(target is IDictionary classic)
			{
				var existed = classic.Contains(this.Property.Name);

				if(existed)
					value = this.ConvertValue(classic[this.Property.Name], conversionType);

				return existed;
			}

			if(this.Member != null && this.Member.DeclaringType.IsAssignableFrom(target.GetType()))
			{
				var result = Reflection.Reflector.TryGetValue(this.Member, ref target, out var memberValue);

				if(result)
					value = this.ConvertValue(memberValue, conversionType);

				return result;
			}

			return false;
		}

		public object GetValue(object target, Type conversionType = null)
		{
			if(target == null)
				throw new ArgumentNullException(nameof(target));

			if(target is IDictionary<string, object> generic)
				return this.ConvertValue(generic.TryGetValue(this.Property.Name, out var value) ? value : null, conversionType);

			if(target is IDictionary classic)
				return this.ConvertValue(classic.Contains(this.Property.Name) ? classic[this.Property.Name] : null, conversionType);

			if(this.Member != null)
				return this.ConvertValue(Reflection.Reflector.GetValue(this.Member, ref target), conversionType);

			throw new InvalidOperationException($"Obtaining the value of the '{this.Property.Name}' property from the specified '{target.GetType().FullName}' target type is not supported.");
		}

		public void SetValue(object target, object value)
		{
			if(target == null)
				throw new ArgumentNullException(nameof(target));

			if(target is IDictionary<string, object> generic)
				generic[this.Property.Name] = value;
			else if(target is IDictionary classic)
				classic[this.Property.Name] = value;
			else if(this.Member != null)
				Reflection.Reflector.SetValue(this.Member, ref target, value);
			else
				throw new InvalidOperationException($"Setting the value of the '{this.Property.Name}' property from the specified '{target.GetType().FullName}' target type is not supported.");
		}
		#endregion

		#region 私有方法
		private object ConvertValue(object value, Type conversionType)
		{
			var converter = this.Converter;

			if(conversionType == null || converter == null)
				return value;

			return Zongsoft.Common.Convert.ConvertValue(value, conversionType, () => converter);
		}
		#endregion
	}
}
