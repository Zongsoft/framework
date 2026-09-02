using System;

namespace Zongsoft.Messaging.Storages.Data.Tests;

internal static class Global
{
	public const string ExternalTestsDisabled = "Set ZONGSOFT_MESSAGING_DATABASE_TESTS=1 to enable external database integration tests.";

	public static bool ExternalTestsEnabled => IsEnabled("ZONGSOFT_MESSAGING_DATABASE_TESTS");

	public static string GetConnectionString(string driver, string fallback) =>
		Environment.GetEnvironmentVariable($"ZONGSOFT_MESSAGING_DATABASE_{driver.ToUpperInvariant()}_CONNECTION_STRING") ?? fallback;

	private static bool IsEnabled(string name)
	{
		var value = Environment.GetEnvironmentVariable(name);
		return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
		       string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase) ||
		       string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
	}
}
