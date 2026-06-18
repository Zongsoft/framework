using System;

namespace Zongsoft.Data.PostgreSql.Tests;

public static class Global
{
	#region 静态属性
	public static bool IsTestingEnabled => System.Diagnostics.Debugger.IsAttached || IsEnabled("ZONGSOFT_DATA_TESTS");
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
