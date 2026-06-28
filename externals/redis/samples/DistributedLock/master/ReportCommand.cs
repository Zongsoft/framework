using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Components;

namespace Zongsoft.Externals.Redis.DistributedLock;

internal sealed class ReportCommand : CommandBase<CommandContext>
{
	public ReportCommand() : base("Report") { }

	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		var expected = context.Options.GetValue<int>("expected", 0);
		var expectViolations = context.Options.Switch("expect-violations") ||
			string.Equals(context.Options.GetValue<string>("scenario", null), "expiry", System.StringComparison.OrdinalIgnoreCase);

		using var redis = Utility.GetRedis(context);

		var entered = await Utility.ReadCounterAsync(redis, Utility.Keys.Entered, cancellation);
		var completed = await Utility.ReadCounterAsync(redis, Utility.Keys.Completed, cancellation);
		var violations = await Utility.ReadCounterAsync(redis, Utility.Keys.Violations, cancellation);
		var active = await Utility.ReadCounterAsync(redis, Utility.Keys.Active, cancellation);

		Write(context.Output, expected, entered, completed, violations, active);

		if(expected <= 0)
			return 0;

		var success =
			entered == expected &&
			completed == expected &&
			active == 0 &&
			(expectViolations ? violations > 0 : violations == 0);

		context.Output.WriteLine(success ? CommandOutletColor.DarkGreen : CommandOutletColor.DarkRed, success ? "Result     : PASS" : "Result     : FAIL");

		return success ? 0 : 2;
	}

	public static void Write(ICommandOutlet output, int expected, long entered, long completed, long violations, long active, int failures = 0, System.TimeSpan? elapsed = null)
	{
		output.WriteLine();
		output.WriteLine(CommandOutletStyles.Bold, CommandOutletColor.Cyan, "Summary");

		if(elapsed.HasValue)
			Utility.WritePair(output, "Elapsed", elapsed.Value);

		if(expected > 0)
			Utility.WritePair(output, "Expected", expected);

		Utility.WritePair(output, "Entered", entered);
		Utility.WritePair(output, "Completed", completed);
		Utility.WritePair(output, "Violations", violations, violations == 0 ? CommandOutletColor.DarkGreen : CommandOutletColor.DarkRed);
		Utility.WritePair(output, "Active", active, active == 0 ? CommandOutletColor.DarkGreen : CommandOutletColor.DarkRed);
		Utility.WritePair(output, "Failures", failures, failures == 0 ? CommandOutletColor.DarkGreen : CommandOutletColor.DarkRed);
	}
}
