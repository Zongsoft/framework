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
using System.Collections.Generic;

using Zongsoft.Text;
using Zongsoft.Services;
using Zongsoft.Communication;

namespace Zongsoft.Externals.Aliyun.Telecom
{
	[Service(typeof(ITransmitter))]
	public class PhoneTransmitter : ITransmitter
	{
		#region 常量定义
		private const string MESSAGE_CHANNEL = "message";
		private const string VOICE_CHANNEL = "voice";
		#endregion

		#region 构造函数
		public PhoneTransmitter(IServiceProvider serviceProvider)
		{
			this.Channels = new[] { MESSAGE_CHANNEL, VOICE_CHANNEL };
			this.Phone = serviceProvider.ResolveRequired<Phone>();
		}
		#endregion

		#region 公共属性
		public Phone Phone { get; }
		public string[] Channels { get; }
		#endregion

		#region 公共方法
		public string GetChannel(string destination) => TextRegular.Chinese.Cellphone.IsMatch(destination, out _) ? MESSAGE_CHANNEL : null;

		public void Transmit(string destination, string template, object data, string channel = null)
		{
			if(data == null)
				throw new ArgumentNullException(nameof(data));

			if(string.IsNullOrEmpty(channel) || string.Equals(channel, MESSAGE_CHANNEL, StringComparison.OrdinalIgnoreCase))
				this.Phone.SendAsync(template, new[] { destination }, new { code = data }).Wait(TimeSpan.FromSeconds(1));
			else if(string.Equals(channel, VOICE_CHANNEL, StringComparison.OrdinalIgnoreCase))
				this.Phone.CallAsync(template, destination, new { code = data }).Wait(TimeSpan.FromSeconds(1));
			else
				throw new ArgumentException($"Unsupported ‘{channel}’ channel.", nameof(channel));
		}
		#endregion
	}
}
