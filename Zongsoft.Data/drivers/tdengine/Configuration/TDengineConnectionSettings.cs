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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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

public sealed class TDengineConnectionSettings : ConnectionSettingsBase<TDengineConnectionSettingsDriver, TDengineConnectionStringBuilder>
{
	#region 构造函数
	public TDengineConnectionSettings(TDengineConnectionSettingsDriver driver, string settings) : base(driver, settings) { }
	public TDengineConnectionSettings(TDengineConnectionSettingsDriver driver, string name, string settings) : base(driver, name, settings) { }
	#endregion

	#region 公共属性
	public ushort Port
	{
		get => this.GetValue<ushort>();
		set => this.SetValue(value);
	}

	[DefaultValue("10s")]
	[Alias(nameof(TDengineConnectionStringBuilder.ConnTimeout))]
	[Alias(nameof(TDengineConnectionStringBuilder.ReadTimeout))]
	[Alias(nameof(TDengineConnectionStringBuilder.WriteTimeout))]
	public TimeSpan Timeout
	{
		get => this.GetValue<TimeSpan>();
		set => this.SetValue(value);
	}

	[ConnectionSetting(true)]
	[Alias(nameof(TDengineConnectionStringBuilder.Host))]
	public string Server
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[ConnectionSetting(true)]
	public string Database
	{
		get => this.GetValue<string>();
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

	[DefaultValue(TDengineConnectionProtocol.Native)]
	[ConnectionSetting(Ignored = true)]
	public TDengineConnectionProtocol Protocol
	{
		get => this.GetValue<TDengineConnectionProtocol>();
		set => this.SetValue(value);
	}

	[DefaultValue(false)]
	[Alias(nameof(TDengineConnectionStringBuilder.UseSSL))]
	public bool Secured
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	[DefaultValue(true)]
	[Alias(nameof(TDengineConnectionStringBuilder.AutoReconnect))]
	public bool Reconnectable
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	[DefaultValue(true)]
	[Alias(nameof(TDengineConnectionStringBuilder.EnableCompression))]
	public bool Compressible
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}
	#endregion

	#region 重写方法
	protected override TDengineConnectionStringBuilder CreateOptions() => new(string.Empty);
	protected override void Populate(TDengineConnectionStringBuilder options)
	{
		base.Populate(options);
		options.Protocol = this.Protocol.ToString();
	}
	#endregion
}