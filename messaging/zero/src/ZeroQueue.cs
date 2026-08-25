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
 * This file is part of Zongsoft.Messaging.ZeroMQ library.
 *
 * The Zongsoft.Messaging.ZeroMQ is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Messaging.ZeroMQ is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Messaging.ZeroMQ library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Common;
using Zongsoft.Components;

namespace Zongsoft.Messaging.ZeroMQ;

public sealed partial class ZeroQueue : MessageQueueBase<ZeroSubscriber, Configuration.ZeroConnectionSettings>
{
	#region 成员字段
	private readonly object _locker = new();
	private Transport _transport;
	private readonly ZeroQueueRuntimeOptions _options;
	private readonly HashSet<string> _exclusion;
	private readonly HashSet<string> _inclusion;
	private Task _initialization;
	#endregion

	#region 构造函数
	public ZeroQueue(string name, Configuration.ZeroConnectionSettings settings) : base(name, settings)
	{
		if(settings == null)
			throw new ArgumentNullException(nameof(settings));

		if(string.IsNullOrWhiteSpace(settings.Server))
			throw new ArgumentException(Properties.Resources.ZeroQueue_ServerRequired_Message, nameof(settings));

		_options = new ZeroQueueRuntimeOptions(
			settings.Server,
			settings.Port == 0 ? ZeroQueueServer.PORT : settings.Port,
			settings.Timeout > TimeSpan.Zero ? settings.Timeout : TimeSpan.FromSeconds(10),
			settings.Heartbeat,
			settings.ReconnectInterval > TimeSpan.Zero ? settings.ReconnectInterval : TimeSpan.FromSeconds(1),
			settings.Client,
			settings.Instance,
			settings.Group,
			settings.Topic);

		this.Instance = GenerateIdentifier(settings);
		(_inclusion, _exclusion) = CreateFilter(settings.Filter, this.Instance);
	}
	#endregion

	#region 公共属性
	public string Instance { get; }
	#endregion

	#region 订阅方法
	protected override ValueTask<ZeroSubscriber> CreateSubscriberAsync(string topic, string tags, IHandler<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation) =>
		ValueTask.FromResult(new ZeroSubscriber(this, topic, handler, options));

	protected override async ValueTask<bool> OnSubscribeAsync(ZeroSubscriber subscriber, CancellationToken cancellation = default)
	{
		await this.EnsureInitializedAsync(cancellation);
		if(subscriber.Options?.Reliability == MessageReliability.LeastOnce && !_transport.HasControl)
			throw new InvalidOperationException(Properties.Resources.ZeroQueue_ControlUnavailable_Message);
		await _transport.SubscribeAsync(subscriber, this.GetPhysicalTopic(subscriber.Topic), cancellation);
		if(subscriber.Options?.Reliability != MessageReliability.LeastOnce)
			await subscriber.SynchronizeAsync(_options.Timeout, cancellation);
		return true;
	}

	protected override void OnUnsubscribed(ZeroSubscriber subscriber) { }
	#endregion

	#region 发布方法
	protected override async ValueTask<string> OnProduceAsync(string topic, string tags, ReadOnlyMemory<byte> data, MessageEnqueueOptions options, CancellationToken cancellation)
	{
		var payload = data.ToArray();
		var threshold = options?.Compression > 0 ? options.Compression : 0;
		var identifier = Guid.NewGuid().ToString("N");
		var reliability = options?.Reliability ?? MessageReliability.MostOnce;
		await this.EnsureInitializedAsync(cancellation);
		if(reliability == MessageReliability.LeastOnce)
		{
			if(!_transport.HasControl)
				throw new InvalidOperationException(Properties.Resources.ZeroQueue_ControlUnavailable_Message);
			return await _transport.PublishAsync(identifier, this.GetPhysicalTopic(topic), this.Instance, tags, payload, threshold, options.Expiration, reliability, cancellation);
		}

		var published = false;
		if(string.IsNullOrEmpty(topic))
		{
			foreach(var subscriber in this.Subscribers)
				published |= await _transport.PublishAsync(identifier, this.GetPhysicalTopic(subscriber.Topic), this.Instance, tags, payload, threshold, TimeSpan.Zero, reliability, cancellation) != null;
		}
		else
		{
			published = await _transport.PublishAsync(identifier, this.GetPhysicalTopic(topic), this.Instance, tags, payload, threshold, TimeSpan.Zero, reliability, cancellation) != null;
		}

		return published ? identifier : null;
	}
	#endregion

