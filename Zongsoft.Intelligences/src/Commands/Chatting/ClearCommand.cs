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

public class ClearCommand() : CommandBase<CommandContext>("Clear")
{
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		var service = (context.Find<IServiceAccessor<IChatService>>(true)?.Value) ??
			throw new CommandException(Properties.Resources.Chat_ServiceRequired_Message);

		var history = service.Sessions.Current?.History;

		if(history == null)
			return ValueTask.FromResult<object>(0);

		var count = history.Count;
		if(count > 0)
			history.Clear();

		if(context.TryGetTerminal(out var terminal))
		{
			if(count == 0)
				terminal.WriteLine(CommandOutletColor.DarkGray, "The history is already empty.");
			else
				terminal.WriteLine(CommandOutletColor.DarkYellow, $"The history has been cleared, {count} messages were removed.");
		}

		return ValueTask.FromResult<object>(count);
	}
}
