using System;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Common;
using Zongsoft.Caching;
using Zongsoft.Terminals;
using Zongsoft.Components;

namespace Zongsoft.Externals.Redis.DistributedCache.Samples;

internal class Program
{
	static async Task Main(string[] args)
	{
		using var cache = new RedisService("Redis",
			Configuration.RedisConnectionSettingsDriver.Instance.GetSettings("Redis", $"server=127.0.0.1:6379;password=xxxxxx;group=Zongsoft.Externals.Redis.Samples;client=Sample-{Guid.NewGuid():N};timeout=10s;deadline=3;idleTimeout=30s;"))
		{
			Namespace = "DistributedCache",
		};

		IDistributedCacheSubscription subscription = null;
		var executor = Terminal.Console.Executor;

		executor.Command("reset", context => Handler.Instance.Reset());
		executor.Command("close", context => cache.Dispose());

		executor.Command("set", async (context, cancellation) =>
		{
			var key = GetRequiredKey(context);
			var requisite = GetRequisite(context.Options.GetValue<string>("requisite"));

			if(context.Arguments.IsEmpty)
				throw new CommandException("Missing the value to set.");

			var value = string.Join(" ", context.Arguments);
			var expiry = context.Options.Contains("expiry") ? context.Options.GetValue<TimeSpan>("expiry") : (TimeSpan?)null;
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();

			var succeeded = expiry.HasValue ?
				await cache.SetValueAsync(key, value, expiry.Value, requisite, cancellation) :
				await cache.SetValueAsync(key, value, requisite, cancellation);

			stopwatch.Stop();

			if(succeeded)
			{
				context.Output.WriteLine(CommandOutletColor.DarkGreen,
					$"OK. The '{key}' entry was set to '{value}' with {(expiry.HasValue ? $"expiry:{expiry.Value}" : "no expiry")} and requisite:{requisite}. (Elapsed: {stopwatch.Elapsed})");
			}
			else
			{
				context.Output.WriteLine(CommandOutletColor.DarkRed,
					$"Failed. The '{key}' entry was not set because the requisite '{requisite}' was not satisfied.");
			}
		});

		executor.Command("get", async (context, cancellation) =>
		{
			if(context.Arguments.IsEmpty)
				return;

			foreach(var key in context.Arguments)
			{
				var (value, expiry) = await cache.GetValueExpiryAsync<string>(key, cancellation);

				if(value == null)
				{
					context.Output.WriteLine(CommandOutletColor.DarkRed, $"The '{key}' cache entry does not exist.");
					return;
				}

				var content = CommandOutletContent.Create()
					.Append(CommandOutletColor.DarkCyan, "Key:")
					.AppendLine(CommandOutletColor.DarkGreen, key)
					.Append(CommandOutletColor.DarkCyan, "Value:")
					.AppendLine(CommandOutletColor.DarkYellow, value)
					.Append(CommandOutletColor.DarkCyan, "Expiry:")
					.AppendLine(CommandOutletColor.DarkGreen, expiry.HasValue ? expiry.Value.ToString() : "Permanent");

				context.Output.Write(content);
			}
		});

		executor.Command("exists", async (context, cancellation) =>
		{
			if(context.Arguments.IsEmpty)
				return;

			foreach(var key in context.Arguments)
			{
				var exists = await cache.ExistsAsync(key, cancellation);
				context.Output.WriteLine(exists ? CommandOutletColor.DarkGreen : CommandOutletColor.DarkRed, $"The '{key}' cache entry {(exists ? "exists" : "does not exist")}.");
			}
		});

		executor.Command("expiry", async (context, cancellation) =>
		{
			if(context.Arguments.IsEmpty)
				return;

			foreach(var key in context.Arguments)
			{
				//带 --expiry 选项时为指定缓存项设置生存时长（0 表示永不过期），否则输出其剩余生存时长
				if(context.Options.Contains("expiry"))
				{
					var expiry = context.Options.GetValue<TimeSpan>("expiry");
					var succeeded = await cache.SetExpiryAsync(key, expiry, cancellation);

					context.Output.WriteLine(succeeded ? CommandOutletColor.DarkGreen : CommandOutletColor.DarkRed,
						succeeded ?
						$"The expiry of '{key}' was set to {(expiry == TimeSpan.Zero ? "Permanent" : expiry.ToString())}." :
						$"Failed to set the expiry of '{key}'.");
					return;
				}

				var remaining = await cache.GetExpiryAsync(key, cancellation);

				context.Output.WriteLine(remaining.HasValue ? CommandOutletColor.DarkGreen : CommandOutletColor.DarkYellow,
					remaining.HasValue ?
					$"The remaining expiry of '{key}' is {remaining.Value}." :
					$"The '{key}' cache entry does not exist.");
			}
		});

		executor.Command("remove", async (context, cancellation) =>
		{
			if(context.Arguments.IsEmpty)
				return;

			foreach(var key in context.Arguments)
			{
				var removed = await cache.RemoveAsync(key, cancellation);
				context.Output.WriteLine(removed ? CommandOutletColor.DarkGreen : CommandOutletColor.DarkRed, $"The '{key}' cache entry was {(removed ? "removed" : "not found")}.");
			}
		});

		executor.Command("count", async (context, cancellation) =>
		{
			var count = await cache.GetCountAsync(cancellation);

			context.Output.WriteLine(CommandOutletColor.DarkGreen, $"The cache contains {count} entr{(count == 1 ? "y" : "ies")}.");
		});

		executor.Command("find", async (context, cancellation) =>
		{
			if(context.Arguments.IsEmpty)
				return;

			foreach(var pattern in context.Arguments)
			{
				var index = 0;

				await foreach(var key in cache.FindAsync(pattern, cancellation))
					context.Output.WriteLine(CommandOutletColor.DarkGreen, $"[{++index}] {key}");

				if(index == 0)
					context.Output.WriteLine(CommandOutletColor.DarkYellow, $"No cache keys matched the pattern: {pattern}");
			}
		});

		executor.Command("purge", async (context, cancellation) =>
		{
			await cache.ClearAsync(cancellation);
			context.Output.WriteLine(CommandOutletColor.DarkGreen, "The cache was cleared.");
		});

		executor.Command("subscribe", async (context, cancellation) =>
		{
			if(subscription != null && !subscription.IsClosed)
			{
				context.Output.WriteLine(CommandOutletColor.DarkRed, "A subscription already exists; please unsubscribe first.");
				return;
			}

			var prefix = context.Options.GetValue<string>("prefix", null);
			var kind = GetNotificationKind(context.Options.GetValue<string>("kind", "all"));

			subscription = await cache.SubscribeAsync(Handler.Instance, new DistributedCacheSubscriptionOptions(prefix, kind), cancellation);

			context.Output.WriteLine(CommandOutletColor.DarkGreen,
				$"The subscription was successful. (Prefix:{(string.IsNullOrEmpty(subscription.Options.Prefix) ? "N/A" : subscription.Options.Prefix)}, Kind:{subscription.Options.Kind})");
		});

		executor.Command("unsubscribe", async (context, cancellation) =>
		{
			if(subscription == null || subscription.IsClosed)
			{
				context.Output.WriteLine(CommandOutletColor.DarkRed, "There is no active subscription.");
				return;
			}

			await subscription.UnsubscribeAsync(cancellation);
			subscription = null;
			context.Output.WriteLine(CommandOutletColor.DarkGreen, "The subscription was cancelled.");
		});

		executor.Command("info", context =>
		{
			var content = CommandOutletContent.Create()
				.AppendLine(CommandOutletColor.Yellow, new string('-', 50))
				.AppendLine(CommandOutletColor.Cyan, "Redis Distributed Cache")
				.Append(CommandOutletColor.DarkCyan, "Name".PadRight(13)).Append(CommandOutletColor.DarkGray, ": ").AppendLine(CommandOutletColor.Green, cache.Name)
				.Append(CommandOutletColor.DarkCyan, "Namespace".PadRight(13)).Append(CommandOutletColor.DarkGray, ": ").AppendLine(CommandOutletColor.Green, string.IsNullOrEmpty(cache.Namespace) ? "N/A" : cache.Namespace)
				.Append(CommandOutletColor.DarkCyan, "Database".PadRight(13)).Append(CommandOutletColor.DarkGray, ": ").AppendLine(CommandOutletColor.Green, cache.DatabaseId)
				.Append(CommandOutletColor.DarkCyan, "Count".PadRight(13)).Append(CommandOutletColor.DarkGray, ": ").AppendLine(CommandOutletColor.Green, cache.GetCount())
				.AppendLine(CommandOutletColor.Yellow, new string('-', 50));

			if(subscription != null && !subscription.IsClosed)
			{
				content.Append(CommandOutletColor.DarkCyan, "Subscription".PadRight(13)).Append(CommandOutletColor.DarkGray, ": ").AppendLine(CommandOutletColor.Green, "Active")
					.Append(CommandOutletColor.DarkCyan, "Prefix".PadRight(13)).Append(CommandOutletColor.DarkGray, ": ").AppendLine(CommandOutletColor.Green, string.IsNullOrEmpty(subscription.Options.Prefix) ? "N/A" : subscription.Options.Prefix)
					.Append(CommandOutletColor.DarkCyan, "Kind".PadRight(13)).Append(CommandOutletColor.DarkGray, ": ").AppendLine(CommandOutletColor.Green, subscription.Options.Kind)
					.Append(CommandOutletColor.DarkCyan, "Pending".PadRight(13)).Append(CommandOutletColor.DarkGray, ": ").AppendLine(CommandOutletColor.Green, subscription.PendingCount);
			}
			else
			{
				content.AppendLine(CommandOutletColor.DarkGray, "Subscription: N/A");
			}

			context.Output.Write(content);
		});

		executor.Aliaser.Set("remove", "del");
		executor.Aliaser.Set("subscribe", "sub");
		executor.Aliaser.Set("unsubscribe", "unsub");

		var splash = CommandOutletContent.Create()
			.AppendLine(CommandOutletColor.Yellow, new string('·', 50))
			.AppendLine(CommandOutletColor.Cyan, "Welcome to the Redis Distributed Cache Client.".Justify(50))
			.AppendLine(CommandOutletColor.Yellow, new string('·', 50));

		await executor.RunAsync(splash);
	}

