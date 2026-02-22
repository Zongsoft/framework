using System;

using Xunit;

namespace Zongsoft.Data.DuckDB.Tests;

public class DatabaseFixture : IDisposable
{
	#region 常量定义
	private const string CONNECTION_STRING = @"database=:memory:?cache=shared";
	#endregion

	#region 私有变量
	private readonly Mapping.Loader _loader;
	#endregion

	#region 构造函数
	public DatabaseFixture()
	{
		Mapping.Loaders.Add(_loader = new Metadata.Profiles.MetadataFileLoader(AppContext.BaseDirectory));
		DataEnvironment.Drivers.Add(DuckDBDriver.Instance);

		this.ConnectionSettings = Configuration.DuckDBConnectionSettingsDriver.Instance.GetSettings(CONNECTION_STRING);
		this.Accessor = DataAccessProvider.Instance.GetAccessor("Test", new DataAccessOptions([this.ConnectionSettings]));
	}
	#endregion

	#region 公共属性
	public DataAccess Accessor { get; }
	public Configuration.DuckDBConnectionSettings ConnectionSettings { get; }
	#endregion

	#region 释放方法
	public void Dispose()
	{
		Mapping.Loaders.Remove(_loader);
		DataEnvironment.Drivers.Remove(DuckDBDriver.Instance);
	}
	#endregion
}

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}
