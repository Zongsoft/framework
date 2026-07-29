using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.Tests;

public class DataConnectionCircuitBreakerTest
{
	[Fact]
	public void TestFailureOpensAndFastRejectionsDoNotRepeatEvents()
	{
		var timeProvider = new ManualTimeProvider();
		var source = new DataSourceMocker();
		var manager = new DataConnectionCircuitBreakerManager(timeProvider);
		var attempts = 0;
		var events = new List<DataConnectionCircuitBreakerStateChangedEventArgs>();
		manager.StateChanged += (_, args) => events.Add(args);

		Assert.Throws<InvalidOperationException>(() => manager.Execute(source, () =>
		{
			attempts++;
			throw new InvalidOperationException("Database unavailable.");
		}));

		var breaker = manager.GetBreaker(source);
		Assert.NotNull(breaker);
		Assert.Equal(DataConnectionCircuitBreakerState.Opened, breaker.State);
		Assert.Equal(1, attempts);

		for(int index = 0; index < 10; index++)
		{
			var exception = Assert.Throws<DataConnectionUnavailableException>(() =>
				manager.Execute(source, () => attempts++));

			Assert.Equal(source.Name, exception.SourceName);
			Assert.Equal(source.Driver.Name, exception.DriverName);
			Assert.Equal(DataConnectionCircuitBreakerState.Opened, exception.State);
			Assert.Equal(TimeSpan.FromSeconds(1), exception.RetryAfter);
		}

		Assert.Equal(1, attempts);
		var stateChanged = Assert.Single(events);
		Assert.Equal(DataConnectionCircuitBreakerState.Closed, stateChanged.OriginalState);
		Assert.Equal(DataConnectionCircuitBreakerState.Opened, stateChanged.CurrentState);
	}

	[Fact]
	public async Task TestHighFrequencyRequestsAreRejectedWithoutReconnect()
	{
		var timeProvider = new ManualTimeProvider();
		var source = new DataSourceMocker();
		var manager = new DataConnectionCircuitBreakerManager(timeProvider);
		var attempts = 0;
		var transitions = 0;
		manager.StateChanged += (_, _) => Interlocked.Increment(ref transitions);

		Assert.Throws<InvalidOperationException>(() =>
			manager.Execute(source, () =>
			{
				Interlocked.Increment(ref attempts);
				throw new InvalidOperationException();
			}));

		var requests = new Task[256];

		for(int index = 0; index < requests.Length; index++)
		{
			requests[index] = Task.Run(() =>
				Assert.Throws<DataConnectionUnavailableException>(() =>
					manager.Execute(source, () => Interlocked.Increment(ref attempts))));
		}

		await Task.WhenAll(requests);

		Assert.Equal(1, attempts);
		Assert.Equal(1, transitions);
		Assert.Equal(DataConnectionCircuitBreakerState.Opened, manager.GetBreaker(source).State);
	}

	[Fact]
	public async Task TestHalfOpenAllowsSingleProbeAndRecovers()
	{
		var timeProvider = new ManualTimeProvider();
		var source = new DataSourceMocker();
		var manager = new DataConnectionCircuitBreakerManager(timeProvider);
		var states = new List<DataConnectionCircuitBreakerState>();
		manager.StateChanged += (_, args) => states.Add(args.CurrentState);

		Assert.Throws<InvalidOperationException>(() =>
			manager.Execute(source, () => throw new InvalidOperationException()));

		timeProvider.Advance(TimeSpan.FromSeconds(1));

		using var entered = new ManualResetEventSlim();
		using var release = new ManualResetEventSlim();
		var probe = Task.Run(() => manager.Execute(source, () =>
		{
			entered.Set();
			release.Wait();
		}));

		Assert.True(entered.Wait(TimeSpan.FromSeconds(5)));
		Assert.Equal(DataConnectionCircuitBreakerState.HalfOpen, manager.GetBreaker(source).State);
		Assert.Throws<DataConnectionUnavailableException>(() => manager.Execute(source, () => { }));

		release.Set();
		await probe;

		Assert.Equal(DataConnectionCircuitBreakerState.Closed, manager.GetBreaker(source).State);
		Assert.Equal(
			[
				DataConnectionCircuitBreakerState.Opened,
				DataConnectionCircuitBreakerState.HalfOpen,
				DataConnectionCircuitBreakerState.Closed,
			],
			states);
	}

