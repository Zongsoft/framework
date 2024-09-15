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
	public sealed class RedisConnectionSettingsDriver : ConnectionSettingsDriver<RedisConnectionSettingDescriptorCollection>
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
			this.Modeler = new RedisModeler(this);
		}
		#endregion

		#region 嵌套子类
		private sealed class RedisMapper(RedisConnectionSettingsDriver driver) : ConnectionSettingsMapper(driver)
		{
		}

		private sealed class RedisModeler(RedisConnectionSettingsDriver driver) : ConnectionSettingsModeler<StackExchange.Redis.ConfigurationOptions>(driver)
		{
			protected override bool OnModel(ref ConfigurationOptions model, string name, object value)
			{
				if(ConnectionSettingDescriptor.Server.Equals(name) && value is string server)
				{
					foreach(var part in server.Slice(';'))
						model.EndPoints.Add(part);
				}
				else if(ConnectionSettingDescriptor.Timeout.Equals(name) && Common.Convert.TryConvertValue<TimeSpan>(value, out var duration))
				{
					model.ConnectTimeout = (int)duration.TotalMilliseconds;
					model.AsyncTimeout = (int)duration.TotalMilliseconds;
					model.SyncTimeout = (int)duration.TotalMilliseconds;
				}

				return base.OnModel(ref model, name, value);
			}
		}
		#endregion
	}

	public sealed class RedisConnectionSettingDescriptorCollection : ConnectionSettingDescriptorCollection
	{
		public RedisConnectionSettingDescriptorCollection()
		{
			this.Add(nameof(ConnectionSettings.UserName), nameof(ConfigurationOptions.User), typeof(string));
			this.Add(nameof(ConnectionSettings.Client), nameof(ConfigurationOptions.ClientName), typeof(string));
			this.Add(nameof(ConnectionSettings.Timeout), nameof(ConfigurationOptions.ConnectTimeout), typeof(TimeSpan));
			this.Add(nameof(ConnectionSettings.Database), nameof(ConfigurationOptions.DefaultDatabase), typeof(string));
			this.Add(nameof(ConnectionSettings.Application), nameof(ConfigurationOptions.ServiceName), typeof(string));
		}
	}
}
