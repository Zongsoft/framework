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
using System.Diagnostics;

namespace Zongsoft.Net.Benchmarks;

internal static class ProcessSnapshot
{
	internal static object Capture()
	{
		using var process = Process.GetCurrentProcess();

		return new
		{
			Timestamp = DateTimeOffset.UtcNow,
			CpuMilliseconds = process.TotalProcessorTime.TotalMilliseconds,
			process.WorkingSet64,
			process.PrivateMemorySize64,
			process.HandleCount,
			Threads = process.Threads.Count,
			ManagedHeapBytes = GC.GetTotalMemory(false),
			TotalAllocatedBytes = GC.GetTotalAllocatedBytes(false),
			Gen0 = GC.CollectionCount(0),
			Gen1 = GC.CollectionCount(1),
			Gen2 = GC.CollectionCount(2),
			GcPauseMilliseconds = GC.GetTotalPauseDuration().TotalMilliseconds,
			Monitor.LockContentionCount,
			ThreadPool.ThreadCount,
			ThreadPool.PendingWorkItemCount,
			ThreadPool.CompletedWorkItemCount,
		};
	}
}
