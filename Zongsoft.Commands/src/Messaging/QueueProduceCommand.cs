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
using System.IO;
using System.Text;
using System.ComponentModel;
using System.Collections.Generic;

using Zongsoft.Services;

namespace Zongsoft.Messaging.Commands
{
	[DisplayName("Text.QueueInCommand.Name")]
	[Description("Text.QueueInCommand.Description")]
	[CommandOption("type", Type = typeof(ContentType), DefaultValue = ContentType.String)]
	[CommandOption("encoding", Type = typeof(Encoding), DefaultValue = "utf-8")]
	[CommandOption("round", Type = typeof(int), DefaultValue = 1, Description = "Text.QueueCommand.Options.Round")]
	[CommandOption("topic", Type = typeof(string))]
	[CommandOption("tags", Type = typeof(string))]
	[CommandOption("qos", Type = typeof(MessageReliability))]
	public class QueueProduceCommand : CommandBase<CommandContext>
	{
		#region 构造函数
		public QueueProduceCommand() : this("Produce") { }
		public QueueProduceCommand(string name) : base(name) { }
		#endregion

		#region 公共属性
		public string Topic { get; set; }
		#endregion

		#region 执行方法
		protected override object OnExecute(CommandContext context)
		{
			var queue = context.CommandNode.FindQueue();
			if(queue == null)
				return null;

			if(context.Expression.Options.TryGetValue<string>("topic", out var value))
				this.Topic = value == "*" ? null : value;

			if(string.IsNullOrEmpty(this.Topic))
				context.Output.WriteLine(CommandOutletColor.DarkMagenta, queue.Name);
			else
				context.Output.WriteLine(
					CommandOutletContent
					.Create(CommandOutletColor.DarkMagenta, queue.Name)
					.Append(":")
					.Append(CommandOutletColor.DarkYellow, this.Topic));

			var options = context.Expression.Options.TryGetValue<MessageReliability>("qos", out var reliability) ? new MessageEnqueueOptions(reliability) : MessageEnqueueOptions.Default;
			var tags = context.Expression.Options.TryGetValue<string>("tags", out value) ? value : null;

			return this.ExecuteCore(context, data => queue.ProduceAsync(this.Topic, tags, data, options).AsTask().GetAwaiter().GetResult());
		}

		private IList<string> ExecuteCore(CommandContext context, Func<byte[], string> invoke)
		{
			var round = context.Expression.Options.GetValue<int>("round");
			var list = new List<string>();
			var messageId = string.Empty;

			for(int i = 0; i < round; i++)
			{
				switch(context.Parameter)
				{
					case byte[] buffer:
						messageId = invoke(buffer);
						PrintMessage(context.Output, list.Count, ContentType.Binary, buffer.Length, Convert.ToBase64String(buffer), messageId);
						list.Add(messageId);
						break;
					case Stream stream:
						using(var reader = new BinaryReader(stream))
						{
							messageId = invoke(reader.ReadBytes((int)(stream.Length - stream.Position)));
							PrintMessage(context.Output, list.Count, ContentType.Binary, (int)stream.Length, stream.ToString(), messageId);
							list.Add(messageId);
						}
						break;
				}

				list.AddRange(this.ResolveValue(context, list.Count, data => data == null ? null : invoke(data)));
			}

			return list;
		}
		#endregion

