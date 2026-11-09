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

using Zongsoft.Common;
using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Externals.Etcd;

[Service(
	typeof(IServiceProvider<ISequence>),
	typeof(IServiceProvider<ISequenceBase>),
	typeof(IServiceProvider<Services.Distributing.IDistributedLockManager>))]
public class EtcdServiceProvider :
	IServiceProvider<ISequence>,
	IServiceProvider<ISequenceBase>,
	IServiceProvider<Services.Distributing.IDistributedLockManager>
{
	#region 静态字段
	private static readonly ConcurrentDictionary<string, EtcdService> _services = new(StringComparer.OrdinalIgnoreCase);
	#endregion

	#region 公共方法
	public static EtcdService GetEtcd(string name)
	{
		if(ApplicationContext.Current?.Configuration == null)
			return null;

		return _services.GetOrAdd(name ?? string.Empty, key => new EtcdService(key, GetConnectionSettings(key)));

		static IConnectionSettings GetConnectionSettings(string name)
		{
			var settings = ApplicationContext.Current.Configuration.GetOption<ConnectionSettingsCollection>("/Externals/Etcd/ConnectionSettings");
			if(settings == null || settings.Count == 0)
				throw new ConfigurationException(Properties.Resources.Etcd_SettingsRequired_Message);

			if(!string.IsNullOrEmpty(name) && settings.TryGetValue(name, Configuration.EtcdConnectionSettingsDriver.NAME, out var setting))
				return setting;

			setting = settings.GetDefault();
			if(setting == null)
				throw new ConfigurationException(string.IsNullOrEmpty(name) ?
					$"Missing the default etcd connection setting." :
					$"The specified '{name}' etcd connection setting does not exist and the default etcd connection setting is not defined.");

			return setting;
		}
	}
	#endregion

	#region 显式实现
	ISequence IServiceProvider<ISequence>.GetService(string name) => GetEtcd(name);
	ISequenceBase IServiceProvider<ISequenceBase>.GetService(string name) => GetEtcd(name);
	Services.Distributing.IDistributedLockManager IServiceProvider<Services.Distributing.IDistributedLockManager>.GetService(string name) => GetEtcd(name);
	#endregion
}
