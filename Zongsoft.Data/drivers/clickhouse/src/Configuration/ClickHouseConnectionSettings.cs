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
using System.ComponentModel;

using ClickHouse.Driver;
using ClickHouse.Driver.ADO;

using Zongsoft.Components;
using Zongsoft.Configuration;

namespace Zongsoft.Data.ClickHouse.Configuration;

public sealed class ClickHouseConnectionSettings : ConnectionSettingsBase<ClickHouseConnectionSettingsDriver, ClickHouseConnectionStringBuilder>
{
	#region 构造函数
	public ClickHouseConnectionSettings(ClickHouseConnectionSettingsDriver driver, string settings) : base(driver, settings) { }
	public ClickHouseConnectionSettings(ClickHouseConnectionSettingsDriver driver, string name, string settings) : base(driver, name, settings) { }
	#endregion

	#region 公共属性
	[Category("Connection")]
	[ConnectionSetting(true)]
	[Alias(nameof(ClickHouseConnectionStringBuilder.Host))]
	public string Server
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[DefaultValue(8123)]
	[Category("Connection")]
	[Alias(nameof(ClickHouseConnectionStringBuilder.Port))]
	public ushort Port
	{
		get => this.GetValue<ushort>();
		set => this.SetValue(value);
	}

	[Category("Connection")]
	[Alias(nameof(ClickHouseConnectionStringBuilder.Path))]
	public string Path
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Category("Connection")]
	[DefaultValue("http")]
	[Alias(nameof(ClickHouseConnectionStringBuilder.Protocol))]
	public string Protocol
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Category("Connection")]
	[ConnectionSetting(true)]
	[Alias(nameof(ClickHouseConnectionStringBuilder.Database))]
	public string Database
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Category("Connection")]
	[Alias(nameof(ClickHouseConnectionStringBuilder.Username))]
	public string UserName
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Category("Connection")]
	[Alias(nameof(ClickHouseConnectionStringBuilder.Password))]
	public string Password
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Category("Other")]
	[DefaultValue(true)]
	[Alias(nameof(ClickHouseConnectionStringBuilder.Compression))]
	public bool Compression
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	[Category("Other")]
	[DefaultValue(false)]
	[Alias(nameof(ClickHouseConnectionStringBuilder.UseSession))]
	public bool UseSession
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	[Category("Other")]
	[Alias(nameof(ClickHouseConnectionStringBuilder.SessionId))]
	public string SessionId
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[DefaultValue("30s")]
	[Category("Connection")]
	[Alias(nameof(ClickHouseConnectionStringBuilder.Timeout))]
	[ConnectionSetting(typeof(Components.Converters.TimeSpanConverter.Seconds))]
	public TimeSpan Timeout
	{
		get => this.GetValue<TimeSpan>();
		set => this.SetValue(value);
	}

	[Category("Other")]
	[DefaultValue(true)]
	[Alias(nameof(ClickHouseConnectionStringBuilder.UseCustomDecimals))]
	public bool UseCustomDecimals
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}
	#endregion
}
