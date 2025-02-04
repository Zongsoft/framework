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
 * This file is part of Zongsoft.Messaging.Kafka library.
 *
 * The Zongsoft.Messaging.Kafka is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Messaging.Kafka is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Messaging.Kafka library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

using Confluent.Kafka;

using Zongsoft.Components;
using Zongsoft.Configuration;

namespace Zongsoft.Messaging.Kafka.Configuration;

public sealed class KafkaConnectionSettings : ConnectionSettingsBase<KafkaConnectionSettingsDriver, ClientConfig>, IMessageQueueSettings
{
	#region 构造函数
	public KafkaConnectionSettings(KafkaConnectionSettingsDriver driver, string settings) : base(driver, settings) { }
	public KafkaConnectionSettings(KafkaConnectionSettingsDriver driver, string name, string settings) : base(driver, name, settings) { }
	#endregion

	#region 公共属性
	[Alias(nameof(ConsumerConfig.GroupId))]
	public string Group
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Alias(nameof(ClientConfig.ClientId))]
	public string Client
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Alias(nameof(ClientConfig.BootstrapServers))]
	public string Server
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	public SecurityProtocol SecurityProtocol
	{
		get => this.GetValue<SecurityProtocol>();
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

	public TimeSpan Timeout
	{
		get => this.GetValue<TimeSpan>();
		set => this.SetValue(value);
	}
	#endregion

	#region 公共方法
	public ConsumerConfig GetConsumerOptions()
	{
		var options = new ConsumerConfig();
		base.Populate(options);
		return options;
	}

	public ProducerConfig GetProducerOptions()
	{
		var options = new ProducerConfig();
		base.Populate(options);
		return options;
	}
	#endregion
}
