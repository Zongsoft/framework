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
using System.Collections;
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Runtime.Serialization;

namespace Zongsoft.Collections.Commands
{
	[CommandOption("count", Type = typeof(int), DefaultValue = 1, Description = "Text.QueueCommand.Options.Count")]
	[CommandOption("round", Type = typeof(int), DefaultValue = 1, Description = "Text.QueueCommand.Options.Round")]
	[CommandOption("queues", Type = typeof(string), Description = "Text.QueueCommand.Options.Queues")]
	public class QueueOutCommand : CommandBase<CommandContext>
	{
		#region 构造函数
		public QueueOutCommand() : this("Out")
		{
		}

		public QueueOutCommand(string name) : base(name)
		{
		}
		#endregion

		#region 执行方法
		protected override object OnExecute(CommandContext context)
		{
			var count = context.Expression.Options.GetValue<int>("count");
			int round = context.Expression.Options.GetValue<int>("round");
			var result = new List<object>(count);

			if(count < 1)
				throw new CommandOptionValueException("count", count);

			if(round < 1)
				throw new CommandOptionValueException("round", round);

			var queues = QueueCommandHelper.GetQueues(context.CommandNode, context.Expression.Options.GetValue<string>("queues"));

			foreach(var queue in queues)
			{
				if(queue.Count == 0)
				{
					context.Output.WriteLine(CommandOutletColor.DarkRed, string.Format(Properties.Resources.Text_QueueIsEmpty, queue.Name));
					continue;
				}

				for(int i = 0; i < round; i++)
				{
					var items = queue.Dequeue(count);

					if(items == null)
						break;

					foreach(var item in items)
					{
						result.Add(item);
					}

					context.Output.WriteLine(Serializer.Json.Serialize(items));
					context.Output.WriteLine(CommandOutletColor.DarkGreen, string.Format(Properties.Resources.Text_QueueOutCommand_Message, i + 1, count, queue.Name));
				}
			}

			return result.ToArray();
		}
		#endregion
	}
}
