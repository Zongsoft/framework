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

using Zongsoft.Messaging;
using Zongsoft.Components;

using StackExchange.Redis;

namespace Zongsoft.Externals.Redis.Messaging;

public class RedisQueue : MessageQueueBase<RedisSubscriber, Configuration.RedisConnectionSettings>, IAsyncDisposable
{
	#region 常量定义
	private const int DEFAULT_MAXIMUM_LENGTH = 100000;
	#endregion

	#region 成员字段
	private IDatabase _database;
	private IConnectionMultiplexer _connection;
	private RedisConnectionLease _connectionLease;
	private TaskCompletionSource<bool> _disposal;
	#endregion

	#region 构造函数
	public RedisQueue(string name, Configuration.RedisConnectionSettings settings) : base(name, settings)
	{
		if(settings == null)
			throw new ArgumentNullException(nameof(settings));

		_connectionLease = RedisConnectionPool.Acquire(settings.GetOptions());
		_connection = _connectionLease.Connection;
		_database = _connection.GetDatabase();
		this.Capabilities = GetCapabilities(_connection);
		this.MaximumLength = settings.MaximumLength == 0 ? DEFAULT_MAXIMUM_LENGTH : settings.MaximumLength;
		this.UseApproximateMaximumLength = settings.UseApproximateMaximumLength;
		this.Features.Add(MessageQueueFeature.Compression);
	}

	public RedisQueue(string name, IDatabase database, Configuration.RedisConnectionSettings settings = null) : base(name, settings)
	{
		_database = database ?? throw new ArgumentNullException(nameof(database));
		this.Capabilities = GetCapabilities(database.Multiplexer);
		this.MaximumLength = settings == null || settings.MaximumLength == 0 ? DEFAULT_MAXIMUM_LENGTH : settings.MaximumLength;
		this.UseApproximateMaximumLength = settings?.UseApproximateMaximumLength ?? true;
		this.Features.Add(MessageQueueFeature.Compression);
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置消息流保留的最大消息数，非正数表示不进行裁剪。</summary>
	public int MaximumLength { get; set; }
	/// <summary>获取或设置是否使用近似裁剪消息流。</summary>
	public bool UseApproximateMaximumLength { get; set; }
	/// <summary>获取该队列当前可用的 Redis 服务端能力。</summary>
	public RedisCapabilities Capabilities { get; }
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
		var payload = data.ToArray();
		var compression = options?.Compression ?? default;
		var compressor = default(string);
		if(compression.CanCompress(payload.Length))
		{
			payload = compression.Compress(payload);
			compressor = compression.Name;
		}

		using var activity = RedisDiagnostics.ActivitySource.StartActivity("redis.queue.produce", System.Diagnostics.ActivityKind.Producer);
		return await this.Database.StreamAddAsync(
			this.GetQueueName(topic),
			RedisQueueUtility.GetMessagePayload(payload, tags, compressor),
			maxLength: this.MaximumLength > 0 ? this.MaximumLength : null,
			useApproximateMaxLength: this.UseApproximateMaximumLength).WaitAsync(cancellation);
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
	protected override MessageReliability Reliability => MessageReliability.LeastOnce;
	protected override string GetTopic(string topic) => this.Settings == null ? topic ?? string.Empty : base.GetTopic(topic);
	#endregion

	#region 资源释放
	public async ValueTask DisposeAsync()
	{
		await this.DisposeAsyncCore();
		base.Dispose();
		GC.SuppressFinalize(this);
	}

	protected override void Dispose(bool disposing)
	{
		if(disposing)
			this.DisposeAsyncCore().GetAwaiter().GetResult();
	}

	private Task DisposeAsyncCore()
	{
		var disposal = Volatile.Read(ref _disposal);
		if(disposal == null)
		{
			var candidate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			disposal = Interlocked.CompareExchange(ref _disposal, candidate, null) ?? candidate;
			if(ReferenceEquals(disposal, candidate))
				_ = DisposeAsyncCore(candidate);
		}

		return disposal.Task;
	}

	private async Task DisposeAsyncCore(TaskCompletionSource<bool> completion)
	{
		try
		{
			foreach(var subscriber in this.Subscribers)
			{
				try
				{
					await subscriber.DisposeAsync();
				}
				catch(Exception exception)
				{
					Zongsoft.Diagnostics.Logging.GetLogging(typeof(RedisQueue)).Error(exception);
				}
			}

			Interlocked.Exchange(ref _database, null);
			Interlocked.Exchange(ref _connection, null);
			var lease = Interlocked.Exchange(ref _connectionLease, null);
			if(lease != null)
				await lease.DisposeAsync();

			completion.TrySetResult(true);
		}
		catch(Exception exception)
		{
			completion.TrySetException(exception);
		}
	}
	#endregion

	#region 私有方法
	private static RedisCapabilities GetCapabilities(IConnectionMultiplexer connection)
	{
		var found = false;
		var result = (RedisCapabilities)(-1);

		foreach(var endpoint in connection.GetEndPoints())
		{
			var server = connection.GetServer(endpoint);
			if(server.IsReplica)
				continue;

			result &= RedisCapabilityMatrix.GetCapabilities(server.Version);
			found = true;
		}

		return found ? result : RedisCapabilities.None;
	}
	#endregion
}
