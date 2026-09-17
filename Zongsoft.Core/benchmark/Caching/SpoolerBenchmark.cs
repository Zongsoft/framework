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
	protected override bool WritesFile => false;
}

// A full file workload can take seconds; allow the pilot to choose one invocation.
[MinInvokeCount(1)]
public class SpoolerFileBenchmark : SpoolerBenchmarkBase
{
	protected override bool WritesFile => true;
}

[MemoryDiagnoser]
public abstract class SpoolerBenchmarkBase
{
	public const int RecordCount = 10_000;
	private const int RecordSize = 128;
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

	[Params(100, 1_000)]
	public int BatchSize { get; set; }

	[Params(1, 4)]
	public int Producers { get; set; }

	protected abstract bool WritesFile { get; }

	[GlobalSetup]
	public async Task SetupAsync()
	{
		_batch = new int[this.BatchSize];
		_producers = new Task[this.Producers];
		_records = new byte[RecordCount][];
		for(int index = 0; index < _records.Length; index++)
			_records[index] = Encoding.ASCII.GetBytes(index.ToString("D8") + new string('x', RecordSize - 10) + "\r\n");

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
					throw new InvalidOperationException("A record was not consumed.");
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

	private async Task<long> RunAsync(int method, int count)
	{
		if(_batchCount != 0 || !_spooler.IsEmpty)
			throw new InvalidOperationException("The previous workload was not drained.");

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
			throw new InvalidOperationException($"Consumption mismatch: {_consumed}/{count}, checksum={_checksum}.");
		var expectedCalls = method == 0 ? count : (count + this.BatchSize - 1) / this.BatchSize;
		// Competing bounded writers can trigger partially filled Spooler batches.
		if(method == 2 ? _calls < expectedCalls || _calls > count : _calls != expectedCalls)
			throw new InvalidOperationException($"Unexpected callback count: {_calls}/{expectedCalls}.");
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
				throw new InvalidOperationException($"Invalid or duplicate record: {value}.");
			_seen[value] = true;
		}
		_consumed++;
		_checksum += value;
		return stream == null ? ValueTask.CompletedTask : stream.WriteAsync(_records[value].AsMemory());
	}

	private void ValidateFile(int count)
	{
		var bytes = File.ReadAllBytes(_path);
		if(bytes.Length != count * RecordSize)
			throw new InvalidOperationException("Incorrect output file length.");
		var seen = new bool[count];
		for(int offset = 0; offset < bytes.Length; offset += RecordSize)
		{
			var id = int.Parse(Encoding.ASCII.GetString(bytes, offset, 8));
			if((uint)id >= (uint)count || seen[id] || !bytes.AsSpan(offset, RecordSize).SequenceEqual(_records[id]))
				throw new InvalidOperationException("Incorrect output file contents.");
			seen[id] = true;
		}
	}

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
}
