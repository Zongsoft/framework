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

public class ClearCommand() : CommandBase<CommandContext>("Clear")
{
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		var service = (context.Find<IServiceAccessor<IChatService>>(true)?.Value) ??
			throw new CommandException("The chat service is not found.");

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
