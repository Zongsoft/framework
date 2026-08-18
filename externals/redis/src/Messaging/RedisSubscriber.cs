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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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

using Zongsoft.Common;
using Zongsoft.Messaging;
using Zongsoft.Components;
using Zongsoft.Configuration;

using StackExchange.Redis;

namespace Zongsoft.Externals.Redis.Messaging;

/// <summary>
/// 表示Redis消息队列的消费者。
/// </summary>
/// <remarks>
///		<para>参考资料：</para>
///		<list type="bullet">
///			<term>中文：http://www.redis.cn/topics/streams-intro.html"</term>
///			<term>英文：https://redis.io/docs/data-types/streams-tutorial"</term>
///		</list>
/// </remarks>
public class RedisSubscriber : MessageConsumerBase<RedisQueue>
{
	#region 常量定义
	private const long TICKS_PERSECOND = 10000000;
	private const long TICKS_PERHOUR   = TICKS_PERSECOND * 60 * 60;
	private const int CLAIM_BATCH_SIZE = 100;
	private const string DEAD_SUFFIX = ":DEAD!";
	private const string DEAD_SCRIPT = "local entries=redis.call('XRANGE',KEYS[1],ARGV[2],ARGV[2],'COUNT',1); if #entries==0 then return false end; local id=redis.call('XADD',KEYS[2],'MAXLEN','~',ARGV[3],'*',unpack(entries[1][2])); if id then redis.call('XACK',KEYS[1],ARGV[1],ARGV[2]) end; return id";
	#endregion

	#region 私有字段
	private Poller _poller;
	private string _lastMessageId;
	private DateTime _lastClaimTime;
	private TimeSpan _idleTimeout;
	private int _deadline;
	private bool _pendingAcquired;
	private string _pendingMessageId;
	private readonly string _client;
	private readonly string _group;
	#endregion

