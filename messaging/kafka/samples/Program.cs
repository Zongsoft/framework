using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Messaging.Kafka.Samples;

internal class Program
{
	static async Task Main(string[] args)
	{
		Console.WriteLine("Press the Enter key to exit.");
		Console.WriteLine("____________________________");
		Console.WriteLine();

		var settings = Configuration.KafkaConnectionSettingsDriver.Instance.GetSettings($"Server=192.168.2.19:9092;Client=Zongsoft.Messaing.Kafka.Sample;");
		var queue = new KafkaQueue("Kafka", settings);

		var subscriber = await queue.SubscribeAsync("TopicX", async message =>
		{
			if(string.IsNullOrEmpty(message.Identifier))
				Console.WriteLine($"Received: [{message.Topic}] {Encoding.UTF8.GetString(message.Data)}");
			else
				Console.WriteLine($"Received: [{message.Topic}] {Encoding.UTF8.GetString(message.Data)}\t<{message.Identifier}>");

			await message.AcknowledgeAsync();
		});

		Parallel.For(0, 200, async index =>
		{
			var topic = $"TopicX";
			var message = $"Message:#{(index + 1):000}";

			var identifier = await queue.ProduceAsync(topic, Encoding.UTF8.GetBytes(message), MessageEnqueueOptions.Default);

			Console.WriteLine($"Sent: [{topic}] {message}\t<{identifier}>");
		});

		Console.ReadLine();
	}
}
