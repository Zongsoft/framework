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
	public class QueueInCommand : CommandBase<CommandContext>
	{
		#region 构造函数
		public QueueInCommand() : this("In") { }
		public QueueInCommand(string name) : base(name) { }
		#endregion

		#region 公共属性
		public string Topic { get; set; }
		#endregion

		#region 执行方法
		protected override object OnExecute(CommandContext context)
		{
			if(context.Expression.Options.TryGetValue<string>("topic", out var value))
				this.Topic = value == "*" ? null : value;

			var queue = context.CommandNode.FindQueue();

			if(queue != null)
			{
				var options = context.Expression.Options.TryGetValue<MessageReliability>("qos", out var reliability) ? new MessageEnqueueOptions(reliability) : MessageEnqueueOptions.Default;
				return this.ExecuteCore(queue.Name, context, data => queue.Enqueue(data, options));
			}

			if(string.IsNullOrEmpty(this.Topic))
				throw new CommandOptionMissingException("topic");

			var topic = context.CommandNode.FindTopic();

			if(topic != null)
			{
				var options = context.Expression.Options.TryGetValue<MessageReliability>("qos", out var reliability) ? new MessageTopicPublishOptions(reliability) : MessageTopicPublishOptions.Default;
				return this.ExecuteCore(topic.Name, context, data => topic.Publish(data, this.Topic, context.Expression.Options.TryGetValue<string>("tags", out value) ? value : null, options));
			}

			return null;
		}

		private IList<string> ExecuteCore(string name, CommandContext context, Func<byte[], string> invoke)
		{
			var round = context.Expression.Options.GetValue<int>("round");
			var list = new List<string>();

			for(int i = 0; i < round; i++)
			{
				switch(context.Parameter)
				{
					case byte[] buffer:
						list.Add(invoke(buffer));
						break;
					case Stream stream:
						using(var reader = new BinaryReader(stream))
						{
							list.Add(invoke(reader.ReadBytes((int)(stream.Length - stream.Position))));
						}
						break;
				}

				list.AddRange(this.ResolveValue(context, i, data => data == null ? null : invoke(data)));

				context.Output.WriteLine(CommandOutletColor.DarkGreen, string.Format(string.Format(Properties.Resources.Text_QueueInCommand_Message, i + 1, list.Count, name)));
			}

			return list;
		}
		#endregion

		#region 私有方法
		private IEnumerable<string> ResolveValue(CommandContext context, int round, Func<byte[], string> fallback)
		{
			switch(context.Expression.Options.GetValue<ContentType>("type"))
			{
				case ContentType.Round:
					yield return fallback(Encoding.ASCII.GetBytes((round + 1).ToString()));
					break;
				case ContentType.File:
					foreach(var arg in context.Expression.Arguments)
					{
						if(!File.Exists(arg))
						{
							context.Output.WriteLine(string.Format(Properties.Resources.FileOrDirectoryNotExists, arg));
							continue;
						}

						yield return fallback(File.ReadAllBytes(arg.ToString()));
					}
					break;
				case ContentType.String:
					var encoding = context.Expression.Options.GetValue<Encoding>("encoding") ?? Encoding.UTF8;

					foreach(var arg in context.Expression.Arguments)
					{
						yield return fallback(encoding.GetBytes(arg));
					}
					break;
				case ContentType.Byte:
					foreach(var arg in context.Expression.Arguments)
					{
						yield return fallback(new[] { Zongsoft.Common.Convert.ConvertValue<byte>(arg) });
					}
					break;
				case ContentType.Short:
					foreach(var arg in context.Expression.Arguments)
					{
						yield return fallback(BitConverter.GetBytes(Zongsoft.Common.Convert.ConvertValue<short>(arg)));
					}
					break;
				case ContentType.Integer:
					foreach(var arg in context.Expression.Arguments)
					{
						yield return fallback(BitConverter.GetBytes(Zongsoft.Common.Convert.ConvertValue<int>(arg)));
					}
					break;
				case ContentType.Long:
					foreach(var arg in context.Expression.Arguments)
					{
						yield return fallback(BitConverter.GetBytes(Zongsoft.Common.Convert.ConvertValue<long>(arg)));
					}
					break;
				case ContentType.Date:
				case ContentType.DateTime:
					if(context.Expression.Arguments.Length < 1)
						yield return fallback(Encoding.ASCII.GetBytes(DateTime.Now.ToString()));
					else
					{
						foreach(var arg in context.Expression.Arguments)
						{
							yield return fallback(Encoding.ASCII.GetBytes(Zongsoft.Common.Convert.ConvertValue<DateTime>(arg).ToString()));
						}
					}
					break;
				case ContentType.Guid:
					if(context.Expression.Arguments.Length < 1)
						yield return fallback(Guid.NewGuid().ToByteArray());
					else
					{
						foreach(var arg in context.Expression.Arguments)
						{
							yield return fallback(Zongsoft.Common.Convert.ConvertValue<Guid>(arg).ToByteArray());
						}
					}
					break;
			}
		}
		#endregion

		#region 枚举定义
		public enum ContentType
		{
			String,
			Round,
			[Zongsoft.ComponentModel.Alias("int")]
			Integer,
			Short,
			Long,
			Byte,
			Date,
			DateTime,
			Guid,
			File,
		}
		#endregion
	}
}
