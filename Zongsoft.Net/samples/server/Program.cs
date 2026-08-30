using System;
using System.Text;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Common;
using Zongsoft.Terminals;
using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Net.Samples;

internal class Program
{
	static async Task Main(string[] args)
	{
		var server = TcpServer.Headed;
		server.Handler = new Handler(server);
		await server.StartAsync(args.Length == 0 ? ["127.0.0.1", "7969"] : args);

		var executor = Terminal.Console.Executor;
		executor.Command("start", async (context, cancellation) => await server.StartAsync([.. GetArgs(context)], cancellation));
		executor.Command("stop", async (context, cancellation) => await server.StopAsync([], cancellation));

		executor.Command("info", context =>
		{
			context.Output.Write(CommandOutletColor.Cyan, "State: ");
			context.Output.WriteLine(server.IsListening ? CommandOutletColor.Green : CommandOutletColor.Magenta, server.State);
			context.Output.Write(CommandOutletColor.Cyan, "Address: ");
			context.Output.WriteLine(CommandOutletColor.Green, server.Address);
			context.Output.Write(CommandOutletColor.Cyan, "Channels: ");
			context.Output.WriteLine(CommandOutletColor.Green, server.Channels.Count);
		});

		executor.Command("broadcast", async (context, cancellation) =>
		{
			if(context.Arguments.IsEmpty)
				throw new CommandException("Missing the message to broadcast.");

			var message = Encoding.UTF8.GetBytes(string.Join(' ', context.Arguments));
			var count = await server.BroadcastAsync(new ReadOnlySequence<byte>(message), cancellation);
			context.Output.WriteLine(CommandOutletColor.DarkGreen, $"Broadcast to {count} client(s).");
		});

		var splash = CommandOutletContent.Create()
			.AppendLine(CommandOutletColor.Yellow, new string('·', 50))
			.AppendLine(CommandOutletColor.Blue, "Welcome to the TCP Server.".Justify(50))
			.AppendLine(CommandOutletColor.Yellow, new string('·', 50));

		await executor.RunAsync(splash);
		await server.StopAsync([]);
	}

	static System.Collections.Generic.IEnumerable<string> GetArgs(CommandContextBase context)
	{
		foreach(var argument in context.Arguments)
			yield return argument;
	}

	private sealed class Handler(TcpServer<ReadOnlySequence<byte>> server) : HandlerBase<ReadOnlySequence<byte>>
	{
		private readonly TcpServer<ReadOnlySequence<byte>> _server = server;

		protected override async ValueTask OnHandleAsync(ReadOnlySequence<byte> message, Parameters parameters, CancellationToken cancellation)
		{
			var data = message.ToArray();
			var text = Encoding.UTF8.GetString(data);
			Terminal.Console.Executor.Output.WriteLine(CommandOutletColor.Cyan, $"[Received] {text}");

			var response = Encoding.UTF8.GetBytes($"ACK: {text}");
			await _server.BroadcastAsync(new ReadOnlySequence<byte>(response), cancellation);
		}
	}
}
