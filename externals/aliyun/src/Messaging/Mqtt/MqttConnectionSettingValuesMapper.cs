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
 * This file is part of Zongsoft.Externals.Aliyun library.
 *
 * The Zongsoft.Externals.Aliyun is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Aliyun is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Aliyun library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Text;
using System.Collections.Generic;

using Zongsoft.Configuration;

namespace Zongsoft.Externals.Aliyun.Messaging.Mqtt
{
	public class MqttConnectionSettingValuesMapper : ConnectionSettingValuesMapper
	{
		#region 构造函数
		public MqttConnectionSettingValuesMapper() : base("Aliyun.Mqtt") { }
		#endregion

		#region 重写方法
		public override string GetValue(string key, IDictionary<string, string> values)
		{
			if(string.Equals(key, nameof(IConnectionSettingValues.Client), StringComparison.OrdinalIgnoreCase))
				return GetClient(values);
			if(string.Equals(key, nameof(IConnectionSettingValues.UserName), StringComparison.OrdinalIgnoreCase))
				return GetUserName(values);
			if(string.Equals(key, nameof(IConnectionSettingValues.Password), StringComparison.OrdinalIgnoreCase))
				return GetPassword(values);

			return values.TryGetValue(key, out var value) ? value : null;
		}
		#endregion

		#region 私有方法
		private static string GetClient(IDictionary<string, string> values)
		{
			if(values.TryGetValue(nameof(IConnectionSettingValues.Client), out var client) && !string.IsNullOrWhiteSpace(client))
				return client;

			if(values.TryGetValue(nameof(IConnectionSettingValues.Group), out var group) && !string.IsNullOrWhiteSpace(group))
				return values[nameof(IConnectionSettingValues.Client)] = group + "@@@C" + Zongsoft.Common.Randomizer.GenerateString();

			return null;
		}

		private static string GetUserName(IDictionary<string, string> values)
		{
			return
				values.TryGetValue(nameof(IConnectionSettingValues.UserName), out var identity) &&
				values.TryGetValue(nameof(IConnectionSettingValues.Instance), out var instance) ?
				$"Signature|{identity}|{instance}" : null;
		}

		private static string GetPassword(IDictionary<string, string> values)
		{
			if(!values.TryGetValue(nameof(IConnectionSettingValues.Password), out var password) || string.IsNullOrEmpty(password))
				return null;

			var client = GetClient(values);

			if(string.IsNullOrEmpty(client))
				return null;

			using(var encipher = new System.Security.Cryptography.HMACSHA1(Encoding.UTF8.GetBytes(password)))
			{
				var data = Encoding.UTF8.GetBytes(client);
				return Convert.ToBase64String(encipher.ComputeHash(data));
			}
		}
		#endregion
	}
}
