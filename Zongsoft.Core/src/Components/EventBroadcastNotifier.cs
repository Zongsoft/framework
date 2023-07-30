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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Messaging;
using Zongsoft.Communication;

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

			//获取事件名称
			var name = message.Tags;
			if(string.IsNullOrEmpty(name))
				return;

			//根据收到的消息解析出其对应的事件参数
			(var argument, IDictionary<string, object> parameters) = Events.Marshaler.Unmarshal(name, message.Data);

			//定义异步通知操作的任务集
			var tasks = new List<Task>();

			foreach(var provider in this.Services.ResolveAll<IEventSubscriptionProvider>(name))
			{
				var subscriptions = provider.GetSubscriptionsAsync(name, argument, parameters, cancellation);

				await foreach(var subscription in subscriptions)
				{
					foreach(var notification in subscription.Notifications)
						tasks.Add(this.NotifyAsync(notification, cancellation).AsTask());
				}

				if(tasks != null && tasks.Count > 0)
				{
					//等待所有通知任务完成
					await Task.WhenAll(tasks);

					//清空已完成的任务集
					tasks.Clear();
				}
			}

			//执行消息应答
			await message.AcknowledgeAsync(cancellation);
		}
		#endregion

		#region 虚拟方法
		protected virtual ITransmitter GetTransmitter(IEventSubscriptionNotification notification) => (this.Services ?? ApplicationContext.Current.Services).Resolve<ITransmitter>(notification.Notifier);
		protected virtual ValueTask NotifyAsync(IEventSubscriptionNotification notification, CancellationToken cancellation) =>
			this.GetTransmitter(notification)?.TransmitAsync(notification.Destination, notification.Template, notification.Argument, notification.Channel, cancellation) ?? ValueTask.CompletedTask;
		#endregion
	}
}