using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Common;
using Zongsoft.Terminals;
using Zongsoft.Components;

namespace Zongsoft.Messaging.ZeroMQ.Samples;

internal class Program
{
	static async Task Main(string[] args)
	{
		using var server = new ZeroQueueServer();
		await server.StartAsync(["--incoming:32101", "--outgoing:32102"]);

		var executor = Terminal.Console.Executor;
		executor.Command("start", async (context, cancellation) => await server.StartAsync([.. GetArgs(context)], cancellation));
		executor.Command("stop", async (context, cancellation) => await server.StopAsync([.. GetArgs(context)], cancellation));

		executor.Command("info", context =>
		{
			context.Output.Write(CommandOutletColor.Cyan, "State: ");
			context.Output.WriteLine(server.State == WorkerState.Running ? CommandOutletColor.Green : CommandOutletColor.Magenta, server.State);
			context.Output.Write(CommandOutletColor.Cyan, "Port: ");
			context.Output.WriteLine(CommandOutletColor.Green, server.Port);
		});

		var splash = CommandOutletContent.Create()
			.AppendLine(CommandOutletColor.Yellow, new string('·', 50))
			.AppendLine(CommandOutletColor.Blue, "Welcome to the ZeroMQ Server.".Justify(50))
			.AppendLine(CommandOutletColor.Yellow, new string('·', 50));

		//运行终端命令执行器
		await executor.RunAsync(splash);
	}

	static IEnumerable<string> GetArgs(CommandContextBase context)
	{
		foreach(var option in context.Options)
		{
			if(option.Key.Length == 1)
				yield return option.Value == null ? $"-{option.Key}" : $"-{option.Key}:{option.Value}";
			else
				yield return option.Value == null ? $"--{option.Key}" : $"--{option.Key}:{option.Value}";
		}

		foreach(var argument in context.Arguments)
			yield return argument;
	}
}
