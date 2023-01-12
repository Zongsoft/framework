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
using Zongsoft.Configuration;

namespace Zongsoft.Messaging.Commands
{
	[DisplayName("Text.QueueCommand.Name")]
	[Description("Text.QueueCommand.Description")]
	[CommandOption("name", typeof(string), Description = "Text.QueueCommand.Options.Name")]
	public class QueueCommand : Services.Commands.HostCommandBase<IMessageQueue>
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

			foreach(var provider in serviceProvider.ResolveAll<IMessageQueueProvider>())
			{
				var queue = GetQueue(provider, name);

				if(queue != null)
				{
					this.Queue = queue;
					break;
				}
			}

			static IMessageQueue GetQueue(IMessageQueueProvider provider, string name)
			{
				try
				{
					return provider.Queue(name);
				}
				catch
				{
					return null;
				}
			}
		}
		#endregion

		#region 公共属性
		public IMessageQueue Queue { get => this.Host; set => this.Host = value; }
		#endregion

		#region 执行方法
		protected override object OnExecute(CommandContext context)
		{
			string name;

			if(context.Expression.Options.TryGetValue("name", out name))
			{
				if(string.IsNullOrEmpty(name))
					return this.Queue;

				var providers = _serviceProvider.ResolveAll<IMessageQueueProvider>();

				if(providers == null)
					throw new CommandException(string.Format(Properties.Resources.Text_QueueCommand_NotFoundQueue, name));

				foreach(var provider in providers)
				{
					var queue = provider.Queue(name);

					if(queue != null)
					{
						this.Queue = queue;

						//打印队列信息
						context.Output.WriteLine(CommandOutletColor.Green, queue.Name);
						PrintConnectionSetting(context.Output, queue.ConnectionSetting?.Values);

						return queue;
					}
				}
			}

			if(this.Queue == null)
				throw new CommandException(string.Format(Properties.Resources.Text_CannotObtainCommandTarget, "Queue"));

			if(this.Queue != null)
			{
				//打印队列信息
				context.Output.WriteLine(CommandOutletColor.Green, this.Queue.Name);
				PrintConnectionSetting(context.Output, this.Queue.ConnectionSetting?.Values);

				return this.Queue;
			}

			return null;
		}

		private static void PrintConnectionSetting(ICommandOutlet output, IConnectionSettingValues values)
		{
			if(values == null)
				return;

			var content = CommandOutletContent
				.Create(CommandOutletColor.DarkYellow, nameof(values.Server))
				.Append(CommandOutletColor.DarkGray, "=")
				.Append(CommandOutletColor.DarkGreen, values.Server)
				.Append(CommandOutletColor.DarkMagenta, ",")
				.Append(CommandOutletColor.DarkYellow, nameof(values.Instance))
				.Append(CommandOutletColor.DarkGray, "=")
				.Append(CommandOutletColor.DarkGreen, values.Instance)
				.Append(CommandOutletColor.DarkMagenta, ",")
				.Append(CommandOutletColor.DarkYellow, nameof(values.Client))
				.Append(CommandOutletColor.DarkGray, "=")
				.Append(CommandOutletColor.DarkGreen, values.Client);

			output.WriteLine(content);
		}
		#endregion
	}
}
