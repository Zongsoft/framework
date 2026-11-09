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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Linq;
using System.Reflection;

using Microsoft.Extensions.Configuration;

namespace Zongsoft.Configuration;

public static class ConfigurationUtility
{
	public static bool IsCollection(this IConfiguration configuration) =>
		configuration is IConfigurationSection section &&
		section.Key != null &&
		section.Key.Length > 1 &&
		section.Key[0] == '[' &&
		section.Key[^1] == ']';

	public static string GetConfigurationPath(string key) => string.IsNullOrEmpty(key) ? string.Empty : key.Trim('/').Replace('/', ':');
	public static Type GetImplementedContract(Type actual, params Type[] expectedTypes)
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

	internal static IConfigurationResolver GetResolver(Type type)
	{
		var attribute = type.GetConfigurationAttribute();

		if(attribute != null && attribute.ResolverType != null)
			return Activator.CreateInstance(attribute.ResolverType) as IConfigurationResolver ?? ConfigurationResolver.Default;

		return ConfigurationResolver.Default;
	}

	internal static ConfigurationAttribute GetConfigurationAttribute(this Type type)
	{
		if(type == null || type == typeof(object))
			return null;

		var attribute = type.GetCustomAttribute<ConfigurationAttribute>(true);

		if(attribute != null)
			return attribute;

		foreach(var contract in type.GetTypeInfo().ImplementedInterfaces)
		{
			attribute = contract.GetCustomAttribute<ConfigurationAttribute>(true);

			if(attribute != null)
				return attribute;
		}

		return GetConfigurationAttribute(type.BaseType);
	}
}