	[Fact]
	public void TestRepeatedProbeFailuresBackOffExponentially()
	{
		var timeProvider = new ManualTimeProvider();
		var source = new DataSourceMocker();
		var manager = new DataConnectionCircuitBreakerManager(timeProvider);

		Assert.Throws<InvalidOperationException>(() =>
			manager.Execute(source, () => throw new InvalidOperationException()));

		var breaker = manager.GetBreaker(source);
		Assert.Equal(timeProvider.GetUtcNow().AddSeconds(1), breaker.RetryAt);

		timeProvider.Advance(TimeSpan.FromSeconds(1));
		Assert.Throws<InvalidOperationException>(() =>
			manager.Execute(source, () => throw new InvalidOperationException()));
		Assert.Equal(timeProvider.GetUtcNow().AddSeconds(2), breaker.RetryAt);

		timeProvider.Advance(TimeSpan.FromSeconds(2));
		manager.Execute(source, () => { });

		Assert.Equal(DataConnectionCircuitBreakerState.Closed, breaker.State);
		Assert.Null(breaker.RetryAt);
	}

	[Fact]
	public async Task TestCallerCancellationDoesNotOpenCircuit()
	{
		var timeProvider = new ManualTimeProvider();
		var source = new DataSourceMocker();
		var manager = new DataConnectionCircuitBreakerManager(timeProvider);
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			manager.ExecuteAsync(
				source,
				token => Task.FromCanceled(token),
				cancellation.Token));

