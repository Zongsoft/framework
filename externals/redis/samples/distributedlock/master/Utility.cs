using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Components;
using Zongsoft.Externals.Redis;
using Zongsoft.Services.Distributing;

namespace Zongsoft.Externals.Redis.DistributedLock;

internal static class Utility
{
	public const string DefaultHost = "127.0.0.1";
	public const int DefaultPort = 6379;
	public const int DefaultDatabase = 15;
	public const string DefaultPassword = "xxxxxx";
	public const string DefaultFramework = "net10.0";

	public static string GetNamespace(CommandContext context)
	{
		var @namespace = context.Options.GetValue<string>("namespace", null);

		if(!string.IsNullOrWhiteSpace(@namespace))
			return @namespace;

		var runId = context.Options.GetValue<string>("run-id", null);
		var scenario = context.Options.GetValue<string>("scenario", "mutex");

		if(string.IsNullOrWhiteSpace(runId))
			runId = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");

		return $"DistributedLock:{runId}:{scenario}";
	}

	public static string GetConnectionString(CommandContext context)
	{
		var connection = context.Options.GetValue<string>("connection", null);

		if(!string.IsNullOrWhiteSpace(connection))
			return connection;

		connection = Environment.GetEnvironmentVariable("REDIS_CONNECTION");

		if(!string.IsNullOrWhiteSpace(connection))
			return connection;

		var password = context.Options.GetValue<string>("password", null);

		if(string.IsNullOrEmpty(password))
			password = Environment.GetEnvironmentVariable("REDIS_PASSWORD") ?? DefaultPassword;

		return $"server={DefaultHost};port={DefaultPort};password={password};database={DefaultDatabase};client=Zongsoft.RedisDistributedLock;";
	}

	public static RedisService GetRedis(CommandContext context, string @namespace = null) =>
		new("distributed-lock", GetConnectionString(context))
		{
			Namespace = @namespace ?? GetNamespace(context),
			Tokenizer = DistributedLockTokenizer.Guid,
		};

	public static string ResolveSlaverExecutable(string path)
	{
		if(!string.IsNullOrWhiteSpace(path))
			return Path.GetFullPath(path);

		var configuration =
#if DEBUG
			"Debug";
#else
			"Release";
#endif

		var fileName = OperatingSystem.IsWindows() ?
			"Zongsoft.Externals.Redis.DistributedLock.Slaver.exe" :
			"Zongsoft.Externals.Redis.DistributedLock.Slaver";

		return Path.GetFullPath(Path.Combine(
			AppContext.BaseDirectory,
			"..",
			"..",
			"..",
			"..",
			"slaver",
			"bin",
			configuration,
			DefaultFramework,
			fileName));
	}

	public static async ValueTask ResetAsync(RedisService redis, CancellationToken cancellation)
	{
		foreach(var key in Keys.All)
			await redis.RemoveAsync(key, cancellation);
	}

	public static ValueTask<long> ReadCounterAsync(RedisService redis, string key, CancellationToken cancellation) =>
		redis.GetValueAsync<long>(key, cancellation);

	public static string Sanitize(string connectionString)
	{
		var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		for(var index = 0; index < parts.Length; index++)
		{
			if(parts[index].StartsWith("password=", StringComparison.OrdinalIgnoreCase))
				parts[index] = "password=***";
		}

		return string.Join(';', parts);
	}

	public static void WritePair<T>(ICommandOutlet output, string name, T value) =>
		WritePair(output, name, value, CommandOutletColor.Green);

	public static void WritePair<T>(ICommandOutlet output, string name, T value, CommandOutletColor valueColor)
	{
		var content = CommandOutletContent.Create(CommandOutletColor.DarkCyan, name.PadRight(11))
			.Append(CommandOutletColor.DarkGray, ": ")
			.AppendLine(valueColor, value);

		output.Write(content);
	}

	public static class Keys
	{
		public const string Lock = "lock";
		public const string Active = "active";
		public const string Entered = "entered";
		public const string Completed = "completed";
		public const string Violations = "violations";

		public static readonly string[] All =
		[
			Lock,
			Active,
			Entered,
			Completed,
			Violations,
		];
	}
}
