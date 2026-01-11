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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Diagnostics.Protocols.Server library.
 *
 * The Zongsoft.Diagnostics.Protocols.Server is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Diagnostics.Protocols.Server is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Diagnostics.Protocols.Server library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Diagnostics.Protocols.Server;

public static partial class Listener
{
	private static Task HandleAsync(ICollection<IHandler> handlers, object argument, Parameters parameters, CancellationToken cancellation = default)
	{
		if(handlers == null || handlers.Count == 0)
			return Task.CompletedTask;

		return Parallel.ForEachAsync(handlers, cancellation, async (handler, cancellation) =>
		{
			if(handler == null)
				return;

			try
			{
				await handler.HandleAsync(argument, parameters, cancellation);
			}
			catch(Exception ex)
			{
				Logging.GetLogging(handler).Error(ex);
			}
		});
	}

	private static DateTimeOffset GetTimestamp(ulong timeUnixNano) => DateTimeOffset.FromUnixTimeMilliseconds((long)(timeUnixNano / 1_000_000));
}
