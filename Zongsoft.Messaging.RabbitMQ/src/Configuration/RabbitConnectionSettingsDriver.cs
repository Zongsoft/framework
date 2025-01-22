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
 * This file is part of Zongsoft.Messaging.RabbitMQ library.
 *
 * The Zongsoft.Messaging.RabbitMQ is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Messaging.RabbitMQ is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Messaging.RabbitMQ library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections;
using System.Collections.Generic;

using RabbitMQ.Client;

using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Messaging.RabbitMQ.Configuration;

public class RabbitConnectionSettingsDriver : ConnectionSettingsDriver<ConnectionFactory, RabbitConnectionSettingDescriptorCollection>
{
	#region 常量定义
	internal const string NAME = "RabbitMQ";
	#endregion

	#region 单例字段
	public static readonly RabbitConnectionSettingsDriver Instance = new();
	#endregion

	#region 私有构造
	private RabbitConnectionSettingsDriver() : base(NAME)
	{
		this.Mapper = new RabbitMapper(this);
		this.Populator = new RabbitPopulator(this);
	}
	#endregion

	#region 嵌套子类
	private sealed class RabbitMapper(RabbitConnectionSettingsDriver driver) : MapperBase(driver)
	{
		protected override bool OnMap(ConnectionSettingDescriptor descriptor, IDictionary<object, string> values, out object value)
		{
			if(ConnectionSettingDescriptor.Client.Equals(descriptor))
			{
				if(values.TryGetValue(descriptor, out var client) && !string.IsNullOrEmpty(client))
					value = client;
				else
					value = $"C{Common.Randomizer.GenerateString()}";

				return true;
			}

			return base.OnMap(descriptor, values, out value);
		}
	}

	private sealed class RabbitPopulator(RabbitConnectionSettingsDriver driver) : PopulatorBase(driver)
	{
		protected override bool OnPopulate(ref ConnectionFactory model, ConnectionSettingDescriptor descriptor, object value)
		{
			if(ConnectionSettingDescriptor.Port.Equals(descriptor) && Common.Convert.TryConvertValue<int>(value, out var port) && port > 0)
			{
				model.Port = port == 0 ? -1 : Math.Abs(port);
				return true;
			}

			if(ConnectionSettingDescriptor.Server.Equals(descriptor) && value is string server)
			{
				if(server.Contains("://") && Uri.TryCreate(server, UriKind.RelativeOrAbsolute, out var url))
					model.Uri = url;
				else
					model.HostName = server;

				return true;
			}

			if(ConnectionSettingDescriptor.Timeout.Equals(descriptor) && Common.Convert.TryConvertValue<TimeSpan>(value, out var timeout) && timeout > TimeSpan.Zero)
			{
				model.SocketReadTimeout = timeout;
				model.SocketWriteTimeout = timeout;
				model.ContinuationTimeout = timeout;
				model.RequestedConnectionTimeout = timeout;
				model.HandshakeContinuationTimeout = timeout;
				return true;
			}

			return base.OnPopulate(ref model, descriptor, value);
		}
	}
	#endregion
}

public sealed class RabbitConnectionSettingDescriptorCollection : ConnectionSettingDescriptorCollection
{
	public readonly static ConnectionSettingDescriptor<string> Client = new(nameof(Client), nameof(ConnectionFactory.ClientProvidedName), null, null);
	public readonly static ConnectionSettingDescriptor<string> Server = new(nameof(Server), nameof(ConnectionFactory.HostName), null, null);
	public readonly static ConnectionSettingDescriptor<string> UserName = new(nameof(UserName));
	public readonly static ConnectionSettingDescriptor<string> Password = new(nameof(Password));
	public readonly static ConnectionSettingDescriptor<int> Port = new(nameof(Port), -1);
	public readonly static ConnectionSettingDescriptor<string> Queue = new(nameof(Queue));
	public readonly static ConnectionSettingDescriptor<string> Container = new(nameof(Container), nameof(ConnectionConfig.VirtualHost), "/");
	public readonly static ConnectionSettingDescriptor<TimeSpan> Heartbeat = new(nameof(Heartbeat), nameof(ConnectionFactory.RequestedHeartbeat), TimeSpan.FromSeconds(30));
	public readonly static ConnectionSettingDescriptor<int> Concurrency = new(nameof(Concurrency), nameof(ConnectionFactory.ConsumerDispatchConcurrency), 1);
	public readonly static ConnectionSettingDescriptor<string> Certificate = new(nameof(Certificate), nameof(ConnectionFactory.Ssl.CertPath), false);
	public readonly static ConnectionSettingDescriptor<bool> Reconnectable = new(nameof(Reconnectable), nameof(ConnectionFactory.AutomaticRecoveryEnabled), false, true);

	public RabbitConnectionSettingDescriptorCollection()
	{
		this.Add(Client);
		this.Add(Server);
		this.Add(Port);
		this.Add(Queue);
		this.Add(UserName);
		this.Add(Password);
		this.Add(Container);
		this.Add(Heartbeat);
		this.Add(Concurrency);
		this.Add(Certificate);
		this.Add(Reconnectable);
	}
}
