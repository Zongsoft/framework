using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Opc.Ua;
using Opc.Ua.Client;

using Zongsoft.Services;
using Zongsoft.Terminals;
using Zongsoft.Components;
using Zongsoft.Collections;
using Zongsoft.Configuration;

namespace Zongsoft.Externals.Opc.Samples;

internal class Commands
{
	public static void Info(TerminalCommandContext context, OpcClient client)
	{
		if(client == null)
			return;

		Console.WriteLine($"[{client.Name}] Connected:{client.IsConnected}");

		if(client.Subscriptions.Any())
		{
			int index = 0;
			Console.WriteLine(new string('-', 50));

			foreach(var subscription in client.Subscriptions)
			{
				PrintSubscription(subscription, ++index);
			}
		}
	}

	private static void PrintSubscription(Subscription subscription, int index)
	{
		if(subscription == null)
			return;

		var content = CommandOutletContent.Create(CommandOutletColor.Gray, $"[{index}]")
			.AppendLine(CommandOutletColor.DarkYellow, subscription.DisplayName)
			.AppendLine(CommandOutletColor.Gray, "{")
			.AppendLine(CommandOutletColor.Gray, "}");

		ConsoleTerminal.Instance.WriteLine();
	}
}
