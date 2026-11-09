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

	[DefaultValue(10000)]
	[ConnectionSetting(Ignored = true)]
	public int Deadline
	{
		get => this.GetValue<int>();
		set => this.SetValue(value);
	}

	[DefaultValue("30s")]
	[ConnectionSetting(Ignored = true)]
	public TimeSpan IdleTimeout
	{
		get => this.GetValue<TimeSpan>();
		set => this.SetValue(value);
	}

	/// <summary>获取或设置消息流保留的最大消息数，零表示使用默认值，负数表示不限制。</summary>
	[DefaultValue(100000)]
	[ConnectionSetting(Ignored = true)]
	public int MaximumLength
	{
		get => this.GetValue<int>();
		set => this.SetValue(value);
	}

	/// <summary>获取或设置是否使用近似裁剪消息流。</summary>
	[DefaultValue(true)]
	[ConnectionSetting(Ignored = true)]
	public bool UseApproximateMaximumLength
	{
		get => this.GetValue<bool>();
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
