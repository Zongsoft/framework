using System;

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
	public static readonly TimeSpan StateExpiry = TimeSpan.FromMinutes(10);

	public static string GetNamespace(CommandContext context)
	{
		var @namespace = context.Options.GetValue<string>("namespace", null);

		if(!string.IsNullOrWhiteSpace(@namespace))
			return @namespace;

		var runId = context.Options.GetValue<string>("run-id", null);
		var scenario = context.Options.GetValue<string>("scenario", "mutex");

		if(string.IsNullOrWhiteSpace(runId))
			throw new CommandException("Missing the run-id option. Specify --run-id:<id> or --namespace:<namespace>.");

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

	public static RedisService GetRedis(CommandContext context, string @namespace) =>
		new("distributed-lock", GetConnectionString(context))
		{
			Namespace = @namespace,
			Tokenizer = DistributedLockTokenizer.Guid,
		};

	public static class Keys
	{
		public const string Lock = "lock";
		public const string Active = "active";
		public const string Entered = "entered";
		public const string Completed = "completed";
		public const string Violations = "violations";
	}
}
