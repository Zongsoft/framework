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
using System.ComponentModel;

using TDengine.Data;
using TDengine.Data.Client;

using Zongsoft.Components;
using Zongsoft.Configuration;

namespace Zongsoft.Data.TDengine.Configuration;

public sealed class TDengineConnectionSettingsDriver : ConnectionSettingsDriver<TDengineConnectionSettings>
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
		this.Populator = new TDenginePopulator(this);
	}
	#endregion

	#region 嵌套子类
	private sealed class TDengineMapper(TDengineConnectionSettingsDriver driver) : MapperBase(driver) { }
	private sealed class TDenginePopulator(TDengineConnectionSettingsDriver driver) : PopulatorBase(driver) { }
	#endregion
}

public sealed class TDengineConnectionSettings : ConnectionSettingsBase<TDengineConnectionSettingsDriver>
{
	#region 构造函数
	public TDengineConnectionSettings(TDengineConnectionSettingsDriver driver, string settings) : base(driver, settings) { }
	public TDengineConnectionSettings(TDengineConnectionSettingsDriver driver, string name, string settings) : base(driver, name, settings) { }
	#endregion

	#region 公共属性
	public ushort Port { get; set; }

	[Alias(nameof(TDengineConnectionStringBuilder.ConnTimeout))]
	[Alias(nameof(TDengineConnectionStringBuilder.ReadTimeout))]
	[Alias(nameof(TDengineConnectionStringBuilder.WriteTimeout))]
	public TimeSpan Timeout { get; set; }
	public string Timezone { get; set; }
	[ConnectionSetting(true)]
	public string Server { get; set; }
	[Alias("DB")]
	public string Database { get; set; }
	public string UserName { get; set; }
	public string Password { get; set; }
	[DefaultValue(TDengineConnectionProtocol.Native)]
	public TDengineConnectionProtocol Protocol { get; set; }

	[Alias(nameof(TDengineConnectionStringBuilder.UseSSL))]
	public bool Secured { get; set; }
	[Alias(nameof(TDengineConnectionStringBuilder.AutoReconnect))]
	public bool Reconnectable { get; set; }
	[Alias(nameof(TDengineConnectionStringBuilder.EnableCompression))]
	public bool Compressible { get; set; }
	#endregion

	#region 公共方法
	public TDengineConnectionStringBuilder GetOptions()
	{
		var options = new TDengineConnectionStringBuilder(string.Empty)
		{
			Host = this.Server,
			Database = this.Database,
			Username = this.UserName,
			Password = this.Password,
			Protocol = this.Protocol.ToString(),
		};

		if(this.Port > 0)
			options.Port = this.Port;

		if(!string.IsNullOrEmpty(this.Timezone))
			options.Timezone = TimeZoneInfo.FromSerializedString(this.Timezone);

		return options;
	}
	#endregion
}
