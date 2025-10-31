using System;

namespace Zongsoft.Data.PostgreSql.Tests;

public static class Global
{
	#region 静态属性
	public static bool IsTestingEnabled => System.Diagnostics.Debugger.IsAttached;
	#endregion
}
