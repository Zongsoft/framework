using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Data.Common;
using Zongsoft.Data.Common.Expressions;

namespace Zongsoft.Data.Tests;

public class DataMultiplexerTest
{
	[Fact]
	public async Task TestConcurrentInitialization()
	{
		const int CONCURRENCY = 16;

		var source = new DataSourceMocker();
		var sourceProvider = new DataSourceProviderMocker(source);
		var dataProvider = DataProvider.Create("Test", sourceProvider);
		var multiplexer = dataProvider.Multiplexer;

		using var gate = new ManualResetEventSlim();
		var context = new DataAccessContextMocker();
		var tasks = new Task<IDataSource>[CONCURRENCY];

		for(int i = 0; i < tasks.Length; i++)
		{
			tasks[i] = Task.Factory.StartNew(() =>
			{
				gate.Wait();
				return multiplexer.GetSource(context);
			},
			CancellationToken.None,
			TaskCreationOptions.LongRunning,
			TaskScheduler.Default);
		}

		gate.Set();
		await Task.WhenAll(tasks);

		Assert.Equal(1, sourceProvider.Count);
		Assert.All(tasks, task => Assert.Same(source, task.Result));
	}

	[Fact]
	public void TestInitializationRetry()
	{
		var source = new DataSourceMocker();
		var sourceProvider = new DataSourceProviderMocker(source, 1);
		var dataProvider = DataProvider.Create("Test", sourceProvider);
		var context = new DataAccessContextMocker();

		Assert.Throws<DataException>(() => dataProvider.Multiplexer.GetSource(context));
		Assert.Same(source, dataProvider.Multiplexer.GetSource(context));
		Assert.Equal(2, sourceProvider.Count);
	}

	private sealed class DataSourceProviderMocker(IDataSource source, int emptyResults = 0) : IDataSourceProvider
	{
		private int _count;
		private int _emptyResults = emptyResults;

		public int Count => _count;

		public IEnumerable<IDataSource> GetSources(string name)
		{
			Interlocked.Increment(ref _count);
			Thread.Sleep(100);

			if(Interlocked.Decrement(ref _emptyResults) >= 0)
				return [];

			return [source];
		}
	}

	private sealed class DataAccessContextMocker : IDataAccessContextBase
	{
		public string Name => "Test";
		public DataAccessMethod Method => DataAccessMethod.Select;
		public IDataAccess DataAccess => null;
		public ClaimsPrincipal Principal => null;
		public TFeature GetFeature<TFeature>() => default;
	}

	private sealed class DataSourceMocker : IDataSource
	{
		public string Name => "Test";
		public string ConnectionString => string.Empty;
		public DataAccessMode Mode { get; set; } = DataAccessMode.All;
		public IDataDriver Driver { get; } = new DataDriverMocker();
		public FeatureCollection Features => null;
		public IDictionary<string, object> Properties { get; } = new Dictionary<string, object>();

		public DataTable GetSchema(string name, bool refresh = false) => null;
		public bool Equals(IDataSource other) => object.ReferenceEquals(this, other);
	}

	private sealed class DataDriverMocker : IDataDriver
	{
		public string Name => "Mock";
		public FeatureCollection Features => null;
		public IDataRecordGetter Getter => null;
		public IDataParameterSetter Setter => null;
		public IDataImporter Importer => null;
		public IStatementBuilder Builder => null;

		public Exception OnError(IDataAccessContext context, Exception exception) => exception;
		public DbCommand CreateCommand() => throw new NotSupportedException();
		public DbCommand CreateCommand(string text, CommandType commandType = CommandType.Text) => throw new NotSupportedException();
		public DbCommand CreateCommand(IDataAccessContextBase context, IStatementBase statement) => throw new NotSupportedException();
		public DbConnection CreateConnection(string connectionString = null) => throw new NotSupportedException();
		public DbConnectionStringBuilder CreateConnectionBuilder(string connectionString = null) => throw new NotSupportedException();
	}
}