	#region 内部方法
	internal bool Validate(string identifier)
	{
		if(_exclusion != null && _exclusion.Contains(identifier))
			return false;

		return _inclusion == null || _inclusion.Count == 0 || _inclusion.Contains(identifier);
	}

	internal string GetLogicalTopic(string topic)
	{
		if(string.IsNullOrEmpty(_options.Group) || string.IsNullOrEmpty(topic))
			return topic;

		var prefix = _options.Group + ":";
		return topic.StartsWith(prefix, StringComparison.Ordinal) ? topic[prefix.Length..] : topic;
	}

	internal ValueTask UnsubscribeAsync(ZeroSubscriber subscriber, CancellationToken cancellation) =>
		_transport == null ? ValueTask.CompletedTask : _transport.UnsubscribeAsync(subscriber, cancellation);

	internal void Pause(ZeroSubscriber subscriber, Message message) => _transport?.Pause(subscriber, message);
	internal void Resume(ZeroSubscriber subscriber) => _transport?.Resume(subscriber);
	internal TimeSpan Timeout => _options.Timeout;
	#endregion

	#region 重写方法
	protected override MessageReliability Reliability => MessageReliability.LeastOnce;

	protected override string GetTopic(string topic)
	{
		topic = string.IsNullOrEmpty(topic) ? _options.Topic ?? string.Empty : topic;
		return topic == "*" ? string.Empty : topic;
	}
	#endregion

	#region 私有方法
	private async ValueTask EnsureInitializedAsync(CancellationToken cancellation)
	{
		Task initialization;

		lock(_locker)
		{
			_transport ??= new Transport(_options, this.Instance, () => this.GetHeartbeatTopics());

			if(_initialization == null || _initialization.IsCanceled || _initialization.IsFaulted)
				_initialization = _transport.StartAsync(CancellationToken.None).AsTask();

			initialization = _initialization;
		}

		if(cancellation.CanBeCanceled)
			await initialization.WaitAsync(cancellation);
		else
			await initialization;
	}

	private string GetPhysicalTopic(string topic) => string.IsNullOrEmpty(_options.Group) ? topic : $"{_options.Group}:{topic}";
	private string[] GetHeartbeatTopics()
	{
		var topics = new List<string>();

		foreach(var subscriber in this.Subscribers)
			topics.Add(this.GetPhysicalTopic(subscriber.Topic));

		return topics.ToArray();
	}

	private static (HashSet<string> inclusion, HashSet<string> exclusion) CreateFilter(string filter, string instance)
	{
		HashSet<string> inclusion = null;
		HashSet<string> exclusion = null;

		if(string.IsNullOrWhiteSpace(filter))
			return (null, new HashSet<string>([instance]));

		var parts = filter.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		for(int i = 0; i < parts.Length; i++)
		{
			switch(parts[i])
			{
				case "*":
					exclusion?.Clear();
					inclusion?.Clear();
					break;
				case ".":
				case "~":
					exclusion?.Remove(instance);
					(inclusion ??= new HashSet<string>()).Add(instance);
					break;
				default:
					if(parts[i][0] == '!')
					{
						if(parts[i].Length == 1)
							exclusion?.Clear();
						else
						{
							var value = parts[i][1..];
							if(value is "." or "~")
								value = instance;

							(exclusion ??= new HashSet<string>()).Add(value);
						}
					}
					else
					{
						exclusion?.Remove(parts[i]);
						(inclusion ??= new HashSet<string>()).Add(parts[i]);
					}
					break;
			}
		}

		return (inclusion, exclusion);
	}

	private static string GenerateIdentifier(Configuration.ZeroConnectionSettings settings)
	{
		if(string.IsNullOrEmpty(settings.Instance) || settings.Instance == "*")
		{
			return string.IsNullOrEmpty(settings.Client) ?
				Randomizer.GenerateString(10) :
				$"{settings.Client}-{unchecked((uint)Randomizer.GenerateInt32()):X}";
		}

		return settings.Instance;
	}
	#endregion

	#region 处置方法
	protected override void Dispose(bool disposing)
	{
		if(!disposing)
			return;

		foreach(var subscriber in this.Subscribers)
			subscriber.DisposeAsync().AsTask().GetAwaiter().GetResult();

		_transport?.DisposeAsync().AsTask().GetAwaiter().GetResult();
	}
	#endregion
}

internal readonly record struct ZeroQueueRuntimeOptions(
	string Server,
	ushort Port,
	TimeSpan Timeout,
	TimeSpan Heartbeat,
	TimeSpan ReconnectInterval,
	string Client,
	string Instance,
	string Group,
	string Topic);
