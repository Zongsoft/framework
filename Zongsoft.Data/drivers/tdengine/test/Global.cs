using System;

namespace Zongsoft.Data.TDengine.Tests;

public static class Global
{
	#region 初始化器
	static Global()
	{
		DataEnvironment.Drivers.Add(TDengineDriver.Instance);
		Mapping.Loaders.Add(new Metadata.Profiles.MetadataFileLoader(AppContext.BaseDirectory));
	}
	#endregion

	#region 连接常量
	internal const string CONNECTION_STRING = @"Server=192.168.2.200;UserName=program;Password=xxxxxx;Database=automao;AutoReconnect=true;EnableCompression=true;";
	#endregion

	#region 连接设置
	public static Configuration.TDengineConnectionSettings ConnectionSettings => Configuration.TDengineConnectionSettingsDriver.Instance.GetSettings(CONNECTION_STRING);
	#endregion
}
