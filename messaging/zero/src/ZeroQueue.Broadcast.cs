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
using System.Text;
using System.Threading;
using System.Collections.Generic;

using NetMQ;
using NetMQ.Sockets;
using NetMQ.Monitoring;

namespace Zongsoft.Messaging.ZeroMQ;

public sealed partial class ZeroQueue
{
	private sealed partial class Transport
	{
		private sealed class ZeroBroadcast
		{
			#region 常量定义
			private static readonly UTF8Encoding UTF8 = new(false, true);
			#endregion

			#region 成员字段
			private readonly string _identifier;
			private readonly ZeroQueueRuntimeOptions _options;
			private readonly NetMQPoller _poller;
			private readonly Func<string[]> _heartbeatTopics;
			private readonly Action _disconnected;
			private readonly Dictionary<ZeroSubscriber, string> _subscriberTopics = new();
			private readonly Dictionary<ZeroSubscriber, SubscriberSocket> _subscribers = new();
			private readonly HashSet<ZeroSubscriber> _paused = [];
			private readonly HashSet<string> _subscriptions = new(StringComparer.Ordinal);
			private XPublisherSocket _publisher;
			private NetMQMonitor _publisherMonitor;
			private string _epoch;
			private ushort _outgoingPort;
			private DateTime _nextHeartbeat;
			#endregion

			#region 构造函数
			public ZeroBroadcast(ZeroQueueRuntimeOptions options, string identifier, NetMQPoller poller, Func<string[]> heartbeatTopics, Action disconnected)
			{
				_options = options;
				_identifier = identifier;
				_poller = poller;
				_heartbeatTopics = heartbeatTopics;
				_disconnected = disconnected;
				_nextHeartbeat = DateTime.UtcNow + options.Heartbeat;
			}
			#endregion

			#region 公共属性
			public bool IsConnected => _publisher != null && !_publisher.IsDisposed;
			#endregion

			#region 连接管理
			public void Stop() => this.Disconnect(true);
			public void Connect(string epoch, ushort incoming, ushort outgoing)
			{
				this.Disconnect(false);
				_epoch = epoch;
				_outgoingPort = outgoing;
				_subscriptions.Clear();

				var publisher = new XPublisherSocket();
				NetMQMonitor monitor = null;
				try
				{
					publisher.Options.HeartbeatInterval = TimeSpan.FromSeconds(30);
					publisher.ReceiveReady += this.OnSubscriptionReady;
					monitor = new NetMQMonitor(publisher, $"inproc://{nameof(ZeroQueue)}-{_identifier}-{Guid.NewGuid():N}", SocketEvents.Disconnected);
					monitor.Disconnected += this.OnPublisherDisconnected;
					publisher.Connect(Protocol.GetAddress(_options.Server, incoming));
					_poller.Add(publisher);
					monitor.AttachToPoller(_poller);
					_publisher = publisher;
					_publisherMonitor = monitor;

					foreach(var entry in _subscriberTopics)
						this.Attach(entry.Key, entry.Value);
				}
				catch
				{
					if(monitor != null)
					{
						monitor.Disconnected -= this.OnPublisherDisconnected;
						if(monitor.IsRunning)
							monitor.DetachFromPoller();
						monitor.Dispose();
					}

					foreach(var subscriber in new List<ZeroSubscriber>(_subscribers.Keys))
						this.Detach(subscriber);

					publisher.ReceiveReady -= this.OnSubscriptionReady;
					if(!publisher.IsDisposed)
						_poller.RemoveAndDispose(publisher);
					_publisher = null;
					throw;
				}
			}

			public void Disconnect(bool removeRegistrations)
			{
				_subscriptions.Clear();
				foreach(var subscriber in new List<ZeroSubscriber>(_subscribers.Keys))
					this.Detach(subscriber);

				if(removeRegistrations)
					_subscriberTopics.Clear();

				var monitor = _publisherMonitor;
				_publisherMonitor = null;
				if(monitor != null)
				{
					monitor.Disconnected -= this.OnPublisherDisconnected;
					if(monitor.IsRunning)
						monitor.DetachFromPoller();
					monitor.Dispose();
				}

				var publisher = _publisher;
				_publisher = null;
				if(publisher != null && !publisher.IsDisposed)
				{
					publisher.ReceiveReady -= this.OnSubscriptionReady;
					_poller.RemoveAndDispose(publisher);
				}

				_epoch = null;
				_outgoingPort = 0;
			}

			private void OnPublisherDisconnected(object sender, NetMQMonitorSocketEventArgs args)
			{
				_subscriptions.Clear();
				_epoch = null;
				_disconnected();
			}
			#endregion