	#region 构造函数
	public RedisSubscriber(RedisQueue queue, string topic, IHandler<Message> handler, MessageSubscribeOptions options = null) : this(queue, topic, null, handler, options) { }
	public RedisSubscriber(RedisQueue queue, string topic, string tags, IHandler<Message> handler, MessageSubscribeOptions options = null) : base(queue, topic, tags, handler, options)
	{
		_group = queue.Settings?.Group;
		_client = string.IsNullOrWhiteSpace(queue.Settings?.Client) ? "C" + Randomizer.GenerateString() : queue.Settings.Client;
		_poller = new Poller(this);

		//初始化属性值
		this.Deadline = queue.Settings?.Deadline ?? 10000;
		this.IdleTimeout = queue.Settings?.IdleTimeout ?? TimeSpan.FromSeconds(30);
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置未应答消息的超时时长，默认为<c>30</c>秒。</summary>
	public TimeSpan IdleTimeout
	{
		get => _idleTimeout;
		set => _idleTimeout = value > TimeSpan.Zero ? value : throw new ArgumentOutOfRangeException();
	}

	/// <summary>获取或设置未应答消息转为死信的阈值，如果为零则表示不开启死信功能。默认为<c>10000</c>。</summary>
	public int Deadline
	{
		get => _deadline;
		set => _deadline = Math.Max(value, 0);
	}
	#endregion

	#region 重写方法
	protected override ValueTask OnCloseAsync(CancellationToken cancellation)
	{
		_poller?.Stop();
		return ValueTask.CompletedTask;
	}
	#endregion

	#region 内部方法
	internal async ValueTask<bool> SubscribeAsync(CancellationToken cancellation)
	{
		cancellation.ThrowIfCancellationRequested();

		if(!string.IsNullOrEmpty(_group))
		{
			try
			{
				await this.Queue.Database
					.StreamCreateConsumerGroupAsync(this.Queue.GetQueueName(this.Topic), _group, "$", true)
					.WaitAsync(cancellation);
			}
			catch(RedisServerException exception) when(exception.Message.StartsWith("BUSYGROUP", StringComparison.OrdinalIgnoreCase))
			{
				//消费组已由其他订阅者创建，直接加入该组。
			}
		}

		_poller.Start();
		return true;
	}

	internal Message Receive(MessageDequeueOptions options, CancellationToken cancellation)
	{
		cancellation.ThrowIfCancellationRequested();
		options ??= MessageDequeueOptions.Default;

		//获取已完成的任务结果
		var result = this.GetReceiveResult(this.GetReceiveTask(options), cancellation);

		if(string.IsNullOrEmpty(_group))
		{
			//如果是无分组(即全局广播)接受模式则更新最后接收到的消息编号
			if(!result.IsEmpty)
				_lastMessageId = result.Identifier;
		}
		else if(result.IsEmpty) //如果是分组接受模式并且队列组已被消费完
		{
			//如果距离上次转移的时长已达到阈值
			//注意：由于XAutoClaim指令会重置未应答记录的空闲时长，因此不能每次IdleTimeout都调用，必须以更长(譬如每小时)间隔进行调用。
			if((DateTime.UtcNow - _lastClaimTime).Ticks >= Math.Max(_idleTimeout.Ticks, TICKS_PERHOUR))
			{
				//将超时未应答的消息转移给当前消费者
				this.Queue.Database.StreamAutoClaimIdsOnly(
					this.Queue.GetQueueName(this.Topic),
					_group,
					_client,
					(long)_idleTimeout.TotalMilliseconds,
					"0",
					CLAIM_BATCH_SIZE);

				//更新最后转移时间
				_lastClaimTime = DateTime.UtcNow;
			}

			//翻转从未应答列表中获取数据的标记
			_pendingAcquired = !_pendingAcquired;
		}

		//返回已完成的任务结果
		return result;
	}
	#endregion

	#region 私有方法
	private Task<StreamEntry[]> GetReceiveTask(MessageDequeueOptions options)
	{
		var database = this.Queue.Database;
		var queueKey = this.Queue.GetQueueName(this.Topic);

		if(string.IsNullOrEmpty(_group))
			return string.IsNullOrEmpty(_lastMessageId) ?
				database.StreamRangeAsync(queueKey, "-", "+", 1, Order.Ascending) :
				database.StreamReadAsync(queueKey, _lastMessageId, 1);

		//判断是否为处理未应答消息
		if(_pendingAcquired)
		{
			//获取当前消费者超时未应答的消息
			var pendings = database.GetPendingMessages(
				queueKey,
				_group,
				_client,
				_idleTimeout,
				1,
				RedisQueueUtility.IncreaseId(_pendingMessageId));

			//如果没有超时未应答的消息则返回空任务
			if(pendings == null || pendings.Length == 0)
			{
				_pendingMessageId = null;
				return Task.FromResult(Array.Empty<StreamEntry>());
			}

			_pendingMessageId = pendings[0].MessageId;

			//如果启用死信队列特性，且超时未应答的消息投递次数已达到阈值则转为死信
			if(_deadline > 0 && pendings[0].DeliveryCount >= _deadline)
			{
				var deadId = this.Dead(database, queueKey, pendings[0].MessageId);

				//如果死信队列投递成功则返回空任务
				if(!string.IsNullOrEmpty(deadId))
					return Task.FromResult(Array.Empty<StreamEntry>());
			}

			//返回当前超时未应答的消息
			//注意：因为XReadGroup指令是获取大于指定编号的消息，因此必须对当前超时未应答的消息编号递减一个数值
			return database.StreamReadGroupAsync(queueKey, _group, _client, RedisQueueUtility.DecreaseId(_pendingMessageId), 1);
		}

		//返回最新的未投递消息
		return database.StreamReadGroupAsync(queueKey, _group, _client, ">", 1, false, options.Timeout > TimeSpan.Zero ? options.Timeout : null);
	}

	private Message GetReceiveResult(Task<StreamEntry[]> task, CancellationToken cancellation)
	{
		StreamEntry[] result;

		try
		{
			result = task.IsCompletedSuccessfully ? task.Result : task.WaitAsync(cancellation).GetAwaiter().GetResult();
		}
		catch(RedisTimeoutException)
		{
			return Message.Empty;
		}

		//如果任务结果为空则返回空消息
		if(result == null || result.Length == 0)
			return Message.Empty;

		//构建接收到的消息
		var entry = result[0];
		var message = string.IsNullOrEmpty(_group) ?
			new Message(entry.Id, this.Topic, entry.GetMessageData()) { Tags = entry.GetMessageTags() } :
			new Message(entry.Id, this.Topic, entry.GetMessageData(), entry.GetMessageTags(), Acknowledge);

		message.Timestamp = GetTimestamp(entry.Id);
		return message;

		async ValueTask Acknowledge(CancellationToken cancellation)
		{
			cancellation.ThrowIfCancellationRequested();
			await this.Queue.Database.StreamAcknowledgeAsync(this.Queue.GetQueueName(this.Topic), _group, entry.Id).WaitAsync(cancellation);
		}

		static DateTime GetTimestamp(RedisValue identifier)
		{
			var text = (string)identifier;
			var index = text?.IndexOf('-') ?? -1;

			if(index > 0 && long.TryParse(text.AsSpan(0, index), out var milliseconds))
			{
				try
				{
					return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds).UtcDateTime;
				}
				catch(ArgumentOutOfRangeException)
				{
				}
			}

			return DateTime.UtcNow;
		}
	}

	private string Dead(IDatabase database, string key, string id)
	{
		//如果指定队列就是死信队列则返回空
		if(key.EndsWith(DEAD_SUFFIX, StringComparison.Ordinal))
			return null;

		var result = database.ScriptEvaluate(
			DEAD_SCRIPT,
			[(RedisKey)key, GetDeadQueueKey(key)],
			[_group, id, this.Queue.MaximumLength > 0 ? this.Queue.MaximumLength : 100000]);

		return result.IsNull ? null : (string)result;

		static RedisKey GetDeadQueueKey(string key)
		{
			var opening = key.IndexOf('{');
			var closing = opening < 0 ? -1 : key.IndexOf('}', opening + 1);

			return opening >= 0 && closing > opening + 1 ? key + DEAD_SUFFIX : $"{{{key}}}{DEAD_SUFFIX}";
		}
	}
	#endregion

	#region 处置方法
	protected override ValueTask DisposeAsync(bool disposing)
	{
		var poller = Interlocked.Exchange(ref _poller, null);
		poller?.Dispose();

		return base.DisposeAsync(disposing);
	}
	#endregion

	#region 嵌套子类
	private class Poller(RedisSubscriber subscriber) : MessagePollerBase
	{
		private RedisSubscriber _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));

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
