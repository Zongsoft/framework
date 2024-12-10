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
	private static Handler _handler;

	private static volatile int _count;

	static async Task Main(string[] args)
	{
		Console.WriteLine("Welcome to the Client.");
		Console.WriteLine(new string('-', 50));

		_queue = new ZeroQueue("ZeroMQ", new ConnectionSettings("ZeroMQ", "server=127.0.0.1;port=5001;"));
		_handler = new Handler();

		while(true)
		{
			var text = Console.ReadLine()?.Trim();

			if(string.IsNullOrEmpty(text))
				continue;

			switch(text.ToLowerInvariant())
			{
				case "exit":
					_queue.Dispose();
					return;
				case "reset":
					_count = 0;
					_handler.Reset();
					break;
				case "clear":
					Console.Clear();
					break;
				case "sub":
				case "subscribe":
					await _queue.SubscribeAsync(TOPIC, _handler);
					break;
				case "unsub":
				case "unsubscribe":
					await _queue.Subscribers[TOPIC].UnsubscribeAsync();
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

	private static void SendAsync(int round = 1)
	{
		Parallel.For(0, round, async index =>
		{
			var count = Interlocked.Increment(ref _count);
			await _queue.ProduceAsync(TOPIC, Encoding.UTF8.GetBytes($"Message#{count}-{index + 1}"));
			Console.WriteLine($"[{count}].{index + 1} Sent.");
		});
	}

	internal sealed class Handler : HandlerBase<Message>
	{
		private volatile int _count;
		public void Reset() => _count = 0;
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
