/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.com>
 *
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Terminals;
using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Messaging.Commands;

[CommandOption("acknowledgeable", 'a', typeof(bool), true, "QueueSubscribeCommand.Acknowledgeable")]
[CommandOption("format", 'f', typeof(QueueMessageFormat), QueueMessageFormat.Raw, "QueueSubscribeCommand.Format")]
public class QueueSubscribeCommand : CommandBase<CommandContext>
{
	#region 构造函数
	public QueueSubscribeCommand() : this("Subscribe") { }
	public QueueSubscribeCommand(string name) : base(name) { }
	#endregion

	#region 重写方法
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation) =>
		context.ReactiveAsync(this.OnEnterAsync, this.OnExitAsync, cancellation);

	private async ValueTask OnEnterAsync(CommandContext context, CancellationToken cancellation)
	{
		var queue = context.Find<QueueCommand>(true)?.Queue ?? throw new CommandException(Properties.Resources.Messaging_QueueRequired_Message);

		context.Output.WriteLine(CommandOutletColor.Green, string.Format(Properties.Resources.QueueSubscribeCommand_Welcome, queue.Name));
		context.Output.WriteLine(CommandOutletColor.DarkYellow, Properties.Resources.QueueSubscribeCommand_Prompt + Environment.NewLine);

		var handler = new QueueHandler(context);
		var consumers = new List<IMessageConsumer>(context.Arguments.Count);

		foreach(var argument in context.Arguments)
		{
			var index = argument.IndexOfAny([':', '?']);
			var consumer = index > 0 && index < argument.Length ?
				await queue.SubscribeAsync(argument[..index], argument[(index + 1)..], handler, cancellation) :
				await queue.SubscribeAsync(argument, handler, cancellation);

			consumers.Add(consumer);
		}

		if(consumers.Count > 0)
			context.Result = consumers;
	}

	private async ValueTask OnExitAsync(CommandContext context, Exception exception, CancellationToken cancellation)
	{
		if(context.Result is IEnumerable<IMessageConsumer> consumers)
		{
			foreach(var consumer in consumers)
				await consumer.UnsubscribeAsync(cancellation);
		}
	}
	#endregion

	#region 嵌套子类
	private class QueueHandler : HandlerBase<Message>
	{
		private int _count;
		private readonly bool _acknowledgeable;
		private readonly CommandContext _context;
		private readonly QueueMessageFormat _format;

		public QueueHandler(CommandContext context)
		{
			_context = context;
			_format = context.Options.GetValue<QueueMessageFormat>("format");
			_acknowledgeable = context.Options.GetValue<bool>("acknowledgeable");
		}

		protected override async ValueTask OnHandleAsync(Message message, Parameters parameters, CancellationToken cancellation)
		{
			var topic = string.IsNullOrEmpty(message.Topic) ? "*" : message.Topic;
			var content = CommandOutletContent
				.Create(CommandOutletColor.DarkGray, $"[{Interlocked.Increment(ref _count)}] ")
				.Append(CommandOutletColor.DarkGreen, topic);

			if(!string.IsNullOrEmpty(message.Tags))
				content.Append(CommandOutletColor.DarkGray, $"({message.Tags})");

			if(!string.IsNullOrEmpty(message.Identity))
				content.Append(CommandOutletColor.DarkCyan, $"@{message.Identity}");

			content.Append(CommandOutletColor.DarkYellow, $" {message.Timestamp.ToLocalTime()} ");
			content.AppendLine(CommandOutletColor.DarkMagenta, message.Identifier);
			content.Append(
				_format == QueueMessageFormat.Text ?
				Encoding.UTF8.GetString(message.Data) :
				System.Convert.ToBase64String(message.Data)
			);

			if(_acknowledgeable)
			{
				//输出内容
				_context.Output.Write(content);

				//应答消息
				await message.AcknowledgeAsync(cancellation);

				//追加“已应答”提示文本
				_context.Output.WriteLine(CommandOutletColor.Blue, $" ({Properties.Resources.Acknowledged})");
			}
			else
			{
				//输出内容
				_context.Output.WriteLine(content);
			}
		}
	}
	#endregion
}

public enum QueueMessageFormat
{
	Raw,
	Text,
}
