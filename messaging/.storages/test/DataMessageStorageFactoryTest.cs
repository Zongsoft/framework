using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Configuration;
using Zongsoft.Data;
using Zongsoft.Services;

namespace Zongsoft.Messaging.Storages.Data.Tests;

[Collection(DatabaseMessageStorageCollection.Name)]
public sealed class DataMessageStorageFactoryTest(SQLiteDatabaseFixture fixture)
{
	private readonly SQLiteDatabaseFixture _fixture = fixture;

	public static IEnumerable<object[]> Factories =>
	[
		["SQLite"],
		["MySql"],
		["PostgreSql"],
		["MsSql"],
	];

	[Theory]
	[MemberData(nameof(Factories))]
	public async Task Create_ExactNamedConnectionCreatesIndependentStorages(string option)
	{
		var factory = GetFactory(option);
		using var scope = this.CreateScope(option, out var provider);
		await using var application = new ApplicationScope(scope.Provider);

		DataMessageStorage first = factory.Create("QueueServer");
		DataMessageStorage second = factory.Create("QueueServer");
		DataMessageStorage normalized = factory.Create(" QueueServer ");

		Assert.Equal(option, factory.Driver);
		Assert.Equal("QueueServer", first.ConnectionSettings.Name);
		Assert.Equal("Zongsoft.Messaging.Storage:QueueServer:database-test-identifier", first.Partition);
		Assert.NotSame(first, second);
		Assert.NotSame(second, normalized);
		Assert.Same(_fixture.Accessor, first.Accessor);
		Assert.Equal(3, provider.Count);
		Assert.All(provider.Names, name => Assert.Equal("QueueServer", name));
		Assert.Throws<ConfigurationException>(() => factory.Create("missing"));
	}

	[Fact]
	public void StaticInstancesExposeCanonicalDriverNamesAndPrivateConstruction()
	{
		Assert.Equal("MySql", DataMessageStorageFactory.MySql.Driver);
		Assert.Equal("MsSql", DataMessageStorageFactory.MsSql.Driver);
		Assert.Equal("SQLite", DataMessageStorageFactory.Sqlite.Driver);
		Assert.Equal("PostgreSql", DataMessageStorageFactory.PostgreSql.Driver);
		Assert.NotSame(DataMessageStorageFactory.MySql, DataMessageStorageFactory.MsSql);
		Assert.All(typeof(DataMessageStorageFactory).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic), constructor => Assert.True(constructor.IsPrivate));
		Assert.DoesNotContain(typeof(DataMessageStorageFactory).Assembly.GetTypes(), type => type.Name.EndsWith("MessageStorageProvider", StringComparison.Ordinal));
	}

	[Fact]
	public async Task DriverMismatchThrowsConfigurationExceptionWithoutRequestingAccessor()
	{
		using var scope = this.CreateScope("SQLite", out var provider);
		await using var application = new ApplicationScope(scope.Provider);

		Assert.Throws<ConfigurationException>(() => DataMessageStorageFactory.MySql.Create("QueueServer"));
		Assert.Equal(0, provider.Count);
	}

	private ServiceScope CreateScope(string option, out AccessorProvider accessorProvider)
	{
		EnsureConnectionDrivers();
		var configuration = new ConfigurationBuilder()
			.AddOptionFile(Path.Combine(AppContext.BaseDirectory, "options", $"{option}.option"))
			.Build();
		var services = new ServiceCollection();
		services.AddSingleton<IConfigurationRoot>(configuration);
		services.AddSingleton<IDataAccessProvider>(accessorProvider = new AccessorProvider(_fixture.Accessor));
		return new ServiceScope(new ServiceProviderFactory().CreateServiceProvider(services));
	}

	private static void EnsureConnectionDrivers()
	{
		ConnectionSettings.Drivers.Add(Zongsoft.Data.SQLite.Configuration.SQLiteConnectionSettingsDriver.Instance);
		ConnectionSettings.Drivers.Add(Zongsoft.Data.MySql.Configuration.MySqlConnectionSettingsDriver.Instance);
		ConnectionSettings.Drivers.Add(Zongsoft.Data.PostgreSql.Configuration.PostgreSqlConnectionSettingsDriver.Instance);
		ConnectionSettings.Drivers.Add(Zongsoft.Data.MsSql.Configuration.MsSqlConnectionSettingsDriver.Instance);
	}

	private static DataMessageStorageFactory GetFactory(string driver) => driver switch
	{
		"SQLite" => DataMessageStorageFactory.Sqlite,
		"MySql" => DataMessageStorageFactory.MySql,
		"PostgreSql" => DataMessageStorageFactory.PostgreSql,
		"MsSql" => DataMessageStorageFactory.MsSql,
		_ => throw new ArgumentOutOfRangeException(nameof(driver)),
	};

	private sealed class AccessorProvider(IDataAccess accessor) : IDataAccessProvider
	{
		public int Count { get; private set; }
		public List<string> Names { get; } = [];

		public IDataAccess GetAccessor(string name = null) => this.GetAccessor(name, null);
		public IDataAccess GetAccessor(string name, IDataAccessOptions options = null)
		{
			this.Count++;
			this.Names.Add(name);
			return accessor;
		}
	}

	private sealed class ServiceScope(IServiceProvider provider) : IDisposable
	{
		public IServiceProvider Provider { get; } = provider;
		public void Dispose() => (this.Provider as IDisposable)?.Dispose();
	}

	private sealed class ApplicationScope : IAsyncDisposable
	{
		private static readonly FieldInfo _CurrentField_ = typeof(ApplicationContext).GetField("_current", BindingFlags.Static | BindingFlags.NonPublic);
		private readonly IApplicationContext _previous;
		private readonly TestApplicationContext _current;

		public ApplicationScope(IServiceProvider services)
		{
			_previous = ApplicationContext.Current;
			_current = new TestApplicationContext(services);
		}

		public async ValueTask DisposeAsync()
		{
			try { await _current.DisposeAsync(); }
			finally { _CurrentField_.SetValue(null, _previous); }
		}
	}

	private sealed class TestApplicationContext(IServiceProvider services) : ApplicationContext(services);
}
