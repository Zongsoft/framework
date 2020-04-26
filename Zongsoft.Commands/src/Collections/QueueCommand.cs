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
using System.Collections.Generic;

using Zongsoft.Services;

namespace Zongsoft.Collections.Commands
{
	[CommandOption("name", typeof(string), Description = "Text.QueueCommand.Options.Name")]
	public class QueueCommand : CommandBase<CommandContext>
	{
		#region 成员字段
		private IQueue _queue;
		private IQueueProvider _queueProvider;

		private System.IServiceProvider _serviceProvider;
		#endregion

		#region 构造函数
		public QueueCommand(IServiceProvider serviceProvider) : base("Queue")
		{
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

			_queue = (IQueue)serviceProvider.GetService(typeof(IQueue));
			_queueProvider = (IQueueProvider)serviceProvider.GetService(typeof(IQueueProvider));
		}

		public QueueCommand(IServiceProvider serviceProvider, string name) : base(name)
		{
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

			_queue = (IQueue)serviceProvider.GetService(typeof(IQueue));
			_queueProvider = (IQueueProvider)serviceProvider.GetService(typeof(IQueueProvider));
		}
		#endregion

		#region 公共属性
		public IQueue Queue
		{
			get => _queue;
			set => _queue = value ?? throw new ArgumentNullException();
		}

		public IQueueProvider QueueProvider
		{
			get => _queueProvider;
			set => _queueProvider = value ?? throw new ArgumentNullException();
		}
		#endregion

		#region 执行方法
		protected override object OnExecute(CommandContext context)
		{
			if(context.Expression.Options.TryGetValue("name", out string name))
			{
				var parts = name.Split('@');

				if(parts.Length == 2)
					_queueProvider = _serviceProvider.Match<IQueueProvider>(parts[1]);
				else
					_queueProvider = (IQueueProvider)_serviceProvider.GetService(typeof(IQueueProvider));

				if(_queueProvider == null)
					throw new CommandException(Properties.Resources.Text_QueueCommand_MissingQueueProvider);

				_queue = _queueProvider.GetQueue(parts[0]);

				if(_queue == null)
					throw new CommandException(string.Format(Properties.Resources.Text_QueueCommand_NotFoundQueue, name));
			}

			if(_queue == null)
				throw new CommandException(string.Format(Properties.Resources.Text_CannotObtainCommandTarget, "Queue"));

			//打印队列信息
			context.Output.WriteLine(string.Format(Properties.Resources.Text_QueueCommand_Message, _queue.Name, _queue.Count, _queue.GetType().FullName, _queue.ToString()));

			return _queue;
		}
		#endregion
	}
}
