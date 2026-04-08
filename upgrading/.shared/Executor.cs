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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Upgrading library.
 *
 * The Zongsoft.Upgrading is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Upgrading is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Upgrading library. If not, see <http://www.gnu.org/licenses/>.
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
