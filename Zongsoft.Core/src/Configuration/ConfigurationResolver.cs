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
using System.ComponentModel;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;

namespace Zongsoft.Configuration
{
	public class ConfigurationResolver : IConfigurationResolver
	{
		#region 构造函数
		public ConfigurationResolver()
		{
		}
		#endregion

		#region 公共属性
		public IConfigurationRecognizer Recognizer
		{
			get; set;
		}
		#endregion

		#region 公共方法
		public object Resolve(Type type, IConfiguration configuration, ConfigurationBinderOptions options)
		{
			if(type == null)
				throw new ArgumentNullException(nameof(type));

			var instance = this.CreateInstance(type);
			this.Resolve(instance, configuration, options);
			return instance;
		}

		public void Resolve(object instance, IConfiguration configuration, ConfigurationBinderOptions options)
		{
			if(instance == null)
				throw new ArgumentNullException(nameof(instance));

			var type = instance.GetType();

			if(type == typeof(IConfigurationSection))
				return;

			foreach(var child in configuration.GetChildren())
			{
				this.ResolveInstance(instance, child, options);
			}
		}

		private void ResolveInstance(object instance, IConfiguration configuration, ConfigurationBinderOptions options)
		{
			if(configuration is IConfigurationSection section)
			{
				var properties = this.GetProperties(instance.GetType().GetTypeInfo());

				if(section.Value != null)
				{
					if(properties.TryGetValue(section.Key, out var property))
					{
						if(property.SetValue(instance, section.Value))
							return;

						throw new InvalidOperationException(string.Format(Properties.Resources.Error_FailedBinding, section.Path, instance.GetType()));
					}

					this.OnUnrecognize(instance, section.Key, section.Value);
				}
				else
				{
					if(properties.TryGetValue(section.Key, out var property))
					{
						var target = property.GetValue(instance);

						if(target == null)
							target = this.Resolve(property.PropertyType, configuration, options);
						else
							this.Resolve(target, configuration, options);

						property.SetValue(instance, target);
					}
					else
					{
						var dictionaryType = GetImplementedContract(instance.GetType(), typeof(IDictionary<,>));

						if(dictionaryType != null)
						{
							if(dictionaryType.GenericTypeArguments[0] != typeof(string))
								return;

							var valueType = dictionaryType.GenericTypeArguments[1];
							var setter = dictionaryType.GetTypeInfo().GetDeclaredProperty("Item");

							Reflection.Reflector.SetValue(setter, ref instance, this.Resolve(valueType, configuration, options), new object[] { section.Key });

							return;
						}

						var collectionType = GetImplementedContract(instance.GetType(), typeof(ICollection<>));

						if(collectionType != null)
						{
							var valueType = collectionType.GenericTypeArguments[0];
							var add = collectionType.GetTypeInfo().GetDeclaredMethod("Add");

							add.Invoke(instance, new object[] { this.Resolve(valueType, configuration, options) });

							return;
						}

						this.OnUnrecognize(instance, configuration);
					}
				}
			}
		}
		#endregion

		#region 虚拟方法
		protected virtual object CreateInstance(Type type)
		{
			if(type.IsArray)
			{
				if(type.GetArrayRank() > 1)
					throw new InvalidOperationException(string.Format(Properties.Resources.Error_UnsupportedMultidimensionalArray, type));

				return Array.CreateInstance(type.GetElementType(), 0);
			}

			if(type.IsInterface)
			{
				var contract = GetImplementedContract(type,
					typeof(IReadOnlyDictionary<,>),
					typeof(IDictionary<,>));

				if(contract != null)
				{
					var dictionaryType = typeof(Dictionary<,>).MakeGenericType(contract.GenericTypeArguments[0], contract.GenericTypeArguments[1]);
					return dictionaryType.GenericTypeArguments[0] != typeof(string) ?
						Activator.CreateInstance(dictionaryType) :
						Activator.CreateInstance(dictionaryType, new object[] { StringComparer.OrdinalIgnoreCase });
				}

				contract = GetImplementedContract(type,
					typeof(IReadOnlyList<>),
					typeof(IList<>),
					typeof(IReadOnlyCollection<>),
					typeof(ICollection<>),
					typeof(IEnumerable<>));

				if(contract != null)
				{
					var listType = typeof(List<>).MakeGenericType(contract.GenericTypeArguments[0]);
					return Activator.CreateInstance(listType);
				}
			}

			try
			{
				if(type.IsInterface || type.IsAbstract)
					return Zongsoft.Data.Model.Build(type);

				return Activator.CreateInstance(type);
			}
			catch(Exception ex)
			{
				throw new InvalidOperationException(string.Format(Properties.Resources.Error_FailedToActivate, type), ex);
			}
		}

