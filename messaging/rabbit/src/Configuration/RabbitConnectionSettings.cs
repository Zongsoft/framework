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

using RabbitMQ.Client;

using Zongsoft.Components;
using Zongsoft.Configuration;

namespace Zongsoft.Messaging.RabbitMQ.Configuration;

public sealed class RabbitConnectionSettings : ConnectionSettingsBase<RabbitConnectionSettingsDriver, ConnectionFactory>, IMessageQueueSettings
{
	#region 构造函数
	public RabbitConnectionSettings(RabbitConnectionSettingsDriver driver, string settings) : base(driver, settings) { }
	public RabbitConnectionSettings(RabbitConnectionSettingsDriver driver, string name, string settings) : base(driver, name, settings) { }
	#endregion

	#region 公共属性
	public string Queue
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

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

	public string Client
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[DefaultValue("/")]
	[Alias(nameof(ConnectionFactory.VirtualHost))]
	public string Container
	{
		get => this.GetValue<string>();
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

	[DefaultValue("60s")]
	[Alias(nameof(ConnectionFactory.SocketReadTimeout))]
	[Alias(nameof(ConnectionFactory.SocketWriteTimeout))]
	[Alias(nameof(ConnectionFactory.ContinuationTimeout))]
	[Alias(nameof(ConnectionFactory.RequestedConnectionTimeout))]
	[Alias(nameof(ConnectionFactory.HandshakeContinuationTimeout))]
	public TimeSpan Timeout
	{
		get => this.GetValue<TimeSpan>();
		set => this.SetValue(value);
	}

	[DefaultValue("30s")]
	[Alias(nameof(ConnectionFactory.RequestedHeartbeat))]
	public TimeSpan Heartbeat
	{
		get => this.GetValue<TimeSpan>();
		set => this.SetValue(value);
	}

	[DefaultValue(1)]
	[Alias(nameof(ConnectionFactory.ConsumerDispatchConcurrency))]
	public int Concurrency
	{
		get => this.GetValue<int>();
		set => this.SetValue(value);
	}

	[Alias($"{nameof(ConnectionFactory.Ssl)}.{nameof(ConnectionFactory.Ssl.CertPath)}")]
	public string Certificate
	{
		get => this.GetValue<string>();
		set => this.SetValue(value);
	}

	[Alias(nameof(ConnectionFactory.AutomaticRecoveryEnabled))]
	public bool Reconnectable
	{
		get => this.GetValue<bool>();
		set => this.SetValue(value);
	}
	#endregion

	#region 重写方法
	protected override void Populate(ConnectionFactory options)
	{
		base.Populate(options);

		options.ClientProvidedName = string.IsNullOrEmpty(this.Client) ?
			$"C{Common.Randomizer.GenerateString()}" :
			this.Client;

		if(!string.IsNullOrEmpty(this.Certificate))
			options.Ssl.CertPath = this.Certificate;

		options.Port = this.Port == 0 ? -1 : this.Port;

		if(this.Server.Contains("://") && Uri.TryCreate(this.Server, UriKind.RelativeOrAbsolute, out var url))
			options.Uri = url;
		else
			options.HostName = this.Server;
	}
	#endregion
}
