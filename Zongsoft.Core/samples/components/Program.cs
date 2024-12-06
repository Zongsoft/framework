using System;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Services;
using Zongsoft.Terminals;
using Zongsoft.Messaging;
using Zongsoft.Messaging.Mqtt;
using Zongsoft.Configuration;

using Terminal = Zongsoft.Terminals.ConsoleTerminal;
using System.Diagnostics.Metrics;
using Zongsoft.Collections;

namespace Zongsoft.Components.Samples;

internal class Program
{
	private static int _count;
	private static EventExchanger _exchanger;

	static void Main(string[] args)
	{
		//string CONNECTION_STRING = $"server=192.168.2.200;username=program;password=Yuanshan.MQTT@2024;client=Zongsoft.Sample#{Random.Shared.NextInt64()};";
		string CONNECTION_STRING = $"server=192.168.2.200;username=program;password=Yuanshan.MQTT@2024;";

		Initialize(CONNECTION_STRING);

		Terminal.Instance.WriteLine(CommandOutletColor.DarkYellow, "Input `exit`  to quit the program.");
		Terminal.Instance.WriteLine(CommandOutletColor.DarkYellow, "Input `start` to start the event exchanger.");
		Terminal.Instance.WriteLine(CommandOutletColor.DarkYellow, "Input `stop`  to stop the event exchanger.");
		Terminal.Instance.Write(CommandOutletColor.Red, "Tips: ");
		Terminal.Instance.WriteLine(CommandOutletColor.DarkGray, "Input other text is added to the cache as value.");
		Terminal.Instance.WriteLine();

		var text = string.Empty;

		do
		{
			text = Console.ReadLine();

			switch(text)
			{
				case "start":
				case "restart":
					_exchanger.Start();
					Terminal.Instance.WriteLine(CommandOutletColor.DarkGreen, "The event exchanger has started.");
					break;
				case "stop":
					_exchanger.Stop();
					Terminal.Instance.WriteLine(CommandOutletColor.DarkMagenta, "The event exchanger has stopped.");
					break;
				default:
					for(int i = 0; i < 100; i++)
						Raise(int.TryParse(text, out var number) ? number : 1);

					break;
			}
		} while(text != "exit");
	}

	private static void Initialize(string connectionString)
	{
		var context = new ApplicationContext();
		var queue = new MqttQueue("Mqtt", new ConnectionSettings("Mqtt", connectionString));
		_exchanger = new EventExchanger
		{
			Queue = queue,
			Topic = "Samples/Events"
		};

		context.Workers.Add(_exchanger);

		//挂载事件处理程序
		Module.Current.Events.Register($"{nameof(Module.Current.Events.Acquirer)}.{nameof(Module.EventRegistry.AcquirerEvent.Acquired)}", Handler.Instance);
	}

	public static void Raise(int count = 1)
	{
		var meter = new Models.Meter($"Meter#{Interlocked.Increment(ref _count)}", $"Code#{Random.Shared.NextInt64()}");

		for(int i = 0; i < count; i++)
			meter.Metrics.Add($"Metric#{i + 1}", $"Code#{i + 1}", Random.Shared.NextDouble());

		Module.Current.Events.Acquirer.OnAcquired(meter);
	}

	private sealed class Handler : HandlerBase<Models.Meter>
	{
		public static readonly Handler Instance = new();

		protected override ValueTask OnHandleAsync(Models.Meter argument, Parameters parameters, CancellationToken cancellation)
		{
			Console.WriteLine(argument);
			return ValueTask.CompletedTask;
		}
	}
}
