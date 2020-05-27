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
 * This file is part of Zongsoft.Externals.Aliyun library.
 *
 * The Zongsoft.Externals.Aliyun is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Aliyun is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Aliyun library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Net.Http;

using Zongsoft.Messaging;

namespace Zongsoft.Externals.Aliyun.Messaging
{
	public class TopicSubscription : Zongsoft.Messaging.ITopicSubscription
	{
		#region 成员字段
		private Topic _topic;
		#endregion

		#region 构造函数
		public TopicSubscription(Topic topic)
		{
			if(topic == null)
				throw new ArgumentNullException(nameof(topic));

			_topic = topic;
		}
		#endregion

		#region 公共属性
		public Topic Topic
		{
			get
			{
				return _topic;
			}
		}
		#endregion

		#region 公共方法
		public Zongsoft.Messaging.TopicSubscription Get(string name)
		{
			throw new NotImplementedException();
		}

		public bool Subscribe(string name, string url, object state = null)
		{
			throw new NotImplementedException();
		}

		public bool Subscribe(string name, string url, TopicSubscriptionFallbackBehavior behavior, object state = null)
		{
			throw new NotImplementedException();
		}

		public bool Subscribe(string name, string url, string tags, object state = null)
		{
			throw new NotImplementedException();
		}

		public bool Subscribe(string name, string url, string tags, TopicSubscriptionFallbackBehavior behavior, object state = null)
		{
			throw new NotImplementedException();
		}

		public bool Unsubscribe(string name)
		{
			throw new NotImplementedException();
		}
		#endregion
	}
}
