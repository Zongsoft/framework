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
using System.Collections.Generic;

namespace Zongsoft.Configuration
{
	public class ConfigurationRecognizer : IConfigurationRecognizer
	{
		#region 单例字段
		public static readonly ConfigurationRecognizer Default = new ConfigurationRecognizer();
		#endregion

		#region 构造函数
		protected ConfigurationRecognizer()
		{
		}
		#endregion

		#region 公共方法
		public void Recognize(object target, string name, string value)
		{
			if(target == null)
				return;

			var unrecognizedProperty = this.GetUnrecognizedProperty(target.GetType().GetTypeInfo());

			if(unrecognizedProperty.Property == null)
				return;

			var dictionary = Reflection.Reflector.GetValue(unrecognizedProperty.Property, ref target);

			if(dictionary == null && unrecognizedProperty.CanWrite)
			{
				if(unrecognizedProperty.PropertyType.IsAbstract)
					dictionary = Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(string), unrecognizedProperty.DictionaryType.GenericTypeArguments[1]), new object[] { StringComparer.OrdinalIgnoreCase });
				else
					dictionary = Activator.CreateInstance(unrecognizedProperty.PropertyType);

				Reflection.Reflector.SetValue(unrecognizedProperty.Property, ref target, dictionary);
			}

			if(dictionary != null)
				unrecognizedProperty.SetDictionaryValue(dictionary, name, value);
		}
		#endregion

		#region 虚拟方法
		protected virtual UnrecognizedPropertyToken GetUnrecognizedProperty(TypeInfo type)
		{
			if(type == null)
				throw new ArgumentNullException(nameof(type));

			var attribute = type.GetConfigurationAttribute();

			if(attribute == null || string.IsNullOrEmpty(attribute.UnrecognizedProperty))
				return default;

			var unrecognizedProperty = type.GetProperty(attribute.UnrecognizedProperty) ??
				throw new ArgumentException(string.Format(Zongsoft.Properties.Resources.Error_PropertyNotExists, type, attribute.UnrecognizedProperty));

			if(!unrecognizedProperty.CanRead)
				throw new InvalidOperationException(string.Format(Zongsoft.Properties.Resources.Error_PropertyCannotRead, type, attribute.UnrecognizedProperty));

			var dictionaryType = ConfigurationUtility.GetImplementedContract(unrecognizedProperty.PropertyType, typeof(IDictionary<,>))?.GetTypeInfo();

			if(dictionaryType == null || dictionaryType.GenericTypeArguments[0] != typeof(string))
				throw new InvalidOperationException(string.Format(Properties.Resources.Error_InvalidUnrecognizedProperty, unrecognizedProperty.Name));

			return new UnrecognizedPropertyToken(unrecognizedProperty, dictionaryType);
		}
		#endregion

		#region 内部结构
		protected readonly struct UnrecognizedPropertyToken
		{
			public readonly PropertyInfo Property;
			public readonly TypeInfo DictionaryType;

			public bool CanRead => this.Property?.CanRead ?? false;
			public bool CanWrite => this.Property?.CanWrite ?? false;
			public Type PropertyType => this.Property?.PropertyType;

			public UnrecognizedPropertyToken(PropertyInfo property, TypeInfo dictionaryType)
			{
				this.Property = property;
				this.DictionaryType = dictionaryType;
			}

			public void SetDictionaryValue(object dictionary, string key, string value)
			{
				if(dictionary == null)
					throw new ArgumentNullException(nameof(dictionary));

				var property = this.Property;
				var dictionaryType = this.DictionaryType;

				if(property == null || dictionaryType == null)
					return;

				if(dictionary != null && Common.Convert.TryConvertValue(value, dictionaryType.GenericTypeArguments[1], () => ConfigurationUtility.GetConverter(property), out var convertedValue))
					Reflection.Reflector.SetValue(dictionary, "Item", convertedValue, new object[] { key });
			}
		}
		#endregion
	}
}
