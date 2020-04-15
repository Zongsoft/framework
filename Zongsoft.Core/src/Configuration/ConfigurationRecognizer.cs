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
using System.ComponentModel;
using System.Collections.Generic;

namespace Zongsoft.Configuration
{
	public class ConfigurationRecognizer<T> : IConfigurationRecognizer<T>
	{
		#region 成员字段
		private int _initialized;
		private Dictionary<string, PropertyToken> _properties;
		#endregion

		#region 构造函数
		public ConfigurationRecognizer()
		{
		}

		public ConfigurationRecognizer(string unrecognizedPropertyName)
		{
			if(!string.IsNullOrEmpty(unrecognizedPropertyName))
			{
				var unrecognizedProperty = typeof(T).GetProperty(unrecognizedPropertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ??
					throw new ArgumentException(string.Format(Zongsoft.Properties.Resources.Error_PropertyNotExists, typeof(T), unrecognizedPropertyName));

				if(!unrecognizedProperty.CanRead)
					throw new InvalidOperationException(string.Format(Zongsoft.Properties.Resources.Error_PropertyCannotRead, typeof(T), unrecognizedPropertyName));

				var dictionaryType = ConfigurationBinder.GetImplementedContracts(unrecognizedProperty.PropertyType, typeof(IDictionary<,>))?.GetTypeInfo();

				if(dictionaryType != null || dictionaryType.GenericTypeArguments[0] == typeof(string))
					this.UnrecognizedProperty = new UnrecognizedPropertyToken(unrecognizedProperty, dictionaryType);
			}
		}
		#endregion

		#region 保护属性
		protected UnrecognizedPropertyToken UnrecognizedProperty { get; }

		protected IReadOnlyDictionary<string, PropertyToken> Properties
		{
			get
			{
				this.Initialize();
				return _properties;
			}
		}
		#endregion

		#region 初始化器
		private void Initialize()
		{
			if(_initialized != 0)
				return;

			if(System.Threading.Interlocked.CompareExchange(ref _initialized, 1, 0) != 0)
				return;

			var type = typeof(T).GetTypeInfo();
			_properties = new Dictionary<string, PropertyToken>();

			do
			{
				foreach(var property in type.DeclaredProperties)
				{
					var name = property.GetCustomAttribute<ConfigurationPropertyAttribute>(true)?.Name;

					if(string.IsNullOrEmpty(name) || string.Equals(name, property.Name))
					{
						_properties.TryAdd(property.Name, new PropertyToken(property, GetConverter(property)));
					}
					else
					{
						var token = new PropertyToken(name, property, GetConverter(property));

						_properties.TryAdd(property.Name, token);
						_properties.TryAdd(name, token);
					}
				}

				type = type.BaseType.GetTypeInfo();
			}
			while(type != typeof(object).GetTypeInfo());
		}
		#endregion

		#region 公共方法
		public void Recognize(T target, string name, string value)
		{
			this.Initialize();

			if(_properties.TryGetValue(name, out var property))
				property.SetValue(target, value);
			else
				this.OnUnrecognize(target, name, value);
		}
		#endregion

		#region 虚拟方法
		protected virtual void OnUnrecognize(T target, string name, string value)
		{
			var unrecognizedProperty = this.UnrecognizedProperty;

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

		#region 私有方法
		private static TypeConverter GetConverter(PropertyInfo property)
		{
			var attribute = property.GetCustomAttribute<TypeConverterAttribute>(true);

			if(attribute != null && !string.IsNullOrEmpty(attribute.ConverterTypeName))
				return Activator.CreateInstance(Type.GetType(attribute.ConverterTypeName)) as TypeConverter;

			return null;
		}
		#endregion

		#region 内部结构
		protected readonly struct PropertyToken
		{
			public readonly PropertyInfo Property;
			public readonly TypeConverter Converter;
			public readonly string ConfigurationKey;

			public PropertyToken(PropertyInfo property, TypeConverter converter)
			{
				this.ConfigurationKey = property.Name;
				this.Property = property;
				this.Converter = converter;
			}

			public PropertyToken(string key, PropertyInfo property, TypeConverter converter)
			{
				this.ConfigurationKey = key;
				this.Property = property;
				this.Converter = converter;
			}

			public string Name => this.Property.Name;
			public bool CanRead => this.Property.CanRead;
			public bool CanWrite => this.Property.CanWrite;
			public Type PropertyType => this.Property.PropertyType;

			public object GetValue(T target)
			{
				return Reflection.Reflector.GetValue(this.Property, ref target);
			}

			public void SetValue(T target, string value)
			{
				if(!this.CanWrite)
					return;

				var converter = this.Converter;

				if(Zongsoft.Common.Convert.TryConvertValue(value, this.PropertyType, () => converter, out var convertedValue))
					Reflection.Reflector.SetValue(this.Property, ref target, value);
			}

			public override string ToString()
			{
				return $"{this.Property.Name}:{this.Property.PropertyType.FullName}";
			}
		}

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

				if(dictionary != null && Common.Convert.TryConvertValue(value, dictionaryType.GenericTypeArguments[1], () => GetConverter(property), out var convertedValue))
					Reflection.Reflector.SetValue(dictionary, "Item", convertedValue, new object[] { key });
			}
		}
		#endregion
	}
}
