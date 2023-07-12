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
 * Copyright (C) 2010-2023 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Messaging;
using Zongsoft.Communication;
using Zongsoft.Serialization;

namespace Zongsoft.Components
{
	public abstract class EventBroadcastHandlerBase<T> : IMessageHandler
	{
		#region 构造函数
		protected EventBroadcastHandlerBase(IServiceProvider services) => this.Services = services;
		#endregion

		#region 保护属性
		protected IServiceProvider Services { get; }
		#endregion

		#region 处理方法
		public ValueTask HandleAsync(Message message, CancellationToken cancellation = default)
		{
			if(message.IsEmpty || cancellation.IsCancellationRequested)
				return ValueTask.CompletedTask;

			var context = this.GetContext(message);
			var subscriptions = this.GetSubscriptions(context);
			var tasks = subscriptions.Any() ? new List<Task>() : null;

			foreach(var subscription in subscriptions)
			{
				foreach(var notification in subscription.Notifications)
					tasks.Add(this.NotifyAsync(notification, cancellation).AsTask());
			}

			return new ValueTask(Task.WhenAll(tasks));
		}
		#endregion

		#region 抽象方法
		protected abstract IEnumerable<IEventSubscription> GetSubscriptions(EventContext<T> context);
		#endregion

		#region 虚拟方法
		protected virtual EventContext<T> GetContext(Message message) => Serializer.Json.Deserialize<EventContext<T>>(message.Data);
		protected virtual ITransmitter GetTransmitter(IEventSubscriptionNotification notification) => (this.Services ?? ApplicationContext.Current.Services).Resolve<ITransmitter>(notification.Notifier);
		protected virtual ValueTask NotifyAsync(IEventSubscriptionNotification notification, CancellationToken cancellation) =>
			this.GetTransmitter(notification)?.TransmitAsync(notification.Destination, notification.Template, notification.Argument, notification.Channel, cancellation) ?? ValueTask.CompletedTask;
		#endregion
	}
}