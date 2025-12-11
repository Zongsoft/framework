using System;

using Xunit;

namespace Zongsoft.Data.PostgreSql.Tests;

public class DatabaseFixture : IDisposable
{
	#region 常量定义
	private const string CONNECTION_STRING = @"server=127.0.0.1;database=zongsoft;username=program;password=xxxxxx;";
	#endregion

	#region 私有变量
	private readonly Mapping.Loader _loader;
	#endregion

	#region 构造函数
	public DatabaseFixture()
	{
		var command = new Zongsoft.Data.Metadata.Profiles.MetadataCommand(null, "TruncateLog");
		command.Scriptor.SetScript(PostgreSqlDriver.NAME, "TRUNCATE TABLE \"Log\"");
		Mapping.Commands.Add(command);

		Mapping.Loaders.Add(_loader = new Metadata.Profiles.MetadataFileLoader(AppContext.BaseDirectory));
		DataEnvironment.Drivers.Add(PostgreSqlDriver.Instance);

		this.ConnectionSettings = Configuration.PostgreSqlConnectionSettingsDriver.Instance.GetSettings(CONNECTION_STRING);
		this.Accessor = DataAccessProvider.Instance.GetAccessor("Zongsoft.Data.PostgreSql.Tests", new DataAccessOptions([this.ConnectionSettings]));
	}
	#endregion

	#region 公共属性
	public DataAccess Accessor { get; }
	public Configuration.PostgreSqlConnectionSettings ConnectionSettings { get; }
	#endregion

	#region 释放方法
	public void Dispose()
	{
		Mapping.Loaders.Remove(_loader);
		DataEnvironment.Drivers.Remove(PostgreSqlDriver.Instance);
	}
	#endregion
}

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}
