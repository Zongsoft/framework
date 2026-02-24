using System;
using System.IO;

using Zongsoft.Data.Metadata;

using Xunit;

namespace Zongsoft.Data.SQLite.Tests;

public class DatabaseFixture : IDisposable
{
	#region 常量定义
	private const string CONNECTION_STRING = @"DataSource=file::memory:?cache=shared";
	#endregion

	#region 私有变量
	private readonly Mapping.Loader _loader;
	#endregion

	#region 构造函数
	public DatabaseFixture()
	{
		Mapping.Commands
			.Add("TruncateLog", DataCommandMutability.Delete)
			.Script(SQLiteDriver.NAME, "DELETE FROM \"Log\"; DELETE FROM sqlite_sequence WHERE name='Log';");

		Mapping.Loaders.Add(_loader = new Metadata.Profiles.MetadataFileLoader(AppContext.BaseDirectory));
		DataEnvironment.Drivers.Add(SQLiteDriver.Instance);

		this.ConnectionSettings = Configuration.SQLiteConnectionSettingsDriver.Instance.GetSettings(CONNECTION_STRING);
		this.Accessor = DataAccessProvider.Instance.GetAccessor("Test", new DataAccessOptions([this.ConnectionSettings]));
		this.Accessor.Sequencer.Sequence = new Zongsoft.Data.Tests.SequenceMocker(TimeSpan.FromMilliseconds(10));

		using var reader = File.OpenText(@"../../../script/init.sql");
		Mapping.Commands.Add("InitSQL").Script(SQLiteDriver.NAME, reader.ReadToEnd());
		this.Accessor.Execute("InitSQL");
	}
	#endregion

	#region 公共属性
	public DataAccess Accessor { get; }
	public Configuration.SQLiteConnectionSettings ConnectionSettings { get; }
	#endregion

	#region 释放方法
	public void Dispose()
	{
		Mapping.Loaders.Remove(_loader);
		DataEnvironment.Drivers.Remove(SQLiteDriver.Instance);
	}
	#endregion
}

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}
