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
 * This file is part of Zongsoft.Externals.Aliyun library.
 *
 * The Zongsoft.Externals.Aliyun is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Aliyun is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Aliyun library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

namespace Zongsoft.Externals.Aliyun.Messaging.Mqtt;

public class MqttConnectionSettings : Zongsoft.Configuration.ConnectionSettingsBase<MqttConnectionSettingsDriver>
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
