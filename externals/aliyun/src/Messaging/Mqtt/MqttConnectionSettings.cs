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
using System.Collections.Generic;

namespace Zongsoft.Externals.Aliyun.Messaging.Mqtt;

public class MqttConnectionSettings : Zongsoft.Configuration.ConnectionSettingsBase<MqttConnectionSettingsDriver>, Zongsoft.Messaging.IMessageQueueSettings
{
	#region 构造函数
	public MqttConnectionSettings(string value = null) : base(MqttConnectionSettingsDriver.Instance, value) { }
	public MqttConnectionSettings(string name, string value) : base(MqttConnectionSettingsDriver.Instance, name, value) { }
	#endregion

	#region 公共属性
	public string Group
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	public string Client
	{
		get => GetClient(this.Entries);
		set => this.SetValue(value);
	}

	public string Server
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	public ushort Port
	{
		get => this.GetValue<ushort>();
		set => this.SetValue(value);
	}

	public TimeSpan Timeout
	{
		get => this.GetValue<TimeSpan>();
		set => this.SetValue(value);
	}

	public string Charset
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	public string Encoding
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	public string Provider
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	public string Database
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	public string UserName
	{
		get => GetUserName(this.Entries);
		set => this.SetValue(value);
	}

	public string Password
	{
		get => GetPassword(this.Entries);
		set => this.SetValue(value);
	}

	public string Instance
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	public string Application
	{
		get => this.GetValue<string>() ?? Services.ApplicationContext.Current?.Name;
		set => this.SetValue(value);
	}
	#endregion

	#region 私有方法
	private static string GetClient(IDictionary<string, string> values)
	{
		if(values.TryGetValue(nameof(Client), out var client) && !string.IsNullOrWhiteSpace(client))
			return client;

		if(values.TryGetValue(nameof(Group), out var group) && !string.IsNullOrWhiteSpace(group))
			return group + "@@@" + Common.Randomizer.GenerateString();

		return null;
	}

	private static string GetUserName(IDictionary<string, string> values) =>
		values.TryGetValue(nameof(UserName), out var identity) &&
		values.TryGetValue(nameof(Instance), out var instance) ?
		$"Signature|{identity}|{instance}" : null;

	private static string GetPassword(IDictionary<string, string> values)
	{
		if(!values.TryGetValue(nameof(Password), out var password) || string.IsNullOrEmpty(password))
			return null;

		var client = GetClient(values);
		if(string.IsNullOrEmpty(client))
			return null;

		using(var encipher = new System.Security.Cryptography.HMACSHA1(System.Text.Encoding.UTF8.GetBytes(password)))
		{
			var data = System.Text.Encoding.UTF8.GetBytes(client);
			return Convert.ToBase64String(encipher.ComputeHash(data));
		}
	}
	#endregion
}
