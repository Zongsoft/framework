using System;
using System.Threading.Tasks;

namespace Zongsoft.Data.ClickHouse.Tests;

public class DatabaseFixture : IAsyncDisposable
{
	#region 私有变量
	private readonly Mapping.Loader _loader;
	#endregion

	#region 构造函数
	public DatabaseFixture()
	{
		if(!Global.IsTestingEnabled)
			return;

		var connectionString = Environment.GetEnvironmentVariable("ZONGSOFT_CLICKHOUSE_TEST_CONNECTION_STRING");
		if(string.IsNullOrWhiteSpace(connectionString))
			throw new InvalidOperationException("ZONGSOFT_CLICKHOUSE_TEST_CONNECTION_STRING is required for ClickHouse tests.");

		Mapping.Loaders.Add(_loader = new Metadata.Profiles.MetadataFileLoader(AppContext.BaseDirectory));
		DataEnvironment.Drivers.Add(ClickHouseDriver.Instance);

		this.ConnectionSettings = Configuration.ClickHouseConnectionSettingsDriver.Instance.GetSettings(connectionString);
		this.Accessor = DataAccessProvider.Instance.GetAccessor("Test", new DataAccessOptions([this.ConnectionSettings]));
	}
	#endregion

	#region 公共属性
	public DataAccess Accessor { get; }
	public Configuration.ClickHouseConnectionSettings ConnectionSettings { get; }
	#endregion

	#region 释放方法
	public async ValueTask DisposeAsync()
	{
		try
		{
			if(this.Accessor != null)
				await this.Accessor.DisposeAsync();
		}
		finally
		{
			if(_loader != null)
			{
				Mapping.Loaders.Remove(_loader);
				DataEnvironment.Drivers.Remove(ClickHouseDriver.Instance);
			}
		}
	}
	#endregion
}

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}
