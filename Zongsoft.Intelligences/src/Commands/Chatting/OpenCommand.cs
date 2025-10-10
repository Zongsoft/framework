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

using Zongsoft.Services;
using Zongsoft.Terminals;
using Zongsoft.Components;

namespace Zongsoft.Intelligences.Commands.Chatting;

[CommandOption(SESSION_OPTION, typeof(string))]
public class OpenCommand() : CommandBase<CommandContext>("Open")
{
	private const string SESSION_OPTION = "session";

	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		var service = (context.Find<IServiceAccessor<IChatService>>(true)?.Value) ??
			throw new CommandException("The chat service required by this command was not found.");

		var identifier = context.GetOptions().GetValue<string>(SESSION_OPTION) ?? context.Arguments[0];

		if(string.IsNullOrEmpty(identifier))
			return ValueTask.FromResult<object>(Create(context, service));
		else
			return ValueTask.FromResult<object>(Activate(context, service, identifier));
	}

	private static IChatSession Create(CommandContext context, IChatService service)
	{
		var session = service.Sessions.Create();

		if(context.TryGetTerminal(out var terminal))
			terminal.WriteLine(CommandOutletColor.DarkGreen, $"The session(chatroom) identified as '{session.Identifier}' has been created.");

		return session;
	}

	private static IChatSession Activate(CommandContext context, IChatService service, string identifier)
	{
		var session = service.Sessions.Activate(identifier);

		if(context.TryGetTerminal(out var terminal))
		{
			if(session == null)
				terminal.WriteLine(CommandOutletColor.DarkRed, $"The session(chatroom) identified as '{session.Identifier}' was actived.");
			else
				terminal.WriteLine(CommandOutletColor.DarkGreen, $"The session(chatroom) identified as '{session.Identifier}' has not found.");
		}

		return session;
	}
}
