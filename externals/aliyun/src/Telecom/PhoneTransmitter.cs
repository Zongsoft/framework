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

namespace Zongsoft.Externals.Aliyun.Telecom;

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
			await this.Phone.SendAsync(template, new[] { destination }, data, cancellation: cancellation);
		else if(string.Equals(channel, VOICE_CHANNEL, StringComparison.OrdinalIgnoreCase))
			await this.Phone.CallAsync(template, destination, data, cancellation: cancellation);
		else
			throw new ArgumentException(string.Format(Properties.Resources.Pushing_ChannelUnsupported_Message, channel), nameof(channel));
	}
	#endregion

	#region 服务匹配
	public bool Match(string name) => string.Equals(name, this.Name, StringComparison.OrdinalIgnoreCase);
	bool IMatchable.Match(object parameter) => parameter is string name && this.Match(name);
	#endregion
}
