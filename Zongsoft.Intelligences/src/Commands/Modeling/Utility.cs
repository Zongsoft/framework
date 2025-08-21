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
