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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Data.TDengine library.
 *
 * The Zongsoft.Data.TDengine is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data.TDengine is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data.TDengine library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

using TDengine.Data;
using TDengine.Data.Client;

using Zongsoft.Configuration;

namespace Zongsoft.Data.TDengine.Configuration;

public sealed class TDengineConnectionSettingsDriver : ConnectionSettingsDriver<TDengineConnectionSettingDescriptorCollection>
{
	#region 常量定义
	internal const string NAME = "TDengine";
	#endregion

	#region 单例字段
	public static readonly TDengineConnectionSettingsDriver Instance = new();
	#endregion

	#region 私有构造
	private TDengineConnectionSettingsDriver() : base(NAME)
	{
		this.Mapper = new TDengineMapper(this);
		this.Modeler = new TDengineModeler(this);
	}
	#endregion

	#region 嵌套子类
	private sealed class TDengineMapper(TDengineConnectionSettingsDriver driver) : ConnectionSettingsMapper(driver)
	{
		protected override bool OnMap(string name, IDictionary<string, string> values, out object value)
		{
			return base.OnMap(name, values, out value);
		}
	}

	private sealed class TDengineModeler(TDengineConnectionSettingsDriver driver) : ConnectionSettingsModeler<TDengineConnectionStringBuilder>(driver)
	{
		protected override TDengineConnectionStringBuilder CreateModel(IConnectionSettings settings) => new(settings.Value)
		{
			Host = settings.Server,
			Database = settings.Database,
		};
	}
	#endregion
}

public sealed class TDengineConnectionSettingDescriptorCollection : ConnectionSettingDescriptorCollection
{
	public readonly static ConnectionSettingDescriptor<string> Server = new(nameof(Server), nameof(TDengineConnectionStringBuilder.Host), null, ConnectionSettingDescriptor.Server.Label, ConnectionSettingDescriptor.Server.Description);
	public readonly static ConnectionSettingDescriptor<int> Port = new(nameof(Port), nameof(TDengineConnectionStringBuilder.Port), 6030, ConnectionSettingDescriptor.Port.Label, ConnectionSettingDescriptor.Port.Description);
	public readonly static ConnectionSettingDescriptor<string> Database = new(nameof(Database), "DB", null, ConnectionSettingDescriptor.Database.Label, ConnectionSettingDescriptor.Database.Description);
	public readonly static ConnectionSettingDescriptor<TDengineConnectionProtocol> Protocol = new(nameof(Protocol), nameof(TDengineConnectionStringBuilder.Protocol), TDengineConnectionProtocol.Native);
	public readonly static ConnectionSettingDescriptor<string> Timezone = new(nameof(Timezone), nameof(TDengineConnectionStringBuilder.Timezone), null, null, null);
	public readonly static ConnectionSettingDescriptor<string> Token = new(nameof(Token), nameof(TDengineConnectionStringBuilder.Token), null, null, null);
	public readonly static ConnectionSettingDescriptor<bool> AutoReconnect = new(nameof(AutoReconnect), nameof(TDengineConnectionStringBuilder.AutoReconnect), true, null, null);
	public readonly static ConnectionSettingDescriptor<bool> EnableCompression = new(nameof(EnableCompression), nameof(TDengineConnectionStringBuilder.EnableCompression), true, null, null);

	public TDengineConnectionSettingDescriptorCollection()
	{
		this.Add(Server);
		this.Add(Port);
		this.Add(Token);
		this.Add(Database);
		this.Add(Protocol);
		this.Add(Timezone);
		this.Add(AutoReconnect);
		this.Add(EnableCompression);
		this.Add(ConnectionSettingDescriptor.UserName);
		this.Add(ConnectionSettingDescriptor.Password);
		this.Add(ConnectionSettingDescriptor.Timeout);
	}
}
