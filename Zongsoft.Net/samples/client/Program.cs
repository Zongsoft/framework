using System;
using System.IO;
using System.Net;
using System.Text;
using System.Linq;
using System.Buffers;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;

using Zongsoft.Common;
using Zongsoft.Terminals;
using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Net.Samples;

internal static class Program
{
	#region 程序入口
	private static async Task Main(string[] args)
	{
		using var cancellation = new CancellationTokenSource();
		var interrupted = false;
		Console.CancelKeyPress += Cancel;

		try
		{
			var address = SampleProtocol.GetAddress(args);
			if(args.Contains("--batch"))
			{
				if(args.Contains("--custom"))
					throw new ArgumentException(null, nameof(args));

				cancellation.CancelAfter(TimeSpan.FromSeconds(60));
				await RunBatchAsync(address, cancellation.Token);
				return;
			}

			var scripted = args.Contains("--smoke") || args.Contains("--heartbeat");
			var responses = scripted ? Channel.CreateBounded<string>(1) : null;
			await using var client = new TcpClient<ReadOnlySequence<byte>>(SampleProtocol.Create(args.Contains("--custom")))
			{
				Address = address,
				Handler = new Handler(responses?.Writer),
			};

			if(scripted)
			{
				cancellation.CancelAfter(TimeSpan.FromSeconds(60));
				var heartbeat = args.Contains("--heartbeat");

				for(var i = 0; i < (heartbeat ? 3 : 1); i++)
				{
					var text = heartbeat ? "PING" : "hello";

					using(var message = text.Encode())
						await client.SendAsync(message.Memory, cancellation.Token);

					var response = await responses.Reader.ReadAsync(cancellation.Token);

					if(response != (heartbeat ? "PONG" : "ACK: hello"))
						throw new InvalidDataException();

					if(heartbeat && i < 2)
						await Task.Delay(TimeSpan.FromSeconds(1), cancellation.Token);
				}
			}
			else
			{
				var executor = Terminal.Console.Executor;
				executor.Command("connect", async (_, token) => { await client.ConnectAsync(cancellation: token); });
				executor.Command("disconnect", async (_, token) => await client.DisconnectAsync(token));
				executor.Command("info", _ => Console.WriteLine($"{client.Address} received={client.TotalBytesReceived} sent={client.TotalBytesSent}"));
				executor.Command("send", async (context, token) =>
				{
					if(context.Arguments.IsEmpty)
						throw new CommandException(Client.Properties.Resources.Network_MessageRequired_Message);

					using(var message = string.Join(' ', context.Arguments).Encode())
						await client.SendAsync(message.Memory, token);
				});
				await executor.RunAsync(cancellation: cancellation.Token);
			}

			await client.DisconnectAsync();
			Console.WriteLine("DISCONNECTED");
		}
		catch(OperationCanceledException) when(interrupted) { }
		finally { Console.CancelKeyPress -= Cancel; }

		void Cancel(object sender, ConsoleCancelEventArgs args)
		{
			args.Cancel = true;
			interrupted = true;
			cancellation.Cancel();
		}
	}
	#endregion

	#region 合批设备
	//原始 Socket 只用于模拟设备控制写入边界，库的二进制发送入口始终遵循分帧器。
	private static async Task RunBatchAsync(EndPoint address, CancellationToken cancellation)
	{
		using var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
		await socket.ConnectAsync(address, cancellation);
		var packetizer = TcpClient.Headed.Packetizer;
		var writer = new ArrayBufferWriter<byte>(2048);
		var boundary = 0;

		for(var i = 0; i < 17; i++)
		{
			if(i == 16)
				boundary = writer.WrittenCount + 2;

			using(var message = $"batch-{i:00}".Encode())
				packetizer.Pack(writer, new ReadOnlySequence<byte>(message.Memory));
		}

		//设备主动等待积累会增加延迟；这里显式限制为 17 帧和 20 毫秒。
		await Task.Delay(20, cancellation);
		await SendAsync(writer.WrittenMemory[..boundary]);
		var buffer = new byte[2048];
		var buffered = 0;
		var received = 0;
		await ReceiveAsync(16);
		Console.WriteLine("BATCH first=16, tail=2 header bytes");
		await SendAsync(writer.WrittenMemory[boundary..]);
		await ReceiveAsync(17);
		socket.Shutdown(SocketShutdown.Send);
		Console.WriteLine("BATCH completed=17");

		async Task SendAsync(ReadOnlyMemory<byte> bytes)
		{
			while(!bytes.IsEmpty)
			{
				var sent = await socket.SendAsync(bytes, SocketFlags.None, cancellation);
				if(sent == 0)
					throw new EndOfStreamException();

				bytes = bytes[sent..];
			}
		}

		async Task ReceiveAsync(int target)
		{
			while(received < target)
			{
				var read = await socket.ReceiveAsync(buffer.AsMemory(buffered), SocketFlags.None, cancellation);
				if(read == 0)
					throw new EndOfStreamException();

				buffered += read;
				var input = new ReadOnlySequence<byte>(buffer.AsMemory(0, buffered));

				while(packetizer.Unpack(ref input, out var package))
				{
					if(Encoding.UTF8.GetString(package) != $"ACK: batch-{received:00}")
						throw new InvalidDataException();

					received++;
				}

				input.CopyTo(buffer);
				buffered = (int)input.Length;
			}
		}
	}
	#endregion

	#region 接收处理
	private sealed class Handler(ChannelWriter<string> responses) : HandlerBase<TcpReceiveContext<ReadOnlySequence<byte>>>
	{
		protected override ValueTask OnHandleAsync(TcpReceiveContext<ReadOnlySequence<byte>> context, Parameters parameters, CancellationToken cancellation)
		{
			var text = Encoding.UTF8.GetString(context.Package);
			Console.WriteLine($"[{context.Channel.Identifier}] {text}");
			return responses == null ? ValueTask.CompletedTask : responses.WriteAsync(text, cancellation);
		}
	}
	#endregion
}
