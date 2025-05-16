using System;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Common;
using Zongsoft.Terminals;
using Zongsoft.Components;

namespace Zongsoft.Externals.Opc.Samples;

internal class Program
{
	static async Task Main(string[] args)
	{
		using var server = new OpcServer("OpcServer");
		await server.StartAsync(args);

		var executor = Terminal.Console.Executor;
		executor.Command("start", async (context, cancellation) => await server.StartAsync(args, cancellation));
		executor.Command("stop", async (context, cancellation) => await server.StopAsync(args, cancellation));

		await executor.RunAsync($"Welcome to the OPC-UA Server.{Environment.NewLine}{new string('-', 50)}");
	}
}