		Assert.Equal(DataConnectionCircuitBreakerState.Closed, manager.GetBreaker(source).State);
	}

	[Fact]
	public async Task TestCanceledProbeReturnsCircuitToOpen()
	{
		var timeProvider = new ManualTimeProvider();
		var source = new DataSourceMocker();
		var manager = new DataConnectionCircuitBreakerManager(timeProvider);

		Assert.Throws<InvalidOperationException>(() =>
			manager.Execute(source, () => throw new InvalidOperationException()));

		timeProvider.Advance(TimeSpan.FromSeconds(1));
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			manager.ExecuteAsync(
				source,
				token => Task.FromCanceled(token),
				cancellation.Token));

		var breaker = manager.GetBreaker(source);
		Assert.Equal(DataConnectionCircuitBreakerState.Opened, breaker.State);
		Assert.Equal(timeProvider.GetUtcNow().AddSeconds(1), breaker.RetryAt);
		Assert.Throws<DataConnectionUnavailableException>(() => manager.Execute(source, () => { }));
	}

	[Fact]
	public async Task TestEnabledControl()
	{
		var timeProvider = new ManualTimeProvider();
		var manager = new DataConnectionCircuitBreakerManager(timeProvider);
		var interfaceManager = Assert.IsAssignableFrom<ICircuitBreakerManager>(manager);
		var source = new DataSourceMocker();
		var breaker = manager.GetBreaker(source);

		Assert.True(manager.Enabled);
		Assert.NotNull(breaker);
		Assert.Same(breaker, interfaceManager.GetBreaker(source));

		Assert.Throws<InvalidOperationException>(() =>
			manager.Execute(source, () => throw new InvalidOperationException()));
		Assert.Equal(DataConnectionCircuitBreakerState.Opened, breaker.State);

		manager.Enabled = false;

		Assert.False(manager.Enabled);
		Assert.Same(breaker, manager.GetBreaker(source));
		Assert.Equal(DataConnectionCircuitBreakerState.Closed, breaker.State);
		Assert.Equal(100, manager.Execute(source, () => 100));
		Assert.Equal(200, await manager.ExecuteAsync(source, _ => Task.FromResult(200)));

		manager.Enabled = true;

		Assert.True(manager.Enabled);
		Assert.Same(breaker, manager.GetBreaker(source));
	}

	[Fact]
	public void TestDriverAndSessionsShareCircuitBreaker()
	{
		var source = new DataSourceMocker();
		var driver = Assert.IsType<DataDriverMocker>(source.Driver);
		var interfaceDriver = Assert.IsAssignableFrom<IDataDriver>(driver);
		using var first = new DataSession(source);
		using var second = new DataSession(source);

		Assert.Same(driver.CircuitBreaker, interfaceDriver.CircuitBreaker);
		Assert.Same(driver.CircuitBreaker.GetBreaker(source), first.CircuitBreaker);
		Assert.Same(first.CircuitBreaker, second.CircuitBreaker);
	}

	[Fact]
	public void TestDriverCircuitBreakerIsolatesDataSources()
	{
		var driver = new DataDriverMocker();
		var first = new DataSourceMocker(driver);
		var second = new DataSourceMocker(driver);
		var firstBreaker = driver.CircuitBreaker.GetBreaker(first);
		var secondBreaker = driver.CircuitBreaker.GetBreaker(second);
		var attempts = 0;

		Assert.NotSame(firstBreaker, secondBreaker);
		Assert.Throws<InvalidOperationException>(() =>
			firstBreaker.Execute(() => throw new InvalidOperationException()));

		secondBreaker.Execute(() => attempts++);

		Assert.Equal(DataConnectionCircuitBreakerState.Opened, firstBreaker.State);
		Assert.Equal(DataConnectionCircuitBreakerState.Closed, secondBreaker.State);
		Assert.Equal(1, attempts);
		Assert.Throws<DataConnectionUnavailableException>(() =>
			firstBreaker.Execute(() => attempts++));
		Assert.Equal(1, attempts);
	}

	[Fact]
	public void TestStateSubscriberFailureDoesNotAffectConnectionFailure()
	{
		var timeProvider = new ManualTimeProvider();
		var source = new DataSourceMocker();
		var manager = new DataConnectionCircuitBreakerManager(timeProvider);
		manager.StateChanged += (_, _) => throw new ApplicationException();

		var exception = Assert.Throws<InvalidOperationException>(() =>
			manager.Execute(source, () => throw new InvalidOperationException("Original.")));

		Assert.Equal("Original.", exception.Message);
		Assert.Equal(DataConnectionCircuitBreakerState.Opened, manager.GetBreaker(source).State);
	}

	private sealed class ManualTimeProvider : TimeProvider
	{
		private readonly object _sync = new();
		private DateTimeOffset _utcNow = new(2026, 7, 29, 0, 0, 0, TimeSpan.Zero);

		public override DateTimeOffset GetUtcNow()
		{
			lock(_sync)
				return _utcNow;
		}

		public void Advance(TimeSpan duration)
		{
			lock(_sync)
				_utcNow += duration;
		}
	}

	private sealed class DataSourceMocker : IDataSource
	{
		public DataSourceMocker(IDataDriver driver = null)
		{
			this.Driver = driver ?? new DataDriverMocker();
			this.Features = new FeatureCollection(this.Driver.Features);
			this.Properties = new Dictionary<string, object>
			{
				[DataConnectionCircuitBreakerOptions.BREAK_DURATION_PROPERTY] = TimeSpan.FromSeconds(1),
				[DataConnectionCircuitBreakerOptions.MAXIMUM_BREAK_DURATION_PROPERTY] = TimeSpan.FromSeconds(8),
				[DataConnectionCircuitBreakerOptions.JITTER_PROPERTY] = 0,
			};
		}

		public string Name => "Test";
		public string ConnectionString => string.Empty;
		public DataAccessMode Mode { get; set; } = DataAccessMode.All;
		public IDataDriver Driver { get; }
		public FeatureCollection Features { get; }
		public IDictionary<string, object> Properties { get; }

		public DataTable GetSchema(string name, bool refresh = false) => null;
		public bool Equals(IDataSource other) => object.ReferenceEquals(this, other);
	}

	private sealed class DataDriverMocker : DataDriverBase
	{
		public override string Name => "Mock";
		public override IStatementBuilder Builder => null;

		public override DbCommand CreateCommand(string text, CommandType commandType = CommandType.Text) => throw new NotSupportedException();
		public override DbConnection CreateConnection(string connectionString = null) => throw new NotSupportedException();
		public override DbConnectionStringBuilder CreateConnectionBuilder(string connectionString = null) => throw new NotSupportedException();

		protected override IDataImporter CreateImporter() => null;
		protected override ExpressionVisitorBase CreateVisitor() => null;
	}
}
