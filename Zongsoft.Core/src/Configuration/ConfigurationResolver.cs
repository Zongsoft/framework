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

namespace Zongsoft.Configuration;

public class ConfigurationResolver : IConfigurationResolver
{
	#region 单例字段
	public static readonly ConfigurationResolver Default = new();
	#endregion

	#region 构造函数
	protected ConfigurationResolver() { }
	#endregion

	#region 公共属性
	public IConfigurationRecognizerProvider Recognizers { get; set; }
	#endregion

	#region 解析方法
	public object Resolve(Type type, IConfiguration configuration, ConfigurationBinderOptions options)
	{
		if(type == null)
			throw new ArgumentNullException(nameof(type));

		var instance = this.CreateInstance(type, configuration);
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
		if(configuration is not IConfigurationSection section)
			return;

		if(section.IsCollection())
		{
			if(Collections.CollectionUtility.TryAdd(instance, section.Value))
				return;

			throw new ConfigurationException($"Failed to add `{section.Path}={section.Value}` configuration collection entry");
		}

		var properties = this.GetProperties(instance.GetType().GetTypeInfo());

		if(section.Value != null)
		{
			if(properties.TryGetValue(section.Key, out var property))
			{
				if(SetPathInfo(instance, property, section))
					return;

				if(property.SetValue(instance, section.Value))
					return;

				throw new InvalidOperationException(string.Format(Properties.Resources.Error_FailedBinding, section.Path, property.PropertyType));
			}

			this.OnUnrecognize(instance, properties, section, options);
		}
		else
		{
			if(properties.TryGetValue(section.Key, out var property))
			{
				var target = property.GetValue(instance);

				if(target == null)
					target = ConfigurationUtility.GetResolver(property.PropertyType).Resolve(property.PropertyType, configuration, options);
				else
					this.Resolve(target, configuration, options);

				property.SetValue(instance, target);
			}
			else
			{
				if(!this.ResolveCollection(instance, section, properties, options))
					this.OnUnrecognize(instance, properties, section, options);
			}
		}
	}

	private bool ResolveCollection(object instance, IConfigurationSection configuration, IDictionary<string, PropertyToken> properties, ConfigurationBinderOptions options)
	{
		var dictionaryType = ConfigurationUtility.GetImplementedContract(instance.GetType(), typeof(IDictionary<,>));

		if(dictionaryType != null)
		{
			object key = configuration.Key;

			//如果字典键类型不是字符串并且配置键文本转换失败则抛出异常
			if(dictionaryType.GenericTypeArguments[0] != typeof(string) && !Common.Convert.TryConvertValue(key, dictionaryType.GenericTypeArguments[0], out key))
				throw new ConfigurationException($"Unable to convert the ‘{key}’ configuration key to a dictionary key type of ‘{dictionaryType.GenericTypeArguments[0].FullName}’ type.");

			var valueType = dictionaryType.GenericTypeArguments[1];
			var setter = dictionaryType.GetTypeInfo().GetDeclaredProperty("Item");

			Reflection.Reflector.SetValue(setter, ref instance, ConfigurationUtility.GetResolver(valueType).Resolve(valueType, configuration, options), [key]);

			return true;
		}

		var collectionType = ConfigurationUtility.GetImplementedContract(instance.GetType(), typeof(ICollection<>));

		if(collectionType != null)
		{
			var valueType = collectionType.GenericTypeArguments[0];
			var add = collectionType.GetTypeInfo().GetDeclaredMethod("Add");

			add.Invoke(instance, [ConfigurationUtility.GetResolver(valueType).Resolve(valueType, configuration, options)]);

			return true;
		}

		//返回失败
		return false;
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
	protected virtual object CreateInstance(Type type, IConfiguration configuration)
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

			return Activator.CreateInstance(type, GetConstructorArguments(type, configuration as IConfigurationSection));
		}
		catch(Exception ex)
		{
			throw new InvalidOperationException(string.Format(Properties.Resources.Error_FailedToActivate, type), ex);
		}

		static object[] GetConstructorArguments(Type type, IConfigurationSection section)
		{
			if(section == null || section.Key == null)
				return null;

			var constructors = type.GetConstructors();

			for(int i = 0; i < constructors.Length; i++)
			{
				if(!constructors[i].IsPublic)
					continue;

				var args = FindConstructor(constructors[i], section);

				if(args != null)
					return args;
			}

			return null;

			static object[] FindConstructor(ConstructorInfo constructor, IConfigurationSection section)
			{
				object[] args = null;
				var parameters = constructor.GetParameters();

				for(int i = 0; i < parameters.Length; i++)
				{
					if(parameters[i].ParameterType == typeof(string) && (parameters[i].Name == "name" || parameters[i].Name == "key"))
					{
						args ??= new object[parameters.Length];
						args[i] = section.Key;
					}
					else if(parameters[i].HasDefaultValue)
					{
						args ??= new object[parameters.Length];
						args[i] = parameters[i].DefaultValue;
					}
					else
					{
						return null;
					}
				}

