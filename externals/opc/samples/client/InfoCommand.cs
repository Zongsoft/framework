using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Common;
using Zongsoft.Components;

namespace Zongsoft.Externals.Opc.Samples;

public sealed class InfoCommand(OpcClient client) : CommandBase<CommandContext>("Info")
{
	private readonly OpcClient _client = client ?? throw new ArgumentNullException(nameof(client));

	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		var content = CommandOutletContent.Create(CommandOutletColor.DarkGreen, _client.Name)
			.Append(CommandOutletColor.DarkGray, " (")
			.Append(_client.IsConnected ? CommandOutletColor.Green : CommandOutletColor.Magenta, _client.IsConnected ? "Connected" : "Unconnect")
			.Append(CommandOutletColor.DarkGray, "|")
			.Append(CommandOutletColor.Cyan, $"{_client.State}")
			.AppendLine(CommandOutletColor.DarkGray, ")")
			.AppendLine(CommandOutletColor.DarkYellow, _client.Settings?.ToString());

		if(context.Arguments.Count > 0)
		{
			for(int i = 0; i < context.Arguments.Count; i++)
			{
				if(context.Arguments.TryGetValue<uint>(i, out var id) && _client.Subscribers.TryGetValue(id, out var subscriber))
					DumpSubscriber(content.Last, subscriber, -1, context.Options.GetValue("detailed", false));
			}
		}
		else if(_client.Subscribers.Count > 0)
		{
			int index = 0;

			foreach(var subscriber in _client.Subscribers)
			{
				if(index > 0)
					content.Last.AppendLine();

				DumpSubscriber(content.Last, subscriber, ++index, context.Options.GetValue("detailed", false));
			}
		}

		context.Output.Write(content);
		return default;
	}

	private static void DumpSubscriber(CommandOutletContent content, Subscriber subscriber, int index, bool detailed)
	{
		if(subscriber == null)
			return;

		if(index >= 0)
			content.Append(CommandOutletColor.DarkMagenta, $"#{index} ");

		content.Last
			.Append(CommandOutletColor.DarkCyan, $"[{subscriber.Identifier}]")
			.Append(CommandOutletColor.DarkYellow, subscriber.Description)
			.Append(CommandOutletColor.DarkGray, " (")
			.Append(subscriber.Registered ? CommandOutletColor.Green : CommandOutletColor.Magenta, subscriber.Registered ? "Registered" : "Unregistered")
			.Append(CommandOutletColor.DarkGray, ")")
			.Append(CommandOutletColor.DarkGray, " {")
			.Append(CommandOutletColor.Cyan, $"{subscriber.Statistics.NotificationCount:#,0000}")
			.AppendLine(CommandOutletColor.DarkGray, "}");

		var entryIndex = 1;
		foreach(var entry in subscriber.Entries)
		{
			if(subscriber.Entries.Count > 11 && entryIndex >= 10 && entryIndex != subscriber.Entries.Count)
			{
				if(entryIndex == 10)
					content.Last.AppendLine(CommandOutletColor.Gray, "\t\t... ...");

				entryIndex++;
				continue;
			}

			content.Last
				.Append(CommandOutletColor.DarkGray, $"\t[{entryIndex++}] ")
				.Append(CommandOutletColor.DarkGreen, entry.Name);

			if(entry.Type == null)
				content.Last.AppendLine();
			else
				content.Last
					.Append(CommandOutletColor.DarkGray, "@")
					.AppendLine(CommandOutletColor.DarkYellow, TypeAlias.GetAlias(entry.Type));
		}

		if(detailed && subscriber.Subscription != null)
		{
			content.Last.AppendLine(CommandOutletColor.Gray, "{");

			//获取订阅对象的公共实例属性集
			var properties = subscriber.Subscription.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);

			foreach(var property in properties)
			{
				if(!property.CanRead || property.GetIndexParameters().Length > 0)
					continue;

				content.Last
					.Append(CommandOutletColor.DarkGreen, $"\t{property.Name}")
					.Append(CommandOutletColor.DarkGray, " : ")
					.Append(CommandOutletColor.DarkCyan, $"({TypeAlias.GetAlias(property.PropertyType)}) ");

				var value = property.GetValue(subscriber.Subscription);

				if(value == null)
					content.Last.AppendLine(CommandOutletColor.DarkGray, "NULL");
				else
					content.Last.AppendLine(CommandOutletColor.DarkYellow, value.ToString());
			}

			content.Last.AppendLine(CommandOutletColor.Gray, "}");
		}
	}
}
