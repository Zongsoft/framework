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
using System.Collections.Concurrent;

using Zongsoft.Services;
using Zongsoft.Communication;

namespace Zongsoft.Messaging
{
	public class TopicReceiverBase : Communication.IReceiver
	{
		#region 事件声明
		public event EventHandler<ChannelFailureEventArgs> Failed;
		public event EventHandler<ReceivedEventArgs> Received;
		#endregion

		#region 成员字段
		private IExecutionHandler _handler;
		#endregion

		#region 构造函数
		protected TopicReceiverBase(ITopic topic)
		{
			this.Topic = topic ?? throw new ArgumentNullException(nameof(topic));
		}
		#endregion

		#region 公共属性
		public ITopic Topic { get; }

		public IExecutionHandler Handler
		{
			get => _handler;
			set => _handler = value ?? throw new ArgumentNullException();
		}
		#endregion

		#region 虚拟方法
		protected virtual void OnFail(Exception exception)
		{
			//激发“Failed”事件
			this.Failed?.Invoke(this, new ChannelFailureEventArgs(null, exception));
		}

		protected virtual void OnReceive(TopicMessage message)
		{
			if(message == null)
				return;

			//激发“Received”事件
			this.Received?.Invoke(this, new ReceivedEventArgs(null, message));

			if(_handler != null)
				_handler.HandleAsync(message);
		}
		#endregion
	}
}
