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
		try { onError?.Invoke(exception); } catch { }
	}
}
