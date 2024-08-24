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
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Common;
using Zongsoft.Services;
using Zongsoft.Communication;

namespace Zongsoft.Externals.Aliyun.Telecom
{
	[DisplayName("PhoneTransmitter.Title")]
	[Description("PhoneTransmitter.Description")]
	[Service(typeof(ITransmitter))]
	public class PhoneTransmitter : ITransmitter, IMatchable, IMatchable<string>
	{
		#region 常量定义
		private const string MESSAGE_CHANNEL = "message";
		private const string VOICE_CHANNEL = "voice";
		#endregion

		#region 成员字段
		private TransmitterDescriptor _descriptor;
		#endregion

		#region 构造函数
		public PhoneTransmitter() { }
		#endregion

		#region 公共属性
		public string Name => "Phone";

		[ServiceDependency(IsRequired = true)]
		public Phone Phone { get; set; }

		public TransmitterDescriptor Descriptor
		{
			get
			{
				if(_descriptor == null)
				{
					_descriptor = new TransmitterDescriptor(this.Name, AnnotationUtility.GetDisplayName(this.GetType()), AnnotationUtility.GetDescription(this.GetType()));

					var channel = _descriptor.Channel(MESSAGE_CHANNEL, Properties.Resources.Text_Phone_Message);
					foreach(var option in this.Phone.Options.Message.Templates)
					{
						var template = channel.Template(option.Name);

						foreach(var parameter in option.Parameters)
							template.Parameter(parameter.Name, parameter.Title, parameter.Description);
					}

					channel = _descriptor.Channel(VOICE_CHANNEL, Properties.Resources.Text_Phone_Voice);
					foreach(var option in this.Phone.Options.Voice.Templates)
					{
						var template = channel.Template(option.Name);

						foreach(var parameter in option.Parameters)
							template.Parameter(parameter.Name, parameter.Title, parameter.Description);
					}
				}

				return _descriptor;
			}
		}
		#endregion

		#region 公共方法
		public async ValueTask TransmitAsync(string destination, string channel, string template, object data, CancellationToken cancellation)
		{
			if(data == null)
				throw new ArgumentNullException(nameof(data));

			if(string.IsNullOrEmpty(channel) || string.Equals(channel, MESSAGE_CHANNEL, StringComparison.OrdinalIgnoreCase))
				await this.Phone.SendAsync(template, new[] { destination }, data, cancellation:cancellation);
			else if(string.Equals(channel, VOICE_CHANNEL, StringComparison.OrdinalIgnoreCase))
				await this.Phone.CallAsync(template, destination, data, cancellation:cancellation);
			else
				throw new ArgumentException($"Unsupported ‘{channel}’ channel.", nameof(channel));
		}
		#endregion

		#region 服务匹配
		public bool Match(string name) => string.Equals(name, this.Name, StringComparison.OrdinalIgnoreCase);
		bool IMatchable.Match(object parameter) => parameter is string name && this.Match(name);
		#endregion
	}
}
