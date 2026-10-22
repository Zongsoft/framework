using System;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Data.TDengine.Tests;

public class DatabaseFixture : IAsyncDisposable
{
	#region 常量定义
	private const string CONNECTION_STRING = @"Server=127.0.0.1;Port=6041;UserName=root;Password=taosdata;Database=zongsoft;AutoReconnect=true;EnableCompression=true;";
	#endregion

	#region 私有变量
	private readonly Mapping.Loader _loader;
	#endregion

	#region 构造函数
	public DatabaseFixture()
	{
		Mapping.Loaders.Add(_loader = new Metadata.Profiles.MetadataFileLoader(AppContext.BaseDirectory));
		DataEnvironment.Drivers.Add(TDengineDriver.Instance);

		this.ConnectionSettings = Configuration.TDengineConnectionSettingsDriver.Instance.GetSettings(CONNECTION_STRING);
		this.ConnectionSettings.Protocol = Configuration.TDengineConnectionProtocol.WebSocket;
		this.Accessor = DataAccessProvider.Instance.GetAccessor("Zongsoft.Data.TDengine.Tests", new DataAccessOptions([this.ConnectionSettings]));
	}
	#endregion

	#region 公共属性
	public DataAccess Accessor { get; }
	public Configuration.TDengineConnectionSettings ConnectionSettings { get; }
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
			DataEnvironment.Drivers.Remove(TDengineDriver.Instance);
		}
	}
	#endregion
}

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
}
