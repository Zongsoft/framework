using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics.Metrics;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Xunit;

namespace Zongsoft.Caching.Tests;

[CollectionDefinition(nameof(SpoolerDiagnosticsCollection), DisableParallelization = true)]
public sealed class SpoolerDiagnosticsCollection { }

[Collection(nameof(SpoolerDiagnosticsCollection))]
public class SpoolerDiagnosticsTest
{
	[Fact]
	public async Task Queue_LateSubscription_TracksConsumptionClearAndClosure()
	{
		var clock = new SpoolerTimeProvider();
		var values = new List<int>();
		await using var first = new Spooler<int>((items, _) =>
		{
			values.AddRange(items);
			return ValueTask.CompletedTask;
		}, TimeSpan.FromDays(1), clock: clock);
		await using var second = new Spooler<string>((_, _) => ValueTask.CompletedTask, TimeSpan.FromDays(1), clock: clock);
		await first.PutAsync(10, TestContext.Current.CancellationToken);
		await first.PutAsync(20, TestContext.Current.CancellationToken);
		await second.PutAsync("A", TestContext.Current.CancellationToken);
		using var measurements = new Measurements();

		Assert.Equal(3, measurements.Queued());
		Assert.Empty(measurements.Find(Measurements.ACCEPTED));

		await first.FlushAsync(TestContext.Current.CancellationToken);

		Assert.Equal(new[] { 10, 20 }, values);
		Assert.Equal(1, measurements.Queued());

		second.Clear();

		Assert.Equal(0, measurements.Queued());

		await first.PutAsync(30, TestContext.Current.CancellationToken);
		await second.PutAsync("B", TestContext.Current.CancellationToken);
		await second.PutAsync("C", TestContext.Current.CancellationToken);

		Assert.Equal(3, measurements.Queued());

		await first.DisposeAsync();

		Assert.Equal(2, measurements.Queued());

		await first.DisposeAsync();
		await second.DisposeAsync();

		Assert.Equal(0, measurements.Queued());
		Assert.Equal(3, measurements.Find(Measurements.ACCEPTED).Sum(sample => sample.Value));
		Assert.All(measurements.Find(Measurements.ACCEPTED), sample =>
		{
			Assert.IsType<Counter<long>>(sample.Instrument);
			Assert.Equal("{item}", sample.Instrument.Unit);
			Assert.Empty(sample.Tags);
		});
	}

