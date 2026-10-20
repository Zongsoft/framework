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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using BenchmarkDotNet.Attributes;

using Zongsoft.Caching;

namespace Zongsoft.Benchmarks;

/// <summary>测量有界及无界缓冲的快速接收路径；每次操作包含一批投递和清空。</summary>
[MemoryDiagnoser]
public class SpoolerAdmissionBenchmark
{
	public const int RecordCount = 1_024;
	private Spooler<int> _spooler;
	private int _callbacks;

	[Params(0, RecordCount)]
	public int Capacity { get; set; }

	[GlobalSetup]
	public async Task SetupAsync()
	{
		_spooler = new(this.ConsumeAsync, TimeSpan.FromDays(1), this.Capacity);
		var admitted = await this.PutAndClearAsync();

		if(admitted != RecordCount || !_spooler.IsEmpty || _callbacks != 0)
			throw new InvalidOperationException(global::Zongsoft.Core.Benchmarks.Properties.Resources.Benchmark_WorkloadNotDrained_Message);
	}

	//One operation is 1,024 accepted items plus Clear; it does not measure isolated PutAsync latency.
	[Benchmark]
	public async ValueTask<int> PutAndClearAsync()
	{
		for(int index = 0; index < RecordCount; index++)
			await _spooler.PutAsync(index);

		var count = _spooler.Count;
		_spooler.Clear();
		return count;
	}

	[GlobalCleanup]
	public async Task CleanupAsync()
	{
		if(_spooler != null)
			await _spooler.DisposeAsync();
	}

	private ValueTask ConsumeAsync(IEnumerable<int> values, CancellationToken cancellation)
	{
		_callbacks++;

		foreach(var value in values)
		{
		}

		return ValueTask.CompletedTask;
	}
}
