using System;

using Xunit;

namespace Zongsoft.Data.ClickHouse.Tests;

public class DatabaseFixture : IDisposable
{
	#region 常量定义
	private const string CONNECTION_STRING = @"server=http://192.168.2.222:8086;database=Test;organization=0f99a31eaaa485f5;token=DqImDLUR5rf3OObPO0qGw-YNTxh8MBW-gZBgFKAPFkizRVuYK7DEkdeB-Oxq4RmSizpaXcqzNVq_ZT6JjrB2eQ==";
	#endregion

	#region 私有变量
	private readonly Mapping.Loader _loader;
	#endregion

	#region 构造函数
	public DatabaseFixture()
	{
		Mapping.Loaders.Add(_loader = new Metadata.Profiles.MetadataFileLoader(AppContext.BaseDirectory));
		DataEnvironment.Drivers.Add(ClickHouseDriver.Instance);

		this.ConnectionSettings = Configuration.ClickHouseConnectionSettingsDriver.Instance.GetSettings(CONNECTION_STRING);
		this.Accessor = DataAccessProvider.Instance.GetAccessor("Test", new DataAccessOptions([this.ConnectionSettings]));
	}
	#endregion

	#region 公共属性
	public DataAccess Accessor { get; }
	public Configuration.ClickHouseConnectionSettings ConnectionSettings { get; }
	#endregion

	#region 释放方法
	public void Dispose()
	{
		Mapping.Loaders.Remove(_loader);
		DataEnvironment.Drivers.Remove(ClickHouseDriver.Instance);
	}
	#endregion
}

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}
