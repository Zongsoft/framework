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

namespace Zongsoft.Externals.Opc.Configuration;

public class OpcConnectionSettings : ConnectionSettingsBase<OpcConnectionSettingsDriver>
{
	#region 构造函数
	public OpcConnectionSettings(OpcConnectionSettingsDriver driver, string settings) : base(driver, settings) { }
	public OpcConnectionSettings(OpcConnectionSettingsDriver driver, string name, string settings) : base(driver, name, settings) { }
	#endregion

	#region 公共属性
	[Alias("Url")]
	[ConnectionSetting(true)]
	public string Server
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	public string Client
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	public string Instance
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[DefaultValue("30s")]
	public TimeSpan Timeout
	{
		get => this.GetValue<TimeSpan>();
		set => this.SetValue(value);
	}

	[DefaultValue("10s")]
	public TimeSpan Heartbeat
	{
		get => this.GetValue<TimeSpan>();
		set => this.SetValue(value);
	}

	[Alias("User")]
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

	[Category("Security")]
	public string Certificate
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Category("Security")]
	public string CertificateSecret
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Category("Security")]
	public OpcSecurityMode SecurityMode
	{
		get => this.GetValue<OpcSecurityMode>();
		set => this.SetValue(value);
	}

	[Category("Security")]
	public string SecurityPolicy
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	public string Locales
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}
	#endregion
}