		protected virtual IDictionary<string, PropertyToken> GetProperties(TypeInfo type)
		{
			var properties = new Dictionary<string, PropertyToken>(StringComparer.OrdinalIgnoreCase);

			do
			{
				foreach(var property in type.DeclaredProperties)
				{
					var token = new PropertyToken(property, GetConverter(property));

					properties.TryAdd(property.Name, token);

					if(!string.Equals(token.ConfigurationKey, property.Name))
						properties.TryAdd(token.ConfigurationKey, token);
				}

				type = type.BaseType.GetTypeInfo();
			}
			while(type != typeof(object).GetTypeInfo());

			return properties;
		}

		protected virtual UnrecognizedPropertyToken GetUnrecognizedProperty(TypeInfo type)
		{
			if(type == null)
				throw new ArgumentNullException(nameof(type));

			var attribute = type.GetCustomAttribute<ConfigurationResolverAttribute>(true);

			if(attribute == null || string.IsNullOrEmpty(attribute.UnrecognizedProperty))
				return default;

			var unrecognizedProperty = type.GetDeclaredProperty(attribute.UnrecognizedProperty) ??
				throw new ArgumentException(string.Format(Zongsoft.Properties.Resources.Error_PropertyNotExists, type, attribute.UnrecognizedProperty));

			if(!unrecognizedProperty.CanRead)
				throw new InvalidOperationException(string.Format(Zongsoft.Properties.Resources.Error_PropertyCannotRead, type, attribute.UnrecognizedProperty));

			var dictionaryType = ConfigurationBinder.GetImplementedContracts(unrecognizedProperty.PropertyType, typeof(IDictionary<,>))?.GetTypeInfo();

			if(dictionaryType != null || dictionaryType.GenericTypeArguments[0] == typeof(string))
				return new UnrecognizedPropertyToken(unrecognizedProperty, dictionaryType);

			return default;
		}

		protected virtual void OnUnrecognize(object target, string name, string value)
		{
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

		protected void OnUnrecognize(object target, IConfiguration configuration)
		{
			if(configuration is IConfigurationSection section && section.Value != null)
				this.OnUnrecognize(target, section.Path, section.Value);

			foreach(var child in configuration.GetChildren())
			{
				this.OnUnrecognize(target, child);
			}
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

		private static Type GetImplementedContract(Type actual, params Type[] expectedTypes)
		{
			if(actual.IsGenericType && expectedTypes.Contains(actual.GetGenericTypeDefinition()))
				return actual;

			var contracts = actual.GetTypeInfo().ImplementedInterfaces;

			foreach(var contract in contracts)
			{
				if(contract.IsGenericType && expectedTypes.Contains(contract.GetGenericTypeDefinition()))
					return contract;
			}

			return null;
		}
		#endregion

		#region 内部结构
		protected class PropertyToken
		{
			private string _configurationKey;

			public readonly PropertyInfo Property;
			public readonly TypeConverter Converter;

			public PropertyToken(PropertyInfo property, TypeConverter converter)
			{
				this.Property = property;
				this.Converter = converter;
			}

			public string Name => this.Property.Name;
			public bool CanRead => this.Property.CanRead;
			public bool CanWrite => this.Property.CanWrite;
			public Type PropertyType => this.Property.PropertyType;

			public string ConfigurationKey
			{
				get
				{
					if(string.IsNullOrEmpty(_configurationKey))
					{
						_configurationKey = this.Property.GetCustomAttribute<ConfigurationPropertyAttribute>(true)?.Name;

						if(string.IsNullOrEmpty(_configurationKey))
							_configurationKey = this.Property.Name;
					}

					return _configurationKey;
				}
			}

			public object GetValue(object target)
			{
				return Reflection.Reflector.GetValue(this.Property, ref target);
			}

			public bool SetValue(object target, object value)
			{
				if(!this.CanWrite)
					return false;

				var converter = this.Converter;

				if(Zongsoft.Common.Convert.TryConvertValue(value, this.PropertyType, () => converter, out var convertedValue))
				{
					Reflection.Reflector.SetValue(this.Property, ref target, convertedValue);
					return true;
				}

				return false;
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

			public bool SetDictionaryValue(object dictionary, string key, string value)
			{
				if(dictionary == null)
					throw new ArgumentNullException(nameof(dictionary));

				var property = this.Property;
				var dictionaryType = this.DictionaryType;

				if(property == null || dictionaryType == null)
					return false;

				if(dictionary != null && Common.Convert.TryConvertValue(value, dictionaryType.GenericTypeArguments[1], () => GetConverter(property), out var convertedValue))
				{
					Reflection.Reflector.SetValue(dictionary, "Item", convertedValue, new object[] { key });
					return true;
				}

				return false;
			}
		}
		#endregion
	}
}
