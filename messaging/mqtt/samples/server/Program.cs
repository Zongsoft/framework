using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Common;
using Zongsoft.Terminals;
using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Messaging.Mqtt.Samples;

internal class Program
{
	static async Task Main(string[] args)
	{
		using var server = new MqttQueueServer() { Handler = Handler.Instance };
		await server.StartAsync([]);

		var executor = Terminal.Console.Executor;
		executor.Command("start", async (context, cancellation) => await server.StartAsync([], cancellation));
		executor.Command("stop", async (context, cancellation) => await server.StopAsync([], cancellation));

		executor.Command("info", async context =>
		{
			context.Output.Write(CommandOutletColor.Cyan, "State: ");
			context.Output.WriteLine(server.State == WorkerState.Running ? CommandOutletColor.Green : CommandOutletColor.Magenta, server.State);
			context.Output.Write(CommandOutletColor.Cyan, "Port: ");
			context.Output.WriteLine(CommandOutletColor.Green, server.Port);

			var channels = server.Channels;
			context.Output.Write(CommandOutletColor.Cyan, "Channels: ");
			context.Output.WriteLine(CommandOutletColor.Green, channels.Count);

			var index = 0;
			foreach(var channel in channels)
			{
				context.Output.WriteLine(CommandOutletColor.DarkGreen,
					$"  [{++index}] {channel.Identifier}@{channel.Address} ({channel.ProtocolVersion})");
				context.Output.WriteLine(CommandOutletColor.DarkGray,
					$"      Connected: {channel.ConnectedTimestamp:O}");
				context.Output.WriteLine(CommandOutletColor.DarkGray,
					$"      Sent: {channel.SentApplicationMessagesCount} messages/{channel.SentPacketsCount} packets/{channel.BytesSent} bytes");
				context.Output.WriteLine(CommandOutletColor.DarkGray,
					$"      Received: {channel.ReceivedApplicationMessagesCount} messages/{channel.ReceivedPacketsCount} packets/{channel.BytesReceived} bytes");
			}

			var sessions = server.Sessions;
			context.Output.Write(CommandOutletColor.Cyan, "Sessions: ");
			context.Output.WriteLine(CommandOutletColor.Green, sessions.Count);

			index = 0;
			foreach(var session in sessions)
			{
				context.Output.WriteLine(CommandOutletColor.DarkGreen, $"  [{++index}] {session.Identifier}");
				context.Output.WriteLine(CommandOutletColor.DarkGray,
					$"      Created: {session.CreatedTimestamp:O}; Disconnected: {session.DisconnectedTimestamp:O}");
				context.Output.WriteLine(CommandOutletColor.DarkGray,
					$"      Expiry: {session.ExpiryInterval}s; Pending: {session.PendingApplicationMessagesCount}");
			}

			var topic = context.Options.GetValue<string>("topic");
			if(!string.IsNullOrEmpty(topic))
			{
				var message = await server.GetRetainedMessageAsync(topic);
				context.Output.Write(CommandOutletColor.Cyan, "Retained: ");

				if(message.IsEmpty)
					context.Output.WriteLine(CommandOutletColor.Magenta, "N/A");
				else
				{
					context.Output.WriteLine(CommandOutletColor.Green, $"{message.Topic} ({message.Data.Length} bytes)");
					context.Output.WriteLine(CommandOutletColor.Gray, Encoding.UTF8.GetString(message.Data));
				}
			}
			else
			{
				var messages = await server.GetRetainedMessagesAsync();
				context.Output.Write(CommandOutletColor.Cyan, "Retained Messages: ");
				context.Output.WriteLine(CommandOutletColor.Green, messages.Length);

				for(int i = 0; i < messages.Length; i++)
					context.Output.WriteLine(CommandOutletColor.DarkGreen,
						$"  [{i + 1}] {messages[i].Topic} ({messages[i].Data.Length} bytes)");
			}
		});

		var splash = CommandOutletContent.Create()
			.AppendLine(CommandOutletColor.Yellow, new string('·', 50))
			.AppendLine(CommandOutletColor.Blue, "Welcome to the MQTT Broker.".Justify(50))
			.AppendLine(CommandOutletColor.Yellow, new string('·', 50));

		await executor.RunAsync(splash);
	}

	internal sealed class Handler : HandlerBase<Message>
	{
		#region 单例字段
		public static readonly Handler Instance = new();
		#endregion

		#region 重写方法
		protected override ValueTask OnHandleAsync(Message message, Parameters parameters, CancellationToken cancellation)
		{
			var content = CommandOutletContent.Create()
				.Append(CommandOutletColor.Cyan, "[Received]")
				.Append(CommandOutletColor.DarkYellow, $" {message.Identity ?? "N/A"}")
				.Append(CommandOutletColor.DarkCyan, " Topic:")
				.AppendLine(CommandOutletColor.DarkGreen, message.Topic)
				.AppendLine(CommandOutletColor.Gray, Encoding.UTF8.GetString(message.Data));

			Terminal.Console.Executor.Output.Write(content);
			return ValueTask.CompletedTask;
		}
		#endregion
	}
}
