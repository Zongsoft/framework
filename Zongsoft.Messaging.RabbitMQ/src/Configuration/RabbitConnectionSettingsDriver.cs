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

public class RabbitConnectionSettingsDriver : ConnectionSettingsDriver<RabbitConnectionSettingDescriptorCollection>
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
		this.Modeler = new RabbitModeler(this);
	}
	#endregion

	#region 嵌套子类
	private sealed class RabbitMapper(RabbitConnectionSettingsDriver driver) : ConnectionSettingsMapper(driver)
	{
		protected override bool OnMap(string name, IDictionary<string, string> values, out object value)
		{
			if(ConnectionSettingDescriptor.Client.Equals(name))
			{
				if(values.TryGetValue(name, out var client) && !string.IsNullOrEmpty(client))
				{
					value = client;
					return true;
				}
			}

			return base.OnMap(name, values, out value);
		}
	}

	private sealed class RabbitModeler(RabbitConnectionSettingsDriver driver) : ConnectionSettingsModeler<ConnectionFactory>(driver)
	{
		protected override bool OnModel(ref ConnectionFactory model, string name, object value)
		{
			if(ConnectionSettingDescriptor.Port.Equals(name) && Common.Convert.TryConvertValue<ushort>(value, out var port) && port > 0)
			{
				model.Port = port;
				return true;
			}

			if(ConnectionSettingDescriptor.Timeout.Equals(name) && Common.Convert.TryConvertValue<TimeSpan>(value, out var timeout) && timeout > TimeSpan.Zero)
			{
				model.SocketReadTimeout = timeout;
				model.SocketWriteTimeout = timeout;
				model.ContinuationTimeout = timeout;
				model.RequestedConnectionTimeout = timeout;
				model.HandshakeContinuationTimeout = timeout;
				return true;
			}

			return base.OnModel(ref model, name, value);
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
	public readonly static ConnectionSettingDescriptor<ushort> Port = new(nameof(Port));
	public readonly static ConnectionSettingDescriptor<string> Certificate = new(nameof(Certificate), nameof(ConnectionFactory.Ssl.CertPath), null, null, null);
	public readonly static ConnectionSettingDescriptor<bool> Reconnectable = new(nameof(Reconnectable), nameof(ConnectionFactory.AutomaticRecoveryEnabled), (object)true);

	public RabbitConnectionSettingDescriptorCollection()
	{
		this.Add(Client);
		this.Add(Server);
		this.Add(Port);
		this.Add(UserName);
		this.Add(Password);
		this.Add(Certificate);
		this.Add(Reconnectable);
	}
}
