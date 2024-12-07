using System;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Services;

using Terminal = Zongsoft.Terminals.ConsoleTerminal;

namespace Zongsoft.Components.Samples;

internal class Program
{
	static async Task Main(string[] args)
	{
		ApplicationContext context = new();

		Terminal.Instance.WriteLine(CommandOutletColor.DarkYellow, "Input `exit`  to quit the program.");
		Terminal.Instance.WriteLine(CommandOutletColor.DarkYellow, "Input `start` to start the event exchanger.");
		Terminal.Instance.WriteLine(CommandOutletColor.DarkYellow, "Input `stop`  to stop the event exchanger.");
		Terminal.Instance.Write(CommandOutletColor.Red, "Tips: ");
		Terminal.Instance.WriteLine(CommandOutletColor.DarkGray, "Input other text is added to the cache as value.");
		Terminal.Instance.WriteLine();

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
					Terminal.Instance.WriteLine(CommandOutletColor.DarkGray, $"#{sample.Exchanger.Identifier} {sample.Exchanger.Queue.ConnectionSettings}");
					break;
				case "clear":
					Console.Clear();
					break;
				case "reset":
					sample.Reset();
					Terminal.Instance.WriteLine(CommandOutletColor.DarkGreen, "The counter has reset.");
					break;
				case "start":
				case "restart":
					sample.Exchanger.Start();
					Terminal.Instance.WriteLine(CommandOutletColor.DarkGreen, "The event exchanger has started.");
					break;
				case "stop":
					sample.Exchanger.Stop();
					Terminal.Instance.WriteLine(CommandOutletColor.DarkMagenta, "The event exchanger has stopped.");
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
								Terminal.Instance.WriteLine(CommandOutletColor.Magenta, $"Invalid format.");
							break;
						case 2:
							if(int.TryParse(parts[0], out quantity) && int.TryParse(parts[1], out var round))
								await sample.RaiseAsync(round, quantity);
							else
								Terminal.Instance.WriteLine(CommandOutletColor.Magenta, $"Invalid format.");
							break;
						default:
							Terminal.Instance.WriteLine(CommandOutletColor.Magenta, $"Invalid format.");
							break;
					}

					break;
			}
		}
	}

}
