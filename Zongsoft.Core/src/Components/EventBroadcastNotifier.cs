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
	public class EventBroadcastNotifier : IMessageHandler
	{
		#region 构造函数
		public EventBroadcastNotifier(IServiceProvider services)
		{
			this.Services = services ?? throw new ArgumentNullException(nameof(services));
		}
		#endregion

		#region 公共属性
		public IServiceProvider Services { get; }
		#endregion

		#region 处理方法
		public async ValueTask HandleAsync(Message message, CancellationToken cancellation = default)
		{
			if(message.IsEmpty || cancellation.IsCancellationRequested)
				return;

			//根据收到的消息解析出其对应的事件上下文
			var context = this.GetContext(message);

			if(context != null)
			{
				IList<Task> tasks = null;
				var subscriptions = this.GetSubscriptionsAsync(context, cancellation);

				await foreach(var subscription in subscriptions)
				{
					tasks ??= new List<Task>();

					foreach(var notification in subscription.Notifications)
						tasks.Add(this.NotifyAsync(notification, cancellation).AsTask());
				}

				//等待所有通知任务完成
				if(tasks != null && tasks.Count > 0)
					await Task.WhenAll(tasks);
			}

			//执行消息应答
			await message.AcknowledgeAsync(cancellation);
		}
		#endregion

		#region 虚拟方法
		protected virtual EventContextBase GetContext(Message message)
		{
			//将消息的标签作为事件的限定名称
			var descriptor = Events.GetEvent(message.Tags);

			//如果指定的事件未定义则返回空
			if(descriptor == null)
				return null;

			var type = descriptor.GetType().IsGenericType ? descriptor.GetType().GenericTypeArguments[0] : null;
			return type == null ? null : (EventContextBase)Serializer.Json.Deserialize(message.Data, typeof(EventContext<>).MakeGenericType(type));
		}

		protected virtual IAsyncEnumerable<IEventSubscription> GetSubscriptionsAsync(EventContextBase context, CancellationToken cancellation)
		{
			var type = context.GetType();
			var provider = type.IsGenericType ?
				this.Services.Resolve(typeof(IEventSubscriptionProvider<>).MakeGenericType(type.GenericTypeArguments[0])) ?? this.Services.Resolve<IEventSubscriptionProvider>(context) :
				this.Services.Resolve<IEventSubscriptionProvider>(context);

			return EventSubscriptionProviderUtility.GetSubscriptionsAsync(provider, context, cancellation);
		}

		protected virtual ITransmitter GetTransmitter(IEventSubscriptionNotification notification) => (this.Services ?? ApplicationContext.Current.Services).Resolve<ITransmitter>(notification.Notifier);
		protected virtual ValueTask NotifyAsync(IEventSubscriptionNotification notification, CancellationToken cancellation) =>
			this.GetTransmitter(notification)?.TransmitAsync(notification.Destination, notification.Template, notification.Argument, notification.Channel, cancellation) ?? ValueTask.CompletedTask;
		#endregion
	}
}