using System;
using System.Threading.Tasks;

using Zongsoft.Common;
using Zongsoft.Terminals;
using Zongsoft.Components;

namespace Zongsoft.Messaging.ZeroMQ.Samples;

internal class Program
{
	static async Task Main(string[] args)
	{
		using var server = new ZeroQueueServer()
		{
			Discovery = "tcp://127.0.0.1:7969",
			Incoming = "tcp://127.0.0.1:32101",
			Outgoing = "tcp://127.0.0.1:32102",
		};
		await server.StartAsync([]);

		var executor = Terminal.Console.Executor;
		executor.Command("start", async (context, cancellation) => await server.StartAsync([], cancellation));
		executor.Command("stop", async (context, cancellation) => await server.StopAsync([], cancellation));

		executor.Command("info", context =>
		{
			context.Output.Write(CommandOutletColor.Cyan, "State: ");
			context.Output.WriteLine(server.State == WorkerState.Running ? CommandOutletColor.Green : CommandOutletColor.Magenta, server.State);
			context.Output.Write(CommandOutletColor.Cyan, "Discovery: ");
			context.Output.WriteLine(CommandOutletColor.Green, server.Discovery);
			context.Output.Write(CommandOutletColor.Cyan, "Incoming: ");
			context.Output.WriteLine(CommandOutletColor.Green, server.Incoming);
			context.Output.Write(CommandOutletColor.Cyan, "Outgoing: ");
			context.Output.WriteLine(CommandOutletColor.Green, server.Outgoing);
		});

		var splash = CommandOutletContent.Create()
			.AppendLine(CommandOutletColor.Yellow, new string('·', 50))
			.AppendLine(CommandOutletColor.Blue, "Welcome to the ZeroMQ Server.".Justify(50))
			.AppendLine(CommandOutletColor.Yellow, new string('·', 50));

		//运行终端命令执行器
		await executor.RunAsync(splash);
	}
}
