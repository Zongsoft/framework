using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.Data.TDengine.Tests;

public static class Global
{
	#region 静态属性
	public static bool IsTestingEnabled => System.Diagnostics.Debugger.IsAttached || IsEnabled("ZONGSOFT_DATA_TESTS");
	#endregion

	public static async ValueTask<T[]> ToArrayAsync<T>(IAsyncEnumerable<T> source, CancellationToken cancellation = default)
	{
		var values = new List<T>();

		await foreach(var value in source.WithCancellation(cancellation))
			values.Add(value);

		return values.ToArray();
	}

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
