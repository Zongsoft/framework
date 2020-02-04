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
using System.Collections.Generic;

using Zongsoft.Services;

namespace Zongsoft.Collections.Commands
{
	internal static class QueueCommandHelper
	{
		public static ICollection<IQueue> GetQueues(CommandTreeNode node, string names)
		{
			var result = new List<IQueue>();
			IQueue queue;

			if(string.IsNullOrWhiteSpace(names))
			{
				queue = FindQueue(node);

				if(queue == null)
					throw new CommandException(string.Format(Properties.Resources.Text_CannotObtainCommandTarget, "Queue"));

				result.Add(queue);
			}
			else
			{
				foreach(var name in names.Split(',', ';'))
				{
					if(!string.IsNullOrWhiteSpace(name))
					{
						queue = FindQueue(node, name);

						if(queue == null)
							throw new CommandException(string.Format(Properties.Resources.Text_CannotObtainCommandTarget, $"Queue[{name}]"));

						result.Add(queue);
					}
				}
			}

			return result;
		}

		private static IQueue FindQueue(CommandTreeNode node, string name = null)
		{
			if(node == null)
				return null;

			if(node.Command is QueueCommand queueCommand)
				return name == null ? queueCommand.Queue : queueCommand.QueueProvider.GetQueue(name);

			return FindQueue(node.Parent, name);
		}
	}
}
