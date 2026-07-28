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
		using var queue = new MqttQueue("MQTT",
			Configuration.MqttConnectionSettingsDriver.Instance.GetSettings("Mqtt", $"server=127.0.0.1:1883;client=Zongsoft.Messaging.Mqtt.Sample-{Guid.NewGuid():N};"));

		var executor = Terminal.Console.Executor;
		executor.Command("reset", context => Handler.Instance.Reset());
		executor.Command("close", context => queue.Dispose());

		executor.Command("info", context =>
		{
			context.Output.WriteLine(CommandOutletColor.Green, queue.ToString());

			if(queue.Subscribers.Count > 0)
			{
				int index = 0;
				context.Output.WriteLine(new string('-', 50));

				foreach(var subscriber in queue.Subscribers)
					context.Output.WriteLine($"[{++index}] {subscriber.Topic}");
			}
		});

		executor.Command("subscribe", async (context, cancellation) =>
		{
			if(context.Arguments.IsEmpty)
				throw new CommandException("Missing the topics for subscribe.");

			for(int i = 0; i < context.Arguments.Count; i++)
			{
				var subscriber = await queue.SubscribeAsync(context.Arguments[i], Handler.Instance, cancellation);

				if(subscriber == null)
					context.Output.WriteLine(CommandOutletColor.DarkRed, $"Failed to subscribe topic: {context.Arguments[i]}");
				else
					context.Output.WriteLine(CommandOutletColor.DarkGreen, $"The subscription to the '{subscriber.Topic}' topic was successful.");
			}
		});

		executor.Command("unsubscribe", async (context, cancellation) =>
		{
			if(context.Arguments.IsEmpty)
				throw new CommandException("Missing the topics for unsubscribe.");

			for(int i = 0; i < context.Arguments.Count; i++)
			{
				if(queue.Subscribers.TryGetValue(context.Arguments[i], out var subscriber))
					await subscriber.UnsubscribeAsync(cancellation);
			}
		});

		executor.Command("produce", async (context, cancellation) =>
		{
			var round = context.Options.GetValue<int>("round", 1);
			var topic = context.Options.GetValue<string>("topic");

			if(string.IsNullOrEmpty(topic))
				throw new CommandOptionException("topic", "The topic is required.");

			var stopwatch = System.Diagnostics.Stopwatch.StartNew();

			for(int i = 0; i < round; i++)
			{
				for(int j = 0; j < context.Arguments.Count; j++)
				{
					var identifier = await queue.ProduceAsync(
						topic,
						Encoding.UTF8.GetBytes($"[{i + 1}]{context.Arguments[j]}"),
						null,
						cancellation);

					context.Output.WriteLine(CommandOutletColor.DarkGreen, $"[{i + 1}] {topic} Sent. (Identifier:{identifier ?? "N/A"})");
				}
			}

			stopwatch.Stop();
			context.Output.WriteLine(CommandOutletColor.Magenta, $"Elapsed: {stopwatch.Elapsed}");
		});

		executor.Aliaser.Set("produce", "send");
		executor.Aliaser.Set("subscribe", "sub");
		executor.Aliaser.Set("unsubscribe", "unsub");

		var splash = CommandOutletContent.Create()
			.AppendLine(CommandOutletColor.Yellow, new string('·', 50))
			.AppendLine(CommandOutletColor.Cyan, "Welcome to the MQTT Client.".Justify(50))
			.AppendLine(CommandOutletColor.Yellow, new string('·', 50));

		await executor.RunAsync(splash);
	}

	internal sealed class Handler : HandlerBase<Message>
	{
		#region 单例字段
		public static readonly Handler Instance = new();
		#endregion

		#region 私有变量
		private volatile int _count;
		#endregion

		#region 重置方法
		public void Reset() => _count = 0;
		#endregion

		#region 重写方法
		protected override async ValueTask OnHandleAsync(Message message, Parameters parameters, CancellationToken cancellation)
		{
			if(message.IsEmpty)
				return;

			var count = Interlocked.Increment(ref _count);
			var content = CommandOutletContent.Create()
				.Append(CommandOutletColor.Cyan, "[Received]")
				.Append(CommandOutletColor.DarkYellow, $"#{count}")
				.Append(CommandOutletColor.DarkCyan, " Topic:")
				.AppendLine(CommandOutletColor.DarkGreen, message.Topic)
				.AppendLine(CommandOutletColor.Gray, Encoding.UTF8.GetString(message.Data));

			Terminal.Console.Executor.Output.Write(content);
			await message.AcknowledgeAsync(cancellation);
		}
		#endregion
	}
}
