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

using Zongsoft.Services;

namespace Zongsoft.Collections.Commands
{
	[CommandOption("queues", Type = typeof(string), Description = "Text.QueueCommand.Options.Queues")]
	public class QueueCountCommand : CommandBase<CommandContext>
	{
		#region 构造函数
		public QueueCountCommand() : this("Count")
		{
		}

		public QueueCountCommand(string name) : base(name)
		{
		}
		#endregion

		#region 执行方法
		protected override object OnExecute(CommandContext context)
		{
			var result = 0;
			var queues = QueueCommandHelper.GetQueues(context.CommandNode, context.Expression.Options.GetValue<string>("queues"));

			foreach(var queue in queues)
			{
				var count = queue.Count;

				if(count > 0)
					context.Output.WriteLine(CommandOutletColor.DarkGreen, string.Format(Properties.Resources.Text_QueueCountCommand_Message, queue.Name, count));
				else
					context.Output.WriteLine(CommandOutletColor.DarkRed, string.Format(Properties.Resources.Text_QueueIsEmpty, queue.Name));
			}

			return result;
		}
		#endregion
	}
}
