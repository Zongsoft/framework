﻿using System;

namespace Zongsoft.Data.Influx.Tests;

public static class Global
{
	#region 静态属性
	public static bool IsTestingEnabled => System.Diagnostics.Debugger.IsAttached;
	#endregion
}
