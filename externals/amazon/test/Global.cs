using System;

namespace Zongsoft.Externals.Amazon.Tests;

public static class Global
{
	#region 静态属性
	public static bool IsTestingEnabled => string.Equals(Environment.GetEnvironmentVariable("ZONGSOFT_AMAZON_TESTS"), "1", StringComparison.Ordinal);
	#endregion
}
