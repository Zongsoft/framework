using System;
using System.Net;
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
		var client = TcpClient.Headed;
		client.Address = GetAddress(args);
		client.Handler = Handler.Instance;

		var executor = Terminal.Console.Executor;
		executor.Command("connect", async (context, cancellation) =>
		{
			await client.ConnectAsync();
			context.Output.WriteLine(CommandOutletColor.DarkGreen, $"Connected to {client.Address}.");
		});

		executor.Command("disconnect", async (context, cancellation) => await client.DisconnectAsync(cancellation));
		executor.Command("info", context =>
		{
			context.Output.Write(CommandOutletColor.Cyan, "Address: ");
			context.Output.WriteLine(CommandOutletColor.Green, client.Address);
			context.Output.Write(CommandOutletColor.Cyan, "Sent: ");
			context.Output.WriteLine(CommandOutletColor.Green, $"{client.TotalBytesSent} bytes");
			context.Output.Write(CommandOutletColor.Cyan, "Received: ");
			context.Output.WriteLine(CommandOutletColor.Green, $"{client.TotalBytesReceived} bytes");
		});

		executor.Command("send", async (context, cancellation) =>
		{
			if(context.Arguments.IsEmpty)
				throw new CommandException("Missing the message to send.");

			var message = Encoding.UTF8.GetBytes(string.Join(' ', context.Arguments));
			await client.SendAsync(new ReadOnlySequence<byte>(message), cancellation);
			context.Output.WriteLine(CommandOutletColor.DarkGreen, $"Sent {message.Length} bytes.");
		});

		var splash = CommandOutletContent.Create()
			.AppendLine(CommandOutletColor.Yellow, new string('·', 50))
			.AppendLine(CommandOutletColor.Cyan, "Welcome to the TCP Client.".Justify(50))
			.AppendLine(CommandOutletColor.Yellow, new string('·', 50));

		await executor.RunAsync(splash);
		await client.DisconnectAsync();
	}

	private static IPEndPoint GetAddress(string[] args)
	{
		var address = IPAddress.Loopback;
		var port = 7969;

		if(args.Length > 0 && !IPAddress.TryParse(args[0], out address))
			throw new ArgumentException($"Invalid IP address: {args[0]}");

		if(args.Length > 1 && !int.TryParse(args[1], out port))
			throw new ArgumentException($"Invalid port number: {args[1]}");

		return new IPEndPoint(address, port);
	}

	private sealed class Handler : HandlerBase<ReadOnlySequence<byte>>
	{
		public static readonly Handler Instance = new();

		protected override ValueTask OnHandleAsync(ReadOnlySequence<byte> message, Parameters parameters, CancellationToken cancellation)
		{
			Terminal.Console.Executor.Output.WriteLine(CommandOutletColor.Cyan, $"[Received] {Encoding.UTF8.GetString(message.ToArray())}");
			return ValueTask.CompletedTask;
		}
	}
}
