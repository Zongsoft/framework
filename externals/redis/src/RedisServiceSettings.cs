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
using System.Collections.Generic;

namespace Zongsoft.Externals.Redis
{
	public class RedisServiceSettings
	{
		#region 成员字段
		private readonly StackExchange.Redis.ConfigurationOptions _options;
		#endregion

		#region 构造函数
		public RedisServiceSettings(StackExchange.Redis.ConfigurationOptions options)
		{
			_options = options ?? throw new ArgumentNullException(nameof(options));
		}
		#endregion

		#region 公共属性
		public IList<EndPoint> Addresses
		{
			get => _options.EndPoints;
		}

		public string Password
		{
			get => _options.Password;
		}

		public int DatabaseId
		{
			get => _options.DefaultDatabase ?? 0;
		}

		public int ConnectionRetries
		{
			get => _options.ConnectRetry;
		}

		public TimeSpan ConnectionTimeout
		{
			get => TimeSpan.FromMilliseconds(_options.ConnectTimeout);
		}

		public TimeSpan OperationTimeout
		{
			get => TimeSpan.FromMilliseconds(_options.SyncTimeout);
		}

		public bool UseTwemproxy
		{
			get => _options.Proxy == StackExchange.Redis.Proxy.Twemproxy;
		}

		public bool AdministrationEnabled
		{
			get => _options.AllowAdmin;
		}

		public bool DnsEnabled
		{
			get => _options.ResolveDns;
		}

		public bool SslEnabled
		{
			get => _options.Ssl;
		}

		public string SslHost
		{
			get => _options.SslHost;
		}

		public string ClientName
		{
			get => _options.ClientName;
		}

		public string ServiceName
		{
			get => _options.ServiceName;
		}
		#endregion

		#region 内部属性
		internal StackExchange.Redis.ConfigurationOptions RedisOptions
		{
			get => _options;
		}
		#endregion

		#region 静态方法
		public static RedisServiceSettings Parse(string connectionString)
		{
			if(string.IsNullOrWhiteSpace(connectionString))
				throw new ArgumentNullException(nameof(connectionString));

			return new RedisServiceSettings(StackExchange.Redis.ConfigurationOptions.Parse(connectionString, true));
		}
		#endregion

		#region 重写方法
		public override string ToString()
		{
			return _options.ToString();
		}
		#endregion
	}
}
