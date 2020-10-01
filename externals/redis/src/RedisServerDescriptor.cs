/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Redis library.
 *
 * The Zongsoft.Externals.Redis is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Redis is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Redis library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Net;

using StackExchange.Redis;

namespace Zongsoft.Externals.Redis
{
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
		public ServerType ServerType
		{
			get;
		}

		public bool IsSlave
		{
			get;
		}

		public bool IsConnected
		{
			get;
		}

		public EndPoint EndPoint
		{
			get;
		}

		public Version Version
		{
			get;
		}
		#endregion
	}
}