	[Fact]
	public async Task PutAsync_RejectedWrites_AreNotCountedAsAccepted()
	{
		using var measurements = new Measurements();
		var clock = new SpoolerTimeProvider();
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		await using var spooler = new Spooler<int>((_, _) => ValueTask.CompletedTask, TimeSpan.FromDays(1), 1, clock);
		await cancellation.CancelAsync();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => spooler.PutAsync(10, cancellation.Token).AsTask());
		await spooler.PutAsync(20, TestContext.Current.CancellationToken);
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => spooler.PutAsync(30, cancellation.Token).AsTask());
		await spooler.DisposeAsync();
		await Assert.ThrowsAsync<ObjectDisposedException>(() => spooler.PutAsync(40).AsTask());

		Assert.Equal(1, Assert.Single(measurements.Find(Measurements.ACCEPTED)).Value);
		Assert.Empty(measurements.Find(Measurements.WAIT));
		Assert.Empty(measurements.Find(Measurements.FLUSH));
		Assert.Empty(measurements.Find(Measurements.FAILURES));
	}

	[Theory]
	[InlineData("explicit", "success")]
	[InlineData("explicit", "canceled")]
	[InlineData("explicit", "error")]
	[InlineData("capacity", "success")]
	[InlineData("capacity", "canceled")]
	[InlineData("capacity", "error")]
	[InlineData("periodic", "success")]
	[InlineData("periodic", "canceled")]
	[InlineData("periodic", "error")]
	public async Task Flush_RecordsTriggerAndOutcome(string trigger, string outcome)
	{
		using var measurements = new Measurements();
		var clock = new SpoolerTimeProvider();
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var failure = new ApplicationException("Consumer failure.");
		var values = new List<int>();
		var spooler = new Spooler<int>(async (items, token) =>
		{
			values.AddRange(items);
			entered.TrySetResult();
			await release.Task;

			if(outcome == "error")
				throw failure;
			if(outcome == "canceled")
				token.ThrowIfCancellationRequested();
		}, TimeSpan.FromDays(1), 1, clock);

		try
		{
			await spooler.PutAsync(42, TestContext.Current.CancellationToken);
			Task operation = trigger switch
			{
				"explicit" => spooler.FlushAsync(cancellation.Token).AsTask(),
				"capacity" => spooler.PutAsync(99, cancellation.Token).AsTask(),
				_ => null,
			};

			if(trigger == "periodic")
				clock.Advance(spooler.Period);

			try
			{
				await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

				Assert.Empty(measurements.Find(Measurements.FLUSH));
				Assert.Empty(measurements.Find(Measurements.FAILURES));

				if(trigger == "periodic")
					operation = spooler.DisposeAsync().AsTask();
				else if(outcome == "canceled")
					await cancellation.CancelAsync();
			}
			finally { release.TrySetResult(); }

			if(outcome == "error")
				Assert.Same(failure, await Assert.ThrowsAsync<ApplicationException>(() => operation.WaitAsync(TimeSpan.FromSeconds(5))));
			else if(outcome == "canceled" && trigger != "periodic")
				await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation.WaitAsync(TimeSpan.FromSeconds(5)));
			else
				await operation.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

			if(trigger != "periodic")
				await spooler.DisposeAsync();

			var flush = Assert.Single(measurements.Find(Measurements.FLUSH));
			Assert.IsType<Histogram<double>>(flush.Instrument);
			Assert.Equal("s", flush.Instrument.Unit);
			Assert.True(flush.Value >= 0);
			Assert.Equal(2, flush.Tags.Length);
			Assert.Equal(trigger, flush.Tag("zongsoft.spooler.trigger"));
			Assert.Equal(outcome, flush.Tag("zongsoft.spooler.outcome"));
			Assert.Equal(new[] { 42 }, values);
			Assert.Equal(trigger == "capacity" && outcome == "success" ? 2 : 1, measurements.Find(Measurements.ACCEPTED).Sum(sample => sample.Value));
			Assert.Equal(0, measurements.Queued());

			if(trigger == "capacity")
			{
				var wait = Assert.Single(measurements.Find(Measurements.WAIT));
				Assert.IsType<Histogram<double>>(wait.Instrument);
				Assert.Equal("s", wait.Instrument.Unit);
				Assert.True(wait.Value >= 0);
			}
			else
				Assert.Empty(measurements.Find(Measurements.WAIT));

			if(outcome == "error")
			{
				var failed = Assert.Single(measurements.Find(Measurements.FAILURES));
				Assert.IsType<Counter<long>>(failed.Instrument);
				Assert.Equal("{failure}", failed.Instrument.Unit);
				Assert.Equal(1, failed.Value);
				Assert.Single(failed.Tags);
				Assert.Equal(trigger, failed.Tag("zongsoft.spooler.trigger"));
			}
			else
				Assert.Empty(measurements.Find(Measurements.FAILURES));
		}
		finally
		{
			release.TrySetResult();
			await Record.ExceptionAsync(() => spooler.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5)));
		}
	}

	[Fact]
	public async Task Flush_UnrelatedCancellation_IsCountedAsFailure()
	{
		using var measurements = new Measurements();
		var clock = new SpoolerTimeProvider();
		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();
		var failure = new OperationCanceledException(cancellation.Token);
		await using var spooler = new Spooler<int>((_, _) => ValueTask.FromException(failure), TimeSpan.FromDays(1), clock: clock);
		await spooler.PutAsync(42, TestContext.Current.CancellationToken);

		Assert.Same(failure, await Assert.ThrowsAnyAsync<OperationCanceledException>(() => spooler.FlushAsync(TestContext.Current.CancellationToken).AsTask()));
		await spooler.DisposeAsync();

		var flush = Assert.Single(measurements.Find(Measurements.FLUSH));
		Assert.Equal("error", flush.Tag("zongsoft.spooler.outcome"));
		var failed = Assert.Single(measurements.Find(Measurements.FAILURES));
		Assert.Equal(1, failed.Value);
		Assert.Equal("explicit", failed.Tag("zongsoft.spooler.trigger"));
	}

	[Fact]
	public async Task MeasurementCallbackThrows_DoesNotChangeBusinessResults()
	{
		using var measurements = new Measurements(true);
		var clock = new SpoolerTimeProvider();
		var failure = new ApplicationException("Consumer failure.");
		var calls = 0;
		var values = new List<int>();
		await using var spooler = new Spooler<int>((items, _) =>
		{
			values.AddRange(items);
			if(++calls > 1)
				throw failure;

			return ValueTask.CompletedTask;
		}, TimeSpan.FromDays(1), 1, clock);
		await spooler.PutAsync(10, TestContext.Current.CancellationToken);
		await spooler.FlushAsync(TestContext.Current.CancellationToken);
		await spooler.PutAsync(20, TestContext.Current.CancellationToken);

		Assert.Same(failure, await Assert.ThrowsAsync<ApplicationException>(() => spooler.PutAsync(30, TestContext.Current.CancellationToken).AsTask()));
		await spooler.DisposeAsync();

		Assert.Equal(new[] { 10, 20 }, values);
		Assert.Equal(2, calls);
		Assert.Equal(2, measurements.Find(Measurements.ACCEPTED).Sum(sample => sample.Value));
		Assert.Equal(2, measurements.Find(Measurements.FLUSH).Count());
		Assert.Equal(1, Assert.Single(measurements.Find(Measurements.FAILURES)).Value);
		Assert.Single(measurements.Find(Measurements.WAIT));
	}

	private sealed class Measurements : IDisposable
	{
		public const string ACCEPTED = "zongsoft.caching.spooler.items.accepted";
		public const string QUEUED = "zongsoft.caching.spooler.items.queued";
		public const string FLUSH = "zongsoft.caching.spooler.flush.duration";
		public const string WAIT = "zongsoft.caching.spooler.put.wait.duration";
		public const string FAILURES = "zongsoft.caching.spooler.failures";

		private readonly MeterListener _listener = new();
		private readonly ConcurrentQueue<Sample> _samples = new();
		private readonly ConcurrentQueue<Sample> _observations = new();

		public Measurements(bool throws = false)
		{
			//确保 .NET 8 释放监听器时解除同步仪表订阅。
			_listener.MeasurementsCompleted = static (_, _) => { };
			_listener.InstrumentPublished = (instrument, listener) =>
			{
				if(instrument.Meter.Name == "Zongsoft.Caching.Spooler")
					listener.EnableMeasurementEvents(instrument);
			};
			_listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) =>
			{
				var sample = new Sample(instrument, value, tags.ToArray());
				if(instrument.IsObservable)
					_observations.Enqueue(sample);
				else
					_samples.Enqueue(sample);

				if(throws)
					throw new InvalidOperationException("Measurement listener failure.");
			});
			_listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) =>
			{
				_samples.Enqueue(new(instrument, value, tags.ToArray()));

				if(throws)
					throw new InvalidOperationException("Measurement listener failure.");
			});
			_listener.Start();
		}

		public double Queued()
		{
			_observations.Clear();
			_listener.RecordObservableInstruments();
			var sample = Assert.Single(_observations, sample => sample.Instrument.Name == QUEUED);
			Assert.IsType<ObservableGauge<long>>(sample.Instrument);
			Assert.Equal("{item}", sample.Instrument.Unit);
			Assert.Empty(sample.Tags);
			return sample.Value;
		}

		public IEnumerable<Sample> Find(string name) => _samples.Where(sample => sample.Instrument.Name == name);
		public void Dispose() => _listener.Dispose();

		public sealed record Sample(Instrument Instrument, double Value, KeyValuePair<string, object>[] Tags)
		{
			public object Tag(string key) => this.Tags.FirstOrDefault(tag => tag.Key == key).Value;
		}
	}
}
