using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Messaging.Mqtt.Samples;

internal class Program
{
	static void Main(string[] args)
	{
		var queue = new MqttQueue("MQTT", Configuration.MqttConnectionSettingsDriver.Instance.GetSettings("Mqtt", $"server=192.168.2.200:5101;username=program;password=xxxxxx;"));

		Parallel.For(0, 100, async index =>
		{
			var id = await queue.ProduceAsync("TopicX", Encoding.UTF8.GetBytes($"Message: #{index + 1}"));
			Console.WriteLine($"Sent: #{index + 1}@{id}");
		});

		Console.WriteLine("______________________");
		Console.WriteLine("Press any key to exit.");
		Console.ReadKey();
	}
}
