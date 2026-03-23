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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Velopack library.
 *
 * The Zongsoft.Externals.Velopack is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Velopack is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Velopack library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.ComponentModel;

using Zongsoft.Components;
using Zongsoft.Configuration;

namespace Zongsoft.Externals.Velopack.Configuration;

public class VelopackConnectionSettings : ConnectionSettingsBase<VelopackConnectionSettingsDriver>
{
	#region 构造函数
	public VelopackConnectionSettings(VelopackConnectionSettingsDriver driver, string settings) : base(driver, settings) { }
	public VelopackConnectionSettings(VelopackConnectionSettingsDriver driver, string name, string settings) : base(driver, name, settings) { }
	#endregion

	#region 公共属性
	[ConnectionSetting(true)]
	public string Url
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	public string Source
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	public string Locator
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

	[Alias("Interval")]
	[DefaultValue("60s")]
	public TimeSpan Period
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
	#endregion
}
