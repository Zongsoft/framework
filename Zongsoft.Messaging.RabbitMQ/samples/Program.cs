using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Messaging.RabbitMQ.Samples;

internal class Program
{
	static async Task Main(string[] args)
	{
		var settings = Configuration.RabbitConnectionSettingsDriver.Instance.GetSettings($"server=192.168.2.221;client=Zongsoft.Messaing.RabbitMQ.Sample;username=program;password=xxxxxx;");
		var queue = new RabbitQueue("RabbitMQ", settings);

		await queue.SubscribeAsync(async message =>
		{
			Console.WriteLine($"Received Message: {Encoding.UTF8.GetString(message.Data)}@{message.Identifier}");
			await message.AcknowledgeAsync();
		});

		Parallel.For(0, 1, async index =>
		{
			var id = await queue.ProduceAsync("TopicX", Encoding.UTF8.GetBytes($"Message: #{index + 1}"));
			Console.WriteLine($"Sent: #{index + 1}@{id}");
		});

		Console.WriteLine("____________________________");
		Console.WriteLine("Press the Enter key to exit.");
		Console.ReadLine();
	}
}
