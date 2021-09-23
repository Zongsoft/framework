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
	[DisplayName("Text.QueueClearCommand.Name")]
	[Description("Text.QueueClearCommand.Description")]
	public class QueueClearCommand : CommandBase<CommandContext>
	{
		#region 构造函数
		public QueueClearCommand() : this("Clear") { }
		public QueueClearCommand(string name) : base(name) { }
		#endregion

		#region 执行方法
		protected override object OnExecute(CommandContext context)
		{
			var queue = context.CommandNode.FindQueue();

			//显示执行成功的信息
			if(queue != null)
			{
				queue.Clear();
				context.Output.WriteLine(Properties.Resources.Text_CommandExecuteSucceed);
			}

			return null;
		}
		#endregion
	}
}
