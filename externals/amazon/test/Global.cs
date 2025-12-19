using System;

namespace Zongsoft.Externals.Amazon.Tests;

public static class Global
{
	#region 静态属性
	public static bool IsTestingEnabled => System.Diagnostics.Debugger.IsAttached;
	#endregion
}
