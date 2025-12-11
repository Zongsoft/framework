using System;

using Xunit;

namespace Zongsoft.Data.MySql.Tests;

public class DatabaseFixture : IDisposable
{
	#region 常量定义
	private const string CONNECTION_STRING = @"Server=127.0.0.1;Database=zongsoft;UserName=program;Password=xxxxxx;Charset=utf8mb4;ConvertZeroDatetime=true;AllowZeroDateTime=true;AllowLoadLocalInfile=true;AllowPublicKeyRetrieval=true;";
	#endregion

	#region 私有变量
	private readonly Mapping.Loader _loader;
	#endregion

	#region 构造函数
	public DatabaseFixture()
	{
		var command = new Zongsoft.Data.Metadata.Profiles.MetadataCommand(null, "TruncateLog");
		command.Scriptor.SetScript(MySqlDriver.NAME, "TRUNCATE TABLE `Log`");
		Mapping.Commands.Add(command);

		Mapping.Loaders.Add(_loader = new Metadata.Profiles.MetadataFileLoader(AppContext.BaseDirectory));
		DataEnvironment.Drivers.Add(MySqlDriver.Instance);

		this.ConnectionSettings = Configuration.MySqlConnectionSettingsDriver.Instance.GetSettings(CONNECTION_STRING);
		this.Accessor = DataAccessProvider.Instance.GetAccessor("Zongsoft.Data.MySql.Tests", new DataAccessOptions([this.ConnectionSettings]));
		this.Accessor.Sequencer.Sequence = new Zongsoft.Data.Tests.SequenceMocker() { Latency = TimeSpan.FromMilliseconds(50) };
	}
	#endregion

	#region 公共属性
	public DataAccess Accessor { get; }
	public Configuration.MySqlConnectionSettings ConnectionSettings { get; }
	#endregion

	#region 释放方法
	public void Dispose()
	{
		Mapping.Loaders.Remove(_loader);
		DataEnvironment.Drivers.Remove(MySqlDriver.Instance);
	}
	#endregion
}

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}
