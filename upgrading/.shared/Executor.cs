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

namespace Zongsoft.Upgrading;

public sealed partial class Executor
{
	static partial void Initialize();

	public static async ValueTask ExecuteAsync(string @event, object sender, ReleaseEventArgs args, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(@event) || args == null || args.Release == null)
			return;

		//初始化命令集
		Initialize();

		foreach(var descriptor in args.Release.Executors)
		{
			if(string.IsNullOrWhiteSpace(descriptor.Event) || string.IsNullOrWhiteSpace(descriptor.Command))
				continue;

			try
			{
				if(descriptor.Event.EndsWith(@event, StringComparison.OrdinalIgnoreCase))
					await Components.CommandExecutor.Default.ExecuteAsync(descriptor.Command, args, cancellation);
			}
			catch(Exception ex)
			{
				await Zongsoft.Diagnostics.Logging.GetLogging<Executor>().ErrorAsync(ex);
			}
		}
	}
}
