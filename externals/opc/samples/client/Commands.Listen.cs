using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Terminals;
using Zongsoft.Components;

namespace Zongsoft.Externals.Opc.Samples;

partial class Commands
{
	public sealed class ListenCommand(OpcClient client) : TerminalReactiveCommandBase("Listen")
	{
		#region 成员字段
		private readonly OpcClient _client = client ?? throw new ArgumentNullException(nameof(client));
		#endregion

		#region 重写方法
		protected override ValueTask OnExitAsync(TerminalCommandContext context, Exception exception, CancellationToken cancellation)
		{
			if(exception != null)
				throw exception;

			if(context.Result is IEnumerable<Subscriber> subscribers)
			{
				foreach(var subscriber in subscribers)
					subscriber.Consume();
			}

			return ValueTask.CompletedTask;
		}

		protected override ValueTask OnEnterAsync(TerminalCommandContext context, CancellationToken cancellation)
		{
			if(!_client.IsConnected)
				throw new CommandException($"The {nameof(OpcClient)}({_client.Name}) is not connected.");

			var subscribers = new List<Subscriber>();

			if(context.Expression.Arguments.Length == 0)
			{
				foreach(var subscriber in _client.Subscribers)
				{
					subscribers.Add(subscriber);
					subscriber.Consume(OnListen);
				}

				//显示欢迎信息
				Welcome(subscribers);

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
				subscriber.Consume(OnListen);
			}

			//显示欢迎信息
			Welcome(subscribers);

			context.Result = subscribers;
			return ValueTask.CompletedTask;
		}
		#endregion

		#region 私有方法
		private static void Welcome(IEnumerable<Subscriber> subscribers)
		{
			var content = CommandOutletContent.Create()
				.AppendLine(new string('-', 80))
				.Append(CommandOutletColor.DarkYellow, "  Welcome to subscription listening mode, press ")
				.Append(CommandOutletColor.Magenta, "Ctrl+C")
				.AppendLine(CommandOutletColor.DarkYellow, " key to exit this mode.")
				.AppendLine(new string('-', 80));

			ConsoleTerminal.Instance.WriteLine(content);
		}

		private static void OnListen(Subscriber subscriber, Subscriber.Entry entry, object value)
		{
			var content = CommandOutletContent.Create()
				.Append(CommandOutletColor.DarkGray, "[")
				.Append(CommandOutletColor.DarkGreen, nameof(Subscriber))
				.Append(CommandOutletColor.DarkGray, "#")
				.Append(CommandOutletColor.DarkCyan, $"{subscriber.Identifier}")
				.Append(CommandOutletColor.DarkGray, "] ")
				.Append(CommandOutletColor.DarkYellow, entry.Name)
				.Append(CommandOutletColor.DarkGray, " : ")
				.AppendValue(value);

			ConsoleTerminal.Instance.Write(content);
		}
		#endregion
	}
}