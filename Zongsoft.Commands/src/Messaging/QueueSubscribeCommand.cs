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
 * This file is part of Zongsoft.Commands library.
 *
 * The Zongsoft.Commands is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Commands is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Commands library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.ComponentModel;

using Zongsoft.Services;

namespace Zongsoft.Messaging.Commands
{
	public class QueueSubscribeCommand : CommandBase<CommandContext>
	{
		#region 构造函数
		public QueueSubscribeCommand() : this("Subscribe") { }
		public QueueSubscribeCommand(string name) : base(name) { }
		#endregion

		protected override object OnExecute(CommandContext context)
		{
			var queue = context.CommandNode.FindQueue();

			if(queue != null)
				return queue.SubscribeAsync().GetAwaiter().GetResult() ? 1 : 0;

			var topic = context.CommandNode.FindTopic();

			if(topic != null)
			{
				if(context.Expression.Arguments.Length == 0)
					throw new CommandException(Properties.Resources.Text_Command_MissingArguments);

				int count = 0;

				foreach(var argument in context.Expression.Arguments)
				{
					var index = argument.IndexOf(':');

					if(index > 0 && index < argument.Length)
						count += topic.SubscribeAsync(argument.Substring(0, index), argument.Substring(index + 1)).GetAwaiter().GetResult() ? 1 : 0;
					else
						count += topic.SubscribeAsync(argument).GetAwaiter().GetResult() ? 1 : 0;
				}

				return count;
			}

			return 0;
		}
	}
}
