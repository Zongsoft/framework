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
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Common;
using Zongsoft.Messaging;

using StackExchange.Redis;

namespace Zongsoft.Externals.Redis.Messaging
{
	internal class RedisSubscriber : MessageConsumerBase
	{
		#region 私有字段
		private Poller _poller;
		private RedisQueue _queue;
		private Task<StreamEntry[]>[] _tasks;
		private string _lastMessageId;
		private DateTime _lastClaimTime;
		private TimeSpan _idleTimeout;
		private bool _pendingAcquired;
		private readonly string _client;
		private readonly string _group;
		#endregion

		#region 构造函数
		public RedisSubscriber(RedisQueue queue, string topics, IMessageHandler handler, MessageSubscribeOptions options = null) : base(topics, null, options, handler)
		{
			_queue = queue ?? throw new ArgumentNullException(nameof(queue));
			_group = queue.ConnectionSetting.Values.Group;
			_client = string.IsNullOrWhiteSpace(queue.ConnectionSetting.Values.Client) ? "C" + Randomizer.GenerateString() : queue.ConnectionSetting.Values.Client;
			_poller = new Poller(this);

			//设置未应答消息的超时时长，默认值为30秒
			this.IdleTimeout = queue.ConnectionSetting.Values.GetValue(nameof(IdleTimeout), TimeSpan.FromSeconds(30));
		}
		#endregion

		#region 公共属性
		/// <summary>获取或设置未应答消息的超时时长，默认为<c>30</c>秒。</summary>
		public TimeSpan IdleTimeout
		{
			get => _idleTimeout;
			set => _idleTimeout = value > TimeSpan.Zero ? value : throw new ArgumentOutOfRangeException();
		}
		#endregion

		#region 重写方法
		protected override async ValueTask OnSubscribeAsync(IEnumerable<string> topics, string tags, MessageSubscribeOptions options, CancellationToken cancellation)
		{
			cancellation.ThrowIfCancellationRequested();

			if(!string.IsNullOrEmpty(_group))
			{
				var database = _queue.Database;

				foreach(var topic in topics)
				{
					var queueKey = GetRedisQueueName(topic);

					//如果指定的队列不存在或指定的消费组不存在则创建它
					if(!await database.KeyExistsAsync(queueKey) || (await database.StreamGroupInfoAsync(queueKey)).All(x => x.Name != _group))
						await database.StreamCreateConsumerGroupAsync(queueKey, _group, "0", true);
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

			try
			{
				//获取已完成的任务结果
				var result = this.GetReceiveResult(_tasks[index], topics[index]);

				if(string.IsNullOrEmpty(_group))
				{
					//如果是无分组(即全局广播)接受模式则更新最后接收到的消息编号
					if(!result.IsEmpty)
						_lastMessageId = result.Identifier;
				}
				else if(result.IsEmpty) //如果是分组接受模式并且队列组已被消费完
				{
					//如果距离上次转移的时长已达到阈值
					if(DateTime.Now - _lastClaimTime >= _idleTimeout)
					{
						//将超时未应答的消息转移给当前消费者
						_queue.Database.StreamAutoClaimIdsOnly(GetRedisQueueName(topics[index]), _group, _client, (long)_idleTimeout.TotalMilliseconds, "0", int.MaxValue, CommandFlags.FireAndForget);

						//更新最后转移时间
						_lastClaimTime = DateTime.Now;
					}

					//翻转从未应答列表中获取数据的标记
					_pendingAcquired = !_pendingAcquired;
				}

				//返回已完成的任务结果
				return result;
			}
			finally
			{
				//更新已完成的任务槽位
				_tasks[index] = GetReceiveTask(topics[index]);
			}
		}
		#endregion

		#region 私有方法
		private Task<StreamEntry[]> GetReceiveTask(string topic)
		{
			var database = _queue.Database;
			var queueKey = GetRedisQueueName(topic);

			if(string.IsNullOrEmpty(_group))
				return string.IsNullOrEmpty(_lastMessageId) ?
					database.StreamRangeAsync(queueKey, "-", "+", 1, Order.Ascending) :
					database.StreamReadAsync(queueKey, _lastMessageId, 1);

			//判断是否为处理未应答消息
			if(_pendingAcquired)
			{
				//获取当前消费者超时未应答的消息
				var pendings = database.GetPendingMessages(queueKey, _group, _client, _idleTimeout, 1);

				if(pendings != null && pendings.Length > 0)
					return database.StreamRangeAsync(queueKey, pendings[0].MessageId, pendings[0].MessageId, 1);

				return Task.FromResult(Array.Empty<StreamEntry>());
			}

			//返回最新的未投递消息
			return database.StreamReadGroupAsync(queueKey, _group, _client, ">", 1);
		}

		private Message GetReceiveResult(Task<StreamEntry[]> task, string topic)
		{
			//如果任务超时则返回空消息
			if(task.Status == TaskStatus.Faulted && AggregateExceptionUtility.Handle<RedisTimeoutException>(task.Exception, ex => Message.Empty) is Message message)
				return message;

			//获取任务的结果
			var result = task.IsCompletedSuccessfully ? task.Result : task.GetAwaiter().GetResult();

			//如果任务结果为空则返回空消息
			if(result == null || result.Length == 0)
				return Message.Empty;

			//构建接收到的消息
			return new Message(result[0].Id, topic, result[0].Values[0].Value, Acknowledge)
			{
				Timestamp = DateTime.UtcNow,
			};

			ValueTask Acknowledge(CancellationToken cancellation)
			{
				return new ValueTask(_queue.Database.StreamAcknowledgeAsync(GetRedisQueueName(topic), _group, result[0].Id));
			}
		}
		#endregion

		#region 私有方法
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private string GetRedisQueueName(string topic) => string.IsNullOrEmpty(topic) ? _queue.Name : $"{_queue.Name}:{topic}";
		#endregion

		#region 处置方法
		protected override void Dispose(bool disposing)
		{
			var poller = Interlocked.Exchange(ref _poller, null);
			if(poller != null)
				poller.Dispose();

			base.Dispose(disposing);
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
				try
				{
					return _subscriber?.Receive(options, cancellation) ?? Message.Empty;
				}
				catch(OperationCanceledException)
				{
					return Message.Empty;
				}
				catch
				{
					throw;
				}
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