			#region 发布订阅
			public bool Publish(PublishCommand command)
			{
				if(!this.IsReady(command.Topic))
					return false;

				this.Send(command);
				return true;
			}

			public bool Subscribe(SubscribeCommand command)
			{
				if(_subscriberTopics.TryGetValue(command.Subscriber, out var topic))
				{
					if(string.Equals(topic, command.Topic, StringComparison.Ordinal))
						return true;
					throw new InvalidOperationException(Properties.Resources.ZeroQueue_SubscriptionTopicImmutable_Message);
				}

				_subscriberTopics.Add(command.Subscriber, command.Topic);
				try
				{
					this.Attach(command.Subscriber, command.Topic);
					return true;
				}
				catch
				{
					_subscriberTopics.Remove(command.Subscriber);
					throw;
				}
			}

			public bool Unsubscribe(ZeroSubscriber subscriber)
			{
				_subscriberTopics.Remove(subscriber);
				this.Detach(subscriber);
				return true;
			}

			private void Send(PublishCommand command)
			{
				var publisher = _publisher ?? throw new InvalidOperationException(Properties.Resources.ZeroQueue_PublisherUninitialized_Message);
				var compressed = command.Compression.CanCompress(command.Data.Length);
				var header = Packetizer.Pack(command.Identity, command.Identifier, command.Topic, command.Tags, compressed ? command.Compression.Name : null);
				var data = compressed ? command.Compression.Compress(command.Data) : command.Data;
				publisher.SendMoreFrame(header).SendFrame(data);
			}

			private void Attach(ZeroSubscriber subscriber, string topic)
			{
				if(string.IsNullOrEmpty(_epoch) || _outgoingPort == 0 || _subscribers.ContainsKey(subscriber))
					return;

				var channel = subscriber.Attach(topic, Protocol.GetAddress(_options.Server, _outgoingPort), _epoch);
				_subscribers.Add(subscriber, channel);
				_poller.Add(channel);
			}

			private void Detach(ZeroSubscriber subscriber)
			{
				var paused = _paused.Remove(subscriber);

				if(!_subscribers.Remove(subscriber, out var channel))
					channel = subscriber.Detach();
				else
					subscriber.Detach();

				if(channel == null || channel.IsDisposed)
					return;

				channel.ReceiveReady -= subscriber.OnReceiveReady;
				if(paused)
					channel.Dispose();
				else
					_poller.RemoveAndDispose(channel);
			}
			#endregion

			#region 订阅就绪
			private void OnSubscriptionReady(object sender, NetMQSocketEventArgs args)
			{
				try
				{
					while(args.Socket.TryReceiveFrameBytes(out var frame))
					{
						if(frame == null || frame.Length == 0 || frame[0] is not (0 or 1))
							continue;

						string topic;
						try { topic = UTF8.GetString(frame, 1, frame.Length - 1); }
						catch(DecoderFallbackException) { continue; }

						if(frame[0] == 1)
							_subscriptions.Add(topic);
						else
							_subscriptions.Remove(topic);
					}
				}
				catch(Exception exception) { Diagnostics.Logging.GetLogging(this).Error(exception); }
			}

			private bool IsReady(string topic)
			{
				if(!this.IsConnected)
					return false;

				foreach(var subscription in _subscriptions)
				{
					if(topic.StartsWith(subscription, StringComparison.Ordinal))
						return true;
				}

				return false;
			}

			#endregion

			#region 背压维护
			public bool Pause(ZeroSubscriber subscriber, Message message)
			{
				if(!_subscribers.TryGetValue(subscriber, out var channel) || !_paused.Add(subscriber))
					return true;

				subscriber.SetPending(message);
				_poller.Remove(channel);
				return true;
			}

			public bool Resume(ZeroSubscriber subscriber)
			{
				if(!_paused.Contains(subscriber) || !subscriber.TryDispatchPending())
					return true;

				_paused.Remove(subscriber);
				if(_subscribers.TryGetValue(subscriber, out var channel) && !channel.IsDisposed)
					_poller.Add(channel);
				return true;
			}

			public void Tick(DateTime now)
			{
				if(_options.Heartbeat > TimeSpan.Zero && now >= _nextHeartbeat)
				{
					_nextHeartbeat = now + _options.Heartbeat;
					try
					{
						foreach(var topic in _heartbeatTopics())
						{
							if(this.IsReady(topic))
								_publisher.SendMoreFrame(Packetizer.Pack(topic)).SendFrameEmpty();
						}
					}
					catch(Exception exception) { Diagnostics.Logging.GetLogging(this).Error(exception); }
				}
			}
			#endregion
		}
	}
}
