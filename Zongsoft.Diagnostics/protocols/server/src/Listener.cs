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
