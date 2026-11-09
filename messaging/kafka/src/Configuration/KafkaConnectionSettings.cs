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
	[ConnectionSetting(Ignored = true)]
	public string Topic
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Alias(nameof(ConsumerConfig.GroupId))]
	public string Group
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Alias(nameof(ConsumerConfig.GroupProtocol))]
	public GroupProtocol GroupProtocol
	{
		get => this.GetValue<GroupProtocol>();
		set => this.SetValue(value);
	}

	[Alias(nameof(ClientConfig.ClientId))]
	public string Client
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Alias(nameof(ClientConfig.BootstrapServers))]
	[ConnectionSetting(true)]
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

	public int CompressionLevel
	{
		get => this.GetValue<int>();
		set => this.SetValue(value);
	}

	public CompressionType CompressionType
	{
		get => this.GetValue<CompressionType>();
		set => this.SetValue(value);
	}

	public IsolationLevel IsolationLevel
	{
		get => this.GetValue<IsolationLevel>();
		set => this.SetValue(value);
	}

	[DefaultValue("10s")]
	[Alias(nameof(ConsumerConfig.HeartbeatIntervalMs))]
	public TimeSpan Heartbeat
	{
		get => this.GetValue<TimeSpan>();
		set => this.SetValue(value);
	}

	[DefaultValue("10s")]
	[Alias(nameof(ClientConfig.SocketTimeoutMs))]
	[Alias(nameof(ClientConfig.SocketConnectionSetupTimeoutMs))]
	[Alias(nameof(ConsumerConfig.SessionTimeoutMs))]
	[Alias(nameof(ProducerConfig.RequestTimeoutMs))]
	[Alias(nameof(ProducerConfig.MessageTimeoutMs))]
	[ConnectionSetting(typeof(Components.Converters.TimeSpanConverter.Milliseconds))]
	public TimeSpan Timeout
	{
		get => this.GetValue<TimeSpan>();
		set => this.SetValue(value);
	}

	[Alias(nameof(ProducerConfig.TransactionalId))]
	public string TransactionId
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[DefaultValue("60s")]
	[Alias(nameof(ProducerConfig.TransactionTimeoutMs))]
	[ConnectionSetting(typeof(Components.Converters.TimeSpanConverter.Milliseconds))]
	public TimeSpan TransactionTimeout
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
		options.SaslUsername = this.UserName;
		options.SaslPassword = this.Password;

		if(string.IsNullOrEmpty(options.ClientId))
			options.ClientId = $"C{Common.Randomizer.GenerateString()}";

		//手动设置消费者配置属性
		options.GroupId = string.IsNullOrEmpty(this.Group) ? $"G{Common.Randomizer.GenerateString()}" : this.Group;
		options.GroupProtocol = this.GroupProtocol;
		options.IsolationLevel = this.IsolationLevel;

		if(this.Heartbeat > TimeSpan.Zero)
			options.HeartbeatIntervalMs = (int)this.Heartbeat.TotalMilliseconds;
		if(this.Timeout > TimeSpan.Zero)
			options.SessionTimeoutMs = (int)this.Timeout.TotalMilliseconds;

		return options;
	}

	public ProducerConfig GetProducerOptions()
	{
		var options = new ProducerConfig();
		base.Populate(options);
		options.SaslUsername = this.UserName;
		options.SaslPassword = this.Password;

		if(string.IsNullOrEmpty(options.ClientId))
			options.ClientId = $"C{Common.Randomizer.GenerateString()}";

		//手动设置生产者配置属性
		options.CompressionType = this.CompressionType;
		options.CompressionLevel = this.CompressionLevel;
		options.TransactionalId = this.TransactionId;

		if(this.TransactionTimeout > TimeSpan.Zero)
			options.TransactionTimeoutMs = (int)this.TransactionTimeout.TotalMilliseconds;

		if(this.Timeout > TimeSpan.Zero)
		{
			options.RequestTimeoutMs = (int)this.Timeout.TotalMilliseconds;
			options.MessageTimeoutMs = (int)this.Timeout.TotalMilliseconds;
		}

		return options;
	}
	#endregion
}
