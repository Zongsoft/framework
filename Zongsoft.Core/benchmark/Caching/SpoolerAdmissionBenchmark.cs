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
