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
using System.Linq;
using System.ComponentModel;

using Zongsoft.Services;

namespace Zongsoft.Messaging.Commands
{
	[DisplayName("Text.QueueCommand.Name")]
	[Description("Text.QueueCommand.Description")]
	[CommandOption("queue", typeof(string), Description = "Text.QueueCommand.Options.Queue")]
	[CommandOption("topic", typeof(string), Description = "Text.QueueCommand.Options.Topic")]
	public class QueueCommand : CommandBase<CommandContext>
	{
		#region 成员字段
		private readonly IServiceProvider _serviceProvider;
		#endregion

		#region 构造函数
		public QueueCommand(IServiceProvider serviceProvider) : base("Queue")
		{
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		}

		public QueueCommand(IServiceProvider serviceProvider, string name) : base(name)
		{
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
			this.Queue = serviceProvider.ResolveAll<IMessageQueue>().FirstOrDefault(queue => queue.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
		}
		#endregion

		#region 公共属性
		public IMessageQueue Queue { get; set; }
		public IMessageTopic Topic { get; set; }
		#endregion

		#region 执行方法
		protected override object OnExecute(CommandContext context)
		{
			string name;

			if(context.Expression.Options.TryGetValue("queue", out name))
			{
				if(string.IsNullOrEmpty(name))
					return this.Queue;

				var providers = _serviceProvider.ResolveAll<IMessageQueueProvider>();

				if(providers == null)
					throw new CommandException(string.Format(Properties.Resources.Text_QueueCommand_NotFoundQueue, name));

				foreach(var provider in providers)
				{
					var queue = provider.GetQueue(name);

					if(queue != null)
					{
						this.Queue = queue;
						this.Topic = null;

						return queue; ;
					}
				}
			}

			if(context.Expression.Options.TryGetValue("topic", out name))
			{
				if(string.IsNullOrEmpty(name))
					return this.Topic;

				var providers = _serviceProvider.ResolveAll<IMessageTopicProvider>();

				if(providers == null)
					throw new CommandException(string.Format(Properties.Resources.Text_QueueCommand_NotFoundQueue, name));

				foreach(var provider in providers)
				{
					var topic = provider.GetTopic(name);

					if(topic != null)
					{
						this.Topic = topic;
						this.Queue = null;

						return topic; ;
					}
				}
			}

			if(this.Queue == null && this.Topic == null)
				throw new CommandException(string.Format(Properties.Resources.Text_CannotObtainCommandTarget, "Queue"));

			return null;
		}
		#endregion
	}
}
