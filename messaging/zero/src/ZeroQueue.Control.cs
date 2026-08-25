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
using System.Threading.Tasks;
using System.Collections.Generic;

using NetMQ;
using NetMQ.Sockets;

namespace Zongsoft.Messaging.ZeroMQ;

public sealed partial class ZeroQueue
{
	private sealed partial class Transport
	{
		private sealed class ZeroControl
		{
			#region 常量定义
			private static readonly TimeSpan PING_INTERVAL = TimeSpan.FromSeconds(3);
			#endregion

			#region 成员字段
			private readonly string _session = Guid.NewGuid().ToString("N");
			private readonly ZeroQueueRuntimeOptions _options;
			private readonly NetMQPoller _poller;
			private readonly Action<Command> _post;
			private readonly Dictionary<string, Subscription> _subscriptions = new(StringComparer.Ordinal);
			private readonly Dictionary<string, PublishControlCommand> _publishes = new(StringComparer.Ordinal);
			private DealerSocket _dealer;
			private DateTime _nextPing;
			#endregion

			#region 构造函数
			public ZeroControl(ZeroQueueRuntimeOptions options, NetMQPoller poller, Action<Command> post)
			{
				_options = options;
				_poller = poller;
				_post = post;
			}
			#endregion

			#region 发布与确认
			public async ValueTask<string> PublishAsync(string identifier, string topic, string identity, string tags, byte[] data, TimeSpan expiration, CancellationToken cancellation)
			{
				var timestamp = DateTime.UtcNow;
				var command = new PublishControlCommand(
					identifier,
					topic,
					identity,
					tags,
					data,
					timestamp,
					expiration > TimeSpan.Zero ? timestamp + expiration : default,
					timestamp + _options.Timeout,
					cancellation);

				_post(command);
				await command.Completion.Task;
				return command.Accepted ? identifier : null;
			}

			public ValueTask AcknowledgeAsync(string subscription, string identifier, TimeSpan delay, CancellationToken cancellation)
			{
				if(delay <= TimeSpan.Zero)
				{
					var command = new AcknowledgeControlCommand(subscription, identifier, cancellation);
					_post(command);
					return new ValueTask(command.Completion.Task);
				}

				return DelayAsync(this, subscription, identifier, delay, cancellation);

				static async ValueTask DelayAsync(ZeroControl control, string subscription, string identifier, TimeSpan delay, CancellationToken cancellation)
				{
					await Task.Delay(delay, cancellation);
					await control.AcknowledgeAsync(subscription, identifier, TimeSpan.Zero, cancellation);
				}
			}
			#endregion

			#region 连接管理
			public void Connect(ushort port)
			{
				this.Disconnect(false);
				if(port == 0)
					return;

				var dealer = new DealerSocket();
				try
				{
					dealer.Options.Identity = Encoding.UTF8.GetBytes(_session);
					dealer.Options.HeartbeatInterval = PING_INTERVAL;
					dealer.ReceiveReady += this.OnReceiveReady;
					dealer.Connect(ZeroUtility.GetTcpAddress(_options.Server, port));
					_poller.Add(dealer);
					_dealer = dealer;
					_nextPing = DateTime.UtcNow + PING_INTERVAL;

					foreach(var subscription in _subscriptions.Values)
						this.Register(subscription);
					foreach(var command in _publishes.Values)
						this.Send(command);
				}
				catch
				{
					dealer.ReceiveReady -= this.OnReceiveReady;
					if(!dealer.IsDisposed)
						_poller.RemoveAndDispose(dealer);
					throw;
				}
			}

			public void Disconnect(bool removeRegistrations)
			{
				var dealer = _dealer;
				_dealer = null;
				if(dealer != null && !dealer.IsDisposed)
				{
					dealer.ReceiveReady -= this.OnReceiveReady;
					_poller.RemoveAndDispose(dealer);
				}

				foreach(var subscription in _subscriptions.Values)
					subscription.Registered = false;

				if(removeRegistrations)
					_subscriptions.Clear();
			}
			#endregion

			#region 命令执行
			public bool Execute(ControlCommand command) => command switch
			{
				PublishControlCommand publish => this.Publish(publish),
				AcknowledgeControlCommand acknowledge => this.Acknowledge(acknowledge),
				CancelControlPublishCommand cancel => this.Cancel(cancel.Publish),
				_ => true,
			};