				return args;
			}
		}
	}

	protected virtual IDictionary<string, PropertyToken> GetProperties(TypeInfo type)
	{
		var properties = new Dictionary<string, PropertyToken>(StringComparer.OrdinalIgnoreCase);

		do
		{
			foreach(var property in type.DeclaredProperties)
			{
				//如果属性不能写并且还不是集合类型，则忽略对该属性的解析
				if(!property.CanWrite && !Common.TypeExtension.IsCollection(property.PropertyType))
					continue;

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

	protected virtual void OnUnrecognize(object target, IDictionary<string, PropertyToken> properties, IConfigurationSection configuration, ConfigurationBinderOptions options)
	{
		var unrecognizedProperty = ConfigurationRecognizerProvider.GetUnrecognizedProperty(target.GetType());

		if(unrecognizedProperty == null)
		{
			if(configuration.Value == null && this.ResolveDefaultProperty(target, FindDefaultProperty(properties.Values), properties, configuration, options))
				return;

			if(options != null && options.UnrecognizedError)
				throw new ConfigurationException($"The specified '{configuration.Path}' configuration section cannot be bound to a member of the '{target.GetType()}' type.");

			return;
		}

		if(configuration.Value == null)
		{
			var attribute = unrecognizedProperty.GetCustomAttribute<ConfigurationPropertyAttribute>(true);

			//如果指定的未识别属性名被注解为空或星号，则表示它是一个默认集合属性
			if(attribute != null && (string.IsNullOrEmpty(attribute.Name) || attribute.Name == "*"))
			{
				//解析默认集合成功则返回，否则抛出异常
				if(this.ResolveDefaultProperty(target, properties[unrecognizedProperty.Name], properties, configuration, options))
					return;

				throw new ConfigurationException($"The {unrecognizedProperty.Name} property of type '{target.GetType().FullName}' is annotated as the default collection, but the binding for this property fails.");
			}
		}

		var recognizers = this.Recognizers ?? ConfigurationRecognizerProvider.Default;
		var recognizer = recognizers.GetRecognize(target.GetType()) ??
			throw new ConfigurationException($"Unable to get a recognizer of type '{target.GetType().FullName}'.");

		if(!recognizer.Recognize(target, configuration, options) && (options != null && options.UnrecognizedError))
			throw new ConfigurationException($"The '{configuration.Path}' configuration section is not recognized.");
	}
	#endregion

	#region 私有方法
	private bool ResolveDefaultProperty(object instance, PropertyToken property, IDictionary<string, PropertyToken> properties, IConfigurationSection configuration, ConfigurationBinderOptions options)
	{
		if(property == null)
			return false;

		var value = property.GetValue(instance);

		if(value == null)
		{
			if(!property.CanWrite)
				return false;

			value = this.CreateInstance(property.PropertyType, configuration);

			if(!property.SetValue(instance, value))
				throw new ConfigurationException($"The default collection property {property.Name} of '{instance.GetType().FullName}' type is null and it cannot be set.");
		}

		return this.ResolveCollection(value, configuration, this.GetProperties(value.GetType().GetTypeInfo()), options);
	}

	private static PropertyToken FindDefaultProperty(IEnumerable<PropertyToken> properties)
	{
		foreach(var property in properties)
		{
			if(string.IsNullOrEmpty(property.Alias) || property.Alias == "*")
				return property;
		}

		return null;
	}

	private static bool SetPathInfo(object instance, PropertyToken property, IConfigurationSection entry)
	{
		if(property.PropertyType == typeof(System.IO.FileInfo))
		{
			var fileProvider = GetFileProvider(Zongsoft.Services.ApplicationContext.Current?.Configuration?.Providers, entry);

			if(fileProvider != null)
			{
				var path = fileProvider.Source.FileProvider.GetFileInfo(entry.Value);
				return property.SetValue(instance, new System.IO.FileInfo(path.PhysicalPath));
			}
		}
		else if(property.PropertyType == typeof(System.IO.DirectoryInfo))
		{
			var fileProvider = GetFileProvider(Zongsoft.Services.ApplicationContext.Current?.Configuration?.Providers, entry);

			if(fileProvider != null)
			{
				var path = fileProvider.Source.FileProvider.GetFileInfo(entry.Value);
				return property.SetValue(instance, new System.IO.DirectoryInfo(path.PhysicalPath));
			}
		}

		return false;

		static FileConfigurationProvider GetFileProvider(IEnumerable<IConfigurationProvider> providers, IConfigurationSection section)
		{
			if(providers == null)
				return null;

			foreach(var provider in providers)
			{
				if(provider is FileConfigurationProvider fileConfiguration)
				{
					if(provider.TryGet(section.Path, out var value) && string.Equals(value, section.Value))
						return fileConfiguration;
				}
				else if(provider is ICompositeConfigurationProvider composite)
				{
					var found = GetFileProvider(composite.Providers, section);

					if(found != null)
						return found;
				}
			}

			return null;
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
			this.Property = property ?? throw new ArgumentNullException(nameof(property));
			this.Converter = ConfigurationUtility.GetConverter(property);
			this.Alias = property.Name;

			var attribute = property.GetCustomAttribute<ConfigurationPropertyAttribute>(true);
			if(attribute != null)
				this.Alias = attribute.Name;
		}
		#endregion

		#region 公共字段
		public readonly TypeConverter Converter;
		public readonly PropertyInfo Property;
		public readonly string Alias;
		#endregion

		#region 公共属性
		public string Name => this.Property.Name;
		public bool CanRead => this.Property.CanRead;
		public bool CanWrite => this.Property.CanWrite;
		public Type PropertyType => this.Property.PropertyType;
		public bool IsCollection => Common.TypeExtension.IsCollection(this.Property.PropertyType);

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
		public object GetValue(object target) => Reflection.Reflector.GetValue(this.Property, ref target);

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

			if(Common.Convert.TryConvertValue(value, this.PropertyType, () => this.Converter, out var convertedValue))
			{
				Reflection.Reflector.SetValue(this.Property, ref target, convertedValue);
				return true;
			}

			return false;
		}
		#endregion

		#region 重写方法
		public bool Equals(PropertyToken other) => other is not null && this.Property.Equals(other.Property);
		public override bool Equals(object obj) => obj is PropertyToken other && this.Equals(other);
		public override int GetHashCode() => this.Property.GetHashCode();
		public override string ToString() => $"{this.Property.Name}:{this.Property.PropertyType.FullName}";
		#endregion
	}
	#endregion
}
