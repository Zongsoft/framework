using System;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Components;
using Zongsoft.Services.Distributing;

namespace Zongsoft.Externals.Redis.DistributedLock;

internal sealed class RunCommand : CommandBase<CommandContext>
{
	public RunCommand() : base("Run") { }

	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		var settings = RunSettings.Get(context);

		using var redis = Utility.GetRedis(context, settings.Namespace);
		using var timeout = new CancellationTokenSource(settings.Timeout);
		using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellation, timeout.Token);

		for(var index = 0; index < settings.Iterations; index++)
		{
			await using var locker = settings.RenewalInterval.HasValue ?
				await redis.AcquireAsync(Utility.Keys.Lock, new DistributedLockOptions(settings.Expiry) { RenewalInterval = settings.RenewalInterval }, linked.Token) :
				await redis.AcquireAsync(Utility.Keys.Lock, settings.Expiry, linked.Token);
			await locker.EnterAsync(linked.Token);

			var activeEntered = await redis.IncreaseAsync(Utility.Keys.Active, 1, 0, Utility.StateExpiry, linked.Token);
			var entered = await redis.IncreaseAsync(Utility.Keys.Entered, 1, 0, Utility.StateExpiry, linked.Token);

			try
			{
				if(activeEntered != 1)
				{
					await redis.IncreaseAsync(Utility.Keys.Violations, 1, 0, Utility.StateExpiry, linked.Token);
					WriteViolation(context.Output, settings.WorkerId, index + 1, activeEntered, entered);
				}
				else if(settings.Verbose)
				{
					WriteEntered(context.Output, settings.WorkerId, index + 1, entered);
				}

				//进入临界区后，立即用当前锁的栅栏令牌(FencingToken)执行一次存储写入
				await WriteFenceAsync(redis, context.Output, settings, index + 1, locker.FencingToken, linked.Token);

				//模拟业务处理（临界区）
				await Task.Delay(settings.Hold, linked.Token);

				//业务处理结束后再执行一次栅栏令牌校验的存储写入：
				//若锁已经过期并被其他进程重新获取，本次写入的令牌必然小于存储中的最大令牌，从而被拒绝
				await WriteFenceAsync(redis, context.Output, settings, index + 1, locker.FencingToken, linked.Token);
			}
			finally
			{
				var activeLeft = await redis.DecreaseAsync(Utility.Keys.Active, 1, 0, Utility.StateExpiry, CancellationToken.None);
				await redis.IncreaseAsync(Utility.Keys.Completed, 1, 0, Utility.StateExpiry, CancellationToken.None);

				if(activeLeft < 0)
				{
					await redis.IncreaseAsync(Utility.Keys.Violations, 1, 0, Utility.StateExpiry, CancellationToken.None);
					context.Output.WriteLine(CommandOutletColor.DarkRed, $"worker={settings.WorkerId} active counter dropped below zero.");
				}
			}
		}

		return 0;
	}

	private static async ValueTask WriteFenceAsync(RedisService redis, ICommandOutlet output, RunSettings settings, int iteration, long fencingToken, CancellationToken cancellation)
	{
		//读取存储中已记录的最大栅栏令牌
		var stored = await redis.GetValueAsync<long>(Utility.Keys.Fence, cancellation);

		if(fencingToken >= stored)
		{
			//当前令牌不小于存储中的最大令牌，写入被接受并更新存储中的最大令牌
			await redis.SetValueAsync(Utility.Keys.Fence, fencingToken, Utility.StateExpiry, cancellation: cancellation);

			if(settings.Verbose)
				WriteFenceWrite(output, settings.WorkerId, iteration, fencingToken);
		}
		else
		{
			//存储中已存在更大的令牌，说明锁已易主，本次写入被拒绝（过期写入）
			await redis.IncreaseAsync(Utility.Keys.Stale, 1, 0, Utility.StateExpiry, cancellation);
			WriteStaleWrite(output, settings.WorkerId, iteration, fencingToken, stored);
		}
	}

	private static void WriteEntered(ICommandOutlet output, int workerId, int iteration, long entered)
	{
		var content = CommandOutletContent.Create(CommandOutletColor.DarkGray, "worker=")
			.Append(CommandOutletColor.DarkGreen, workerId)
			.Append(CommandOutletColor.DarkGray, " iteration=")
			.Append(CommandOutletColor.DarkGreen, iteration)
			.Append(CommandOutletColor.DarkGray, " entered=")
			.AppendLine(CommandOutletColor.DarkGreen, entered);

		output.Write(content);
	}

	private static void WriteViolation(ICommandOutlet output, int workerId, int iteration, long active, long entered)
	{
		var content = CommandOutletContent.Create(CommandOutletStyles.Bold, CommandOutletColor.DarkRed, "violation ")
			.Append(CommandOutletColor.DarkGray, "worker=")
			.Append(CommandOutletColor.DarkYellow, workerId)
			.Append(CommandOutletColor.DarkGray, " iteration=")
			.Append(CommandOutletColor.DarkYellow, iteration)
			.Append(CommandOutletColor.DarkGray, " active=")
			.Append(CommandOutletColor.DarkRed, active)
			.Append(CommandOutletColor.DarkGray, " entered=")
			.AppendLine(CommandOutletColor.DarkYellow, entered);

		output.Write(content);
	}

	private static void WriteFenceWrite(ICommandOutlet output, int workerId, int iteration, long fencingToken)
	{
		var content = CommandOutletContent.Create(CommandOutletColor.DarkGray, "fence-write ")
			.Append(CommandOutletColor.DarkGray, "worker=")
			.Append(CommandOutletColor.DarkGreen, workerId)
			.Append(CommandOutletColor.DarkGray, " iteration=")
			.Append(CommandOutletColor.DarkGreen, iteration)
			.Append(CommandOutletColor.DarkGray, " token=")
			.AppendLine(CommandOutletColor.DarkGreen, fencingToken);

		output.Write(content);
	}

	private static void WriteStaleWrite(ICommandOutlet output, int workerId, int iteration, long fencingToken, long stored)
	{
		var content = CommandOutletContent.Create(CommandOutletStyles.Bold, CommandOutletColor.DarkRed, "stale-write ")
			.Append(CommandOutletColor.DarkGray, "worker=")
			.Append(CommandOutletColor.DarkYellow, workerId)
			.Append(CommandOutletColor.DarkGray, " iteration=")
			.Append(CommandOutletColor.DarkYellow, iteration)
			.Append(CommandOutletColor.DarkGray, " token=")
			.Append(CommandOutletColor.DarkRed, fencingToken)
			.Append(CommandOutletColor.DarkGray, " stored=")
			.AppendLine(CommandOutletColor.DarkYellow, stored);

		output.Write(content);
	}

	private sealed class RunSettings
	{
		public int WorkerId { get; private set; }
		public int Iterations { get; private set; }
		public string Namespace { get; private set; }
		public TimeSpan Expiry { get; private set; }
		public TimeSpan Hold { get; private set; }
		public TimeSpan Timeout { get; private set; }
		public TimeSpan? RenewalInterval { get; private set; }
		public bool Verbose { get; private set; }

		public static RunSettings Get(CommandContext context)
		{
			var scenario = context.Options.GetValue<string>("scenario", "mutex");
			var settings = new RunSettings
			{
				WorkerId = context.Options.GetValue<int>("worker-id", Environment.ProcessId),
				Iterations = context.Options.GetValue<int>("iterations", 80),
				Namespace = Utility.GetNamespace(context),
				Expiry = context.Options.GetValue<TimeSpan>("expiry", TimeSpan.FromSeconds(5)),
				Hold = context.Options.GetValue<TimeSpan>("hold", TimeSpan.FromMilliseconds(50)),
				Timeout = context.Options.GetValue<TimeSpan>("timeout", TimeSpan.FromMinutes(5)),
				RenewalInterval = context.Options.Contains("renewal-interval") ? context.Options.GetValue<TimeSpan>("renewal-interval") : (TimeSpan?)null,
				Verbose = context.Options.Switch("verbose"),
			};

			if(string.Equals(scenario, "expiry", StringComparison.OrdinalIgnoreCase))
			{
				settings.Expiry = TimeSpan.FromMilliseconds(300);
				settings.Hold = TimeSpan.FromMilliseconds(900);
			}
			else if(string.Equals(scenario, "renew", StringComparison.OrdinalIgnoreCase))
			{
				settings.Expiry = TimeSpan.FromMilliseconds(300);
				settings.Hold = TimeSpan.FromMilliseconds(900);
				settings.RenewalInterval ??= TimeSpan.FromMilliseconds(100);
			}

			return settings;
		}
	}
}