			public bool Subscribe(SubscribeCommand command)
			{
				foreach(var current in _subscriptions.Values)
				{
					if(ReferenceEquals(current.Subscriber, command.Subscriber))
						return current.Registered;
				}

				var subscription = new Subscription(Guid.NewGuid().ToString("N"), command.Topic, command.Subscriber, command);
				_subscriptions.Add(subscription.Identifier, subscription);
				this.Register(subscription);
				return false;
			}

			public bool Unsubscribe(ZeroSubscriber subscriber)
			{
				Subscription subscription = null;
				foreach(var current in _subscriptions.Values)
				{
					if(ReferenceEquals(current.Subscriber, subscriber))
					{
						subscription = current;
						break;
					}
				}

				if(subscription == null || !_subscriptions.Remove(subscription.Identifier))
					return true;

				subscription.Command?.Completion.TrySetCanceled();
				if(_dealer != null && !_dealer.IsDisposed)
					_dealer.SendMoreFrame("UNREGISTER").SendMoreFrame(_session).SendFrame(subscription.Identifier);
				return true;
			}

			private bool Publish(PublishControlCommand command)
			{
				if(_publishes.ContainsKey(command.Identifier))
					return false;

				_publishes.Add(command.Identifier, command);
				command.CancellationRegistration = command.CallerCancellation.Register(() => _post(new CancelControlPublishCommand(command)));
				this.Send(command);
				return false;
			}

			private bool Acknowledge(AcknowledgeControlCommand command)
			{
				var dealer = _dealer ?? throw new InvalidOperationException(Properties.Resources.ZeroQueue_PublisherUninitialized_Message);
				dealer.SendMoreFrame("ACK").SendMoreFrame(_session).SendMoreFrame(command.Subscription).SendFrame(command.Identifier);
				return true;
			}

			private bool Cancel(PublishControlCommand command)
			{
				if(!_publishes.Remove(command.Identifier))
					return true;

				command.CancellationRegistration.Dispose();
				command.Completion.TrySetCanceled(command.CallerCancellation);
				return true;
			}
			#endregion

			#region 协议收发
			private void Register(Subscription subscription)
			{
				if(_dealer == null || _dealer.IsDisposed)
					return;

				_dealer.SendMoreFrame("REGISTER").SendMoreFrame(_session).SendMoreFrame(subscription.Identifier).SendFrame(subscription.Topic);
			}

			private void Send(PublishControlCommand command)
			{
				if(_dealer == null || _dealer.IsDisposed)
					return;

				_dealer.SendMoreFrame("PUBLISH")
					.SendMoreFrame(command.Identifier)
					.SendMoreFrame(command.Topic)
					.SendMoreFrame(command.Identity)
					.SendMoreFrame(command.Tags ?? string.Empty)
					.SendMoreFrame(command.Timestamp.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture))
					.SendMoreFrame(command.Expiration == default ? "0" : command.Expiration.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture))
					.SendFrame(command.Data);
			}

			private void OnReceiveReady(object sender, NetMQSocketEventArgs args)
			{
				var message = new NetMQMessage();
				while(args.Socket.TryReceiveMultipartMessage(ref message))
				{
					try { this.Process(message); }
					catch(Exception exception) { Diagnostics.Logging.GetLogging(this).Error(exception); }
					message = new NetMQMessage();
				}
			}

