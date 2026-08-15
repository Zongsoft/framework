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
using Zongsoft.Components;

using StackExchange.Redis;

namespace Zongsoft.Externals.Redis.Messaging;

public class RedisQueue : MessageQueueBase<RedisSubscriber, Configuration.RedisConnectionSettings>
{
	#region 成员字段
	private IDatabase _database;
	private IConnectionMultiplexer _connection;
	#endregion

	#region 构造函数
	public RedisQueue(string name, Configuration.RedisConnectionSettings settings) : base(name, settings)
	{
		if(settings == null)
			throw new ArgumentNullException(nameof(settings));

		_connection = ConnectionMultiplexer.Connect(settings.GetOptions());
		_database = _connection.GetDatabase();
	}

	public RedisQueue(string name, IDatabase database, Configuration.RedisConnectionSettings settings = null) : base(name, settings)
	{
		_database = database ?? throw new ArgumentNullException(nameof(database));
	}
	#endregion

	#region 内部属性
	internal IDatabase Database => _database ?? throw new ObjectDisposedException(nameof(RedisQueue));
	internal string GetQueueName(string topic) => this.Settings == null && string.IsNullOrEmpty(topic) ? this.Name : RedisQueueUtility.GetQueueName(this.Name, topic);
	#endregion

	#region 生成方法
	protected override async ValueTask<string> OnProduceAsync(string topic, string tags, ReadOnlyMemory<byte> data, MessageEnqueueOptions options, CancellationToken cancellation)
	{
		if(string.IsNullOrEmpty(topic) && this.Settings != null)
			throw new ArgumentNullException(nameof(topic));

		cancellation.ThrowIfCancellationRequested();
		return await this.Database.StreamAddAsync(this.GetQueueName(topic), RedisQueueUtility.GetMessagePayload(data, tags)).WaitAsync(cancellation);
	}
	#endregion

	#region 订阅方法
	protected override ValueTask<bool> OnSubscribeAsync(RedisSubscriber subscriber, CancellationToken cancellation = default)
	{
		return subscriber.SubscribeAsync(cancellation);
	}

	protected override ValueTask<RedisSubscriber> CreateSubscriberAsync(string topic, string tags, IHandler<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation)
	{
		return ValueTask.FromResult(new RedisSubscriber(this, topic, tags, handler, options));
	}
	#endregion

	#region 重写方法
	protected override string GetTopic(string topic) => this.Settings == null ? topic ?? string.Empty : base.GetTopic(topic);
	#endregion

	#region 资源释放
	protected override void Dispose(bool disposing)
	{
		if(disposing)
		{
			foreach(var subscriber in this.Subscribers)
				subscriber.DisposeAsync().AsTask().GetAwaiter().GetResult();

			Interlocked.Exchange(ref _database, null);
			Interlocked.Exchange(ref _connection, null)?.Dispose();
		}
	}
	#endregion
}
