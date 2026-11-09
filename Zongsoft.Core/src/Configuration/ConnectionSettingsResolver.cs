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
using System.Collections.Concurrent;

using Microsoft.Extensions.Configuration;

namespace Zongsoft.Configuration;

internal class ConnectionSettingsResolver : IConfigurationResolver
{
	#region 静态字段
	private static readonly ConcurrentDictionary<Type, Delegate> _creators = new();
	#endregion

	#region 公共方法
	public void Attach(object target, IConfiguration configuration, ConfigurationBinderOptions options) => ConfigurationResolver.Default.Attach(target, configuration, options);
	public void Resolve(object target, IConfiguration configuration, ConfigurationBinderOptions options) => ConfigurationResolver.Default.Resolve(target, configuration, options);
	public object Resolve(Type type, IConfiguration configuration, ConfigurationBinderOptions options)
	{
		IConnectionSettings result = null;
		var driverName = configuration.GetSection(nameof(IConnectionSettings.Driver))?.Value;

		if(!string.IsNullOrEmpty(driverName) && ConnectionSettings.Drivers.TryGetValue(driverName, out var driver))
			result = Create(driver, configuration.GetSection(nameof(ConnectionSettings.Name))?.Value, configuration.GetSection(nameof(ConnectionSettings.Value))?.Value);

		result ??= new ConnectionSettings(configuration.GetSection(nameof(ConnectionSettings.Name)).Value, null);
		this.Resolve(result, configuration, options);
		return result;
	}
	#endregion

	#region 私有方法
	private static IConnectionSettings Create(IConnectionSettingsDriver driver, string name, string connectionString)
	{
		if(driver == null)
			return null;

		var creator = _creators.GetOrAdd(driver.GetType(), (key, driver) => GetCreator(driver), driver);
		return (IConnectionSettings)creator?.DynamicInvoke(name, connectionString);

		static Delegate GetCreator(IConnectionSettingsDriver driver)
		{
			if(driver == null)
				return null;

			var contracts = driver.GetType().GetInterfaces();

			for(int i = 0; i < contracts.Length; i++)
			{
				if(contracts[i].IsGenericType && contracts[i].GetGenericTypeDefinition() == typeof(IConnectionSettingsDriver<>))
				{
					var mapping = driver.GetType().GetInterfaceMap(contracts[i]);

					if(mapping.TargetType == null)
						continue;

					for(int j = 0; j < mapping.TargetMethods.Length; j++)
					{
						if(mapping.TargetMethods[j].Name == nameof(IConnectionSettingsDriver<IConnectionSettings>.GetSettings) &&
						   mapping.TargetMethods[j].ReturnType == contracts[i].GenericTypeArguments[0] &&
						   mapping.TargetMethods[j].GetParameters().Length == 2)
						{
							var invokerType = typeof(Func<,,>).MakeGenericType(typeof(string), typeof(string), mapping.TargetMethods[j].ReturnType);
							return mapping.TargetMethods[j].CreateDelegate(invokerType, driver);
						}
					}
				}
			}

			return null;
		}
	}
	#endregion
}