	private static string GetRequiredKey(CommandContext context)
	{
		var key = context.Options.GetValue<string>("key");

		if(string.IsNullOrEmpty(key))
			throw new CommandOptionException("key", "The key is required.");

		return key;
	}

	private static CacheRequisite GetRequisite(string text) => (text ?? "always").ToLowerInvariant() switch
	{
		"alway" or "always" => CacheRequisite.Always,
		"exists" => CacheRequisite.Exists,
		"notexists" or "not-exists" => CacheRequisite.NotExists,
		_ => throw new CommandOptionValueException("requisite", text),
	};

	private static DistributedCacheNotificationKind GetNotificationKind(string text) => (text ?? "all").ToLowerInvariant() switch
	{
		"all" => DistributedCacheNotificationKind.All,
		"updated" => DistributedCacheNotificationKind.Updated,
		"removed" => DistributedCacheNotificationKind.Removed,
		"expired" => DistributedCacheNotificationKind.Expired,
		"evicted" => DistributedCacheNotificationKind.Evicted,
		_ => throw new CommandOptionValueException("kind", text),
	};

	internal sealed class Handler : HandlerBase<DistributedCacheNotification>
	{
		#region 单例字段
		public static readonly Handler Instance = new();
		#endregion

		#region 私有变量
		private volatile int _count;
		#endregion

		#region 重置方法
		public void Reset() => _count = 0;
		#endregion

		#region 重写方法
		protected override ValueTask OnHandleAsync(DistributedCacheNotification notification, Zongsoft.Collections.Parameters parameters, CancellationToken cancellation)
		{
			var count = Interlocked.Increment(ref _count);
			var content = CommandOutletContent.Create()
				.Append(CommandOutletColor.Cyan, "[Received]")
				.Append(CommandOutletColor.DarkYellow, $"#{count}")
				.Append(CommandOutletColor.DarkCyan, " Kind:")
				.Append(CommandOutletColor.DarkGreen, notification.Kind)
				.Append(CommandOutletColor.DarkCyan, " Key:")
				.AppendLine(CommandOutletColor.DarkGreen, notification.Key);

			Terminal.Console.Executor.Output.Write(content);
			return ValueTask.CompletedTask;
		}
		#endregion
	}
}
