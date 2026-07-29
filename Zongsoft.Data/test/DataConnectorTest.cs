using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

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
		connector.Breaker.StateChanged += (_, args) => events.Add(args);

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
		connector.Breaker.StateChanged += (_, _) => Interlocked.Increment(ref transitions);

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
		connector.Breaker.StateChanged += (_, args) => states.Add(args.CurrentState);

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
	}

	[Fact]
	public void TestRepeatedProbeFailuresBackOffExponentially()
	{
		var timeProvider = new ManualTimeProvider();
		var source = new DataSourceMocker();
		var connector = CreateConnector(source, timeProvider);

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
			DataConnectorManager.GetConnector(first).Connect<object>(() => throw new InvalidOperationException()));

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

	[Fact]
	public void TestConnectionInfrastructurePublicBoundary()
	{
		Assert.Null(typeof(IDataDriver).GetProperty("CircuitBreaker"));
		Assert.Null(typeof(DataDriverBase).GetProperty("CircuitBreaker"));
		Assert.Null(typeof(DataSession).GetProperty("CircuitBreaker"));
		Assert.DoesNotContain(typeof(IDataDriver).Assembly.GetExportedTypes(), type => type.Name.Contains("CircuitBreaker"));
		Assert.DoesNotContain(typeof(IDataDriver).Assembly.GetExportedTypes(), type => type.Name == "DataImportContextExtension");
		Assert.DoesNotContain(typeof(IDataDriver).Assembly.GetTypes(), type => type.Name.StartsWith("DataConnectionCircuitBreaker"));
		Assert.True(typeof(DataConnector).IsPublic);
		Assert.True(typeof(DataConnectorManager).IsAbstract && typeof(DataConnectorManager).IsSealed);
		Assert.Empty(typeof(DataConnector).GetConstructors());
		Assert.All(
			new[] { "CircuitBreaker", "CircuitBreakerOptions", "CircuitBreakerState", "CircuitBreakerStateChangedEventArgs" },
			name => Assert.NotNull(typeof(DataConnector).GetNestedType(name, System.Reflection.BindingFlags.NonPublic)));
		var optionsType = typeof(DataConnector).GetNestedType("CircuitBreakerOptions", System.Reflection.BindingFlags.NonPublic);
		Assert.NotNull(optionsType.GetProperty("Duration"));
		Assert.NotNull(optionsType.GetProperty("MaximumDuration"));
		Assert.Null(optionsType.GetProperty("BreakDuration"));
		Assert.Null(optionsType.GetProperty("MaximumBreakDuration"));
		Assert.DoesNotContain(
			typeof(IDataDriver).Assembly.GetTypes(),
			type => type.DeclaringType == null && type.Name.StartsWith("CircuitBreaker"));
		Assert.Equal(typeof(DataConnector), typeof(DataSession).GetProperty(nameof(DataSession.Connector))?.PropertyType);
		Assert.DoesNotContain(
			typeof(DataImporterBase).GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly),
			method => method.Name is "Connect" or "ConnectAsync");
	}

	private static DataConnector CreateConnector(IDataSource source, TimeProvider timeProvider) => new(
		source,
		new CircuitBreakerOptions
		{
			Duration = TimeSpan.FromSeconds(1),
			MaximumDuration = TimeSpan.FromSeconds(8),
			Jitter = 0,
		},
		timeProvider);

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
		private readonly Func<CancellationToken, Task> _open;

		public DataDriverMocker(Func<CancellationToken, Task> open = null) =>
			_open = open ?? (_ => Task.CompletedTask);

		public override string Name => "Mock";
		public override IStatementBuilder Builder => null;

		public override DbCommand CreateCommand(string text, CommandType commandType = CommandType.Text) => throw new NotSupportedException();
		public override DbCommand CreateCommand(IDataAccessContextBase context, IStatementBase statement) => new DbCommandMocker();
		public override DbConnection CreateConnection(string connectionString = null) => new DbConnectionMocker(_open);
		public override DbConnectionStringBuilder CreateConnectionBuilder(string connectionString = null) => throw new NotSupportedException();

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
