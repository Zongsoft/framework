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
 * Copyright (C) 2025-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Intelligences library.
 *
 * The Zongsoft.Intelligences is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Intelligences is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Intelligences library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.AI;

using Zongsoft.Services;
using Zongsoft.Terminals;
using Zongsoft.Components;

namespace Zongsoft.Intelligences.Commands.Chatting;

public class HistoryCommand() : CommandBase<CommandContext>("History")
{
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		var service = (context.Find<IServiceAccessor<IChatService>>(true)?.Value) ??
			throw new CommandException("The chat service is not found.");

		var terminal = context.GetTerminal();
		var session = service.Sessions.Current;

		if(session == null)
		{
			terminal?.WriteLine("No actived chatting session.");
			return ValueTask.FromResult<object>(null);
		}

		if(session.History.IsEmpty)
		{
			terminal?.WriteLine($"The history of the '{session.Identifier}' session is empty.");
			return ValueTask.FromResult<object>(null);
		}

		if(terminal != null)
		{
			terminal.WriteLine(CommandOutletColor.DarkYellow, session);
			terminal.WriteLine(CommandOutletColor.DarkGray, new string('-', session.ToString().Length));

			foreach(var message in session.History)
			{
				var content = CommandOutletContent.Create(CommandOutletColor.Cyan, message.Role.ToString())
					.Append(CommandOutletColor.DarkGray, ": ")
					.AppendLine(GetMessageColor(message.Role), message.Text);

				terminal.Write(content);
			}
		}

		return ValueTask.FromResult<object>(session.History);
	}

	private static CommandOutletColor GetMessageColor(ChatRole role) => role == ChatRole.User ? CommandOutletColor.DarkGreen : CommandOutletColor.DarkYellow;
}
