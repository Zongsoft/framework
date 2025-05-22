using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Terminals;
using Zongsoft.Components;

namespace Zongsoft.Externals.Opc.Samples;

partial class Commands
{
	public sealed class ListenCommand(OpcClient client) : CommandBase<CommandContext>("Listen")
	{
		#region 成员字段
		private readonly OpcClient _client = client ?? throw new ArgumentNullException(nameof(client));
		#endregion

		#region 重写方法
		protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
		{
			return context.ReactiveAsync(this.OnEnterAsync, this.OnExitAsync, cancellation);
		}

		private ValueTask OnEnterAsync(CommandContext context, CancellationToken cancellation)
		{
			if(!_client.IsConnected)
				throw new CommandException($"The {nameof(OpcClient)}({_client.Name}) is not connected.");

			var subscribers = new List<Subscriber>();

			if(context.Expression.Arguments.Length == 0)
			{
				foreach(var subscriber in _client.Subscribers)
				{
					subscribers.Add(subscriber);
					subscriber.Consume((subscriber, entry, value) => Dump(context.Output, subscriber, entry, value));
				}

				//显示欢迎信息
				context.Output.WriteLine(Welcome(subscribers));

				context.Result = subscribers;
				return ValueTask.CompletedTask;
			}

			for(int i = 0; i < context.Expression.Arguments.Length; i++)
			{
				if(!uint.TryParse(context.Expression.Arguments[i], out var id))
				{
					context.Output.WriteLine(CommandOutletColor.DarkMagenta, $"The specified '{context.Expression.Arguments[i]}' subscriber identifier is an illegal integer.");
					continue;
				}

				if(!_client.Subscribers.TryGetValue(id, out var subscriber))
				{
					context.Output.WriteLine(CommandOutletColor.DarkMagenta, $"The specified '{id}' subscriber does not exist.");
					continue;
				}

				subscribers.Add(subscriber);

				//挂载消费者的通知回调函数
				subscriber.Consume((subscriber, entry, value) => Dump(context.Output, subscriber, entry, value));
			}

			//显示欢迎信息
			context.Output.WriteLine(Welcome(subscribers));

			context.Result = subscribers;
			return ValueTask.CompletedTask;
		}

		private ValueTask OnExitAsync(CommandContext context, Exception exception, CancellationToken cancellation)
		{
			if(exception != null)
				throw exception;

			if(context.Result is IEnumerable<Subscriber> subscribers)
			{
				//依次取消所有订阅者的消费事件
				foreach(var subscriber in subscribers)
					subscriber.Consume();
			}

			return ValueTask.CompletedTask;
		}
		#endregion

		#region 私有方法
		private static CommandOutletContent Welcome(IEnumerable<Subscriber> subscribers) => CommandOutletContent.Create()
			.AppendLine(new string('-', 80))
			.Append(CommandOutletColor.DarkYellow, "  Welcome to subscription listening mode, press ")
			.Append(CommandOutletColor.Magenta, "Ctrl+C")
			.AppendLine(CommandOutletColor.DarkYellow, " key to exit this mode.")
			.AppendLine(new string('-', 80));

		private static CommandOutletContent GetConsumer(Subscriber subscriber, Subscriber.Entry entry, object value) => CommandOutletContent.Create()
			.Append(CommandOutletColor.DarkGray, "[")
			.Append(CommandOutletColor.DarkGreen, nameof(Subscriber))
			.Append(CommandOutletColor.DarkGray, "#")
			.Append(CommandOutletColor.DarkCyan, $"{subscriber.Identifier}")
			.Append(CommandOutletColor.DarkGray, "] ")
			.Append(CommandOutletColor.DarkYellow, entry.Name)
			.Append(CommandOutletColor.DarkGray, " : ")
			.AppendValue(value);

		private static void Dump(ICommandOutlet output, Subscriber subscriber, Subscriber.Entry entry, object value) => output.Write(GetConsumer(subscriber, entry, value));
		#endregion
	}
}