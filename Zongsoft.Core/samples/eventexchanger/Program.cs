using System;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Services;
using Zongsoft.Terminals;

namespace Zongsoft.Components.Samples;

internal class Program
{
	static async Task Main(string[] args)
	{
		ApplicationContext context = new();

		Terminal.WriteLine(CommandOutletColor.DarkYellow, "Input `exit`  to quit the program.");
		Terminal.WriteLine(CommandOutletColor.DarkYellow, "Input `start` to start the event exchanger.");
		Terminal.WriteLine(CommandOutletColor.DarkYellow, "Input `stop`  to stop the event exchanger.");
		Terminal.Write(CommandOutletColor.Red, "Tips: ");
		Terminal.WriteLine(CommandOutletColor.DarkGray, "Input other text is added to the cache as value.");
		Terminal.WriteLine();

		var text = string.Empty;
		var sample = new EventExchangerSample();

		while(true)
		{
			text = Console.ReadLine().Trim();

			switch(text)
			{
				case "exit":
					return;
				case "info":
					foreach(var channel in EventExchanger.Instance.Channels)
						Terminal.WriteLine(CommandOutletColor.DarkGray, $"{channel}");
					break;
				case "clear":
					Console.Clear();
					break;
				case "reset":
					sample.Reset();
					Terminal.WriteLine(CommandOutletColor.DarkGreen, "The counter has reset.");
					break;
				case "start":
				case "restart":
					EventExchanger.Instance.Start();
					Terminal.WriteLine(CommandOutletColor.DarkGreen, "The event exchanger has started.");
					break;
				case "stop":
					EventExchanger.Instance.Stop();
					Terminal.WriteLine(CommandOutletColor.DarkMagenta, "The event exchanger has stopped.");
					break;
				default:
					if(string.IsNullOrWhiteSpace(text))
						continue;

					var parts = text.Split(['@', '/'], StringSplitOptions.TrimEntries);
					int quantity;

					switch(parts.Length)
					{
						case 1:
							if(int.TryParse(parts[0], out quantity))
								await sample.RaiseAsync(1, quantity);
							else
								Terminal.WriteLine(CommandOutletColor.Magenta, $"Invalid format.");
							break;
						case 2:
							if(int.TryParse(parts[0], out quantity) && int.TryParse(parts[1], out var round))
								await sample.RaiseAsync(round, quantity);
							else
								Terminal.WriteLine(CommandOutletColor.Magenta, $"Invalid format.");
							break;
						default:
							Terminal.WriteLine(CommandOutletColor.Magenta, $"Invalid format.");
							break;
					}

					break;
			}
		}
	}

}
