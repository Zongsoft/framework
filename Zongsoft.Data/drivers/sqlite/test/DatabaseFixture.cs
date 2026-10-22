using System;
using System.Threading.Tasks;
using System.IO;

using Zongsoft.Data.Metadata;

using Xunit;

namespace Zongsoft.Data.SQLite.Tests;

public class DatabaseFixture : IAsyncLifetime
{
	#region 静态字段
	private static readonly string DATABASE_FILE = Path.Combine(AppContext.BaseDirectory, "test.db");
	private static readonly string CONNECTION_STRING = $"DataSource={DATABASE_FILE};PRAGMA:optimize;PRAGMA:journal_mode=WAL;PRAGMA:synchronous=NORMAL;PRAGMA:temp_store=MEMORY;";
	#endregion

	#region 私有变量
	private readonly Mapping.Loader _loader;
	#endregion

	#region 构造函数
	public DatabaseFixture()
	{
		File.Delete(DATABASE_FILE);
		File.Delete($"{DATABASE_FILE}-wal");
		File.Delete($"{DATABASE_FILE}-shm");

		Mapping.Commands
			.Add("TruncateLog", DataCommandMutability.Delete)
			.Script(SQLiteDriver.NAME, "DELETE FROM \"Log\"; DELETE FROM sqlite_sequence WHERE name='Log';");

		Mapping.Loaders.Add(_loader = new Metadata.Profiles.MetadataFileLoader(AppContext.BaseDirectory));
		DataEnvironment.Drivers.Add(SQLiteDriver.Instance);

		this.ConnectionSettings = Configuration.SQLiteConnectionSettingsDriver.Instance.GetSettings(CONNECTION_STRING);
		this.Accessor = DataAccessProvider.Instance.GetAccessor("Test", new DataAccessOptions([this.ConnectionSettings]));
		this.Accessor.Sequencer.Sequence = new Zongsoft.Data.Tests.SequenceMocker(TimeSpan.FromMilliseconds(10));
	}
	#endregion

	#region 公共属性
	public DataAccess Accessor { get; }
	public Configuration.SQLiteConnectionSettings ConnectionSettings { get; }
	#endregion

	#region 初始化
	public async ValueTask InitializeAsync()
	{
		using var reader = File.OpenText(@"../../../script/init.sql");
		Mapping.Commands.Add("InitSQL").Script(SQLiteDriver.NAME, await reader.ReadToEndAsync());
		await this.Accessor.ExecuteAsync("InitSQL");
	}
	#endregion

	#region 释放方法
	public async ValueTask DisposeAsync()
	{
		try
		{
			await ((IAsyncDisposable)this.Accessor).DisposeAsync();
		}
		finally
		{
			Mapping.Loaders.Remove(_loader);
			DataEnvironment.Drivers.Remove(SQLiteDriver.Instance);
		}
	}
	#endregion
}

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}
