using System;
using System.Threading.Tasks;

namespace Zongsoft.Externals.Etcd.Tests;

public static class Global
{
	public const string Server = "127.0.0.1:2379";
	public const string ConnectionString = "server=127.0.0.1;port=2379;timeout=5s";

	public static bool IsTestingEnabled => System.Diagnostics.Debugger.IsAttached || IsEnabled("ZONGSOFT_ETCD_TESTS");

	public static async Task<bool> IsAvailableAsync()
	{
		try
		{
			using var source = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(2));
			await using var service = new EtcdService("test", ConnectionString);
			await service.HeartbeatAsync(source.Token);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private static bool IsEnabled(string name)
	{
		var value = Environment.GetEnvironmentVariable(name);
		return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
		       string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase) ||
		       string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
	}
}
