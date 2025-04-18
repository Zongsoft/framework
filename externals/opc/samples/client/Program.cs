﻿using System;
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
					//for(int i = 1; i < parts.Length; i++)
					//	await _client.SubscribeAsync(parts[i], _handler);
					break;
				case "unsub":
				case "unsubscribe":
					//for(int i = 0; i < parts.Length; i++)
					//{
					//	if(_client.Subscribers.TryGetValue(parts[i], out var subscriber))
					//		await subscriber.UnsubscribeAsync();
					//}
					break;
				case "connect":
					await _client.ConnectAsync(parts.Length > 1 ? parts[1] : "opc.tcp://localhost:4841/OpcServer");
					break;
				case "read":
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
