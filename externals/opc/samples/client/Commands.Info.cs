using System;
using System.Reflection;

using Zongsoft.Common;
using Zongsoft.Terminals;
using Zongsoft.Components;

namespace Zongsoft.Externals.Opc.Samples;

internal partial class Commands
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
				PrintSubscriber(content, subscriber, ++index, context.Expression.Options.GetValue("detailed", false));
			}
		}

		context.Output.Write(content);
	}

	private static void PrintSubscriber(CommandOutletContent content, Subscriber subscriber, int index, bool detailed)
	{
		if(subscriber == null)
			return;

		content.Append(CommandOutletColor.DarkMagenta, $"#{index}")
			.Append(CommandOutletColor.DarkCyan, $" [{subscriber.Identifier}]")
			.Append(CommandOutletColor.DarkYellow, subscriber.Description)
			.Append(CommandOutletColor.DarkGray, " (")
			.Append(subscriber.Registered ? CommandOutletColor.Green : CommandOutletColor.Magenta, subscriber.Registered ? "Registered" : "Unregistered")
			.AppendLine(CommandOutletColor.DarkGray, ")");

		for(int i = 0; i < subscriber.Entries.Count; i++)
		{
			content
				.Append(CommandOutletColor.DarkGray, $"\t[{i + 1}] ")
				.Append(CommandOutletColor.DarkGreen, subscriber[i].Name);

			if(subscriber[i].Type == null)
				content.AppendLine();
			else
				content
					.Append(CommandOutletColor.DarkGray, "@")
					.AppendLine(CommandOutletColor.DarkYellow, TypeAlias.GetAlias(subscriber[i].Type));
		}

		if(detailed && subscriber.Subscription != null)
		{
			content.AppendLine(CommandOutletColor.Gray, "{");

			//获取订阅对象的公共实例属性集
			var properties = subscriber.Subscription.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);

			foreach(var property in properties)
			{
				if(!property.CanRead || property.GetIndexParameters().Length > 0)
					continue;

				content.Append(CommandOutletColor.DarkGreen, $"\t{property.Name}")
					   .Append(CommandOutletColor.DarkGray, " : ")
					   .Append(CommandOutletColor.DarkCyan, $"({TypeAlias.GetAlias(property.PropertyType)}) ");

				var value = property.GetValue(subscriber.Subscription);

				if(value == null)
					content.AppendLine(CommandOutletColor.DarkGray, "NULL");
				else
					content.AppendLine(CommandOutletColor.DarkYellow, value.ToString());
			}

			content.AppendLine(CommandOutletColor.Gray, "}");
		}
	}
}
