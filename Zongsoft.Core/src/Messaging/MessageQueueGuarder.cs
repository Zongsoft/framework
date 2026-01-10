/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.com>
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
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.Generic;

using Zongsoft.Services;
using Zongsoft.Components;
using Zongsoft.Configuration;

namespace Zongsoft.Messaging;

public class MessageQueueGuarder : WorkerBase
{
	#region 私有变量
	private IMessageConsumer[] _subscribers;
	#endregion

	#region 构造函数
	public MessageQueueGuarder() { }
	public MessageQueueGuarder(string name) : base(name) { }
	#endregion

	#region 公共属性
	[TypeConverter(typeof(MessageQueueConverter))]
	public IMessageQueue Queue { get; set; }
	public IHandler<Message> Handler { get; set; }
	public Options.QueueOptionsCollection Options { get; set; }
	#endregion

	#region 启停方法
	protected async override Task OnStartAsync(string[] args, CancellationToken cancellation)
	{
		var queue = this.Queue;
		if(queue == null)
		{
			Zongsoft.Diagnostics.Logging.GetLogging(this).Warn($"The message queue guarder named '{this.Name}' cannot be started because its '{nameof(this.Queue)}' property is null.");
			return;
		}

		var options = this.GetSubscriptionOptions(out var filters);
		var consumers = new List<IMessageConsumer>();

		foreach(var filter in filters)
		{
			var consumer = await queue.SubscribeAsync(filter.Topic, filter.Tags, this.Handler, options, cancellation);

			if(consumer != null)
				consumers.Add(consumer);
		}

		if(args != null && args.Length > 0)
		{
			for(int i = 0; i < args.Length; i++)
			{
				var filter = Zongsoft.Messaging.Options.QueueSubscriptionFilter.Parse(args[i]);

				if(!string.IsNullOrEmpty(filter.Topic))
				{
					var consumer = await queue.SubscribeAsync(filter.Topic, filter.Tags, this.Handler, options, cancellation);

					if(consumer != null)
						consumers.Add(consumer);
				}
			}
		}

		_subscribers = consumers.ToArray();
	}

	protected async override Task OnStopAsync(string[] args, CancellationToken cancellation)
	{
		var subscribers = Interlocked.Exchange(ref _subscribers, null);

		if(subscribers != null)
		{
			for(int i = 0; i < subscribers.Length; i++)
				await subscribers[i].UnsubscribeAsync(cancellation);
		}
	}
	#endregion

	#region 私有方法
	private MessageSubscribeOptions GetSubscriptionOptions(out IEnumerable<Options.QueueSubscriptionFilter> filters)
	{
		var options = this.Options ?? ApplicationContext.Current?.Configuration?.GetOption<Options.QueueOptionsCollection>("Messaging/Queues");

		if(options != null && options.TryGetValue(this.Queue.Name, out var option) && option.Subscription != null)
		{
			filters = option.Subscription.Filters ?? Array.Empty<Options.QueueSubscriptionFilter>();
			return new MessageSubscribeOptions(option.Subscription.Reliability, option.Subscription.Fallback);
		}

		filters = [];
		return null;
	}
	#endregion
}
