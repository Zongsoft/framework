using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using BenchmarkDotNet.Attributes;

using Zongsoft.Caching;

namespace Zongsoft.Benchmarks;

public class SpoolerMemoryBenchmark : SpoolerBenchmarkBase
{
	#region 重写属性
	protected override bool WritesFile => false;
	#endregion
}

// A full file workload can take seconds; allow the pilot to choose one invocation.
[MinInvokeCount(1)]
public class SpoolerFileBenchmark : SpoolerBenchmarkBase
{
	#region 重写属性
	protected override bool WritesFile => true;
	#endregion
}

[MemoryDiagnoser]
public abstract class SpoolerBenchmarkBase
{
	public const int RecordCount = 10_000;
	private const int RECORD_SIZE = 128;
	private readonly SemaphoreSlim _batchGate = new(1, 1);
	private readonly SemaphoreSlim _sinkGate = new(1, 1);
	private Spooler<int> _spooler;
	private int[] _batch;
	private int _batchCount;
	private byte[][] _records;
	private Task[] _producers;
	private Func<int, ValueTask>[] _writers;
	private string _directory;
	private string _path;
	private bool[] _seen;
	private int _consumed;
	private long _checksum;
	private int _calls;
	private int _minimumCalls;
	private int _maximumCalls;
	private long _totalCalls;
	private long _runs;

	#region 公共属性
	[Params(100, 1_000)]
	public int BatchSize { get; set; }

	[Params(1, 4)]
	public int Producers { get; set; }
	#endregion

	#region 保护属性
	protected abstract bool WritesFile { get; }
	#endregion

