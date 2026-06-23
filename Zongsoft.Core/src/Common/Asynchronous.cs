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
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading.Tasks;

namespace Zongsoft.Common;

public static class Asynchronous
{
	public static void FireAndForget(this Task task, Action<Exception> onError = null)
	{
		ArgumentNullException.ThrowIfNull(task);

		if(task.IsCompleted)
		{
			if(task.IsFaulted)
				OnError(task.Exception.InnerException ?? task.Exception, onError);

			return;
		}

		_ = AwaitAsync(task, onError);
	}

	public static void FireAndForget(this ValueTask task, Action<Exception> onError = null)
	{
		if(task.IsCompletedSuccessfully || task.IsCanceled)
			return;

		_ = AwaitAsync(task, onError);
	}

	public static void FireAndForget<TResult>(this ValueTask<TResult> task, Action<Exception> onError = null)
	{
		if(task.IsCompletedSuccessfully || task.IsCanceled)
			return;

		_ = AwaitAsync(task, onError);
	}

	private static async Task AwaitAsync(Task task, Action<Exception> onError)
	{
		try
		{
			await task.ConfigureAwait(false);
		}
		catch(OperationCanceledException) { }
		catch(Exception ex)
		{
			OnError(ex, onError);
		}
	}

	private static async Task AwaitAsync(ValueTask task, Action<Exception> onError)
	{
		try
		{
			await task.ConfigureAwait(false);
		}
		catch(OperationCanceledException) { }
		catch(Exception ex)
		{
			OnError(ex, onError);
		}
	}

	private static async Task AwaitAsync<TResult>(ValueTask<TResult> task, Action<Exception> onError)
	{
		try
		{
			await task.ConfigureAwait(false);
		}
		catch(OperationCanceledException) { }
		catch(Exception ex)
		{
			OnError(ex, onError);
		}
	}

	private static void OnError(Exception exception, Action<Exception> onError)
	{
		try { onError?.Invoke(exception); }
		catch { }
	}
}
