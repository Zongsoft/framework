using System;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Messaging.ZeroMQ.Samples;

internal class Program
{
	static async Task Main(string[] args)
	{
		Console.WriteLine("Welcome to the Server.");
		Console.WriteLine(new string('-', 50));

		using var server = new ZeroQueueServer();
		await server.StartAsync(args);

		while(true)
		{
			var text = Console.ReadLine()?.Trim();

			if(string.IsNullOrEmpty(text))
				continue;

			switch(text.ToLowerInvariant())
			{
				case "exit":
					await server.StopAsync(args);
					return;
				case "stop":
					await server.StopAsync(args);
					break;
				case "start":
					await server.StartAsync(args);
					break;
				case "clear":
					Console.Clear();
					break;
				default:
					break;
			}
		}
	}
}
