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
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;

using Zongsoft.Common;
using Zongsoft.Services;
using Zongsoft.Configuration;
using Zongsoft.Configuration.Options;

namespace Zongsoft.Messaging
{
	public class MessageTopicWatcher : WorkerBase
	{
		#region 公共属性
		[TypeConverter(typeof(MessageTopicConverter))]
		public IMessageTopic Queue { get; set; }

		[Options("/Messaging/Topics")]
		public Options.TopicOptionsCollection Options { get; set; }
		#endregion

		#region 启停方法
		protected override void OnStart(string[] args)
		{
			var queue = this.Queue ?? throw new InvalidOperationException($"Missing the required message queue.");
			var options = this.GetSubscriptionOptions(out var filters);

			foreach(var filter in filters)
			{
				queue.SubscribeAsync(filter.Topic, filter.Tags, options).GetAwaiter().GetResult();
			}

			if(args != null && args.Length > 0)
			{
				for(int i = 0; i < args.Length; i++)
				{
					var filter = Zongsoft.Messaging.Options.TopicSubscriptionFilter.Parse(args[i]);

					if(!string.IsNullOrEmpty(filter.Topic))
						queue.SubscribeAsync(filter.Topic, filter.Tags, options).GetAwaiter().GetResult();
				}
			}
		}

		protected override void OnStop(string[] args)
		{
			var subscribers = this.Queue?.Subscribers;

			if(subscribers != null)
			{
				foreach(var subscriber in subscribers)
					subscriber.UnsubscribeAsync().GetAwaiter().GetResult();
			}
		}
		#endregion

		#region 私有方法
		private MessageTopicSubscriptionOptions GetSubscriptionOptions(out IEnumerable<Options.TopicSubscriptionFilter> filters)
		{
			var options = this.Options;

			if(options != null && options.TryGet(this.Queue.Name, out var option) && option.Subscription != null)
			{
				filters = option.Subscription.Filters;
				return new MessageTopicSubscriptionOptions(option.Subscription.Reliability, option.Subscription.Fallback);
			}

			filters = Array.Empty<Options.TopicSubscriptionFilter>();
			return null;
		}
		#endregion
	}
}
