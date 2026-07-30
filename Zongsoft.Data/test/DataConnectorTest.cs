using System;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Xunit;

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

using CircuitBreakerState = Zongsoft.Data.Common.DataConnector.CircuitBreakerState;
using CircuitBreakerOptions = Zongsoft.Data.Common.DataConnector.CircuitBreakerOptions;
using CircuitBreakerStateChangedEventArgs = Zongsoft.Data.Common.DataConnector.CircuitBreakerStateChangedEventArgs;

namespace Zongsoft.Data.Tests;

public class DataConnectorTest
{
	[Fact]
	public void TestFailureOpensAndFastRejectionsDoNotRepeatEvents()
	{
		var timeProvider = new ManualTimeProvider();
		var source = new DataSourceMocker();
		var connector = CreateConnector(source, timeProvider);
		var attempts = 0;
		var events = new List<CircuitBreakerStateChangedEventArgs>();
		var failures = new List<DataConnectionFailureEventArgs>();
		connector.Breaker.StateChanged += (_, args) => events.Add(args);
		connector.Failed += (_, _) => throw new InvalidOperationException("The subscriber failure must be ignored.");
		connector.Failed += (_, args) => failures.Add(args);

		Assert.Throws<InvalidOperationException>(() => connector.Connect<object>(() =>
		{
			attempts++;
			throw new InvalidOperationException("Database unavailable.");
		}));

		Assert.Equal(CircuitBreakerState.Opened, connector.Breaker.State);
		Assert.Equal(1, attempts);

		for(int index = 0; index < 10; index++)
		{
			var exception = Assert.Throws<DataConnectionException>(() =>
				connector.Connect(() => attempts++));

			Assert.Equal(source.Name, exception.SourceName);
			Assert.Equal(source.Driver.Name, exception.DriverName);
			Assert.Equal(TimeSpan.FromSeconds(1), exception.RetryAfter);
		}

		Assert.Equal(1, attempts);
		var stateChanged = Assert.Single(events);
		Assert.Equal(CircuitBreakerState.Closed, stateChanged.OriginalState);
		Assert.Equal(CircuitBreakerState.Opened, stateChanged.CurrentState);

		var failure = Assert.Single(failures);
		Assert.Same(source, failure.Source);
		Assert.Equal(1, failure.FailureCount);
		Assert.True(failure.IsSuspended);
		Assert.Equal(timeProvider.GetUtcNow().AddSeconds(1), failure.RetryAt);
		Assert.Equal(TimeSpan.FromSeconds(1), failure.RetryAfter);
		Assert.IsType<InvalidOperationException>(failure.Exception);
	}

	[Fact]
	public async Task TestHighFrequencyConnectsAreRejectedWithoutReconnect()
	{
		var timeProvider = new ManualTimeProvider();
		var attempts = 0;
		var source = new DataSourceMocker(new DataDriverMocker(_ =>
		{
			Interlocked.Increment(ref attempts);
			throw new InvalidOperationException("Database unavailable.");
		}));
		var connector = CreateConnector(source, timeProvider);
		var transitions = 0;
		var failures = 0;
		connector.Breaker.StateChanged += (_, _) => Interlocked.Increment(ref transitions);
		connector.Failed += (_, _) => Interlocked.Increment(ref failures);

		var requests = new Task[256];

		for(int index = 0; index < requests.Length; index++)
		{
			requests[index] = Task.Run(() =>
				Assert.ThrowsAny<Exception>(() =>
				{
					using var connection = connector.Connect();
				}));
		}

		await Task.WhenAll(requests);

		Assert.Equal(1, attempts);
		Assert.Equal(1, transitions);
		Assert.Equal(1, failures);
		Assert.Equal(CircuitBreakerState.Opened, connector.Breaker.State);
	}

	[Fact]
	public async Task TestHighFrequencySessionQueriesAndMutationsDoNotReconnect()
	{
		var attempts = 0;
		var source = new DataSourceMocker(new DataDriverMocker(_ =>
		{
			Interlocked.Increment(ref attempts);
			throw new InvalidOperationException("Database unavailable.");
		}));
		DataConnectorManager.GetConnector(source).Failed += (_, args) => args.ExceptionHandled = true;

		using(var session = new DataSession(source))
		using(var command = session.Build(null, null))
			Assert.Throws<InvalidOperationException>(() => command.ExecuteNonQuery());

		var requests = new Task[256];

		for(int index = 0; index < requests.Length; index++)
		{
			var query = index % 2 == 0;
			requests[index] = Task.Run(() =>
			{
				using var session = new DataSession(source);
				using var command = session.Build(null, null);

				if(query)
					Assert.Throws<DataConnectionException>(command.ExecuteScalar);
				else
					Assert.Throws<DataConnectionException>(() => command.ExecuteNonQuery());
			});
		}

		await Task.WhenAll(requests);
		Assert.Equal(1, attempts);
	}

