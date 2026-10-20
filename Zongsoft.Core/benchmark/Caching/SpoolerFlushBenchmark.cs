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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using BenchmarkDotNet.Attributes;

using Zongsoft.Caching;

namespace Zongsoft.Benchmarks;

/// <summary>测量完整批量处理，包括并发生产、满载刷新以及最终消费完成。</summary>
[MemoryDiagnoser]
public class SpoolerFlushBenchmark
{
	#region 成员字段
	public const int RecordCount = 4_096;
	private Spooler<int> _spooler;
	private Task[] _producers;
	private bool[] _seen;
	private int _consumed;
	private long _checksum;
	#endregion

	#region 公共属性
	[Params(0, 128)]
	public int Capacity { get; set; }

	[Params(1, 4)]
	public int Producers { get; set; }

	[Params(false, true)]
	public bool AsynchronousCallback { get; set; }
	#endregion

	#region 基准方法
	[GlobalSetup]
	public async Task SetupAsync()
	{
		_producers = new Task[this.Producers];
		_spooler = new(this.ConsumeAsync, TimeSpan.FromDays(1), this.Capacity);
		_seen = new bool[RecordCount];
		var checksum = await this.ProduceAndFlushAsync();

		if(_seen.Any(seen => !seen) || _consumed != RecordCount || checksum != (long)RecordCount * (RecordCount - 1) / 2 || !_spooler.IsEmpty)
			throw new InvalidOperationException(global::Zongsoft.Core.Benchmarks.Properties.Resources.Benchmark_RecordNotConsumed_Message);

		_seen = null;
	}

	//One operation consumes all 4,096 items, including producer scheduling and final FlushAsync.
	[Benchmark]
	public async Task<long> ProduceAndFlushAsync()
	{
		_consumed = 0;
		_checksum = 0;

		if(this.Producers == 1)
			await this.ProduceAsync(0, RecordCount);
		else
		{
			for(int index = 0; index < _producers.Length; index++)
			{
				var start = RecordCount * index / _producers.Length;
				var end = RecordCount * (index + 1) / _producers.Length;
				_producers[index] = Task.Run(() => this.ProduceAsync(start, end));
			}

			await Task.WhenAll(_producers);
		}

		//All producers and their full-buffer callbacks have completed; at most one buffer remains.
		await _spooler.FlushAsync();
		return _checksum;
	}

	[GlobalCleanup]
	public async Task CleanupAsync()
	{
		if(_spooler != null)
			await _spooler.DisposeAsync();
	}
	#endregion

	#region 消费方法
	private async Task ProduceAsync(int start, int end)
	{
		for(int index = start; index < end; index++)
			await _spooler.PutAsync(index);
	}

	private ValueTask ConsumeAsync(IEnumerable<int> values, CancellationToken cancellation)
	{
		if(this.AsynchronousCallback)
			return this.ConsumeAsynchronouslyAsync(values);

		this.Consume(values);
		return ValueTask.CompletedTask;
	}

	private async ValueTask ConsumeAsynchronouslyAsync(IEnumerable<int> values)
	{
		//A scheduling boundary, not simulated disk latency, exercises suspended callback ownership.
		await Task.Yield();
		this.Consume(values);
	}

	private void Consume(IEnumerable<int> values)
	{
		foreach(var value in values)
		{
			if(_seen != null)
			{
				if((uint)value >= (uint)_seen.Length || _seen[value])
					throw new InvalidOperationException(string.Format(global::Zongsoft.Core.Benchmarks.Properties.Resources.Benchmark_RecordInvalid_Message, value));

				_seen[value] = true;
			}

			_consumed++;
			_checksum += value;
		}
	}
	#endregion
}
