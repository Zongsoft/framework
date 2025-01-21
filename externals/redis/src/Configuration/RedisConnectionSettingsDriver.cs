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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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

using StackExchange.Redis;

using Zongsoft.Common;
using Zongsoft.Configuration;

namespace Zongsoft.Externals.Redis.Configuration
{
	public sealed class RedisConnectionSettingsDriver : ConnectionSettingsDriver<ConfigurationOptions, RedisConnectionSettingDescriptorCollection>
	{
		#region 常量定义
		internal const string NAME = "Redis";
		#endregion

		#region 单例字段
		public static readonly RedisConnectionSettingsDriver Instance = new();
		#endregion

		#region 构造函数
		public RedisConnectionSettingsDriver() : base(NAME)
		{
			this.Mapper = new RedisMapper(this);
			this.Populator = new RedisPopulator(this);
		}
		#endregion

		#region 嵌套子类
		private sealed class RedisMapper(RedisConnectionSettingsDriver driver) : MapperBase(driver)
		{
		}

		private sealed class RedisPopulator(RedisConnectionSettingsDriver driver) : PopulatorBase(driver)
		{
			protected override bool OnPopulate(ref ConfigurationOptions model, ConnectionSettingDescriptor descriptor, object value)
			{
				if(ConnectionSettingDescriptor.Server.Equals(descriptor) && value is string server)
				{
					foreach(var part in server.Slice(';'))
						model.EndPoints.Add(part);

					return true;
				}
				else if(ConnectionSettingDescriptor.Timeout.Equals(descriptor) && Common.Convert.TryConvertValue<TimeSpan>(value, out var duration))
				{
					model.ConnectTimeout = (int)duration.TotalMilliseconds;
					model.AsyncTimeout = (int)duration.TotalMilliseconds;
					model.SyncTimeout = (int)duration.TotalMilliseconds;

					return true;
				}

				return base.OnPopulate(ref model, descriptor, value);
			}
		}
		#endregion
	}

	public sealed class RedisConnectionSettingDescriptorCollection : ConnectionSettingDescriptorCollection
	{
		public readonly static ConnectionSettingDescriptor<string> Server = new(nameof(Server), nameof(ConfigurationOptions.EndPoints), true);
		public readonly static ConnectionSettingDescriptor<string> Client = new(nameof(Client), nameof(ConfigurationOptions.ClientName), false);
		public readonly static ConnectionSettingDescriptor<string> UserName = new(nameof(UserName), nameof(ConfigurationOptions.User), false);
		public readonly static ConnectionSettingDescriptor<string> Password = new(nameof(Password));
		public readonly static ConnectionSettingDescriptor<int> Database = new(nameof(Database), false);
		public readonly static ConnectionSettingDescriptor<int> RetryCount = new(nameof(RetryCount), nameof(ConfigurationOptions.ConnectRetry), false);
		public readonly static ConnectionSettingDescriptor<TimeSpan> Timeout = new(nameof(Timeout), nameof(ConfigurationOptions.ConnectTimeout), false);
		public readonly static ConnectionSettingDescriptor<string> Application = new(nameof(Application), nameof(ConfigurationOptions.ServiceName), false);

		public RedisConnectionSettingDescriptorCollection()
		{
			this.Add(Server);
			this.Add(Client);
			this.Add(Timeout);
			this.Add(UserName);
			this.Add(Password);
			this.Add(Database);
			this.Add(RetryCount);
			this.Add(Application);
		}
	}
}
