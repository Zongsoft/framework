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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Etcd library.
 *
 * The Zongsoft.Externals.Etcd is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Etcd is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Etcd library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using dotnet_etcd;
using dotnet_etcd.interfaces;
using dotnet_etcd.DependencyInjection;

using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Externals.Etcd;

public partial class EtcdService
{
	#region 成员字段
	private string _namespace;
	private IConnectionSettings _settings;
	private EtcdClientOptions _options;

	private EtcdClient _client;
	private readonly SemaphoreSlim _connectionLock = new(1, 1);
	#endregion

	#region 构造函数
	public EtcdService(string name)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));

		this.Name = name.Trim();
	}

	public EtcdService(string name, IConnectionSettings settings)
	{
		if(string.IsNullOrWhiteSpace(name))
		{
			if(settings == null || string.IsNullOrEmpty(settings.Name))
				throw new ArgumentNullException(nameof(name));

			name = settings.Name;
		}

		this.Name = name.Trim();
		_settings = settings;
	}

	public EtcdService(string name, string connectionString)
	{
		if(string.IsNullOrWhiteSpace(name))
			throw new ArgumentNullException(nameof(name));
		if(string.IsNullOrWhiteSpace(connectionString))
			throw new ArgumentNullException(nameof(connectionString));

		this.Name = name.Trim();
		_settings = Configuration.EtcdConnectionSettingsDriver.Instance.GetSettings(connectionString);
	}
	#endregion

	#region 公共属性
	public string Name { get; }
	public string Namespace
	{
		get => _namespace;
		set => _namespace = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
	}

	public IConnectionSettings Settings => _settings ??= ApplicationContext.Current?.Configuration.GetConnectionSettings("/Externals/Etcd/ConnectionSettings", this.Name, "etcd");
	#endregion

	public ValueTask HeartbeatAsync(CancellationToken cancellation = default)
	{
		return ValueTask.CompletedTask;
	}
}
