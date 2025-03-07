using System;

namespace Zongsoft.Data.Influx.Tests;

public static class Global
{
	#region 初始化器
	static Global()
	{
		DataEnvironment.Drivers.Add(InfluxDriver.Instance);
		Mapping.Loaders.Add(new Metadata.Profiles.MetadataFileLoader(AppContext.BaseDirectory));
	}
	#endregion

	#region 连接常量
	internal const string CONNECTION_STRING = @"server=http://192.168.2.222:8086;database=Test;organization=0f99a31eaaa485f5;token=DqImDLUR5rf3OObPO0qGw-YNTxh8MBW-gZBgFKAPFkizRVuYK7DEkdeB-Oxq4RmSizpaXcqzNVq_ZT6JjrB2eQ==";
	#endregion

	#region 连接设置
	public static Configuration.InfluxConnectionSettings ConnectionSettings => Configuration.InfluxConnectionSettingsDriver.Instance.GetSettings(CONNECTION_STRING);
	#endregion
}
