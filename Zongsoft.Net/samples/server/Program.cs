using System;
using System.Text;
using System.Linq;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

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
		Console.CancelKeyPress += Cancel;

		try
		{
			var countIndex = Array.IndexOf(args, "--count");
			var count = countIndex < 0 ? 0 : int.Parse(args[countIndex + 1]);
			var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			await using var server = new TcpServer<ReadOnlySequence<byte>>(SampleProtocol.Create(args.Contains("--custom")));
			server.Address = SampleProtocol.GetAddress(args);
			server.Handler = new Handler(count, completed);
			await server.StartAsync(cancellation: cancellation.Token);
			Console.WriteLine($"READY {server.Address}");

			if(count > 0)
				await completed.Task.WaitAsync(TimeSpan.FromSeconds(60), cancellation.Token);
			else
			{
				var executor = Terminal.Console.Executor;
				executor.Command("start", async (_, token) => await server.StartAsync(cancellation: token));
				executor.Command("stop", async (_, token) => await server.StopAsync(cancellation: token));
				executor.Command("info", _ => Console.WriteLine($"{server.State} {server.Address} connections={server.Channels.Count} received={server.TotalBytesReceived} sent={server.TotalBytesSent}"));
				executor.Command("broadcast", async (context, token) =>
				{
					if(context.Arguments.IsEmpty)
						throw new CommandException(Server.Properties.Resources.Network_BroadcastMessageRequired_Message);

					using(var message = string.Join(' ', context.Arguments).Encode())
					{
						var result = await server.BroadcastAsync(new ReadOnlySequence<byte>(message.Memory), token);
						Console.WriteLine($"Broadcast succeeded={result.Succeeded}, failed={result.Failed}, rejected={result.Rejected}");
					}
				});
				await executor.RunAsync(cancellation: cancellation.Token);
			}

			await server.StopAsync();
			Console.WriteLine($"STOPPED connections={server.Channels.Count} frames={server.TotalFramesReceived}");
		}
		catch(OperationCanceledException) when(cancellation.IsCancellationRequested) { }
		finally { Console.CancelKeyPress -= Cancel; }

		void Cancel(object sender, ConsoleCancelEventArgs args)
		{
			args.Cancel = true;
			cancellation.Cancel();
		}
	}
	#endregion

	#region 接收处理
	private sealed class Handler(int count, TaskCompletionSource completed) : HandlerBase<TcpReceiveContext<ReadOnlySequence<byte>>>
	{
		private int _handled;

		protected override async ValueTask OnHandleAsync(TcpReceiveContext<ReadOnlySequence<byte>> context, Parameters parameters, CancellationToken cancellation)
		{
			var text = Encoding.UTF8.GetString(context.Package);
			Console.WriteLine($"[{context.Channel.Identifier}] {text}");

			using(var response = (text == "PING" ? "PONG" : $"ACK: {text}").Encode())
				await context.Channel.SendAsync(response.Memory, cancellation);

			if(count > 0 && Interlocked.Increment(ref _handled) >= count)
				completed.TrySetResult();
		}
	}
	#endregion
}
