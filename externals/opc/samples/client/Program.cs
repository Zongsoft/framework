using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Components;
using Zongsoft.Collections;
using Zongsoft.Configuration;

namespace Zongsoft.Externals.Opc.Samples;

internal class Program
{
	private static OpcClient _client;
	private static volatile int _count;

	static async Task Main(string[] args)
	{
		Console.WriteLine("Welcome to the OPC-UA Client.");
		Console.WriteLine(new string('-', 50));

		_client = new OpcClient();

		while(true)
		{
			var text = Console.ReadLine()?.Trim();

			if(string.IsNullOrEmpty(text))
				continue;

			var parts = text.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

			switch(parts[0].ToLowerInvariant())
			{
				case "exit":
					_client.Dispose();
					return;
				case "info":
					//Console.WriteLine($"[{_client.Instance}] {_client.Settings}");

					//if(_client.Subscribers.Count > 0)
					//{
					//	int index = 0;
					//	Console.WriteLine(new string('-', 50));

					//	foreach(var subscriber in _client.Subscribers)
					//	{
					//		Console.WriteLine($"[{++index}] {subscriber.Topic}");
					//	}
					//}
					break;
				case "clear":
					Console.Clear();
					break;
				case "sub":
				case "subscribe":
					if(parts.Length > 1)
						await _client.SubscribeAsync(parts.AsSpan(1).ToArray(), null);
					break;
				case "unsub":
				case "unsubscribe":
					if(parts.Length > 1)
						await _client.UnsubscribeAsync(parts.AsSpan(1).ToArray());
					break;
				case "connect":
					await _client.ConnectAsync(parts.Length > 1 ? parts[1] : "opc.tcp://localhost:4841/OpcServer");
					break;
				case "exist":
				case "exists":
					var existed = await _client.ExistsAsync(parts[1]);
					Console.WriteLine(existed ? $"The node exists." : "Not found.");
					break;
				case "read":
					break;
				case "get":
					var value = await _client.GetValueAsync(parts[1]);
					Console.WriteLine(value);
					break;
				case "set":
					await _client.SetValueAsync(parts[1], double.Parse(parts[2]));
					break;
				case "write":
					await WriteAsync(parts[1], parts[2]);
					break;
				default:
					break;
			}
		}
	}

	static ValueTask WriteAsync(string key, string text, CancellationToken cancellation = default)
	{
		if(string.IsNullOrEmpty(key))
			return ValueTask.CompletedTask;

		return _client.WriteAsync(key, text, cancellation);
	}
}
