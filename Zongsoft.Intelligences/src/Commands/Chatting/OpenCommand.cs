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
			throw new CommandException(Properties.Resources.Chat_ServiceRequired_Message);

		var identifier = context.Options.GetValue<string>(SESSION_OPTION) ?? context.Arguments[0];

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
