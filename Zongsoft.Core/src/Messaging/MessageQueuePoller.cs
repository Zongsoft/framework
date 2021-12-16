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
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

using Zongsoft.Services;
using Zongsoft.Components;

namespace Zongsoft.Messaging
{
	public class MessageQueuePoller : WorkerBase
	{
		#region 成员字段
		private readonly MessageQueuePoller<MessageQueueMessage> _poller;
		#endregion

		#region 构造函数
		public MessageQueuePoller(string name) : base(name)
		{
			_poller = new MessageQueuePoller<MessageQueueMessage>(message =>
			{
				var handler = this.Handler;

				if(handler == null)
					return;

				if(handler.HandleAsync(this, message).GetAwaiter().GetResult().Succeed)
					message.Acknowledge();
			});
		}
		#endregion

		#region 公共属性
		[System.ComponentModel.TypeConverter(typeof(MessageQueueConverter))]
		public IMessageQueue<MessageQueueMessage> Queue { get => _poller.Queue; set => _poller.Queue = value; }
		public IHandler<MessageQueueMessage> Handler { get; set; }
		#endregion

		#region 重写方法
		protected override void OnStart(string[] args) => _poller.Start(MessageDequeueOptions.Default, 1000);
		protected override void OnStop(string[] args) => _poller.Stop();
		#endregion
	}
}
