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
	private sealed class TDengineMapper(TDengineConnectionSettingsDriver driver) : ConnectionSettingsMapper(driver) { }
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
	public readonly static ConnectionSettingDescriptor<string> Server = new(nameof(Server), nameof(TDengineConnectionStringBuilder.Host), true);
	public readonly static ConnectionSettingDescriptor<int> Port = new(nameof(Port), nameof(TDengineConnectionStringBuilder.Port), 6030);
	public readonly static ConnectionSettingDescriptor<string> Database = new(nameof(Database), "DB", true);
	public readonly static ConnectionSettingDescriptor<TDengineConnectionProtocol> Protocol = new(nameof(Protocol), nameof(TDengineConnectionStringBuilder.Protocol), TDengineConnectionProtocol.Native);
	public readonly static ConnectionSettingDescriptor<string> Timezone = new(nameof(Timezone));
	public readonly static ConnectionSettingDescriptor<string> Token = new(nameof(Token));
	public readonly static ConnectionSettingDescriptor<bool> Reconnectable = new(nameof(Reconnectable), nameof(TDengineConnectionStringBuilder.AutoReconnect), false, true);
	public readonly static ConnectionSettingDescriptor<bool> Compressible = new(nameof(Compressible), nameof(TDengineConnectionStringBuilder.EnableCompression), false, true);

	public TDengineConnectionSettingDescriptorCollection()
	{
		this.Add(Server);
		this.Add(Port);
		this.Add(Token);
		this.Add(Database);
		this.Add(Protocol);
		this.Add(Timezone);
		this.Add(Compressible);
		this.Add(Reconnectable);
		this.Add(ConnectionSettingDescriptor.UserName);
		this.Add(ConnectionSettingDescriptor.Password);
		this.Add(ConnectionSettingDescriptor.Timeout);
	}
}
