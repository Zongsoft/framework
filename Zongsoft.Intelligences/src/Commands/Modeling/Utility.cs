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
using System.Collections.Generic;

using Zongsoft.Terminals;
using Zongsoft.Components;

namespace Zongsoft.Intelligences.Commands.Modeling;

internal static class Utility
{
	public static ValueTask Dump(this CommandContext context, IAsyncEnumerable<IModel> models) => Dump(context.GetTerminal(), models);
	public static async ValueTask Dump(this ITerminal terminal, IAsyncEnumerable<IModel> models)
	{
		if(terminal == null)
			return;

		await foreach(var model in models)
		{
			Dump(terminal, model);
		}
	}

	public static void Dump(this CommandContext context, IModel model) => Dump(context.GetTerminal(), model);
	public static void Dump(this ITerminal terminal, IModel model)
	{
		if(terminal == null)
			return;

		terminal.Dump((object)model);
	}
}
