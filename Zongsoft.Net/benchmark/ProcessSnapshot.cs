/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Net library.
 *
 * The Zongsoft.Net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Net library. If not, see <http://www.gnu.org/licenses/>.
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