	#region 公共方法
	[GlobalSetup]
	public async Task SetupAsync()
	{
		_batch = new int[this.BatchSize];
		_producers = new Task[this.Producers];
		_records = new byte[RecordCount][];

		for(int index = 0; index < _records.Length; index++)
			_records[index] = Encoding.ASCII.GetBytes(index.ToString("D8") + new string('x', RECORD_SIZE - 10) + "\r\n");

		if(this.WritesFile)
		{
			_directory = Path.Combine(Path.GetTempPath(), "Zongsoft-SpoolerBenchmark-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(_directory);
			_path = Path.Combine(_directory, "records.log");
		}

		_spooler = new Spooler<int>(this.ConsumeBatchAsync, TimeSpan.FromDays(1), this.BatchSize);
		_writers = [this.ConsumeSingleAsync, this.PutBatchAsync, value => _spooler.PutAsync(value)];

		// Exact identity checks and file reads run only during setup, never in timed iterations.
		foreach(var count in new[] { 0, 1, this.BatchSize, this.BatchSize + 1, this.BatchSize * 2 + 3, RecordCount }.Distinct())
		{
			for(int method = 0; method < _writers.Length; method++)
			{
				_seen = new bool[count];
				await this.RunAsync(method, count);

				if(_seen.Any(seen => !seen))
					throw new InvalidOperationException(global::Zongsoft.Core.Benchmarks.Properties.Resources.Benchmark_RecordNotConsumed_Message);

				if(this.WritesFile)
					this.ValidateFile(count);

				if(count == RecordCount)
					Console.WriteLine($"VALIDATED {this.GetType().Name} Method={method} BatchSize={this.BatchSize} Producers={this.Producers} Records={_consumed} Calls={_calls} Checksum={_checksum}");
			}
		}
		_seen = null;
		_minimumCalls = int.MaxValue;
		_maximumCalls = 0;
		_totalCalls = 0;
		_runs = 0;
	}

	// One BDN operation is one complete workload, including final consumption, not one enqueue.
	[Benchmark(Baseline = true)]
	public Task<long> Direct() => this.RunAsync(0, RecordCount);

	[Benchmark]
	public Task<long> ManualBatch() => this.RunAsync(1, RecordCount);

	[Benchmark]
	public Task<long> Spooler() => this.RunAsync(2, RecordCount);
	#endregion

	#region 私有方法
	private async Task<long> RunAsync(int method, int count)
	{
		if(_batchCount != 0 || !_spooler.IsEmpty)
			throw new InvalidOperationException(global::Zongsoft.Core.Benchmarks.Properties.Resources.Benchmark_WorkloadNotDrained_Message);

		_consumed = 0;
		_checksum = 0;
		_calls = 0;

		if(this.WritesFile)
		{
			// Identical bounded-file reset cost is included for all three approaches.
			using var stream = new FileStream(_path, FileMode.Create, FileAccess.Write, FileShare.Read);
		}

		var writer = _writers[method];

		if(this.Producers == 1)
			await ProduceAsync(0, count, writer);
		else
		{
			for(int index = 0; index < _producers.Length; index++)
			{
				var start = count * index / _producers.Length;
				var end = count * (index + 1) / _producers.Length;
				_producers[index] = Task.Run(() => ProduceAsync(start, end, writer));
			}

			await Task.WhenAll(_producers);
		}

		if(method == 1 && _batchCount > 0)
		{
			await this.ConsumeBatchAsync(new ArraySegment<int>(_batch, 0, _batchCount));
			_batchCount = 0;
		}
		else if(method == 2)
		{
			while(!_spooler.IsEmpty)
				await _spooler.FlushAsync();
		}

		// All producer-triggered callbacks have completed before this final drain.
		if(_consumed != count || _checksum != (long)count * (count - 1) / 2)
			throw new InvalidOperationException(string.Format(global::Zongsoft.Core.Benchmarks.Properties.Resources.Benchmark_ConsumptionMismatch_Message, _consumed, count, _checksum));

		var expectedCalls = method == 0 ? count : (count + this.BatchSize - 1) / this.BatchSize;
		// Competing bounded writers can trigger partially filled Spooler batches.
		if(method == 2 ? _calls < expectedCalls || _calls > count : _calls != expectedCalls)
			throw new InvalidOperationException(string.Format(global::Zongsoft.Core.Benchmarks.Properties.Resources.Benchmark_CallbackCountMismatch_Message, _calls, expectedCalls));

		_minimumCalls = Math.Min(_minimumCalls, _calls);
		_maximumCalls = Math.Max(_maximumCalls, _calls);
		_totalCalls += _calls;
		_runs++;

		return _checksum;
	}

	private static async Task ProduceAsync(int start, int end, Func<int, ValueTask> writer)
	{
		for(int index = start; index < end; index++)
			await writer(index);
	}

	private async ValueTask PutBatchAsync(int value)
	{
		await _batchGate.WaitAsync();

		try
		{
			_batch[_batchCount++] = value;
			if(_batchCount == _batch.Length)
			{
				await this.ConsumeBatchAsync(new ArraySegment<int>(_batch, 0, _batchCount));
				_batchCount = 0;
			}
		}
		finally
		{
			_batchGate.Release();
		}
	}

	private async ValueTask ConsumeSingleAsync(int value)
	{
		await _sinkGate.WaitAsync();

		try
		{
			_calls++;
			using var stream = this.OpenFile();
			await this.ConsumeRecordAsync(value, stream);
		}
		finally
		{
			_sinkGate.Release();
		}
	}

	private async ValueTask ConsumeBatchAsync(IEnumerable<int> values, CancellationToken cancellation = default)
	{
		await _sinkGate.WaitAsync(cancellation);

		try
		{
			_calls++;
			using var stream = this.OpenFile();

			foreach(var value in values)
				await this.ConsumeRecordAsync(value, stream);
		}
		finally
		{
			_sinkGate.Release();
		}
	}

	private FileStream OpenFile() => this.WritesFile ? new FileStream(_path, FileMode.Append, FileAccess.Write, FileShare.Read) : null;

	private ValueTask ConsumeRecordAsync(int value, FileStream stream)
	{
		if(_seen != null)
		{
			if((uint)value >= (uint)_seen.Length || _seen[value])
				throw new InvalidOperationException(string.Format(global::Zongsoft.Core.Benchmarks.Properties.Resources.Benchmark_RecordInvalid_Message, value));
			_seen[value] = true;
		}
		_consumed++;
		_checksum += value;
		return stream == null ? ValueTask.CompletedTask : stream.WriteAsync(_records[value].AsMemory());
	}

	private void ValidateFile(int count)
	{
		var bytes = File.ReadAllBytes(_path);
		if(bytes.Length != count * RECORD_SIZE)
			throw new InvalidOperationException(global::Zongsoft.Core.Benchmarks.Properties.Resources.Benchmark_OutputLengthInvalid_Message);

		var seen = new bool[count];
		for(int offset = 0; offset < bytes.Length; offset += RECORD_SIZE)
		{
			var id = int.Parse(Encoding.ASCII.GetString(bytes, offset, 8));
			if((uint)id >= (uint)count || seen[id] || !bytes.AsSpan(offset, RECORD_SIZE).SequenceEqual(_records[id]))
				throw new InvalidOperationException(global::Zongsoft.Core.Benchmarks.Properties.Resources.Benchmark_OutputContentsInvalid_Message);

			seen[id] = true;
		}
	}
	#endregion

	#region 公共方法
	[GlobalCleanup]
	public void Cleanup()
	{
		if(_runs > 0)
			Console.WriteLine(FormattableString.Invariant($"CALLBACKS Runs={_runs} Min={_minimumCalls} Max={_maximumCalls} Mean={(double)_totalCalls / _runs:F4}"));

		_spooler?.Dispose();
		_batchGate.Dispose();
		_sinkGate.Dispose();

		if(_path != null && File.Exists(_path))
			File.Delete(_path);
		if(_directory != null && Directory.Exists(_directory))
			Directory.Delete(_directory);
	}
	#endregion
}
