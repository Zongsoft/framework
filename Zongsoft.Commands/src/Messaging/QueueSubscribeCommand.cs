/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Services;
using System.Text;

namespace Zongsoft.Messaging.Commands
{
	[CommandOption("format", typeof(QueueMessageFormat))]
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
					queue.SubscribeAsync(argument.Substring(0, index), argument.Substring(index + 1), handler).GetAwaiter().GetResult() :
					queue.SubscribeAsync(argument, handler).GetAwaiter().GetResult();

				_consumers.Add(consumer);
			}
		}

		protected override void OnListened(CommandContext context, IMessageQueue queue)
		{
			var consumers = Interlocked.Exchange(ref _consumers, null);

			if(consumers != null)
			{
				foreach(var consumer in consumers)
					consumer.UnsubscribeAsync();
			}
		}
		#endregion

		#region 嵌套子类
		private class QueueHandler : IMessageHandler
		{
			private readonly CommandContext _context;
			private readonly QueueMessageFormat _format;

			public QueueHandler(CommandContext context)
			{
				_context = context;
				_format = context.Expression.Options.GetValue<QueueMessageFormat>("format");
			}

			public ValueTask HandleAsync(in Message message, CancellationToken cancellation = default)
			{
				var topic = string.IsNullOrEmpty(message.Topic) ? "*" : message.Topic;

				var content = string.IsNullOrEmpty(message.Tags) ?
					CommandOutletContent.Create(CommandOutletColor.DarkGreen, topic) :
					CommandOutletContent.Create(CommandOutletColor.DarkGreen, topic).Append(CommandOutletColor.DarkGray, $"({message.Tags})");

				if(!string.IsNullOrEmpty(message.Identity))
					content.Append(CommandOutletColor.DarkCyan, $"@{message.Identity}");

				content.Append(CommandOutletColor.DarkYellow, $" {message.Timestamp} ");
				content.AppendLine(CommandOutletColor.DarkMagenta, message.Identifier);

				content.AppendLine(
					_format == QueueMessageFormat.Text ?
					Encoding.UTF8.GetString(message.Data) :
					Convert.ToBase64String(message.Data)
				);

				_context.Output.WriteLine(content);

				//应答消息
				message.Acknowledge();

				return ValueTask.CompletedTask;
			}
		}
		#endregion
	}

	public enum QueueMessageFormat
	{
		None,
		Text,
	}
}