		#region 私有方法
		private IEnumerable<string> ResolveValue(CommandContext context, int cardinal, Func<byte[], string> fallback)
		{
			string messageId;
			byte[] data;

			switch(context.Expression.Options.GetValue<ContentType>("type"))
			{
				case ContentType.File:
					foreach(var arg in context.Expression.Arguments)
					{
						if(!File.Exists(arg))
						{
							context.Output.WriteLine(string.Format(Properties.Resources.FileOrDirectoryNotExists, arg));
							continue;
						}

						data = File.ReadAllBytes(arg.ToString());
						messageId = fallback(data);
						PrintMessage(context.Output, cardinal++, ContentType.File, data.Length, arg, messageId);
						yield return messageId;
					}
					break;
				case ContentType.String:
					var encoding = context.Expression.Options.GetValue<Encoding>("encoding") ?? Encoding.UTF8;

					foreach(var arg in context.Expression.Arguments)
					{
						var text = arg == "#" || arg == "*" ? (cardinal + 1).ToString() : arg;
						data = encoding.GetBytes(text);
						messageId = fallback(data);
						PrintMessage(context.Output, cardinal++, ContentType.String, data.Length, text, messageId);
						yield return messageId;
					}
					break;
				case ContentType.Byte:
					foreach(var arg in context.Expression.Arguments)
					{
						data = new[] { Zongsoft.Common.Convert.ConvertValue<byte>(arg) };
						messageId = fallback(data);
						PrintMessage(context.Output, cardinal++, ContentType.Byte, 1, arg, messageId);
						yield return messageId;
					}
					break;
				case ContentType.Short:
					foreach(var arg in context.Expression.Arguments)
					{
						data = BitConverter.GetBytes(Zongsoft.Common.Convert.ConvertValue<short>(arg));
						messageId = fallback(data);
						PrintMessage(context.Output, cardinal++, ContentType.Short, data.Length, arg, messageId);
						yield return messageId;
					}
					break;
				case ContentType.Integer:
					foreach(var arg in context.Expression.Arguments)
					{
						var number = arg == "#" || arg == "*" ? cardinal + 1 : Zongsoft.Common.Convert.ConvertValue<int>(arg);
						data = BitConverter.GetBytes(number);
						messageId = fallback(data);
						PrintMessage(context.Output, cardinal++, ContentType.Integer, data.Length, number.ToString(), messageId);
						yield return messageId;
					}
					break;
				case ContentType.Long:
					foreach(var arg in context.Expression.Arguments)
					{
						data = BitConverter.GetBytes(Zongsoft.Common.Convert.ConvertValue<long>(arg));
						messageId = fallback(data);
						PrintMessage(context.Output, cardinal++, ContentType.Long, data.Length, arg, messageId);
						yield return messageId;
					}
					break;
				case ContentType.Date:
				case ContentType.DateTime:
					if(context.Expression.Arguments.Length < 1)
					{
						var now = DateTime.Now;
						data = Encoding.ASCII.GetBytes(now.ToString());
						messageId = fallback(data);
						PrintMessage(context.Output, cardinal++, ContentType.DateTime, data.Length, now.ToString(), messageId);
						yield return messageId;
					}
					else
					{
						foreach(var arg in context.Expression.Arguments)
						{
							data = Encoding.ASCII.GetBytes(Zongsoft.Common.Convert.ConvertValue<DateTime>(arg).ToString());
							messageId = fallback(data);
							PrintMessage(context.Output, cardinal++, ContentType.DateTime, data.Length, arg, messageId);
							yield return messageId;
						}
					}
					break;
				case ContentType.Guid:
					if(context.Expression.Arguments.Length < 1)
					{
						var guid = Guid.NewGuid();
						data = guid.ToByteArray();
						messageId = fallback(data);
						PrintMessage(context.Output, cardinal++, ContentType.DateTime, data.Length, guid.ToString(), messageId);
						yield return messageId;
					}
					else
					{
						foreach(var arg in context.Expression.Arguments)
						{
							if(Guid.TryParse(arg, out var guid))
							{
								data = guid.ToByteArray();
								messageId = fallback(data);
								PrintMessage(context.Output, cardinal++, ContentType.DateTime, data.Length, arg, messageId);
								yield return messageId;
							}
						}
					}
					break;
			}
		}

		private void PrintMessage(ICommandOutlet output, int index, ContentType type, int length, string value, string id)
		{
			var content = CommandOutletContent
				.Create(CommandOutletColor.DarkMagenta, $"[{index + 1}] ")
				.Append(CommandOutletColor.DarkGray, "(")
				.Append(CommandOutletColor.DarkCyan, type.ToString())
				.Append(CommandOutletColor.DarkGray, ":")
				.Append(CommandOutletColor.DarkGreen, length.ToString())
				.Append(CommandOutletColor.DarkGray, ")");

			if(!string.IsNullOrEmpty(id))
				content.Append(CommandOutletColor.DarkYellow, id);

			output.WriteLine(content.Append(" " + value));
		}
		#endregion

		#region 枚举定义
		public enum ContentType
		{
			String,
			[Zongsoft.ComponentModel.Alias("int")]
			Integer,
			Short,
			Long,
			Byte,
			Date,
			DateTime,
			Guid,
			File,
			Binary,
		}
		#endregion
	}
}
