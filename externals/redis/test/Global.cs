using System;

using StackExchange.Redis;

namespace Zongsoft.Externals.Redis.Tests;

public static class Global
{
	#region 常量定义
	public const string Server = "127.0.0.1:6379";
	public const string Password = "xxxxxx";
	#endregion

	#region 静态属性
	public static bool IsTestingEnabled => System.Diagnostics.Debugger.IsAttached || IsEnabled("ZONGSOFT_REDIS_TESTS");
	#endregion

	#region 公共方法
	public static bool IsAvailable()
	{
		try
		{
			using var connection = ConnectionMultiplexer.Connect($"{Server},password={Password},connectTimeout=2000");
			return connection.GetDatabase().Ping() >= TimeSpan.Zero;
		}
		catch
		{
			return false;
		}
	}
	#endregion

	#region 私有方法
	private static bool IsEnabled(string name)
	{
		var value = Environment.GetEnvironmentVariable(name);

		return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
		       string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase) ||
		       string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
	}
	#endregion
}
