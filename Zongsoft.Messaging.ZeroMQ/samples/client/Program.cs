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
	private static ZeroQueue _queue;
	private static Handler _handler;

	private static volatile int _count;

	static async Task Main(string[] args)
	{
		Console.WriteLine("Welcome to the Client.");
		Console.WriteLine(new string('-', 50));

		_queue = new ZeroQueue("ZeroMQ", Configuration.ZeroConnectionSettingsDriver.Instance.GetSettings("ZeroMQ", "server=127.0.0.1;client=Zongsoft.Messaging.ZeroMQ.Sample;Group=Demo;"));
		_handler = new Handler();

		while(true)
		{
			var text = Console.ReadLine()?.Trim();

			if(string.IsNullOrEmpty(text))
				continue;

			var parts = text.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

			switch(parts[0].ToLowerInvariant())
			{
				case "exit":
					_queue.Dispose();
					return;
				case "info":
					Console.WriteLine($"[{_queue.Identifier}] {_queue.Settings}");

					if(_queue.Subscribers.Count > 0)
					{
						int index = 0;
						Console.WriteLine(new string('-', 50));

						foreach(var subscriber in _queue.Subscribers)
						{
							Console.WriteLine($"[{++index}] {subscriber.Topic}");
						}
					}
					break;
				case "reset":
					_count = 0;
					_handler.Reset();
					break;
				case "clear":
					Console.Clear();
					break;
				case "sub":
				case "subscribe":
					for(int i = 1; i < parts.Length; i++)
						await _queue.SubscribeAsync(parts[i], _handler);
					break;
				case "unsub":
				case "unsubscribe":
					for(int i = 0; i < parts.Length; i++)
					{
						if(_queue.Subscribers.TryGetValue(parts[i], out var subscriber))
							await subscriber.UnsubscribeAsync();
					}
					break;
				case "send":
				case "produce":
					if(parts.Length > 1)
					{
						var rounded = int.TryParse(parts[1], out var round);
						var stopwatch = new System.Diagnostics.Stopwatch();
						stopwatch.Start();

						for(int i = (rounded ? 2 : 1); i < parts.Length; i++)
							SendAsync(rounded ? Math.Max(round, 1) : 1, parts[i]);

						Console.WriteLine($"Elapsed: {stopwatch.Elapsed}");
					}

					break;
				default:
					break;
			}
		}
	}

	private static void SendAsync(int round, string topic = null)
	{
		Parallel.For(0, round, async index =>
		{
			var count = Interlocked.Increment(ref _count);
			await _queue.ProduceAsync(topic ?? "*", Encoding.UTF8.GetBytes($"Message#{count}-{index + 1}"));
			Console.WriteLine($"[{count}-{index + 1}] {topic} Sent.");
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
			Console.WriteLine($"Received: [{Interlocked.Increment(ref _count)}]{message.Topic}{Environment.NewLine}{text}");
			return ValueTask.CompletedTask;
		}
	}
}
