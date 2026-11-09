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

namespace Zongsoft.Communication;

public static class TransmitterUtility
{
	public static TransmitterDescriptor.Channel Channel(this TransmitterDescriptor descriptor, string name, string title = null, string description = null)
	{
		if(descriptor == null || name == null)
			return null;

		var channel = new TransmitterDescriptor.Channel(name, title, description);
		descriptor.Channels.Add(channel);
		return channel;
	}

	public static TransmitterDescriptor.Template Template(this TransmitterDescriptor descriptor, string name, string title = null, string description = null)
	{
		if(descriptor == null || name == null)
			return null;

		var template = new TransmitterDescriptor.Template(name, title, description);
		descriptor.Templates.Add(template);
		return template;
	}

	public static TransmitterDescriptor.Template Template(this TransmitterDescriptor.Channel channel, string name, string title = null, string description = null)
	{
		if(channel == null || name == null)
			return null;

		var template = new TransmitterDescriptor.Template(name, title, description);
		channel.Templates.Add(template);
		return template;
	}

	public static TransmitterDescriptor.Template.Parameter Parameter(this TransmitterDescriptor.Template template, string name, string title = null, string description = null)
	{
		if(template == null || name == null)
			return null;

		var parameter = new TransmitterDescriptor.Template.Parameter(name, title, description);
		template.Parameters.Add(parameter);
		return parameter;
	}
}