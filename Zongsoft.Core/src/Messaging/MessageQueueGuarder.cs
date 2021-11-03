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
using System.ComponentModel;

using Zongsoft.Services;

namespace Zongsoft.Messaging
{
	public class MessageQueueGuarder : WorkerBase
	{
		#region 公共属性
		[TypeConverter(typeof(MessageQueueConverter))]
		public IMessageQueue Queue { get; set; }

		public Options.QueueOptionsCollection Options { get; set; }
		#endregion

		#region 启停方法
		protected override void OnStart(string[] args)
		{
			var queue = this.Queue ?? throw new InvalidOperationException($"Missing the required message queue.");
			queue.SubscribeAsync(GetSubscriptionOptions());
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
		private MessageQueueSubscriptionOptions GetSubscriptionOptions()
		{
			var options = this.Options;

			if(options != null && options.TryGet(this.Queue.Name, out var option) && option.Subscription != null)
				return new MessageQueueSubscriptionOptions(option.Subscription.Reliability, option.Subscription.Fallback);

			return null;
		}
		#endregion
	}
}
