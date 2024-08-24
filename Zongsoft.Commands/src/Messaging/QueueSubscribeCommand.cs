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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Commands library.
 *
 * The Zongsoft.Commands is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Commands is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Commands library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Messaging.Commands
{
	[CommandOption("acknowledgeable", typeof(bool), true, "Text.QueueSubscribeCommand.Acknowledgeable")]
	[CommandOption("format", typeof(QueueMessageFormat), QueueMessageFormat.Raw, "Text.QueueSubscribeCommand.Format")]
	public class QueueSubscribeCommand : Zongsoft.Services.Commands.HostListenCommandBase<IMessageQueue>
	{
		#region 私有变量
		private ICollection<IMessageConsumer> _consumers;
		#endregion

		#region 构造函数
		public QueueSubscribeCommand() : this("Subscribe") { }
		public QueueSubscribeCommand(string name) : base(name) { }
		#endregion

		#region 重写方法
		protected override void OnListening(CommandContext context, IMessageQueue queue)
		{
			context.Output.WriteLine(CommandOutletColor.Green, string.Format(Properties.Resources.QueueSubscribeCommand_Welcome, queue.Name));
			context.Output.WriteLine(CommandOutletColor.DarkYellow, Properties.Resources.QueueSubscribeCommand_Prompt + Environment.NewLine);

			var handler = new QueueHandler(context);
			_consumers = new List<IMessageConsumer>(context.Expression.Arguments.Length);

			foreach(var argument in context.Expression.Arguments)
			{
				var index = argument.IndexOfAny(new[] { ':', '?' });
				var consumer = index > 0 && index < argument.Length ?
					queue.SubscribeAsync(argument.Substring(0, index), argument.Substring(index + 1), handler).AsTask().GetAwaiter().GetResult() :
					queue.SubscribeAsync(argument, handler).AsTask().GetAwaiter().GetResult();

				_consumers.Add(consumer);
			}
		}

		protected override void OnListened(CommandContext context, IMessageQueue queue)
		{
			var consumers = Interlocked.Exchange(ref _consumers, null);

			if(consumers != null)
			{
				foreach(var consumer in consumers)
					consumer.UnsubscribeAsync().AsTask().GetAwaiter().GetResult();
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
				_format = context.Expression.Options.GetValue<QueueMessageFormat>("format");
				_acknowledgeable = context.Expression.Options.GetValue<bool>("acknowledgeable");
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
					_context.Output.WriteLine(CommandOutletColor.Blue, $" ({Properties.Resources.Text_Acknowledged})");
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
}
