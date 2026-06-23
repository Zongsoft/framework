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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Messaging.ZeroMQ library.
 *
 * The Zongsoft.Messaging.ZeroMQ is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Messaging.ZeroMQ is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Messaging.ZeroMQ library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Messaging.ZeroMQ;

internal static class ZeroUtility
{
	public static string GetTcpAddress(string server, ushort port) => port == 0 ? $"tcp://{server}" : $"tcp://{server}:{port}";
	public static string GetTcpAddress(string server, string port) => string.IsNullOrEmpty(port) ? $"tcp://{server}" : $"tcp://{server}:{port}";

	public static void FireAndForget<TArgument>(this Components.IHandler<TArgument> handler, TArgument argument, CancellationToken cancellation = default) => FireAndForget(handler, argument, null, cancellation);
	public static void FireAndForget<TArgument>(this Components.IHandler<TArgument> handler, TArgument argument, Collections.Parameters parameters, CancellationToken cancellation = default)
	{
		_ = Task.Run(async () =>
		{
			try
			{
				await handler.HandleAsync(argument, parameters, cancellation).ConfigureAwait(false);
			}
			catch(OperationCanceledException) when(cancellation.IsCancellationRequested) { }
			catch(Exception ex) { Diagnostics.Logging.GetLogging(handler).Error(ex); }
		}, CancellationToken.None);
	}
}
