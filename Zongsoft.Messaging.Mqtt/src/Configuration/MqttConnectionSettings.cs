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
 * This file is part of Zongsoft.Messaging.Mqtt library.
 *
 * The Zongsoft.Messaging.Mqtt is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Messaging.Mqtt is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Messaging.Mqtt library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Text;
using System.ComponentModel;

using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Formatter;

using Zongsoft.Components;
using Zongsoft.Configuration;

namespace Zongsoft.Messaging.Mqtt.Configuration;

public sealed class MqttConnectionSettings : ConnectionSettingsBase<MqttConnectionSettingsDriver, MqttClientOptions>, IMessageQueueSettings
{
	#region 构造函数
	public MqttConnectionSettings(MqttConnectionSettingsDriver driver, string settings) : base(driver, settings) { }
	public MqttConnectionSettings(MqttConnectionSettingsDriver driver, string name, string settings) : base(driver, name, settings) { }
	#endregion

	#region 公共属性
	public string Group
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Alias(nameof(MqttClientOptions.ClientId))]
	public string Client
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	public string Server
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

	[DefaultValue("0:01:30")]
	public TimeSpan Timeout
	{
		get => this.GetValue<TimeSpan>();
		set => this.SetValue(value);
	}

	[DefaultValue("0:0:30")]
	[Alias(nameof(MqttClientOptions.KeepAlivePeriod))]
	public TimeSpan KeepAlive
	{
		get => this.GetValue<TimeSpan>();
		set => this.SetValue(value);
	}

	[Alias(nameof(MqttClientOptions.SessionExpiryInterval))]
	public uint Expiration
	{
		get => this.GetValue<uint>();
		set => this.SetValue(value);
	}

	[DefaultValue(false)]
	public bool CleanSession
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}

	[DefaultValue(MqttProtocolVersion.V500)]
	public MqttProtocolVersion ProtocolVersion
	{
		get => this.GetValue<MqttProtocolVersion>();
		set => this.SetValue(value);
	}
	#endregion

	#region 重写方法
	protected override void Populate(MqttClientOptions options)
	{
		base.Populate(options);

		//确保生成的 ClientId 不为空
		if(string.IsNullOrEmpty(options.ClientId))
			options.ClientId = 'C' + Common.Randomizer.GenerateString();

		//设置安全凭证
		options.Credentials = new MqttClientCredentials(this.UserName, string.IsNullOrEmpty(this.Password) ? null : Encoding.UTF8.GetBytes(this.Password));

		//设置服务器地址
		options.ChannelOptions = new MqttClientTcpOptions
		{
			RemoteEndpoint = Common.Convert.ConvertValue<System.Net.EndPoint>(this.Server)
		};
	}
	#endregion
}
