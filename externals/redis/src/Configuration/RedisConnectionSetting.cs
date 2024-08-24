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
using System.Linq;
using System.Collections.Generic;

using Zongsoft.Configuration;

namespace Zongsoft.Externals.Redis.Configuration
{
	public class RedisConnectionSetting : IConnectionSetting
	{
		#region 常量定义
		internal const string DRIVER = "Redis";
		#endregion

		#region 成员字段
		private readonly IConnectionSetting _connectionSetting;
		private readonly StackExchange.Redis.ConfigurationOptions _options;
		#endregion

		#region 构造函数
		public RedisConnectionSetting(IConnectionSetting connectionSetting)
		{
			if(connectionSetting == null)
				throw new ArgumentNullException(nameof(connectionSetting));

			if(!connectionSetting.IsDriver(DRIVER))
				throw new ConfigurationException($"The specified '{connectionSetting}' connection settings is not a Redis configuration.");

			var host = connectionSetting.Values.Server;
			if(connectionSetting.Values.Port != 0)
				host += $":{connectionSetting.Values.Port}";

			var entries = connectionSetting.Values.Mapping
					.Where(entry =>
						!string.IsNullOrEmpty(entry.Key) &&
						!entry.Key.Equals(nameof(ConnectionSetting.Values.Server), StringComparison.OrdinalIgnoreCase) &&
						!entry.Key.Equals(nameof(ConnectionSetting.Values.Port), StringComparison.OrdinalIgnoreCase))
					.Select(entry => $"{entry.Key}={entry.Value}");

			_options = StackExchange.Redis.ConfigurationOptions.Parse(entries.Any() ? host + ',' + string.Join(',', entries) : host, true);
			_connectionSetting = connectionSetting;
		}
		#endregion

		#region 公共属性
		public string Driver { get => _connectionSetting.Driver; set => _connectionSetting.Driver = value; }
		public string Name => _connectionSetting.Name;
		public string Value { get => _connectionSetting.Value; set => _connectionSetting.Value = value; }
		public IConnectionSettingValues Values => _connectionSetting.Values;
		public bool HasProperties => _connectionSetting.HasProperties;
		public IDictionary<string, string> Properties => _connectionSetting.Properties;
		#endregion

		#region 内部属性
		internal StackExchange.Redis.ConfigurationOptions Options => _options;
		#endregion

		#region 公共方法
		public bool IsDriver(string driver) => _connectionSetting.IsDriver(driver);
		public bool Equals(ISetting other) => _connectionSetting.Equals(other);
		public bool Equals(IConnectionSetting other) => _connectionSetting.Equals(other);
		public override bool Equals(object obj) => obj is IConnectionSetting connectionSetting && this.Equals(connectionSetting);
		public override int GetHashCode() => _connectionSetting.GetHashCode();
		public override string ToString() => _connectionSetting.ToString();
		#endregion
	}
}