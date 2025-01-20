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
	public sealed class MqttConnectionSettingsDriver : ConnectionSettingsDriver<ConnectionSettingDescriptorCollection>
	{
		#region 单例字段
		public static readonly MqttConnectionSettingsDriver Instance = new();
		#endregion

		#region 私有构造
		private MqttConnectionSettingsDriver() : base("Aliyun.Mqtt")
		{
			this.Mapper = new MqttMapper(this);
		}
		#endregion

		#region 嵌套子类
		private sealed class MqttMapper(MqttConnectionSettingsDriver driver) : MapperBase(driver)
		{
			#region 重写方法
			protected override bool OnMap(ConnectionSettingDescriptor descriptor, IDictionary<string, string> values, out object value)
			{
				if(ConnectionSettingDescriptor.Client.Equals(descriptor.Name))
					return Common.Convert.TryConvertValue(GetClient(values), out value);
				if(ConnectionSettingDescriptor.UserName.Equals(descriptor.Name))
					return Common.Convert.TryConvertValue(GetUserName(values), out value);
				if(ConnectionSettingDescriptor.Password.Equals(descriptor.Name))
					return Common.Convert.TryConvertValue(GetPassword(values), out value);

				return base.OnMap(descriptor, values, out value);
			}
			#endregion

			#region 私有方法
			private static string GetClient(IDictionary<string, string> values)
			{
				if(values.TryGetValue(nameof(IConnectionSettings.Client), out var client) && !string.IsNullOrWhiteSpace(client))
					return client;

				if(values.TryGetValue(nameof(IConnectionSettings.Group), out var group) && !string.IsNullOrWhiteSpace(group))
					return values[nameof(IConnectionSettings.Client)] = group + "@@@" + Zongsoft.Common.Randomizer.GenerateString();

				return null;
			}

			private static string GetUserName(IDictionary<string, string> values) =>
				values.TryGetValue(nameof(IConnectionSettings.UserName), out var identity) &&
				values.TryGetValue(nameof(IConnectionSettings.Instance), out var instance) ?
				$"Signature|{identity}|{instance}" : null;

			private static string GetPassword(IDictionary<string, string> values)
			{
				if(!values.TryGetValue(nameof(IConnectionSettings.Password), out var password) || string.IsNullOrEmpty(password))
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
		#endregion
	}
}