	[Fact]
	public void TestHalfOpenAllowsSingleProbeAndRecovers()
	{
		var timeProvider = new ManualTimeProvider();
		var source = new DataSourceMocker();
		var connector = CreateConnector(source, timeProvider);
		var states = new List<CircuitBreakerState>();
		var failures = new List<DataConnectionFailureEventArgs>();
		connector.Breaker.StateChanged += (_, args) => states.Add(args.CurrentState);
		connector.Failed += (_, args) => failures.Add(args);

		Assert.Throws<InvalidOperationException>(() =>
			connector.Connect<object>(() => throw new InvalidOperationException()));

		timeProvider.Advance(TimeSpan.FromSeconds(1));
		connector.Connect(() => 100);

		Assert.Equal(CircuitBreakerState.Closed, connector.Breaker.State);
		Assert.Equal(
			[
				CircuitBreakerState.Opened,
				CircuitBreakerState.HalfOpen,
				CircuitBreakerState.Closed,
			],
			states);
		Assert.Single(failures);
	}

	[Fact]
	public void TestRepeatedProbeFailuresBackOffExponentially()
	{
		var timeProvider = new ManualTimeProvider();
		var source = new DataSourceMocker();
		var connector = CreateConnector(source, timeProvider);
		var failures = new List<DataConnectionFailureEventArgs>();
		connector.Failed += (_, args) => failures.Add(args);

		Assert.Throws<InvalidOperationException>(() =>
			connector.Connect<object>(() => throw new InvalidOperationException()));
		Assert.Equal(timeProvider.GetUtcNow().AddSeconds(1), connector.Breaker.RetryAt);

		timeProvider.Advance(TimeSpan.FromSeconds(1));
		Assert.Throws<InvalidOperationException>(() =>
			connector.Connect<object>(() => throw new InvalidOperationException()));
		Assert.Equal(timeProvider.GetUtcNow().AddSeconds(2), connector.Breaker.RetryAt);

		timeProvider.Advance(TimeSpan.FromSeconds(2));
		connector.Connect(() => 100);

		Assert.Equal(CircuitBreakerState.Closed, connector.Breaker.State);
		Assert.Null(connector.Breaker.RetryAt);

		Assert.Throws<InvalidOperationException>(() =>
			connector.Connect<object>(() => throw new InvalidOperationException()));
		Assert.Equal([1, 2, 1], failures.ConvertAll(failure => failure.FailureCount));
	}

