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

using Zongsoft.Configuration;

namespace Zongsoft.Externals.Etcd.Configuration;

public sealed class EtcdConnectionSettings : ConnectionSettingsBase<EtcdConnectionSettingsDriver>
{
	public EtcdConnectionSettings(EtcdConnectionSettingsDriver driver, string settings) : base(driver, settings) { }
	public EtcdConnectionSettings(EtcdConnectionSettingsDriver driver, string name, string settings) : base(driver, name, settings) { }

	[ConnectionSetting(true)]
	[DefaultValue("127.0.0.1")]
	public string Server
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[DefaultValue(2379)]
	public int Port
	{
		get => this.GetValue<int>();
		set => this.SetValue(value);
	}

	public string UserName
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	public string Password
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	public string Application
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[DefaultValue("10s")]
	[ConnectionSetting(typeof(Components.Converters.TimeSpanConverter.Milliseconds))]
	public TimeSpan Timeout
	{
		get => this.GetValue<TimeSpan>();
		set => this.SetValue(value);
	}

	[DefaultValue("15s")]
	public TimeSpan Heartbeat
	{
		get => this.GetValue<TimeSpan>();
		set => this.SetValue(value);
	}
}
