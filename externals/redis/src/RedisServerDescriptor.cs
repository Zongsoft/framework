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
using System.Net;

using StackExchange.Redis;

namespace Zongsoft.Externals.Redis;

public class RedisServerDescriptor
{
	#region 构造函数
	public RedisServerDescriptor(IServer server)
	{
		if(server == null)
			throw new ArgumentNullException(nameof(server));

		this.ServerType = server.ServerType;
		this.IsSlave = server.IsReplica;
		this.IsConnected = server.IsConnected;
		this.EndPoint = server.EndPoint;
		this.Version = server.Version;
	}
	#endregion

	#region 公共属性
	public ServerType ServerType { get; }
	public bool IsSlave { get; }
	public bool IsConnected { get; }
	public EndPoint EndPoint { get; }
	public Version Version { get; }
	#endregion
}
