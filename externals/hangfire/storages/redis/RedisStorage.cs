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
 * Copyright (C) 2010-2022 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Hangfire library.
 *
 * The Zongsoft.Externals.Hangfire is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Hangfire is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Hangfire library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Collections.Generic;

using Hangfire.Server;
using Hangfire.States;
using Hangfire.Storage;
using Hangfire.Logging;

using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Externals.Hangfire.Storages
{
	[Service<global::Hangfire.JobStorage>]
	public class RedisStorage : global::Hangfire.JobStorage, IEquatable<RedisStorage>
	{
		#region 常量定义
		private const string DRIVER = "Redis";
		#endregion

		#region 成员字段
		private readonly object _lock = new();
		private global::Hangfire.Redis.StackExchange.RedisStorage _storage;
		#endregion

		#region 公共属性
		public global::Hangfire.Redis.StackExchange.RedisStorage Storage
		{
			get
			{
				if(_storage == null)
				{
					lock(_lock)
					{
						_storage ??= new global::Hangfire.Redis.StackExchange.RedisStorage(GetRedisConnection());
					}
				}

				return _storage;
			}
		}
		#endregion

		#region 私有方法
		private static IConnectionSettings GetConnectionSetting(ConnectionSettingsCollection settings)
		{
			if(settings == null || settings.Count == 0)
				return null;

			if(settings.TryGetValue("Hangfire", DRIVER, out var setting))
				return setting;

			setting = settings.GetDefault();
			if(setting != null && setting.IsDriver(DRIVER))
				return setting;

			return settings.FirstOrDefault(setting => setting != null && setting.IsDriver(DRIVER));
		}

		private static StackExchange.Redis.ConnectionMultiplexer GetRedisConnection() => GetRedisConnection(GetConnectionSetting(ApplicationContext.Current?.Configuration?.GetOption<ConnectionSettingsCollection>("/Externals/Redis/ConnectionSettings")));
		private static StackExchange.Redis.ConnectionMultiplexer GetRedisConnection(IConnectionSettings settings) => StackExchange.Redis.ConnectionMultiplexer.Connect(GetRedisConfiguration(settings));
		private static StackExchange.Redis.ConfigurationOptions GetRedisConfiguration(IConnectionSettings settings) => settings?.GetOptions<StackExchange.Redis.ConfigurationOptions>();
		#endregion

		#region 重写方法
		public override bool LinearizableReads => this.Storage.LinearizableReads;
		#pragma warning disable CS0618 // 类型或成员已过时
		[Obsolete("This method will be removed in a future version.")]
		public override IEnumerable<IServerComponent> GetComponents() => this.Storage.GetComponents();
		#pragma warning restore CS0618 // 类型或成员已过时
		public override IMonitoringApi GetMonitoringApi() => this.Storage.GetMonitoringApi();
		public override IStorageConnection GetConnection() => this.Storage.GetConnection();
		public override IStorageConnection GetReadOnlyConnection() => this.Storage.GetReadOnlyConnection();
		public override IEnumerable<IStateHandler> GetStateHandlers() => this.Storage.GetStateHandlers();
		public override bool HasFeature(string featureId) => this.Storage.HasFeature(featureId);
		public override void WriteOptionsToLog(ILog logger) => _storage?.WriteOptionsToLog(logger);
		public bool Equals(RedisStorage other) => other != null && other._storage == this._storage;
		public override bool Equals(object obj) => obj is RedisStorage other && this.Storage.Equals(other);
		public override int GetHashCode() => this.Storage.GetHashCode();
		public override string ToString() => _storage == null ? this.GetType().FullName : _storage.ToString();
		#endregion
	}
}