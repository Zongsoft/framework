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

namespace Zongsoft.Externals.Opc.Samples;

internal class Commands
{
	public static void Info(TerminalCommandContext context, OpcClient client)
	{
		if(context == null || client == null)
			return;

		var content = CommandOutletContent.Create(CommandOutletColor.DarkGreen, client.Name)
			.Append(CommandOutletColor.DarkGray, " (")
			.Append(client.IsConnected ? CommandOutletColor.Green : CommandOutletColor.Magenta, client.IsConnected ? "Connected" : "Unconnect")
			.Append(CommandOutletColor.DarkGray, "|")
			.Append(CommandOutletColor.Cyan, $"{client.State}")
			.AppendLine(CommandOutletColor.DarkGray, ")")
			.AppendLine(CommandOutletColor.DarkYellow, client.Settings?.ToString());

		if(client.Subscribers.Count > 0)
		{
			int index = 0;

			foreach(var subscriber in client.Subscribers)
			{
				context.Output.WriteLine();
				PrintSubscription(content, subscriber.Subscription as Subscription, index++);
			}
		}

		context.Output.Write(content);
	}

	private static void PrintSubscription(CommandOutletContent content, Subscription subscription, int index)
	{
		if(subscription == null)
			return;

		content.Append(CommandOutletColor.DarkMagenta, $"#{index}")
			.Append(CommandOutletColor.DarkCyan, $" [{subscription.Id}]")
			.Append(CommandOutletColor.DarkYellow, subscription.DisplayName)
			.Append(CommandOutletColor.DarkGray, " (")
			.Append(subscription.Created ? CommandOutletColor.Green : CommandOutletColor.Magenta, subscription.Created ? "Created" : "Uncreated")
			.AppendLine(CommandOutletColor.DarkGray, ")")
			.AppendLine(CommandOutletColor.Gray, "{")

			.Append(CommandOutletColor.Blue, $"\t{nameof(subscription.Handle)}")
			.Append(CommandOutletColor.DarkGray, ":")
			.AppendLine(CommandOutletColor.DarkYellow, subscription.Handle == null ? "<NULL>" : subscription.Handle.ToString())

			.Append(CommandOutletColor.Blue, $"\t{nameof(subscription.CurrentLifetimeCount)}")
			.Append(CommandOutletColor.DarkGray, ":")
			.AppendLine(CommandOutletColor.DarkYellow, subscription.CurrentLifetimeCount.ToString())

			.Append(CommandOutletColor.Blue, $"\t{nameof(subscription.CurrentKeepAliveCount)}")
			.Append(CommandOutletColor.DarkGray, ":")
			.AppendLine(CommandOutletColor.DarkYellow, subscription.CurrentKeepAliveCount.ToString())

			.Append(CommandOutletColor.Blue, $"\t{nameof(subscription.CurrentPriority)}")
			.Append(CommandOutletColor.DarkGray, ":")
			.AppendLine(CommandOutletColor.DarkYellow, subscription.CurrentPriority.ToString())

			.Append(CommandOutletColor.Blue, $"\t{nameof(subscription.CurrentPublishingEnabled)}")
			.Append(CommandOutletColor.DarkGray, ":")
			.AppendLine(CommandOutletColor.DarkYellow, subscription.CurrentPublishingEnabled.ToString())

			.Append(CommandOutletColor.Blue, $"\t{nameof(subscription.CurrentPublishingInterval)}")
			.Append(CommandOutletColor.DarkGray, ":")
			.AppendLine(CommandOutletColor.DarkYellow, subscription.CurrentPublishingInterval.ToString())

			.Append(CommandOutletColor.Blue, $"\t{nameof(subscription.KeepAliveCount)}")
			.Append(CommandOutletColor.DarkGray, ":")
			.AppendLine(CommandOutletColor.DarkYellow, subscription.KeepAliveCount.ToString())

			.Append(CommandOutletColor.Blue, $"\t{nameof(subscription.LastNotification)}")
			.Append(CommandOutletColor.DarkGray, ":")
			.AppendLine(CommandOutletColor.DarkYellow, subscription.LastNotification?.ToString())

			.Append(CommandOutletColor.Blue, $"\t{nameof(subscription.LastNotificationTime)}")
			.Append(CommandOutletColor.DarkGray, ":")
			.AppendLine(CommandOutletColor.DarkYellow, subscription.LastNotificationTime.ToString())

			.Append(CommandOutletColor.Blue, $"\t{nameof(subscription.LifetimeCount)}")
			.Append(CommandOutletColor.DarkGray, ":")
			.AppendLine(CommandOutletColor.DarkYellow, subscription.LifetimeCount.ToString())

			.Append(CommandOutletColor.Blue, $"\t{nameof(subscription.MaxMessageCount)}")
			.Append(CommandOutletColor.DarkGray, ":")
			.AppendLine(CommandOutletColor.DarkYellow, subscription.MaxMessageCount.ToString())

			.Append(CommandOutletColor.Blue, $"\t{nameof(subscription.MaxNotificationsPerPublish)}")
			.Append(CommandOutletColor.DarkGray, ":")
			.AppendLine(CommandOutletColor.DarkYellow, subscription.MaxNotificationsPerPublish.ToString())

			.Append(CommandOutletColor.Blue, $"\t{nameof(subscription.MinLifetimeInterval)}")
			.Append(CommandOutletColor.DarkGray, ":")
			.AppendLine(CommandOutletColor.DarkYellow, subscription.MinLifetimeInterval.ToString())

			.Append(CommandOutletColor.Blue, $"\t{nameof(subscription.NotificationCount)}")
			.Append(CommandOutletColor.DarkGray, ":")
			.AppendLine(CommandOutletColor.DarkYellow, subscription.NotificationCount.ToString())

			.AppendLine(CommandOutletColor.Gray, "}");

	}
}
