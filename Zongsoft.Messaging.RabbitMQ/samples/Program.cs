using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Messaging.RabbitMQ.Samples;

internal class Program
{
	static async Task Main(string[] args)
	{
		Console.WriteLine("Press the Enter key to exit.");
		Console.WriteLine("____________________________");
		Console.WriteLine();

		var settings = Configuration.RabbitConnectionSettingsDriver.Instance.GetSettings($"server=192.168.2.221;client=Zongsoft.Messaing.RabbitMQ.Sample;username=program;password=xxxxxx;");
		var queue = new RabbitQueue("RabbitMQ", settings);

		var subscriber = await queue.SubscribeAsync(async message =>
		{
			if(string.IsNullOrEmpty(message.Identifier))
				Console.WriteLine($"Received: [{message.Topic}] {Encoding.UTF8.GetString(message.Data)}");
			else
				Console.WriteLine($"Received: [{message.Topic}] {Encoding.UTF8.GetString(message.Data)}\t<{message.Identifier}>");

			await message.AcknowledgeAsync();
		});

		Parallel.For(0, 200, async index =>
		{
			var topic = $"Topic-{index % 10}";
			var message = $"Message:#{(index + 1):000}";

			var identifier = await queue.ProduceAsync(topic, Encoding.UTF8.GetBytes(message), MessageEnqueueOptions.Default);

			Console.WriteLine($"Sent: [{topic}] {message}\t<{identifier}>");
		});

		Console.ReadLine();
	}
}
