using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Components;

namespace Zongsoft.Externals.Redis.DistributedLock;

internal sealed class ResetCommand : CommandBase<CommandContext>
{
	public ResetCommand() : base("Reset") { }

	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		var @namespace = Utility.GetNamespace(context);

		using var redis = Utility.GetRedis(context, @namespace);
		await Utility.ResetAsync(redis, cancellation);

		context.Output.Write(CommandOutletColor.DarkGreen, "Reset completed: ");
		context.Output.WriteLine(CommandOutletColor.DarkYellow, @namespace);

		return @namespace;
	}
}
