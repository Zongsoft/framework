using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Components;
using Zongsoft.Collections;
using Zongsoft.Configuration;

namespace Zongsoft.Messaging.ZeroMQ.Samples;

internal class Program
{
	const string TOPIC = "Topic1";
	private static ZeroQueue _queue;

	static async Task Main(string[] args)
	{
		var server = new ZeroServer();
		await server.StartAsync(args);

		_queue = new ZeroQueue("ZeroMQ", new ConnectionSettings("ZeroMQ", "server=127.0.0.1;port=5001;protocol=tcp;"));
		await _queue.SubscribeAsync(TOPIC, new Handler());

		while(true)
		{
			var text = Console.ReadLine()?.Trim();

			if(string.IsNullOrEmpty(text))
				continue;

			switch(text.ToLowerInvariant())
			{
				case "exit":
					server.Stop(args);
					_queue.Dispose();
					return;
				case "clear":
					Console.Clear();
					break;
				default:
					if(int.TryParse(text, out var round))
					{
						var stopwatch = new System.Diagnostics.Stopwatch();
						stopwatch.Start();
						SendAsync(Math.Abs(round));
						Console.WriteLine($"Elapsed: {stopwatch.Elapsed}");
					}

					break;
			}
		}
	}

	private static volatile int _count;

	private static void SendAsync(int round = 1)
	{
		Parallel.For(0, round, async index =>
		{
			await _queue.ProduceAsync(TOPIC, Encoding.UTF8.GetBytes($"Message#{index + 1}"));
			//Console.WriteLine($"[{Interlocked.Increment(ref _count)}].{index + 1} Sent.");
		});
	}

	internal sealed class Handler : HandlerBase<Message>
	{
		private volatile int _count;

		protected override ValueTask OnHandleAsync(Message message, Parameters parameters, CancellationToken cancellation)
		{
			if(message.IsEmpty)
				return ValueTask.CompletedTask;

			var text = Encoding.UTF8.GetString(message.Data);
			Console.WriteLine($"Received: [{Interlocked.Increment(ref _count)}]{text}");
			return ValueTask.CompletedTask;
		}
	}
}
