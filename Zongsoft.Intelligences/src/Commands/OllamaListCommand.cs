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
using System.Collections.Generic;

using Zongsoft.Terminals;
using Zongsoft.Components;

namespace Zongsoft.Intelligences.Commands;

[CommandOption("running")]
public class OllamaListCommand() : CommandBase<CommandContext>("List")
{
	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		var client = (context.Find<OllamaCommand>(true)?.Client) ??
			throw new CommandException("The Ollama API client is not found.");

		if(context.Expression.Options.Contains("running"))
		{
			var models = await client.ListRunningModelsAsync(cancellation);
			Dump(context.GetTerminal(), models);
			return models;
		}
		else
		{
			var models = await client.ListLocalModelsAsync(cancellation);
			Dump(context.GetTerminal(), models);
			return models;
		}
	}

	private static void Dump(ITerminal terminal, IEnumerable<OllamaSharp.Models.Model> models)
	{
		if(terminal == null)
			return;

		foreach(var model in models)
		{
			terminal.Dump(model);
		}
	}
}
