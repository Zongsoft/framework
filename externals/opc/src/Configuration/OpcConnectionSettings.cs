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
 * This file is part of Zongsoft.Externals.Opc library.
 *
 * The Zongsoft.Externals.Opc is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Opc is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Opc library. If not, see <http://www.gnu.org/licenses/>.
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
	[Alias("Server")]
	[ConnectionSetting(true)]
	public string Url
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	public string Topic
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	public string Group
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

	public string Filter
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

	public string Locales
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}
	#endregion
}
