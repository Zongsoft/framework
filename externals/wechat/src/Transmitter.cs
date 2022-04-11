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
 * This file is part of Zongsoft.Externals.WeChat library.
 *
 * The Zongsoft.Externals.WeChat is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.WeChat is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.WeChat library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.ComponentModel;

using Zongsoft.Common;
using Zongsoft.Services;
using Zongsoft.Communication;

namespace Zongsoft.Externals.Wechat
{
	[DisplayName("Transmitter.Title")]
	[Description("Transmitter.Description")]
	[Service(typeof(ITransmitter))]
	public class Transmitter : ITransmitter
	{
		#region 成员字段
		private TransmitterDescriptor _descriptor;
		#endregion

		#region 构造函数
		public Transmitter() { }
		#endregion

		#region 公共属性
		public string Name => "Wechat";
		public TransmitterDescriptor Descriptor
		{
			get
			{
				if(_descriptor == null)
				{
					_descriptor = new TransmitterDescriptor(this.Name, AnnotationUtility.GetDisplayName(this.GetType()), AnnotationUtility.GetDescription(this.GetType()))
					{
						Channels = new TransmitterDescriptor.ChannelDescriptorCollection
						{
							new ("Message", Properties.Resources.TemplateMessage),
						}
					};
				}

				return _descriptor;
			}
		}
		#endregion

		#region 公共方法
		public void Transmit(string destination, string template, object data, string channel = null)
		{
			if(string.IsNullOrEmpty(destination))
				throw new ArgumentNullException(nameof(destination));
			if(string.IsNullOrEmpty(template))
				throw new ArgumentNullException(nameof(template));

			var index = destination.IndexOf(':');
			if(index <= 0 || index >= destination.Length - 1)
				throw new ArgumentException($"Invalid destination format.");

			if(!ChannelManager.TryGetChannel(destination.Substring(0, index), out var channelObject))
				throw new ArgumentException($"The specified '{destination.Substring(0, index)}' WeChat channel does not exist.");

			channelObject.Messager.SendAsync(destination.Substring(index + 1), template, data).ConfigureAwait(false).GetAwaiter().GetResult();
		}
		#endregion
	}
}
