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
using System.Threading.Tasks;
using System.Diagnostics.Metrics;

using BenchmarkDotNet.Attributes;

namespace Zongsoft.Benchmarks;

/// <summary>比较同一实现订阅与未订阅轻量指标时的接收及完整刷新成本。</summary>
[MemoryDiagnoser]
public class SpoolerTelemetryBenchmark
{
	#region 成员字段
	private const string METER_NAME = "Zongsoft.Caching.Spooler";
	private SpoolerAdmissionBenchmark _admission;
	private SpoolerFlushBenchmark _flush;
	private MeterListener _listener;
	private long _measurements;
	#endregion

	#region 公共属性
	[Params(false, true)]
	public bool MetricsEnabled { get; set; }
	#endregion

	#region 基准方法
	[GlobalSetup(Target = nameof(PutAndClearAsync))]
	public async Task SetupAdmissionAsync()
	{
		this.StartListener();
		_admission = new() { Capacity = SpoolerAdmissionBenchmark.RecordCount };
		await _admission.SetupAsync();
		this.ValidateListener();
	}

	[GlobalSetup(Target = nameof(ProduceAndFlushAsync))]
	public async Task SetupFlushAsync()
	{
		this.StartListener();
		_flush = new() { Capacity = 128, Producers = 1, AsynchronousCallback = false };
		await _flush.SetupAsync();
		this.ValidateListener();
	}

	//Same 1,024-item admission workload as SpoolerAdmissionBenchmark, including Clear.
	[Benchmark]
	public ValueTask<int> PutAndClearAsync() => _admission.PutAndClearAsync();

	//Same 4,096-item consumption workload as SpoolerFlushBenchmark, including final flush.
	[Benchmark]
	public Task<long> ProduceAndFlushAsync() => _flush.ProduceAndFlushAsync();

	[GlobalCleanup]
	public async Task CleanupAsync()
	{
		try
		{
			if(_admission != null)
				await _admission.CleanupAsync();
			if(_flush != null)
				await _flush.CleanupAsync();
		}
		finally { _listener?.Dispose(); }
	}
	#endregion

	#region 指标订阅
	private void StartListener()
	{
		if(!this.MetricsEnabled)
			return;

		_listener = new MeterListener
		{
			InstrumentPublished = (instrument, listener) =>
			{
				if(instrument.Meter.Name == METER_NAME)
					listener.EnableMeasurementEvents(instrument);
			},

			//Ensure .NET 8 removes subscriptions when the listener is disposed.
			MeasurementsCompleted = static (_, _) => { },
		};

		_listener.SetMeasurementEventCallback<long>((_, _, _, _) => _measurements++);
		_listener.SetMeasurementEventCallback<double>((_, _, _, _) => _measurements++);
		_listener.Start();
	}

	private void ValidateListener()
	{
		if(_listener == null)
			return;
		if(_measurements == 0)
			throw new InvalidOperationException(global::Zongsoft.Core.Benchmarks.Properties.Resources.Benchmark_RecordNotConsumed_Message);

		//Measure instrumentation/dispatch overhead only, not exporter work or per-item polling.
		_listener.SetMeasurementEventCallback<long>(static (_, _, _, _) => { });
		_listener.SetMeasurementEventCallback<double>(static (_, _, _, _) => { });
	}
	#endregion
}
