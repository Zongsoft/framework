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
 * This file is part of Zongsoft.Externals.Redis library.
 *
 * The Zongsoft.Externals.Redis is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Redis is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Redis library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Messaging;
using Zongsoft.Configuration;

using StackExchange.Redis;

namespace Zongsoft.Externals.Redis.Messaging
{
	public class RedisQueue : MessageQueueBase
	{
		#region 成员字段
		private IDatabase _database;
		#endregion

		#region 构造函数
		public RedisQueue(string name, IDatabase database, IConnectionSetting settings = null) : base(name, settings)
		{
			_database = database ?? throw new ArgumentNullException(nameof(database));
		}
		#endregion

		#region 内部属性
		internal IDatabase Database => _database ?? throw new ObjectDisposedException(nameof(RedisQueue));
		#endregion

		#region 生成方法
		public override async ValueTask<string> ProduceAsync(string topic, string tags, ReadOnlyMemory<byte> data, MessageEnqueueOptions options = null, CancellationToken cancellation = default)
		{
			if(string.IsNullOrEmpty(topic))
				throw new ArgumentNullException(nameof(topic));

			return await _database.StreamAddAsync(RedisQueueUtility.GetQueueName(this.Name, topic), RedisQueueUtility.GetMessagePayload(data, tags));
		}
		#endregion

		#region 订阅方法
		public override async ValueTask<IMessageConsumer> SubscribeAsync(string topics, string tags, IMessageHandler handler, MessageSubscribeOptions options, CancellationToken cancellation = default)
		{
			var subscriber = new RedisSubscriber(this, topics, handler, options);
			await subscriber.SubscribeAsync(cancellation);
			return subscriber;
		}
		#endregion

		#region 资源释放
		protected override void Dispose(bool disposing)
		{
			Interlocked.Exchange(ref _database, null);
		}
		#endregion
	}
}