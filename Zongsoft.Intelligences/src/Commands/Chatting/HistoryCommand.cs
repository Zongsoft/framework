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
			throw new CommandException(Properties.Resources.Chat_ServiceRequired_Message);

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
