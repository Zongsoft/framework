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
using System.Collections;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;

namespace Zongsoft.Configuration
{
	public class ConfigurationResolver : IConfigurationResolver
	{
		#region 单例字段
		public static readonly ConfigurationResolver Default = new ConfigurationResolver();
		#endregion

		#region 构造函数
		protected ConfigurationResolver()
		{
		}
		#endregion

		#region 公共属性
		public IConfigurationRecognizer Recognizer
		{
			get; set;
		}
		#endregion

		#region 解析方法
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
						var dictionaryType = ConfigurationUtility.GetImplementedContract(instance.GetType(), typeof(IDictionary<,>));

						if(dictionaryType != null)
						{
							if(dictionaryType.GenericTypeArguments[0] != typeof(string))
								return;

							var valueType = dictionaryType.GenericTypeArguments[1];
							var setter = dictionaryType.GetTypeInfo().GetDeclaredProperty("Item");

							Reflection.Reflector.SetValue(setter, ref instance, this.Resolve(valueType, configuration, options), new object[] { section.Key });

							return;
						}

						var collectionType = ConfigurationUtility.GetImplementedContract(instance.GetType(), typeof(ICollection<>));

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

		#region 附加方法
		public void Attach(object instance, IConfiguration configuration, ConfigurationBinderOptions options)
		{
			if(instance == null)
				throw new ArgumentNullException(nameof(instance));

			if(configuration == null)
				throw new ArgumentNullException(nameof(configuration));

			var properties = this.GetProperties(instance.GetType().GetTypeInfo()).Values.Distinct().ToArray();

			foreach(var property in properties)
			{
				if(Common.TypeExtension.IsScalarType(property.PropertyType))
				{
					configuration[property.ConfigurationKey] = property.GetStringValue(instance);
					continue;
				}

				var propertyValue = property.GetValue(instance);

				var dictionaryType = ConfigurationUtility.GetImplementedContract(
					property.PropertyType,
					typeof(IReadOnlyDictionary<,>),
					typeof(IDictionary<,>));

				if(dictionaryType != null)
				{
					//if(propertyValue.GetType() != dictionaryType)
					//	this.Attach(propertyValue, configuration.GetSection(property.ConfigurationKey), options);

					foreach(DictionaryEntry entry in (IEnumerable)propertyValue)
					{
						var key = ConfigurationPath.Combine(property.ConfigurationKey, entry.Key.ToString());
						this.Attach(entry.Value, configuration.GetSection(key), options);
					}

					continue;
				}

				var collectionType = ConfigurationUtility.GetImplementedContract(
					property.PropertyType,
					typeof(IReadOnlyCollection<>),
					typeof(ICollection<>),
					typeof(IEnumerable<>));

				if(collectionType != null)
				{
					//if(propertyValue.GetType() != collectionType)
					//	this.Attach(propertyValue, configuration.GetSection(property.ConfigurationKey), options);

					int index = 0;

					foreach(var entry in (IEnumerable)propertyValue)
					{
						var key = ConfigurationPath.Combine(property.ConfigurationKey, index++.ToString());
						this.Attach(entry, configuration.GetSection(key), options);
					}

					continue;
				}

				this.Attach(propertyValue, configuration.GetSection(property.ConfigurationKey), options);
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
				var contract = ConfigurationUtility.GetImplementedContract(type,
					typeof(IReadOnlyDictionary<,>),
					typeof(IDictionary<,>));

				if(contract != null)
				{
					var dictionaryType = typeof(Dictionary<,>).MakeGenericType(contract.GenericTypeArguments[0], contract.GenericTypeArguments[1]);
					return dictionaryType.GenericTypeArguments[0] != typeof(string) ?
						Activator.CreateInstance(dictionaryType) :
						Activator.CreateInstance(dictionaryType, new object[] { StringComparer.OrdinalIgnoreCase });
				}

				contract = ConfigurationUtility.GetImplementedContract(type, typeof(ISet<>));

				if(contract != null)
				{
					var setType = typeof(HashSet<>).MakeGenericType(contract.GenericTypeArguments[0]);
					return Activator.CreateInstance(setType);
				}

				contract = ConfigurationUtility.GetImplementedContract(type,
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
					var token = new PropertyToken(property);

					properties.TryAdd(property.Name, token);

					if(!string.Equals(token.ConfigurationKey, property.Name))
						properties.TryAdd(token.ConfigurationKey, token);
				}

				type = type.BaseType.GetTypeInfo();
			}
			while(type != typeof(object).GetTypeInfo());

			return properties;
		}

		protected virtual void OnUnrecognize(object target, string name, string value)
		{
			(this.Recognizer ?? ConfigurationRecognizer.Default).Recognize(target, name, value);
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

		#region 内部结构
		protected class PropertyToken : IEquatable<PropertyToken>
		{
			#region 成员字段
			private string _configurationKey;
			#endregion

			#region 构造函数
			public PropertyToken(PropertyInfo property)
			{
				this.Property = property;
				this.Converter = ConfigurationUtility.GetConverter(property);
			}
			#endregion

			#region 公共字段
			public readonly TypeConverter Converter;
			public readonly PropertyInfo Property;
			#endregion

			#region 公共属性
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
			#endregion

			#region 公共方法
			public object GetValue(object target)
			{
				return Reflection.Reflector.GetValue(this.Property, ref target);
			}

			public string GetStringValue(object target)
			{
				return Common.Convert.ConvertValue<string>(
					Reflection.Reflector.GetValue(this.Property, ref target),
					() => this.Converter);
			}

			public bool SetValue(object target, object value)
			{
				if(!this.CanWrite)
					return false;

				if(Zongsoft.Common.Convert.TryConvertValue(value, this.PropertyType, () => this.Converter, out var convertedValue))
				{
					Reflection.Reflector.SetValue(this.Property, ref target, convertedValue);
					return true;
				}

				return false;
			}
			#endregion

			#region 重写方法
			public bool Equals(PropertyToken other)
			{
				if(other == null)
					return false;

				return this.Property.Equals(other.Property);
			}

			public override bool Equals(object obj)
			{
				if(obj == null || obj.GetType() != this.GetType())
					return false;

				return this.Property.Equals((PropertyToken)obj);
			}

			public override int GetHashCode()
			{
				return this.Property.GetHashCode();
			}

			public override string ToString()
			{
				return $"{this.Property.Name}:{this.Property.PropertyType.FullName}";
			}
			#endregion
		}
		#endregion
	}
}
