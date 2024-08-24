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
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Zongsoft.Communication
{
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
}