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
	public static class ConfigurationBinder
	{
		#region 公共方法
		public static void Bind(this IConfiguration configuration, string path, object instance)
		{
			if(string.IsNullOrEmpty(path))
				throw new ArgumentNullException(nameof(path));

			configuration.GetSection(path.Trim('/').Replace('/', ':')).Bind(instance);
		}

		public static void Bind(this IConfiguration configuration, object instance)
		{
			configuration.Bind(instance, o => { });
		}

		public static void Bind(this IConfiguration configuration, object instance, Action<ConfigurationBinderOptions> configureOptions)
		{
			if(configuration == null)
				throw new ArgumentNullException(nameof(configuration));

			if(instance != null)
			{
				var options = new ConfigurationBinderOptions();
				configureOptions?.Invoke(options);
				BindInstance(instance.GetType(), instance, configuration, options);
			}
		}

		public static T GetOption<T>(this IConfiguration configuration)
		{
			return configuration.GetOption<T>(_ => { });
		}

		public static T GetOption<T>(this IConfiguration configuration, Action<ConfigurationBinderOptions> configureOptions)
		{
			if(configuration == null)
				throw new ArgumentNullException(nameof(configuration));

			var result = configuration.GetOption(typeof(T), configureOptions);

			if(result == null)
				return default(T);

			return (T)result;
		}

		public static object GetOption(this IConfiguration configuration, Type type)
		{
			return configuration.GetOption(type, _ => { });
		}

		public static object GetOption(this IConfiguration configuration, Type type, Action<ConfigurationBinderOptions> configureOptions)
		{
			if(configuration == null)
				throw new ArgumentNullException(nameof(configuration));

			var options = new ConfigurationBinderOptions();
			configureOptions?.Invoke(options);
			return BindInstance(type, instance: null, config: configuration, options: options);
		}

		public static T GetOptionValue<T>(this IConfiguration configuration, string path)
		{
			return GetOptionValue(configuration, path, default(T));
		}

		public static T GetOptionValue<T>(this IConfiguration configuration, string path, T defaultValue)
		{
			return (T)GetOptionValue(configuration, typeof(T), path, defaultValue);
		}

		public static object GetOptionValue(this IConfiguration configuration, Type type, string key)
		{
			return GetOptionValue(configuration, type, key, defaultValue: null);
		}

		public static object GetOptionValue(this IConfiguration configuration, Type type, string path, object defaultValue)
		{
			if(string.IsNullOrEmpty(path))
				throw new ArgumentNullException(nameof(path));

			var section = configuration.GetSection(path.Trim('/').Replace('/', ':'));
			var value = section.Value;

			if(value != null)
				return ConvertValue(type, value, section.Path);

			return defaultValue;
		}
		#endregion

		#region 私有方法
		private static object CreateInstance(Type type)
		{
			if(type.IsInterface || type.IsAbstract)
				return Zongsoft.Data.Model.Build(type);

			if(type.IsArray)
			{
				if(type.GetArrayRank() > 1)
					throw new InvalidOperationException(string.Format(Properties.Resources.Error_UnsupportedMultidimensionalArray, type));

				return Array.CreateInstance(type.GetElementType(), 0);
			}

			try
			{
				return Activator.CreateInstance(type);
			}
			catch(Exception ex)
			{
				throw new InvalidOperationException(string.Format(Properties.Resources.Error_FailedToActivate, type), ex);
			}
		}

		private static object BindInstance(Type type, object instance, IConfiguration config, ConfigurationBinderOptions options)
		{
			if(type == typeof(IConfigurationSection))
				return config;

			var section = config as IConfigurationSection;
			var configValue = section?.Value;
			object convertedValue;
			Exception error;

			if(configValue != null && TryConvertValue(type, configValue, section.Path, out convertedValue, out error))
			{
				if(error != null)
					throw error;

				return convertedValue;
			}

			if(config != null && config.GetChildren().Any())
			{
				// If we don't have an instance, try to create one
				if(instance == null)
				{
					// We are already done if binding to a new collection instance worked
					instance = AttemptBindToCollectionInterfaces(type, config, options);

					if(instance != null)
						return instance;

					instance = CreateInstance(type);
				}

				// See if its a Dictionary
				var collectionInterface = FindOpenGenericInterface(typeof(IDictionary<,>), type);
				if(collectionInterface != null)
				{
					BindDictionary(instance, collectionInterface, config, options);
				}
				else if(type.IsArray)
				{
					instance = BindArray((Array)instance, config, options);
				}
				else
				{
					// See if its an ICollection
					collectionInterface = FindOpenGenericInterface(typeof(ICollection<>), type);

					if(collectionInterface != null)
						BindCollection(instance, collectionInterface, config, options);
					else
						BindNonScalar(config, instance, options);
				}
			}

			return instance;
		}

		private static object AttemptBindToCollectionInterfaces(Type type, IConfiguration config, ConfigurationBinderOptions options)
		{
			var typeInfo = type.GetTypeInfo();

			if(!typeInfo.IsInterface)
				return null;

			var collectionInterface = FindOpenGenericInterface(typeof(IReadOnlyList<>), type);
			if(collectionInterface != null)
				return BindToCollection(typeInfo, config, options);

			collectionInterface = FindOpenGenericInterface(typeof(IReadOnlyDictionary<,>), type);
			if(collectionInterface != null)
			{
				var dictionaryType = typeof(Dictionary<,>).MakeGenericType(typeInfo.GenericTypeArguments[0], typeInfo.GenericTypeArguments[1]);
				var instance = Activator.CreateInstance(dictionaryType);
				BindDictionary(instance, dictionaryType, config, options);
				return instance;
			}

			collectionInterface = FindOpenGenericInterface(typeof(IDictionary<,>), type);
			if(collectionInterface != null)
			{
				var instance = Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeInfo.GenericTypeArguments[0], typeInfo.GenericTypeArguments[1]));
				BindDictionary(instance, collectionInterface, config, options);
				return instance;
			}

			collectionInterface = FindOpenGenericInterface(typeof(IReadOnlyCollection<>), type);
			if(collectionInterface != null)
				return BindToCollection(typeInfo, config, options);

			collectionInterface = FindOpenGenericInterface(typeof(ICollection<>), type);
			if(collectionInterface != null)
				return BindToCollection(typeInfo, config, options);

			collectionInterface = FindOpenGenericInterface(typeof(IEnumerable<>), type);
			if(collectionInterface != null)
				return BindToCollection(typeInfo, config, options);

			return null;
		}

		private static void BindNonScalar(this IConfiguration configuration, object instance, ConfigurationBinderOptions options)
		{
			if(instance == null)
				return;

			foreach(var property in GetAllProperties(instance.GetType().GetTypeInfo()))
			{
				BindProperty(property, instance, configuration, options);
			}
		}

		private static void BindProperty(PropertyInfo property, object instance, IConfiguration config, ConfigurationBinderOptions options)
		{
			if(property.GetMethod == null ||
				(!options.BindNonPublicProperties && !property.GetMethod.IsPublic) ||
				property.GetMethod.GetParameters().Length > 0)
			{
				return;
			}

			var propertyValue = property.GetValue(instance);
			var hasSetter = property.SetMethod != null && (property.SetMethod.IsPublic || options.BindNonPublicProperties);

			if(propertyValue == null && !hasSetter)
				return;

			propertyValue = BindInstance(property.PropertyType, propertyValue, config.GetSection(property.Name), options);

			if(propertyValue != null && hasSetter)
				property.SetValue(instance, propertyValue);
		}

		private static object BindToCollection(TypeInfo typeInfo, IConfiguration config, ConfigurationBinderOptions options)
		{
			var type = typeof(List<>).MakeGenericType(typeInfo.GenericTypeArguments[0]);
			var instance = Activator.CreateInstance(type);
			BindCollection(instance, type, config, options);
			return instance;
		}

		private static void BindDictionary(object dictionary, Type dictionaryType, IConfiguration config, ConfigurationBinderOptions options)
		{
			var typeInfo = dictionaryType.GetTypeInfo();

			// IDictionary<K,V> is guaranteed to have exactly two parameters
			var keyType = typeInfo.GenericTypeArguments[0];
			var valueType = typeInfo.GenericTypeArguments[1];
			var keyTypeIsEnum = keyType.GetTypeInfo().IsEnum;

			if(keyType != typeof(string) && !keyTypeIsEnum)
				return;

			var setter = typeInfo.GetDeclaredProperty("Item");

			foreach(var child in config.GetChildren())
			{
				var item = BindInstance(
					type: valueType,
					instance: null,
					config: child,
					options: options);

				if(item != null)
				{
					if(keyType == typeof(string))
					{
						var key = child.Key;
						setter.SetValue(dictionary, item, new object[] { key });
					}
					else if(keyTypeIsEnum)
					{
						var key = Enum.Parse(keyType, child.Key);
						setter.SetValue(dictionary, item, new object[] { key });
					}
				}
			}
		}

		private static void BindCollection(object collection, Type collectionType, IConfiguration config, ConfigurationBinderOptions options)
		{
			var typeInfo = collectionType.GetTypeInfo();

			// ICollection<T> is guaranteed to have exactly one parameter
			var itemType = typeInfo.GenericTypeArguments[0];
			var addMethod = typeInfo.GetDeclaredMethod("Add");

			foreach(var section in config.GetChildren())
			{
				try
				{
					var item = BindInstance(
						type: itemType,
						instance: null,
						config: section,
						options: options);

					if(item != null)
						addMethod.Invoke(collection, new[] { item });
				}
				catch
				{
				}
			}
		}

		private static Array BindArray(Array source, IConfiguration config, ConfigurationBinderOptions options)
		{
			var children = config.GetChildren().ToArray();
			var arrayLength = source.Length;
			var elementType = source.GetType().GetElementType();
			var newArray = Array.CreateInstance(elementType, arrayLength + children.Length);

			// binding to array has to preserve already initialized arrays with values
			if(arrayLength > 0)
				Array.Copy(source, newArray, arrayLength);

			for(int i = 0; i < children.Length; i++)
			{
				try
				{
					var item = BindInstance(
						type: elementType,
						instance: null,
						config: children[i],
						options: options);

					if(item != null)
						newArray.SetValue(item, arrayLength + i);
				}
				catch
				{
				}
			}

			return newArray;
		}

		private static bool TryConvertValue(Type type, string value, string path, out object result, out Exception error)
		{
			error = null;
			result = null;

			if(type == typeof(object))
			{
				result = value;
				return true;
			}

			if(type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
			{
				if(string.IsNullOrEmpty(value))
					return true;

				return TryConvertValue(Nullable.GetUnderlyingType(type), value, path, out result, out error);
			}

			var converter = TypeDescriptor.GetConverter(type);

			if(converter.CanConvertFrom(typeof(string)))
			{
				try
				{
					result = converter.ConvertFromInvariantString(value);
				}
				catch(Exception ex)
				{
					error = new InvalidOperationException(string.Format(Properties.Resources.Error_FailedBinding, path, type), ex);
				}

				return true;
			}

			return false;
		}

		private static object ConvertValue(Type type, string value, string path)
		{
			TryConvertValue(type, value, path, out var result, out var error);

			if(error != null)
				throw error;

			return result;
		}

		private static Type FindOpenGenericInterface(Type expected, Type actual)
		{
			var actualTypeInfo = actual.GetTypeInfo();

			if(actualTypeInfo.IsGenericType && actual.GetGenericTypeDefinition() == expected)
				return actual;

			var interfaces = actualTypeInfo.ImplementedInterfaces;

			foreach(var interfaceType in interfaces)
			{
				if(interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == expected)
					return interfaceType;
			}

			return null;
		}

		private static IEnumerable<PropertyInfo> GetAllProperties(TypeInfo type)
		{
			var allProperties = new List<PropertyInfo>();

			do
			{
				allProperties.AddRange(type.DeclaredProperties);
				type = type.BaseType.GetTypeInfo();
			}
			while(type != typeof(object).GetTypeInfo());

			return allProperties;
		}
		#endregion
	}
}