			private void Process(NetMQMessage message)
			{
				if(message.FrameCount < 2)
					return;

				switch(message[0].ConvertToString())
				{
					case "REGISTERED" when message.FrameCount == 2:
					{
						var identifier = message[1].ConvertToString();
						if(_subscriptions.TryGetValue(identifier, out var subscription))
						{
							subscription.Registered = true;
							subscription.Command?.Completion.TrySetResult();
							subscription.Command = null;
						}
						break;
					}
					case "DELIVER" when message.FrameCount == 9:
					{
						var subscription = message[1].ConvertToString();
						if(!_subscriptions.TryGetValue(subscription, out var registration))
							break;

						var identifier = message[2].ConvertToString();
						var topic = message[3].ConvertToString();
						var producer = message[4].ConvertToString();
						var tags = message[5].ConvertToString();
						var timestamp = long.TryParse(message[6].ConvertToString(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var ticks) ?
							new DateTime(ticks, DateTimeKind.Utc) : DateTime.UtcNow;
						var data = message[8].ToByteArray();
						if(string.IsNullOrWhiteSpace(identifier) || topic == null || data.Length > Packetizer.MaxPayloadSize)
							break;

						if(!registration.Subscriber.Owner.Validate(producer))
						{
							_dealer.SendMoreFrame("ACK").SendMoreFrame(_session).SendMoreFrame(subscription).SendFrame(identifier);
							break;
						}

						registration.Subscriber.Dispatch(new Message(identifier, registration.Subscriber.Owner.GetLogicalTopic(topic), data, tags,
							(delay, cancellation) => this.AcknowledgeAsync(subscription, identifier, delay, cancellation))
						{
							Identity = producer,
							Timestamp = timestamp,
						});
						break;
					}
					case "ACCEPTED" when message.FrameCount == 2:
						this.Complete(message[1].ConvertToString(), true);
						break;
					case "UNROUTABLE" when message.FrameCount == 2:
						this.Complete(message[1].ConvertToString(), false);
						break;
					case "ERROR" when message.FrameCount == 3:
					{
						var code = message[1].ConvertToString();
						var identifier = message[2].ConvertToString();
						if(_subscriptions.TryGetValue(identifier, out var subscription))
							subscription.Command?.Completion.TrySetException(new InvalidOperationException(string.Format(Properties.Resources.ZeroQueue_ReliableProtocolError_Message, code)));
						if(_publishes.Remove(identifier, out var command))
						{
							command.CancellationRegistration.Dispose();
							command.Completion.TrySetException(new InvalidOperationException(string.Format(Properties.Resources.ZeroQueue_ReliableProtocolError_Message, code)));
						}
						break;
					}
				}
			}

			private void Complete(string identifier, bool accepted)
			{
				if(!_publishes.Remove(identifier, out var command))
					return;

				command.CancellationRegistration.Dispose();
				command.Accepted = accepted;
				command.Completion.TrySetResult();
			}
			#endregion

			#region 维护与停止
			public void Tick(DateTime now)
			{
				foreach(var entry in new List<KeyValuePair<string, PublishControlCommand>>(_publishes))
				{
					if(now < entry.Value.Deadline)
						continue;

					_publishes.Remove(entry.Key);
					entry.Value.CancellationRegistration.Dispose();
					entry.Value.Completion.TrySetException(new TimeoutException(Properties.Resources.ZeroQueue_ControlTimeout_Message));
				}

				if(_dealer == null || _dealer.IsDisposed || now < _nextPing)
					return;

				_nextPing = now + PING_INTERVAL;
				foreach(var subscription in _subscriptions.Values)
				{
					if(subscription.Registered)
						_dealer.SendMoreFrame("PING").SendMoreFrame(_session).SendFrame(subscription.Identifier);
					else
						this.Register(subscription);
				}
			}

			public void Stop()
			{
				this.Disconnect(false);
				foreach(var subscription in _subscriptions.Values)
					subscription.Command?.Completion.TrySetException(new ObjectDisposedException(nameof(Transport)));
				foreach(var command in _publishes.Values)
				{
					command.CancellationRegistration.Dispose();
					command.Completion.TrySetException(new ObjectDisposedException(nameof(Transport)));
				}
				_subscriptions.Clear();
				_publishes.Clear();
			}
			#endregion

			#region 嵌套类型
			public abstract class ControlCommand(CancellationToken cancellation = default) : Command(cancellation);

			private sealed class Subscription(string identifier, string topic, ZeroSubscriber subscriber, SubscribeCommand command)
			{
				public readonly string Identifier = identifier;
				public readonly string Topic = topic;
				public readonly ZeroSubscriber Subscriber = subscriber;
				public SubscribeCommand Command = command;
				public bool Registered;
			}

			private sealed class PublishControlCommand(string identifier, string topic, string identity, string tags, byte[] data, DateTime timestamp, DateTime expiration, DateTime deadline, CancellationToken cancellation) : ControlCommand(cancellation)
			{
				public readonly string Identifier = identifier;
				public readonly string Topic = topic;
				public readonly string Identity = identity;
				public readonly string Tags = tags;
				public readonly byte[] Data = data;
				public readonly DateTime Timestamp = timestamp;
				public readonly DateTime Expiration = expiration;
				public readonly DateTime Deadline = deadline;
				public readonly CancellationToken CallerCancellation = cancellation;
				public CancellationTokenRegistration CancellationRegistration;
				public bool Accepted;
			}

			private sealed class AcknowledgeControlCommand(string subscription, string identifier, CancellationToken cancellation) : ControlCommand(cancellation)
			{
				public readonly string Subscription = subscription;
				public readonly string Identifier = identifier;
			}

			private sealed class CancelControlPublishCommand(PublishControlCommand publish) : ControlCommand
			{
				public readonly PublishControlCommand Publish = publish;
			}
			#endregion
		}
	}
}