	[Fact]
	public void TestUnhandledFailureUsesDefaultLogging()
	{
		var logger = new RecordingLogger();
		var culture = CultureInfo.CurrentCulture;
		var uiCulture = CultureInfo.CurrentUICulture;
		Zongsoft.Diagnostics.Logging.Loggers.Add(logger);

		try
		{
			CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
			CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
			var timeProvider = new ManualTimeProvider();
			var source = new DataSourceMocker();
			var connector = CreateConnector(source, timeProvider, false);

			var exception = Assert.Throws<InvalidOperationException>(() =>
				connector.Connect<object>(() => throw new InvalidOperationException("Database unavailable.")));

			var entry = Assert.Single(logger.Entries);
			Assert.Equal(Zongsoft.Diagnostics.LogLevel.Error, entry.Level);
			Assert.Equal("Zongsoft.Data", entry.Source);
			Assert.Same(exception, entry.Exception);
			Assert.Contains(source.Name, entry.Message);
			Assert.Contains(source.Driver.Name, entry.Message);
			Assert.Contains("1 consecutive time(s)", entry.Message);
			Assert.Contains("Connection attempts are suspended until", entry.Message);
			Assert.Contains(timeProvider.GetUtcNow().AddSeconds(1).ToLocalTime().ToString("HH:mm:sszz"), entry.Message);
			Assert.DoesNotContain("ConnectionString", entry.Message, StringComparison.OrdinalIgnoreCase);
			Assert.DoesNotContain("Password", entry.Message, StringComparison.OrdinalIgnoreCase);
			Assert.DoesNotContain("secret", entry.Message, StringComparison.OrdinalIgnoreCase);

			var unavailable = Assert.Throws<DataConnectionException>(() => connector.Connect(() => 100));
			Assert.StartsWith("The 'Test' data source is temporarily unavailable.", unavailable.Message);

			var handled = CreateConnector(new DataSourceMocker(), timeProvider);
			Assert.Throws<InvalidOperationException>(() =>
				handled.Connect<object>(() => throw new InvalidOperationException("Handled database failure.")));
			Assert.Single(logger.Entries);

			logger.Clear();
			CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("zh-CN");
			CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("zh-Hans");
			var localized = CreateConnector(new DataSourceMocker(), timeProvider, false);

			Assert.Throws<InvalidOperationException>(() =>
				localized.Connect<object>(() => throw new InvalidOperationException("Localized database failure.")));

			entry = Assert.Single(logger.Entries);
			Assert.Contains("数据源“Test”", entry.Message);
			Assert.Contains(timeProvider.GetUtcNow().AddSeconds(1).ToLocalTime().ToString("HH:mm:sszz"), entry.Message);
			Assert.DoesNotContain("secret", entry.Message, StringComparison.OrdinalIgnoreCase);

			unavailable = Assert.Throws<DataConnectionException>(() => localized.Connect(() => 100));
			Assert.StartsWith("数据源“Test”暂时不可用。", unavailable.Message);
		}
		finally
		{
			Zongsoft.Diagnostics.Logging.Loggers.Remove(logger);
			CultureInfo.CurrentCulture = culture;
			CultureInfo.CurrentUICulture = uiCulture;
		}
	}

	[Fact]
	public async Task TestCallerCancellationDoesNotOpenProtection()
	{
		var timeProvider = new ManualTimeProvider();
		var connector = CreateConnector(new DataSourceMocker(), timeProvider);
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			connector.ConnectAsync<int>(
				token => Task.FromCanceled<int>(token),
				cancellation.Token));

