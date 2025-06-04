using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Caching;
using Zongsoft.Terminals;
using Zongsoft.Components;

namespace Zongsoft.Externals.Opc.Samples;

partial class Commands
{
	[CommandOption("spooling", typeof(bool), DefaultValue = false)]
	[CommandOption("distinct", typeof(bool), DefaultValue = false)]
	[CommandOption("limit", typeof(int), DefaultValue = 1000)]
	[CommandOption("period", typeof(int), DefaultValue = 1000, Description = "Milliseconds")]
	public sealed class ListenCommand(OpcClient client) : CommandBase<CommandContext>("Listen")
	{
		#region 成员字段
		private readonly OpcClient _client = client ?? throw new ArgumentNullException(nameof(client));
		private Spooler<Metric> _spooler;
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

			if(context.Expression.Arguments.IsEmpty)
			{
				subscribers.AddRange(_client.Subscribers);
			}
			else
			{
				for(int i = 0; i < context.Expression.Arguments.Count; i++)
				{
					if(!context.Expression.Arguments.TryGetValue<uint>(i, out var id))
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
				}
			}

			if(subscribers.Count == 0)
				throw new CommandException($"No subscriptions.");

			if(context.Expression.Options.Contains("spooling"))
				_spooler = new(
					this.OnFlush,
					context.Expression.Options.Contains("distinct"),
					TimeSpan.FromMilliseconds(context.Expression.Options.GetValue<int>("period")),
					context.Expression.Options.GetValue<int>("limit"));

			//挂载消费者的通知回调函数
			foreach(var subscriber in subscribers)
			{
				if(_spooler == null)
					subscriber.Consume((subscriber, entry, value) => Dump(context.Output, subscriber, entry, value));
				else
					subscriber.Consume((subscriber, entry, value) => _spooler.Put(new(entry.Name, value)));
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

			//处置释放缓冲器
			_spooler?.Dispose();

			//重置统计计数器
			_total = 0;
			_timestamp = DateTime.Now;

			return ValueTask.CompletedTask;
		}
		#endregion

		#region 缓冲处理
		private ulong _total;
		private DateTime _timestamp;

		private void OnFlush(IEnumerable<Metric> entries)
		{
			if(_total > 0 && (DateTime.Now - _timestamp).TotalMilliseconds > 500)
				Terminal.WriteLine();

			var count = entries.Count();
			var total = Interlocked.Add(ref _total, (ulong)count);

			var content = CommandOutletContent.Create()
				.Append(CommandOutletColor.DarkCyan, "OnFlush")
				.Append(CommandOutletColor.DarkGray, " : ")
				.Append(CommandOutletColor.DarkGreen, $"{count:#,####}")
				.Append(CommandOutletColor.DarkGray, "/")
				.Append(CommandOutletColor.DarkYellow, $"{total:#,####}")
				.Append(CommandOutletColor.DarkGray, " (")
				.Append(CommandOutletColor.DarkMagenta, $"{DateTime.Now:HH:mm:ss.fff}")
				.Append(CommandOutletColor.DarkGray, ")");

			Terminal.WriteLine(content);

			//更新时间戳
			_timestamp = DateTime.Now;
		}
		#endregion

		#region 私有方法
		private static CommandOutletContent Welcome(IEnumerable<Subscriber> subscribers) => CommandOutletContent.Create()
			.AppendLine(new string('-', 80))
			.Append(CommandOutletColor.DarkYellow, "  Welcome to subscription listening mode, press ")
			.Append(CommandOutletColor.Magenta, "Ctrl+C")
			.AppendLine(CommandOutletColor.DarkYellow, " key to exit this mode.")
			.AppendLine(new string('-', 80));

		private static CommandOutletContent DumpValue(Subscriber subscriber, Subscriber.Entry entry, object value) => CommandOutletContent.Create()
			.Append(CommandOutletColor.DarkGray, "[")
			.Append(CommandOutletColor.DarkGreen, nameof(Subscriber))
			.Append(CommandOutletColor.DarkGray, "#")
			.Append(CommandOutletColor.DarkCyan, $"{subscriber.Identifier}")
			.Append(CommandOutletColor.DarkGray, "] ")
			.Append(CommandOutletColor.DarkYellow, entry.Name)
			.Append(CommandOutletColor.DarkGray, " : ")
			.AppendValue(value);

		private static void Dump(ICommandOutlet output, Subscriber subscriber, Subscriber.Entry entry, object value) => output.Write(DumpValue(subscriber, entry, value));
		#endregion

		#region 嵌套结构
		internal readonly struct Metric(string identifier, object value) : IEquatable<Metric>
		{
			public readonly string Identifier = identifier;
			public readonly object Value = value;

			public bool Equals(Metric other) => string.Equals(this.Identifier, other.Identifier);
			public override bool Equals(object obj) => obj is Metric metric && this.Equals(metric);
			public override int GetHashCode() => HashCode.Combine(this.Identifier);
			public override string ToString() => string.IsNullOrEmpty(this.Identifier) ? string.Empty : $"{this.Identifier}:{this.Value}";
		}
		#endregion
	}
}