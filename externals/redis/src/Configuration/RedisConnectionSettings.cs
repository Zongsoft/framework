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
 * This file is part of Zongsoft.Externals.Redis library.
 *
 * The Zongsoft.Externals.Redis is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Redis is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Redis library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.ComponentModel;

using StackExchange.Redis;

using Zongsoft.Components;
using Zongsoft.Configuration;

namespace Zongsoft.Externals.Redis.Configuration;

public sealed class RedisConnectionSettings : ConnectionSettingsBase<RedisConnectionSettingsDriver, ConfigurationOptions>, Zongsoft.Messaging.IMessageQueueSettings
{
	#region 构造函数
	public RedisConnectionSettings(RedisConnectionSettingsDriver driver, string settings) : base(driver, settings) { }
	public RedisConnectionSettings(RedisConnectionSettingsDriver driver, string name, string settings) : base(driver, name, settings) { }
	#endregion

	#region 公共属性
	[ConnectionSetting(Ignored = true)]
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

	[ConnectionSetting(true)]
	[Alias(nameof(ConfigurationOptions.EndPoints))]
	public EndPointCollection Server
	{
		get => this.GetValue<EndPointCollection>();
		set => this.SetValue(value);
	}

	[Alias(nameof(ConfigurationOptions.ClientName))]
	public string Client
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Alias(nameof(ConfigurationOptions.User))]
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

	[Alias(nameof(ConfigurationOptions.DefaultDatabase))]
	public int Database
	{
		get => this.GetValue<int>();
		set => this.SetValue(value);
	}

	[DefaultValue("10s")]
	[Alias(nameof(ConfigurationOptions.SyncTimeout))]
	[Alias(nameof(ConfigurationOptions.AsyncTimeout))]
	[Alias(nameof(ConfigurationOptions.ConnectTimeout))]
	[ConnectionSetting(typeof(Zongsoft.Components.Converters.TimeSpanConverter.Milliseconds))]
	public TimeSpan Timeout
	{
		get => this.GetValue<TimeSpan>();
		set => this.SetValue(value);
	}

	[Alias(nameof(ConfigurationOptions.ConnectRetry))]
	public int RetryCount
	{
		get => this.GetValue<int>();
		set => this.SetValue(value);
	}

	[DefaultValue(-1)]
	public int KeepAlive
	{
		get => this.GetValue<int>();
		set => this.SetValue(value);
	}

	[DefaultValue("15s")]
	[Alias(nameof(ConfigurationOptions.HeartbeatInterval))]
	public TimeSpan Heartbeat
	{
		get => this.GetValue<TimeSpan>();
		set => this.SetValue(value);
	}
	#endregion

	#region 显式实现
	string Zongsoft.Messaging.IMessageQueueSettings.Server
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}
	#endregion
}
