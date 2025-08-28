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
using System.Net;
using System.ComponentModel;
using System.Collections.Generic;

using Zongsoft.Components;
using Zongsoft.Configuration;

namespace Zongsoft.Externals.Etcd.Configuration;

internal class EtcdConnectionSettings : ConnectionSettingsBase<EtcdConnectionSettingsDriver>
{
	#region 构造函数
	public EtcdConnectionSettings(EtcdConnectionSettingsDriver driver, string settings) : base(driver, settings) { }
	public EtcdConnectionSettings(EtcdConnectionSettingsDriver driver, string name, string settings) : base(driver, name, settings) { }
	#endregion

	#region 公共属性
	public ICollection<EndPoint> Servers { get; set; }

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
	#endregion
}
