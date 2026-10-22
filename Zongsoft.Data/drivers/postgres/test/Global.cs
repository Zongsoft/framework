using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.Data.PostgreSql.Tests;

public static class Global
{
	#region 静态属性
	public static bool IsTestingEnabled => System.Diagnostics.Debugger.IsAttached || IsEnabled("ZONGSOFT_DATA_TESTS");
	#endregion

	#region 异步枚举
	public static async ValueTask<T[]> ToArrayAsync<T>(IAsyncEnumerable<T> source, CancellationToken cancellation = default)
	{
		var items = new List<T>();

		await foreach(var item in source.WithCancellation(cancellation))
			items.Add(item);

		return items.ToArray();
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
