using System;

using Zongsoft.Data.Metadata;

using Xunit;

namespace Zongsoft.Data.MsSql.Tests;

public class DatabaseFixture : IDisposable
{
	#region 常量定义
	private const string CONNECTION_STRING = @"Server=127.0.0.1;Database=zongsoft;UserName=program;Password=xxxxxx;TrustServerCertificate=true;";
	#endregion

	#region 私有变量
	private readonly Mapping.Loader _loader;
	#endregion

	#region 构造函数
	public DatabaseFixture()
	{
		Mapping.Commands
			.Add("TruncateLog", DataCommandMutability.Delete)
			.Script(MsSqlDriver.NAME, "TRUNCATE TABLE [Log]");

		Mapping.Loaders.Add(_loader = new Metadata.Profiles.MetadataFileLoader(AppContext.BaseDirectory));
		DataEnvironment.Drivers.Add(MsSqlDriver.Instance);

		this.ConnectionSettings = Configuration.MsSqlConnectionSettingsDriver.Instance.GetSettings(CONNECTION_STRING);
		this.Accessor = DataAccessProvider.Instance.GetAccessor("Zongsoft.Data.MsSql.Tests", new DataAccessOptions([this.ConnectionSettings]));
		this.Accessor.Sequencer.Sequence = new Zongsoft.Data.Tests.SequenceMocker(TimeSpan.FromMilliseconds(10));
	}
	#endregion

	#region 公共属性
	public DataAccess Accessor { get; }
	public Configuration.MsSqlConnectionSettings ConnectionSettings { get; }
	#endregion

	#region 释放方法
	public void Dispose()
	{
		Mapping.Loaders.Remove(_loader);
		DataEnvironment.Drivers.Remove(MsSqlDriver.Instance);
	}
	#endregion
}

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}
