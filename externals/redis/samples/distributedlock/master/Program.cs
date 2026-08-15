using System;
using System.Threading.Tasks;

using Zongsoft.Common;
using Zongsoft.Components;
using Zongsoft.Terminals;

namespace Zongsoft.Externals.Redis.DistributedLock;

internal class Program
{
	public static async Task<int> Main(string[] args)
	{
		var executor = Terminal.Console.Executor;

		executor.Root.Children.Add(new RunCommand());
		executor.Root.Children.Add(new ResetCommand());
		executor.Root.Children.Add(new ReportCommand());

		executor.Aliaser.Set("run", "start");

		if(args != null && args.Length > 0)
		{
			var result = await executor.ExecuteAsync(GetCommandText(args));
			return result is int code ? code : 0;
		}

		var splash = CommandOutletContent.Create()
			.AppendLine(CommandOutletColor.Yellow, new string('·', 64))
			.AppendLine(CommandOutletColor.Cyan, "Zongsoft Redis Distributed Lock Master".Justify(64))
			.AppendLine(CommandOutletColor.Yellow, new string('·', 64));

		return await executor.RunAsync(splash);
	}

	private static string GetCommandText(string[] args)
	{
		var text = CommandLine.Get(args);

		if(string.IsNullOrWhiteSpace(text))
			return "run";

		if(text.StartsWith("run", StringComparison.OrdinalIgnoreCase) ||
		   text.StartsWith("reset", StringComparison.OrdinalIgnoreCase) ||
		   text.StartsWith("report", StringComparison.OrdinalIgnoreCase))
			return text;

		return $"run {text}";
	}
}
