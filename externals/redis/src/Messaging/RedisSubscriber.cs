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
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Common;
using Zongsoft.Caching;
using Zongsoft.Services;
using Zongsoft.Messaging;

using StackExchange.Redis;

namespace Zongsoft.Externals.Redis.Messaging
{
	internal class RedisSubscriber : MessageConsumerBase
	{
		#region 私有字段
		private Poller _poller;
		private RedisQueue _queue;
		private IDatabaseAsync _database;
		private Task<StreamEntry[]>[] _tasks;
		private readonly string _subscriberId;
		private readonly string _group;
		#endregion

		#region 构造函数
		public RedisSubscriber(RedisQueue queue, string topics, IMessageHandler handler, MessageSubscribeOptions options = null) : base(topics, null, options, handler)
		{
			if(queue == null)
				throw new ArgumentNullException(nameof(queue));

			_poller = new Poller(this);

			if(queue.ConnectionSetting == null || string.IsNullOrEmpty(queue.ConnectionSetting.Values.Client))
			{
				_group = null;
				_subscriberId = Randomizer.GenerateString();
			}
			else
			{
				_group = queue.ConnectionSetting.Values.Group;
				_subscriberId = queue.ConnectionSetting.Values.Client;
			}
		}
		#endregion

		#region 重写方法
		protected override async ValueTask OnSubscribeAsync(IEnumerable<string> topics, string tags, MessageSubscribeOptions options, CancellationToken cancellation)
		{
			if(topics != null && topics.Any())
			{
				if(topics.Count() > 1 || topics.First() != "*")
					throw new NotSupportedException($"This message queue does not support unsubscribing to specified topics.");
			}

			cancellation.ThrowIfCancellationRequested();

			//确保指定的队列分组是存在的，如果不存在则创建它
			if(!string.IsNullOrEmpty(_group))
			{
				foreach(var topic in topics)
				{
					var key = string.IsNullOrEmpty(topic) ? _queue.Name : $"{_queue.Name}:{topic}";

					if(!await _database.KeyExistsAsync(key) || (await _database.StreamGroupInfoAsync(key)).All(x => x.Name != _group))
					{
						await _database.StreamCreateConsumerGroupAsync(key, _group, "0-0", true);
					}
				}
			}
		}

		protected override ValueTask OnUnsubscribeAsync(IEnumerable<string> topics, CancellationToken cancellation)
		{
			return ValueTask.CompletedTask;
		}

		protected override void OnSubscribed()
		{
			_poller?.Start();
		}

		protected override void OnUnsubscribed()
		{
			_poller?.Stop();
		}
		#endregion

		#region 内部方法
		internal ValueTask SubscribeAsync(CancellationToken cancellation) => base.SubscribeAsync(this.Topics, cancellation);
		internal Message Receive(MessageDequeueOptions options, CancellationToken cancellation)
		{
			if(_database == null)
				throw new InvalidOperationException($"The message queue topic to consume is not yet subscribed.");

			if(cancellation.IsCancellationRequested)
				return Message.Empty;

			if(options.Timeout > TimeSpan.Zero)
				cancellation = CancellationTokenSource.CreateLinkedTokenSource(new CancellationTokenSource(options.Timeout).Token, cancellation).Token;

			var tasks = _tasks;
			var topics = this.Topics;

			if(tasks == null || tasks.Length == 0)
			{
				if(topics == null || topics.Length == 0)
					_tasks = new Task<StreamEntry[]>[] { GetReceiveTask(null) };
				else
					_tasks = topics.Select(GetReceiveTask).ToArray();

				tasks = _tasks;
			}

			//等待任务集中最先执行完成的任务
			var index = Task.WaitAny(tasks, cancellation);
			if(index < 0)
				return Message.Empty;

			//获取已完成的任务结果
			var result = this.GetReceiveResult(tasks[index], topics[index]);

			//更新已完成的任务槽位
			_tasks[index] = GetReceiveTask(topics[index]);

			//返回已完成的任务结果
			return result;
		}
		#endregion

		#region 私有方法
		private Task<StreamEntry[]> GetReceiveTask(string topic)
		{
			var key = string.IsNullOrEmpty(topic) ? _queue.Name : $"{_queue.Name}:{topic}";

			//同步方式从消息队列中拉取消息(堵塞当前线程)
			return string.IsNullOrEmpty(_group) ?
				_database.StreamReadAsync(key, StreamPosition.NewMessages) :
				_database.StreamReadGroupAsync(key, _group, _subscriberId);
		}

		private Message GetReceiveResult(Task<StreamEntry[]> task, string topic)
		{
			var result = task.IsCompletedSuccessfully ? task.Result : task.GetAwaiter().GetResult();

			if(result == null || result.Length == 0)
				return Message.Empty;

			//构建接收到的消息
			return new Message(result[0].Id, topic, result[0].Values[0].Value, cancellation => new ValueTask(_database.StreamAcknowledgeAsync(_queue.Name, _group, result[0].Id)))
			{
				Timestamp = DateTime.UtcNow,
			};
		}
		#endregion

		#region 处置方法
		protected override void Dispose(bool disposing)
		{
			var poller = Interlocked.Exchange(ref _poller, null);
			if(poller != null)
				poller.Dispose();

			base.Dispose(disposing);

			Interlocked.Exchange(ref _database, null);
		}
		#endregion

		#region 嵌套子类
		private class Poller : MessagePollerBase
		{
			private RedisSubscriber _subscriber;

			public Poller(RedisSubscriber subscriber)
			{
				_subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
			}

			protected override Message Receive(MessageDequeueOptions options, CancellationToken cancellation)
			{
				return _subscriber?.Receive(options, cancellation) ?? Message.Empty;
			}

			protected override ValueTask OnHandleAsync(Message message, CancellationToken cancellation)
			{
				return _subscriber?.Handler?.HandleAsync(message, cancellation) ?? ValueTask.CompletedTask;
			}

			protected override void Dispose(bool disposing)
			{
				base.Dispose(disposing);
				_subscriber = null;
			}
		}
		#endregion
	}
}