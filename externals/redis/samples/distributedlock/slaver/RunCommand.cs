using System;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Components;

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
			await using var locker = await redis.AcquireAsync(Utility.Keys.Lock, settings.Expiry, linked.Token);
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

				await Task.Delay(settings.Hold, linked.Token);
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

	private sealed class RunSettings
	{
		public int WorkerId { get; private set; }
		public int Iterations { get; private set; }
		public string Namespace { get; private set; }
		public TimeSpan Expiry { get; private set; }
		public TimeSpan Hold { get; private set; }
		public TimeSpan Timeout { get; private set; }
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
				Verbose = context.Options.Switch("verbose"),
			};

			if(string.Equals(scenario, "expiry", StringComparison.OrdinalIgnoreCase))
			{
				settings.Expiry = TimeSpan.FromMilliseconds(300);
				settings.Hold = TimeSpan.FromMilliseconds(900);
			}

			return settings;
		}
	}
}