		Assert.Equal(CircuitBreakerState.Closed, connector.Breaker.State);
	}

	[Fact]
	public void TestConnectorsAreSharedPerSourceAndIsolatedBetweenSources()
	{
		var driver = new DataDriverMocker();
		var first = new DataSourceMocker(driver);
		var second = new DataSourceMocker(driver);

		Assert.Same(DataConnectorManager.GetConnector(first), DataConnectorManager.GetConnector(first));
		Assert.NotSame(DataConnectorManager.GetConnector(first), DataConnectorManager.GetConnector(second));

		using(var firstSession = new DataSession(first))
		using(var secondSession = new DataSession(first))
		{
			Assert.Same(DataConnectorManager.GetConnector(first), firstSession.Connector);
			Assert.Same(firstSession.Connector, secondSession.Connector);
		}

		Assert.Throws<InvalidOperationException>(() =>
		{
			DataConnectorManager.GetConnector(first).Failed += (_, args) => args.ExceptionHandled = true;
			DataConnectorManager.GetConnector(first).Connect<object>(() => throw new InvalidOperationException());
		});

		Assert.Equal(100, DataConnectorManager.GetConnector(second).Connect(() => 100));
		Assert.Equal(CircuitBreakerState.Opened, DataConnectorManager.GetConnector(first).Breaker.State);
		Assert.Equal(CircuitBreakerState.Closed, DataConnectorManager.GetConnector(second).Breaker.State);
	}

	[Fact]
	public async Task TestConcurrentFirstAccessReturnsOneConnector()
	{
		var source = new DataSourceMocker();
		var requests = new Task<DataConnector>[256];

		for(int index = 0; index < requests.Length; index++)
			requests[index] = Task.Run(() => DataConnectorManager.GetConnector(source));

		var connectors = await Task.WhenAll(requests);

		for(int index = 1; index < connectors.Length; index++)
			Assert.Same(connectors[0], connectors[index]);
	}

	private static DataConnector CreateConnector(IDataSource source, TimeProvider timeProvider, bool handleFailures = true)
	{
		var connector = new DataConnector(
			source,
			new CircuitBreakerOptions
			{
				Duration = TimeSpan.FromSeconds(1),
				MaximumDuration = TimeSpan.FromSeconds(8),
				Jitter = 0,
			},
			timeProvider);

		if(handleFailures)
			connector.Failed += (_, args) => args.ExceptionHandled = true;

		return connector;
	}

	private sealed class RecordingLogger : Zongsoft.Diagnostics.ILogger
	{
		private readonly ConcurrentQueue<Zongsoft.Diagnostics.LogEntry> _entries = new();

		public string Name => nameof(RecordingLogger);
		public IReadOnlyCollection<Zongsoft.Diagnostics.LogEntry> Entries => _entries;

		public ValueTask FlushAsync(CancellationToken cancellation = default) => ValueTask.CompletedTask;
		public void Clear()
		{
			while(_entries.TryDequeue(out _)) { }
		}

		public ValueTask LogAsync<TLog>(TLog log, CancellationToken cancellation = default) where TLog : Zongsoft.Diagnostics.ILog
		{
			if(log is Zongsoft.Diagnostics.LogEntry entry)
				_entries.Enqueue(entry);

			return ValueTask.CompletedTask;
		}
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
				[CircuitBreakerOptions.DURATION_PROPERTY] = TimeSpan.FromSeconds(1),
				[CircuitBreakerOptions.MAXIMUM_DURATION_PROPERTY] = TimeSpan.FromSeconds(8),
				[CircuitBreakerOptions.JITTER_PROPERTY] = 0,
			};
		}

		public string Name => "Test";
		public string ConnectionString => "Server=database.example.com;Port=3306;Database=sample;UserName=tester;Password=secret;";
		public DataAccessMode Mode { get; set; } = DataAccessMode.All;
		public IDataDriver Driver { get; }
		public FeatureCollection Features { get; }
		public IDictionary<string, object> Properties { get; }

		public DataTable GetSchema(string name, bool refresh = false) => null;
		public bool Equals(IDataSource other) => object.ReferenceEquals(this, other);
	}

	private sealed class DataDriverMocker : DataDriverBase
	{
		private readonly Func<CancellationToken, Task> _open;

		public DataDriverMocker(Func<CancellationToken, Task> open = null) =>
			_open = open ?? (_ => Task.CompletedTask);

		public override string Name => "Mock";
		public override IStatementBuilder Builder => null;

		public override DbCommand CreateCommand(string text, CommandType commandType = CommandType.Text) => throw new NotSupportedException();
		public override DbCommand CreateCommand(IDataAccessContextBase context, IStatementBase statement) => new DbCommandMocker();
		public override DbConnection CreateConnection(string connectionString = null) => new DbConnectionMocker(_open);
		public override DbConnectionStringBuilder CreateConnectionBuilder(string connectionString = null) => new()
		{
			ConnectionString = connectionString,
		};

		protected override IDataImporter CreateImporter() => null;
		protected override ExpressionVisitorBase CreateVisitor() => null;
	}

	private sealed class DbConnectionMocker(Func<CancellationToken, Task> open) : DbConnection
	{
		private string _connectionString;
		private ConnectionState _state;

		public override string ConnectionString
		{
			get => _connectionString;
			set => _connectionString = value;
		}

		public override string Database => "Test";
		public override string DataSource => "Test";
		public override string ServerVersion => "1.0";
		public override ConnectionState State => _state;

		public override void ChangeDatabase(string databaseName) { }
		public override void Close() => _state = ConnectionState.Closed;

		public override void Open()
		{
			open(CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
			_state = ConnectionState.Open;
		}

		public override async Task OpenAsync(CancellationToken cancellationToken)
		{
			await open(cancellationToken);
			_state = ConnectionState.Open;
		}

		protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
		protected override DbCommand CreateDbCommand() => throw new NotSupportedException();
	}

	private sealed class DbCommandMocker : DbCommand
	{
		private DbConnection _connection;
		private DbTransaction _transaction;

		public override string CommandText { get; set; }
		public override int CommandTimeout { get; set; }
		public override CommandType CommandType { get; set; }
		public override bool DesignTimeVisible { get; set; }
		public override UpdateRowSource UpdatedRowSource { get; set; }

		protected override DbConnection DbConnection
		{
			get => _connection;
			set => _connection = value;
		}

		protected override DbParameterCollection DbParameterCollection => throw new NotSupportedException();

		protected override DbTransaction DbTransaction
		{
			get => _transaction;
			set => _transaction = value;
		}

		public override void Cancel() { }
		public override int ExecuteNonQuery() => 1;
		public override object ExecuteScalar() => 1;
		public override void Prepare() { }
		protected override DbParameter CreateDbParameter() => throw new NotSupportedException();
		protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotSupportedException();
	}
}
