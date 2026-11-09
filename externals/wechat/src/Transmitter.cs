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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Common;
using Zongsoft.Services;
using Zongsoft.Communication;

namespace Zongsoft.Externals.Wechat;

[DisplayName("Transmitter.Title")]
[Description("Transmitter.Description")]
[Service(typeof(ITransmitter))]
public class Transmitter : ITransmitter, IMatchable, IMatchable<string>
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
				_descriptor = new TransmitterDescriptor(this.Name, AnnotationUtility.GetDisplayName(this.GetType()), AnnotationUtility.GetDescription(this.GetType()));
				_descriptor.Channel("Message", Properties.Resources.TemplateMessage);
			}

			return _descriptor;
		}
	}
	#endregion

	#region 公共方法
	public async ValueTask TransmitAsync(string destination, string channel, string template, object data, CancellationToken cancellation)
	{
		if(string.IsNullOrEmpty(destination))
			throw new ArgumentNullException(nameof(destination));
		if(string.IsNullOrEmpty(template))
			throw new ArgumentNullException(nameof(template));

		if(!string.IsNullOrEmpty(channel) && !string.Equals(channel, "Message", StringComparison.OrdinalIgnoreCase))
			throw new ArgumentException(null, nameof(channel));

		var index = destination.IndexOf(':');
		if(index <= 0 || index >= destination.Length - 1)
			throw new ArgumentException(Properties.Resources.Wechat_DestinationInvalid_Message);

		var channelObject = await ChannelManager.FindChannelAsync(destination[..index], cancellation);
		if(channelObject == null)
			throw new ArgumentException(string.Format(Properties.Resources.Wechat_ChannelNotFound_Message, destination[..index]));

		await channelObject.Messager.SendAsync(destination[(index + 1)..], template, data, cancellation);
	}
	#endregion

	#region 服务匹配
	public bool Match(string name) => string.Equals(name, this.Name, StringComparison.OrdinalIgnoreCase);
	bool IMatchable.Match(object parameter) => parameter is string name && this.Match(name);
	#endregion
}
