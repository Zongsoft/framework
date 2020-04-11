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

			configuration.GetSection(ConvertPath(path)).Bind(instance);
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

		public static T GetOption<T>(this IConfiguration configuration, string path = null, Action<ConfigurationBinderOptions> configureOptions = null)
		{
			if(configuration == null)
				throw new ArgumentNullException(nameof(configuration));

			var result = configuration.GetOption(typeof(T), path, configureOptions);

			if(result == null)
				return default(T);

			return (T)result;
		}

		public static object GetOption(this IConfiguration configuration, Type type, string path = null, Action<ConfigurationBinderOptions> configureOptions = null)
		{
			if(configuration == null)
				throw new ArgumentNullException(nameof(configuration));

			var options = new ConfigurationBinderOptions();
			configureOptions?.Invoke(options);

			if(!string.IsNullOrEmpty(path))
				configuration = configuration.GetSection(ConvertPath(path));

			return BindInstance(type, instance: null, configuration: configuration, options: options);
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

			var section = configuration.GetSection(ConvertPath(path));
			var value = section.Value;

			if(value == null)
				return defaultValue;

			if(Common.Convert.TryConvertValue(value, type, out var result))
				return result;

			throw new InvalidOperationException(string.Format(Properties.Resources.Error_FailedBinding, path, type));
		}
		#endregion

		#region 私有方法
		private static string ConvertPath(string path)
		{
			if(string.IsNullOrEmpty(path))
				return path;

			return path.Trim('/').Replace('/', ':');
		}

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

		private static object BindInstance(Type type, object instance, IConfiguration configuration, ConfigurationBinderOptions options)
		{
			if(type == typeof(IConfigurationSection))
				return configuration;

			if(configuration is IConfigurationSection section && section.Value != null)
			{
				if(Common.Convert.TryConvertValue(section.Value, type, out var convertedValue))
					return convertedValue;

				throw new InvalidOperationException(string.Format(Properties.Resources.Error_FailedBinding, section.Path, type));
			}

			if(configuration != null && configuration.GetChildren().Any())
			{
				if(instance == null)
				{
					instance = AttemptBindToCollectionInterfaces(type, configuration, options);

					if(instance != null)
						return instance;

					instance = CreateInstance(type);
				}

				var collectionInterface = GetImplementedContract(typeof(IDictionary<,>), type);

				if(collectionInterface != null)
				{
					BindDictionary(instance, collectionInterface, configuration, options);
				}
				else if(type.IsArray)
				{
					instance = BindArray((Array)instance, configuration, options);
				}
				else
				{
					collectionInterface = GetImplementedContract(typeof(ICollection<>), type);

					if(collectionInterface != null)
						BindCollection(instance, collectionInterface, configuration, options);
					else
						BindProperties(configuration, instance, options);
				}
			}

			return instance;
		}

		private static object AttemptBindToCollectionInterfaces(Type type, IConfiguration configuration, ConfigurationBinderOptions options)
		{
			var typeInfo = type.GetTypeInfo();

			if(!type.IsInterface)
				return null;

			var collectionInterface = GetImplementedContract(typeof(IReadOnlyList<>), type);
			if(collectionInterface != null)
				return GenerateList(typeInfo, configuration, options);

			collectionInterface = GetImplementedContract(typeof(IReadOnlyDictionary<,>), type);
			if(collectionInterface != null)
			{
				var dictionaryType = typeof(Dictionary<,>).MakeGenericType(typeInfo.GenericTypeArguments[0], typeInfo.GenericTypeArguments[1]);
				var instance = Activator.CreateInstance(dictionaryType);
				BindDictionary(instance, dictionaryType, configuration, options);
				return instance;
			}

			collectionInterface = GetImplementedContract(typeof(IDictionary<,>), type);
			if(collectionInterface != null)
			{
				var instance = Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeInfo.GenericTypeArguments[0], typeInfo.GenericTypeArguments[1]));
				BindDictionary(instance, collectionInterface, configuration, options);
				return instance;
			}

			collectionInterface = GetImplementedContract(typeof(IReadOnlyCollection<>), type);
			if(collectionInterface != null)
				return GenerateList(typeInfo, configuration, options);

			collectionInterface = GetImplementedContract(typeof(ICollection<>), type);
			if(collectionInterface != null)
				return GenerateList(typeInfo, configuration, options);

			collectionInterface = GetImplementedContract(typeof(IEnumerable<>), type);
			if(collectionInterface != null)
				return GenerateList(typeInfo, configuration, options);

			return null;
		}

		private static object GenerateList(Type type, IConfiguration configuration, ConfigurationBinderOptions options)
		{
			var listType = typeof(List<>).MakeGenericType(type.GenericTypeArguments[0]);
			var instance = Activator.CreateInstance(listType);

			BindCollection(instance, listType, configuration, options);

			return instance;
		}

		private static void BindProperties(this IConfiguration configuration, object instance, ConfigurationBinderOptions options)
		{
			if(instance == null)
				return;

			foreach(var property in GetAllProperties(instance.GetType().GetTypeInfo()))
			{
				BindProperty(property, instance, configuration, options);
			}
		}

		private static void BindProperty(PropertyInfo property, object instance, IConfiguration configuration, ConfigurationBinderOptions options)
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

			var key = property.Name;

			if(property.IsDefined(typeof(ConfigurationPropertyAttribute), true))
				key = property.GetCustomAttribute<ConfigurationPropertyAttribute>(true).Name;

			propertyValue = BindInstance(property.PropertyType, propertyValue, configuration.GetSection(key), options);

			if(propertyValue != null && hasSetter)
				property.SetValue(instance, propertyValue);
		}

		private static void BindDictionary(object dictionary, Type dictionaryType, IConfiguration configuration, ConfigurationBinderOptions options)
		{
			var typeInfo = dictionaryType.GetTypeInfo();
			var keyType = typeInfo.GenericTypeArguments[0];
			var valueType = typeInfo.GenericTypeArguments[1];
			var keyTypeIsEnum = keyType.GetTypeInfo().IsEnum;

			if(keyType != typeof(string) && !keyTypeIsEnum)
				return;

			var setter = typeInfo.GetDeclaredProperty("Item");

			foreach(var child in configuration.GetChildren())
			{
				if(child.Value == null)
				{
					var item = BindInstance(
						type: valueType,
						instance: null,
						configuration: child,
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
				else
				{
					var property = (PropertyInfo)dictionary
						.GetType()
						.FindMembers(
							MemberTypes.Property,
							BindingFlags.Public | BindingFlags.Instance,
							MatchProperty,
							child.Key)
						.FirstOrDefault();

					if(property != null && property.CanWrite)
						property.SetValue(dictionary, BindInstance(property.PropertyType, dictionary, child, options));
				}
			}
		}

		private static void BindCollection(object collection, Type collectionType, IConfiguration configuration, ConfigurationBinderOptions options)
		{
			var typeInfo = collectionType.GetTypeInfo();
			var itemType = typeInfo.GenericTypeArguments[0];
			var addMethod = typeInfo.GetDeclaredMethod("Add");

			foreach(var child in configuration.GetChildren())
			{
				try
				{
					if(child.Value == null)
					{
						var item = BindInstance(
							type: itemType,
							instance: null,
							configuration: child,
							options: options);

						if(item != null)
							addMethod.Invoke(collection, new[] { item });
					}
					else
					{
						var property = (PropertyInfo)collection
						   .GetType()
						   .FindMembers(
							   MemberTypes.Property,
							   BindingFlags.Public | BindingFlags.Instance,
							   MatchProperty,
							   child.Key)
						   .FirstOrDefault();

						if(property != null && property.CanWrite)
							property.SetValue(collection, BindInstance(property.PropertyType, collection, child, options));
					}
				}
				catch
				{
				}
			}
		}

		private static Array BindArray(Array array, IConfiguration configuration, ConfigurationBinderOptions options)
		{
			var children = configuration.GetChildren().ToArray();
			var arrayLength = array.Length;
			var elementType = array.GetType().GetElementType();
			var result = Array.CreateInstance(elementType, arrayLength + children.Length);

			if(arrayLength > 0)
				Array.Copy(array, result, arrayLength);

			for(int i = 0; i < children.Length; i++)
			{
				try
				{
					var item = BindInstance(
						type: elementType,
						instance: null,
						configuration: children[i],
						options: options);

					if(item != null)
						result.SetValue(item, arrayLength + i);
				}
				catch
				{
				}
			}

			return result;
		}

		private static Type GetImplementedContract(Type expected, Type actual)
		{
			if(actual.IsGenericType && actual.GetGenericTypeDefinition() == expected)
				return actual;

			var contracts = actual.GetTypeInfo().ImplementedInterfaces;

			foreach(var contract in contracts)
			{
				if(contract.IsGenericType && contract.GetGenericTypeDefinition() == expected)
					return contract;
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

		private static bool MatchProperty(MemberInfo member, object state)
		{
			return state is string name &&
				(
					string.Equals(name, member.Name, StringComparison.OrdinalIgnoreCase) ||
					string.Equals(name, member.GetCustomAttribute<ConfigurationPropertyAttribute>(true)?.Name, StringComparison.OrdinalIgnoreCase)
				);
		}
		#endregion
	}
}
