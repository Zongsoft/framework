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

using Zongsoft.Components;
using Zongsoft.Configuration;

namespace Zongsoft.Data.Influx.Configuration;

public sealed class InfluxConnectionSettings : ConnectionSettingsBase<InfluxConnectionSettingsDriver, Common.InfluxConnectionStringBuilder>
{
	#region 构造函数
	public InfluxConnectionSettings(InfluxConnectionSettingsDriver driver, string settings) : base(driver, settings) { }
	public InfluxConnectionSettings(InfluxConnectionSettingsDriver driver, string name, string settings) : base(driver, name, settings) { }
	#endregion

	#region 公共属性
	[DefaultValue("15s")]
	[Alias(nameof(Common.InfluxConnectionStringBuilder.Timeout))]
	public TimeSpan Timeout
	{
		get => this.GetValue<TimeSpan>();
		set => this.SetValue(value);
	}

	[ConnectionSetting(true)]
	[Alias(nameof(Common.InfluxConnectionStringBuilder.Server))]
	public string Server
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[ConnectionSetting(true)]
	[Alias(nameof(Common.InfluxConnectionStringBuilder.Database))]
	public string Database
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Alias(nameof(Common.InfluxConnectionStringBuilder.Organization))]
	public string Organization
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Alias(nameof(Common.InfluxConnectionStringBuilder.Token))]
	public string Token
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[DefaultValue("ms")]
	[Alias(nameof(Common.InfluxConnectionStringBuilder.Precision))]
	public string Precision
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}
	#endregion

	#region 重写方法
	protected override Common.InfluxConnectionStringBuilder CreateOptions() => new(this.Value);
	#endregion
}