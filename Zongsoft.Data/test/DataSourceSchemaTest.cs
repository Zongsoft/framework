using System;
using System.Linq;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Xunit;

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.Tests;

[Collection(nameof(DataSourceSchemaCollection))]
public class DataSourceSchemaTest
{
	private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);

	public enum ProviderStage
	{
		Open,
		Schema,
		Dispose,
	}

	[Fact]
	public async Task GetSchemaAsync_NullAndEmptyName_ReuseDefaultSchemaCache()
	{
		using var fixture = new SchemaFixture();
		var first = await fixture.Source.GetSchemaAsync(null);
		var second = await fixture.Source.GetSchemaAsync(string.Empty);

		Assert.Same(fixture.Driver.Schema, first);
		Assert.Same(first, second);
		var connection = Assert.Single(fixture.Driver.Connections);
		Assert.Null(connection.SchemaName);
		Assert.Equal(1, connection.OpenCount);
		Assert.Equal(1, connection.SchemaCount);
		Assert.Equal(1, connection.DisposeCount);
	}

	[Fact]
	public async Task GetSchemaAsync_NamedCacheAndRefresh_PreserveCaseInsensitiveEntry()
	{
		using var fixture = new SchemaFixture();
		var original = await fixture.Source.GetSchemaAsync("Tables");
		Assert.Same(original, await fixture.Source.GetSchemaAsync("TABLES"));
		Assert.Single(fixture.Driver.Connections);
		var replacement = new DataTable("replacement");
		fixture.Driver.Schema = replacement;

		Assert.Same(replacement, await fixture.Source.GetSchemaAsync("tables", refresh: true));
		Assert.Same(replacement, await fixture.Source.GetSchemaAsync("TaBlEs"));
		Assert.NotSame(original, replacement);
		Assert.Equal(["Tables", "tables"], fixture.Driver.Connections.Select(connection => connection.SchemaName));
		Assert.All(fixture.Driver.Connections, connection => Assert.Equal(1, connection.DisposeCount));
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetSchemaAsync_RefreshFailure_ReturnsNullAndPreservesCachedTable(bool opening)
	{
		using var fixture = new SchemaFixture();
		var cached = await fixture.Source.GetSchemaAsync("Tables");
		var failure = new InvalidOperationException();

		if(opening)
			fixture.Driver.Open = _ => Task.FromException(failure);
		else
			fixture.Driver.Read = (_, _) => Task.FromException<DataTable>(failure);

		Assert.Null(await fixture.Source.GetSchemaAsync("TABLES", refresh: true));
		Assert.Same(cached, await fixture.Source.GetSchemaAsync("tables"));
		Assert.Equal(2, fixture.Driver.Connections.Length);
		Assert.Equal(opening ? 0 : 1, fixture.Driver.Connections[1].SchemaCount);
		Assert.All(fixture.Driver.Connections, connection => Assert.Equal(1, connection.DisposeCount));
	}

	[Theory]
	[InlineData(ProviderStage.Open)]
	[InlineData(ProviderStage.Schema)]
	[InlineData(ProviderStage.Dispose)]
	public async Task GetSchemaAsync_PendingProviderStage_WaitsThroughDisposal(ProviderStage stage)
	{
		using var fixture = new SchemaFixture();
		var entered = NewGate();
		var release = NewGate();
		var trace = new List<ProviderStage>();

		fixture.Driver.Open = _ => WaitAsync(ProviderStage.Open);
		fixture.Driver.Read = async (_, _) =>
		{
			await WaitAsync(ProviderStage.Schema);
			return fixture.Driver.Schema;
		};
		fixture.Driver.Release = () => WaitAsync(ProviderStage.Dispose);

		var first = fixture.Source.GetSchemaAsync("Tables").AsTask();
		Task<DataTable> second = null;

		try
		{
			await entered.Task.WaitAsync(_timeout);
			second = fixture.Source.GetSchemaAsync("tables").AsTask();
			Assert.False(first.IsCompleted);
			Assert.False(second.IsCompleted);
			Assert.Single(fixture.Driver.Connections);
			Assert.Equal((int)stage + 1, trace.Count);
		}
		finally
		{
			release.TrySetResult();
			await Record.ExceptionAsync(() => first.WaitAsync(_timeout));

			if(second != null)
				await Record.ExceptionAsync(() => second.WaitAsync(_timeout));
		}

		Assert.Same(fixture.Driver.Schema, await first);
		Assert.Same(await first, await second);
		Assert.Equal([ProviderStage.Open, ProviderStage.Schema, ProviderStage.Dispose], trace);
		Assert.Equal(1, Assert.Single(fixture.Driver.Connections).DisposeCount);

		async Task WaitAsync(ProviderStage current)
		{
			trace.Add(current);

			if(current == stage)
			{
				entered.TrySetResult();
				await release.Task;
			}
		}
	}

	[Fact]
	public async Task GetSchemaAsync_CancelledWhileWaiting_DoesNotStartAnotherConnection()
	{
		using var fixture = new SchemaFixture();
		using var cancellation = new CancellationTokenSource();
		var entered = NewGate();
		var release = NewGate();

		fixture.Driver.Read = async (_, _) =>
		{
			entered.TrySetResult();
			await release.Task;
			return fixture.Driver.Schema;
		};
		var first = fixture.Source.GetSchemaAsync("Tables").AsTask();

		try
		{
			await entered.Task.WaitAsync(_timeout);
			var waiting = fixture.Source.GetSchemaAsync("Other", cancellation: cancellation.Token).AsTask();
			Assert.False(waiting.IsCompleted);
			cancellation.Cancel();

			var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting.WaitAsync(_timeout));
			Assert.Equal(cancellation.Token, failure.CancellationToken);
			Assert.False(first.IsCompleted);
			Assert.Single(fixture.Driver.Connections);
		}
		finally
		{
			release.TrySetResult();
			await Record.ExceptionAsync(() => first.WaitAsync(_timeout));
		}

		Assert.Same(await first, await fixture.Source.GetSchemaAsync("tables"));
		Assert.Equal(1, Assert.Single(fixture.Driver.Connections).DisposeCount);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task GetSchemaAsync_CancelledDuringProviderOperation_ReleasesCacheGate(bool opening)
	{
		using var fixture = new SchemaFixture();
		using var cancellation = new CancellationTokenSource();
		var entered = NewGate();
		var release = NewGate();

		if(opening)
			fixture.Driver.Open = WaitAsync;
		else
			fixture.Driver.Read = async (_, token) =>
			{
				await WaitAsync(token);
				return fixture.Driver.Schema;
			};

		var operation = fixture.Source.GetSchemaAsync("Tables", cancellation: cancellation.Token).AsTask();

		try
		{
			await entered.Task.WaitAsync(_timeout);
			cancellation.Cancel();
			var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation.WaitAsync(_timeout));
			Assert.Equal(cancellation.Token, failure.CancellationToken);
			Assert.Equal(1, Assert.Single(fixture.Driver.Connections).DisposeCount);
		}
		finally
		{
			release.TrySetResult();
			await Record.ExceptionAsync(() => operation.WaitAsync(_timeout));
		}

		fixture.Driver.Open = null;
		fixture.Driver.Read = null;
		Assert.Same(fixture.Driver.Schema, await fixture.Source.GetSchemaAsync("Tables").AsTask().WaitAsync(_timeout));
		Assert.Equal(2, fixture.Driver.Connections.Length);
		Assert.All(fixture.Driver.Connections, connection => Assert.Equal(1, connection.DisposeCount));

		async Task WaitAsync(CancellationToken token)
		{
			Assert.Equal(cancellation.Token, token);
			entered.TrySetResult();
			await release.Task.WaitAsync(token);
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task GetSchemaAsync_DisposalFailure_OverridesReadOutcome(bool readFails)
	{
		using var fixture = new SchemaFixture();
		var readFailure = new InvalidOperationException();
		var disposalFailure = new ApplicationException();
		fixture.Driver.Release = () => Task.FromException(disposalFailure);

		if(readFails)
			fixture.Driver.Read = (_, _) => Task.FromException<DataTable>(readFailure);

		Assert.Same(disposalFailure, await Assert.ThrowsAsync<ApplicationException>(() => fixture.Source.GetSchemaAsync("Tables").AsTask()));
		Assert.Equal(1, Assert.Single(fixture.Driver.Connections).DisposeCount);
		fixture.Driver.Release = null;
		fixture.Driver.Read = null;

		Assert.Same(fixture.Driver.Schema, await fixture.Source.GetSchemaAsync("Tables").AsTask().WaitAsync(_timeout));
		Assert.Equal(2, fixture.Driver.Connections.Length);
		Assert.All(fixture.Driver.Connections, connection => Assert.Equal(1, connection.SchemaCount));
	}

	[Fact]
	public async Task GetSchemaAsync_ConcurrentRefreshAndCachedRead_AreSerialized()
	{
		using var fixture = new SchemaFixture();
		var cached = await fixture.Source.GetSchemaAsync("Seed");
		var entered = Enumerable.Range(0, 6).Select(_ => NewGate()).ToArray();
		var release = Enumerable.Range(0, 6).Select(_ => NewGate()).ToArray();
		var records = new ConcurrentQueue<(string Name, DataTable Table)>();
		var index = -1;
		var active = 0;

		fixture.Driver.Read = async (name, _) =>
		{
			var current = Interlocked.Increment(ref index);
			Assert.Equal(1, Interlocked.Increment(ref active));
			entered[current].TrySetResult();

			try
			{
				await release[current].Task;
				var table = new DataTable($"Table{current}");
				records.Enqueue((name, table));
				return table;
			}
			finally { Interlocked.Decrement(ref active); }
		};

		var requests = Enumerable.Range(0, entered.Length)
			.Select(number => fixture.Source.GetSchemaAsync(number % 2 == 0 ? "A" : "B", refresh: true).AsTask()).ToArray();
		var cachedRead = fixture.Source.GetSchemaAsync("seed").AsTask();

		try
		{
			Assert.False(cachedRead.IsCompleted);
			Assert.All(requests, task => Assert.False(task.IsCompleted));

			for(var current = 0; current < entered.Length; current++)
			{
				await entered[current].Task.WaitAsync(_timeout);
				Assert.Equal(1, Volatile.Read(ref active));
				Assert.Equal(current + 2, fixture.Driver.Connections.Length);
				release[current].SetResult();
			}

			Assert.All(await Task.WhenAll(requests).WaitAsync(_timeout), table => Assert.NotNull(table));
			Assert.Same(cached, await cachedRead.WaitAsync(_timeout));
			Assert.Equal(6, records.Count);
			Assert.Equal(0, Volatile.Read(ref active));

			foreach(var name in new[] { "A", "B" })
				Assert.Same(records.Last(record => record.Name == name).Table, await fixture.Source.GetSchemaAsync(name));

			Assert.Equal(7, fixture.Driver.Connections.Length);
			Assert.All(fixture.Driver.Connections, connection => Assert.Equal(1, connection.DisposeCount));
		}
		finally
		{
			foreach(var gate in release)
				gate.TrySetResult();

			await Record.ExceptionAsync(() => Task.WhenAll(requests.Append(cachedRead)).WaitAsync(_timeout));
		}
	}

	private static TaskCompletionSource NewGate() => new(TaskCreationOptions.RunContinuationsAsynchronously);

	private sealed class SchemaFixture : IDisposable
	{
		public SchemaFixture()
		{
			this.Driver = new SchemaDriver();
			DataEnvironment.Drivers.Add(this.Driver);
			this.Source = new DataSource($"Schema-{Guid.NewGuid():N}", "Test=Schema", this.Driver.Name);
			DataConnectorManager.GetConnector(this.Source).Failed += (_, args, _) =>
			{
				args.ExceptionHandled = true;
				return ValueTask.CompletedTask;
			};
		}

		public SchemaDriver Driver { get; }
		public DataSource Source { get; }
		public void Dispose() => DataEnvironment.Drivers.Remove(this.Driver.Name);
	}

	private sealed class SchemaDriver : DataDriverBase
	{
		private readonly ConcurrentQueue<SchemaConnection> _connections = new();
		public override string Name { get; } = $"Schema-{Guid.NewGuid():N}";
		public override IStatementBuilder Builder => null;
		public DataTable Schema { get; set; } = new("initial");
		public SchemaConnection[] Connections => _connections.ToArray();
		public Func<CancellationToken, Task> Open { get; set; }
		public Func<string, CancellationToken, Task<DataTable>> Read { get; set; }
		public Func<Task> Release { get; set; }

		public override DbConnection CreateConnection(string connectionString = null)
		{
			var connection = new SchemaConnection(this);
			_connections.Enqueue(connection);
			return connection;
		}

		public override DbConnectionStringBuilder CreateConnectionBuilder(string connectionString = null) => new();
		public override DbCommand CreateCommand(string text, CommandType commandType = CommandType.Text) => throw new NotSupportedException();
		protected override IDataImporter CreateImporter() => null;
		protected override ExpressionVisitorBase CreateVisitor() => null;
	}

	private sealed class SchemaConnection(SchemaDriver driver) : DbConnection
	{
		private ConnectionState _state;
		public override string ConnectionString { get; set; }
		public override string Database => "SchemaTest";
		public override string DataSource => "SchemaTest";
		public override string ServerVersion => "1.0";
		public override ConnectionState State => _state;
		public int OpenCount { get; private set; }
		public int SchemaCount { get; private set; }
		public int DisposeCount { get; private set; }
		public string SchemaName { get; private set; }

		public override void Open() => throw new InvalidOperationException("Synchronous provider access.");
		public override void Close() => throw new InvalidOperationException("Synchronous provider access.");
		public override void ChangeDatabase(string databaseName) => throw new NotSupportedException();
		protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
		protected override DbCommand CreateDbCommand() => throw new NotSupportedException();
		public override DataTable GetSchema() => throw new InvalidOperationException("Synchronous provider access.");
		public override DataTable GetSchema(string collectionName) => throw new InvalidOperationException("Synchronous provider access.");
		public override DataTable GetSchema(string collectionName, string[] restrictionValues) => throw new InvalidOperationException("Synchronous provider access.");

		public override async Task OpenAsync(CancellationToken cancellationToken)
		{
			this.OpenCount++;

			if(driver.Open != null)
				await driver.Open(cancellationToken);

			_state = ConnectionState.Open;
		}

		public override Task<DataTable> GetSchemaAsync(CancellationToken cancellationToken = default) => this.GetSchemaAsync(null, cancellationToken);
		public override Task<DataTable> GetSchemaAsync(string collectionName, CancellationToken cancellationToken = default)
		{
			this.SchemaCount++;
			this.SchemaName = collectionName;
			return driver.Read?.Invoke(collectionName, cancellationToken) ?? Task.FromResult(driver.Schema);
		}

		protected override void Dispose(bool disposing)
		{
			if(disposing)
				throw new InvalidOperationException("Synchronous provider disposal.");
		}

		public override async ValueTask DisposeAsync()
		{
			this.DisposeCount++;

			if(driver.Release != null)
				await driver.Release();

			_state = ConnectionState.Closed;
		}
	}
}

[CollectionDefinition(nameof(DataSourceSchemaCollection), DisableParallelization = true)]
public sealed class DataSourceSchemaCollection;
