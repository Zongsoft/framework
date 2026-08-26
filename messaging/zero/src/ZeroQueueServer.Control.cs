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
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using System.Collections.Generic;

using NetMQ;
using NetMQ.Sockets;

namespace Zongsoft.Messaging.ZeroMQ;

public sealed partial class ZeroQueueServer
{
	private sealed partial class ServerAgent
	{
		private sealed class ZeroControlServer : IAsyncDisposable
		{
			#region 常量定义
			private const int STORAGE_CAPACITY = 1024;
			private static readonly TimeSpan SESSION_TIMEOUT = TimeSpan.FromSeconds(10);
			private static readonly TimeSpan RETRY_INTERVAL = TimeSpan.FromSeconds(1);
			private static readonly TimeSpan STORAGE_RETRY_INTERVAL = TimeSpan.FromMilliseconds(250);
			#endregion

			#region 成员字段
			private readonly int _configuredPort;
			private readonly IMessageStorage _storage;
			private readonly IReadOnlyList<Message> _pendingEntries;
			private readonly NetMQPoller _poller;
			private readonly Action<Action> _dispatch;
			private readonly StorageWorker _worker;
			private readonly Dictionary<string, Registration> _registrations = new(StringComparer.Ordinal);
			private readonly Dictionary<string, Acceptance> _accepting = new(StringComparer.Ordinal);
			private readonly Dictionary<string, DurableEnvelope> _pending = new(StringComparer.Ordinal);
			private readonly Dictionary<string, int> _cursors = new(StringComparer.Ordinal);
			private RouterSocket _router;
			private NetMQTimer _timer;
			private int _active;
			#endregion

			#region 构造函数
			public ZeroControlServer(int port, IMessageStorage storage, IReadOnlyList<Message> pendingEntries, NetMQPoller poller, Action<Action> dispatch)
			{
				_configuredPort = port;
				_storage = storage;
				_pendingEntries = pendingEntries ?? [];
				_poller = poller ?? throw new ArgumentNullException(nameof(poller));
				_dispatch = dispatch ?? throw new ArgumentNullException(nameof(dispatch));
				_worker = storage == null ? null : new StorageWorker(STORAGE_CAPACITY);
			}
			#endregion

			#region 公共属性
			public int Port { get; private set; }
			#endregion

			#region 启停方法
			public void Start()
			{
				if(_storage == null)
				{
					this.Port = 0;
					return;
				}

				var router = new RouterSocket();
				try
				{
					router.Options.RouterMandatory = true;
					router.ReceiveReady += this.OnReceiveReady;
					this.Port = Bind(router, _configuredPort);
					_poller.Add(router);
					_router = router;
					Volatile.Write(ref _active, 1);

					foreach(var entry in _pendingEntries)
					{
						if(!TryDeserialize(entry, out var envelope))
						{
							Diagnostics.Logging.GetLogging(this).Warn(string.Format(Properties.Resources.ZeroQueue_ReliableEntryInvalid_Message, entry.Identifier, _storage.Name));
							continue;
						}

						envelope.NextAttempt = DateTime.UtcNow;
						_pending[envelope.Identifier] = envelope;
						if(envelope.IsExpired)
						{
							envelope.Removal = RemovalReason.Expired;
							envelope.NextStorageAttempt = DateTime.MinValue;
							this.TryRemove(envelope);
						}
					}

					_timer = new NetMQTimer(TimeSpan.FromMilliseconds(250));
					_timer.Elapsed += this.OnTick;
					_poller.Add(_timer);
				}
				catch
				{
					Volatile.Write(ref _active, 0);
					router.ReceiveReady -= this.OnReceiveReady;
					if(!router.IsDisposed)
						_poller.RemoveAndDispose(router);
					this.Port = 0;
					throw;
				}
			}

			public void Stop()
			{
				Volatile.Write(ref _active, 0);

				if(_timer != null)
				{
					_timer.Enable = false;
					_timer.Elapsed -= this.OnTick;
					_poller.Remove(_timer);
					_timer = null;
				}

				var router = _router;
				_router = null;
				if(router != null && !router.IsDisposed)
				{
					router.ReceiveReady -= this.OnReceiveReady;
					_poller.RemoveAndDispose(router);
				}

				this.Port = 0;
				_registrations.Clear();
				_accepting.Clear();
				_pending.Clear();
				_cursors.Clear();
			}
			#endregion

			#region 控制协议
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

				var route = message[0].ToByteArray();
				switch(message[1].ConvertToString())
				{
					case Protocol.Commands.Register when message.FrameCount == 5:
						this.Register(route, message[2].ConvertToString(), message[3].ConvertToString(), message[4].ConvertToString());
						break;
					case Protocol.Commands.Unregister when message.FrameCount == 4:
						this.Unregister(message[2].ConvertToString(), message[3].ConvertToString());
						break;
					case Protocol.Commands.Ping when message.FrameCount == 4:
						this.Ping(route, message[2].ConvertToString(), message[3].ConvertToString());
						break;
					case Protocol.Commands.Publish when message.FrameCount == 10:
						this.Publish(route, message[2].ConvertToString(), message[3].ConvertToString(), message[4].ConvertToString(), message[5].ConvertToString(), message[6].ConvertToString(), message[7].ConvertToString(), message[8].ConvertToString(), message[9].ToByteArray());
						break;
					case Protocol.Commands.Acknowledge when message.FrameCount == 5:
						this.Acknowledge(message[2].ConvertToString(), message[3].ConvertToString(), message[4].ConvertToString());
						break;
				}
			}

			private void Register(byte[] route, string session, string identifier, string topic)
			{
				if(string.IsNullOrWhiteSpace(session) || string.IsNullOrWhiteSpace(identifier) || identifier.Length > Protocol.MaxIdentifierSize ||
				   topic == null || System.Text.Encoding.UTF8.GetByteCount(topic) > Protocol.MaxTopicSize)
					return;

				_registrations[identifier] = new Registration(route, session, identifier, topic, DateTime.UtcNow);
				this.Send(route, Protocol.Commands.Registered, identifier);

				foreach(var envelope in _pending.Values)
				{
					if(envelope.Removal == RemovalReason.None && envelope.Topic.StartsWith(topic, StringComparison.Ordinal))
						envelope.NextAttempt = DateTime.MinValue;
				}
			}

			private void Unregister(string session, string identifier)
			{
				if(_registrations.TryGetValue(identifier, out var registration) && string.Equals(registration.Session, session, StringComparison.Ordinal))
					_registrations.Remove(identifier);
			}

			private void Ping(byte[] route, string session, string identifier)
			{
				if(_registrations.TryGetValue(identifier, out var registration) && string.Equals(registration.Session, session, StringComparison.Ordinal))
				{
					registration.Route = route;
					registration.LastSeen = DateTime.UtcNow;
				}
			}

			private void Publish(byte[] route, string identifier, string topic, string producer, string tags, string timestampText, string expirationText, string compression, byte[] data)
			{
				if(string.IsNullOrWhiteSpace(identifier) || identifier.Length > Protocol.MaxIdentifierSize || topic == null ||
				   System.Text.Encoding.UTF8.GetByteCount(topic) > Protocol.MaxTopicSize || string.IsNullOrWhiteSpace(producer) ||
				   data == null || data.Length > Protocol.MaxPayloadSize ||
				   (!string.IsNullOrEmpty(compression) && !IO.Compression.Compressor.IsSupported(compression)) ||
				   !long.TryParse(timestampText, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var timestampTicks) ||
				   !long.TryParse(expirationText, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var expirationTicks))
				{
					this.Send(route, Protocol.Commands.Error, Protocol.Errors.InvalidPublish, identifier ?? string.Empty);
					return;
				}

				var expiration = expirationTicks <= 0 ? default : new DateTime(expirationTicks, DateTimeKind.Utc);
				var envelope = new DurableEnvelope(identifier, topic, producer, tags, compression, data, new DateTime(timestampTicks, DateTimeKind.Utc), expiration)
				{
					NextAttempt = DateTime.MinValue,
				};

				if(_pending.TryGetValue(identifier, out var current))
				{
					if(!current.Equals(envelope))
						this.Send(route, Protocol.Commands.Error, Protocol.Errors.IdentifierConflict, identifier);
					else
					{
						this.Send(route, Protocol.Commands.Accepted, identifier);
						if(current.Removal == RemovalReason.None)
							current.NextAttempt = DateTime.MinValue;
					}
					return;
				}

				if(_accepting.TryGetValue(identifier, out var accepting))
				{
					if(!accepting.Envelope.Equals(envelope))
						this.Send(route, Protocol.Commands.Error, Protocol.Errors.IdentifierConflict, identifier);
					else
						accepting.Add(route);
					return;
				}

				if(!this.HasRegistration(topic))
				{
					this.Send(route, Protocol.Commands.Unroutable, identifier);
					return;
				}

				if(expiration != default && expiration <= DateTime.UtcNow)
				{
					this.Send(route, Protocol.Commands.Error, Protocol.Errors.Expired, identifier);
					return;
				}

				accepting = new Acceptance(envelope, route);
				_accepting.Add(identifier, accepting);
				if(!_worker.TryExecute(() => _storage.SetAsync(GetStorageMessage(envelope), GetExpiry(envelope)), exception => this.Dispatch(() => this.OnPersisted(identifier, exception))))
				{
					_accepting.Remove(identifier);
					this.Send(route, Protocol.Commands.Error, Protocol.Errors.StorageBusy, identifier);
				}
			}

			private void OnPersisted(string identifier, Exception exception)
			{
				if(Volatile.Read(ref _active) == 0 || !_accepting.Remove(identifier, out var accepting))
					return;

				if(exception != null)
				{
					Diagnostics.Logging.GetLogging(this).Error(exception, string.Format(Properties.Resources.ZeroQueue_StorageSetFailed_Message, _storage.Name, identifier));
					foreach(var route in accepting.Routes)
						this.Send(route, Protocol.Commands.Error, Protocol.Errors.StorageFailure, identifier);
					return;
				}

				_pending.Add(identifier, accepting.Envelope);
				foreach(var route in accepting.Routes)
					this.Send(route, Protocol.Commands.Accepted, identifier);
				this.Deliver(accepting.Envelope);
			}

			private void Acknowledge(string session, string subscription, string identifier)
			{
				if(!_registrations.TryGetValue(subscription, out var registration) || !string.Equals(registration.Session, session, StringComparison.Ordinal))
					return;
				if(!_pending.TryGetValue(identifier, out var envelope) || envelope.Removal != RemovalReason.None || !envelope.Topic.StartsWith(registration.Topic, StringComparison.Ordinal))
					return;

				envelope.Removal = RemovalReason.Acknowledged;
				envelope.NextStorageAttempt = DateTime.MinValue;
				this.TryRemove(envelope);
			}
			#endregion

			#region 投递维护
			private bool HasRegistration(string topic)
			{
				foreach(var registration in _registrations.Values)
				{
					if(topic.StartsWith(registration.Topic, StringComparison.Ordinal))
						return true;
				}

				return false;
			}

			private void Deliver(DurableEnvelope envelope)
			{
				if(envelope.Removal != RemovalReason.None)
					return;

				var registrations = new List<Registration>();
				foreach(var candidate in _registrations.Values)
				{
					if(envelope.Topic.StartsWith(candidate.Topic, StringComparison.Ordinal))
						registrations.Add(candidate);
				}

				if(registrations.Count == 0)
				{
					envelope.NextAttempt = DateTime.UtcNow + RETRY_INTERVAL;
					return;
				}

				registrations.Sort((first, second) => string.CompareOrdinal(first.Identifier, second.Identifier));
				_cursors.TryGetValue(envelope.Topic, out var cursor);
				var registration = registrations[cursor % registrations.Count];
				_cursors[envelope.Topic] = cursor == int.MaxValue ? 0 : cursor + 1;
				envelope.Attempt++;
				envelope.NextAttempt = DateTime.UtcNow + RETRY_INTERVAL;

				try
				{
					_router.SendMoreFrame(registration.Route)
						.SendMoreFrame(Protocol.Commands.Deliver)
						.SendMoreFrame(registration.Identifier)
						.SendMoreFrame(envelope.Identifier)
						.SendMoreFrame(envelope.Topic)
						.SendMoreFrame(envelope.Producer)
						.SendMoreFrame(envelope.Tags ?? string.Empty)
						.SendMoreFrame(envelope.Timestamp.Ticks.ToString(System.Globalization.CultureInfo.InvariantCulture))
						.SendMoreFrame(envelope.Attempt.ToString(System.Globalization.CultureInfo.InvariantCulture))
						.SendMoreFrame(envelope.Compression ?? string.Empty)
						.SendFrame(envelope.Data);
				}
				catch(NetMQException) { }
			}

			private void TryRemove(DurableEnvelope envelope)
			{
				if(envelope.Removal == RemovalReason.None || envelope.RemovalInFlight)
					return;

				if(_worker.TryExecute(async () => _ = await _storage.RemoveAsync(envelope.Identifier).ConfigureAwait(false), exception => this.Dispatch(() => this.OnRemoved(envelope, exception))))
					envelope.RemovalInFlight = true;
				else
					envelope.NextStorageAttempt = DateTime.UtcNow + STORAGE_RETRY_INTERVAL;
			}

			private void OnRemoved(DurableEnvelope envelope, Exception exception)
			{
				if(Volatile.Read(ref _active) == 0 || !_pending.TryGetValue(envelope.Identifier, out var current) || !ReferenceEquals(current, envelope))
					return;

				envelope.RemovalInFlight = false;
				if(exception == null)
				{
					_pending.Remove(envelope.Identifier);
					if(envelope.Removal == RemovalReason.Expired)
						Diagnostics.Logging.GetLogging(this).Warn(string.Format(Properties.Resources.ZeroQueue_ReliableExpired_Message, envelope.Identifier));
					return;
				}

				Diagnostics.Logging.GetLogging(this).Error(exception, string.Format(Properties.Resources.ZeroQueue_StorageRemoveFailed_Message, _storage.Name, envelope.Identifier));
				envelope.NextStorageAttempt = DateTime.UtcNow + STORAGE_RETRY_INTERVAL;
			}

			private void OnTick(object sender, NetMQTimerEventArgs args)
			{
				var now = DateTime.UtcNow;
				foreach(var entry in new List<KeyValuePair<string, Registration>>(_registrations))
				{
					if(now - entry.Value.LastSeen >= SESSION_TIMEOUT)
						_registrations.Remove(entry.Key);
				}

				foreach(var envelope in new List<DurableEnvelope>(_pending.Values))
				{
					if(envelope.Removal != RemovalReason.None)
					{
						if(!envelope.RemovalInFlight && now >= envelope.NextStorageAttempt)
							this.TryRemove(envelope);
						continue;
					}

					if(envelope.IsExpired)
					{
						envelope.Removal = RemovalReason.Expired;
						envelope.NextStorageAttempt = DateTime.MinValue;
						this.TryRemove(envelope);
						continue;
					}

					if(now >= envelope.NextAttempt)
						this.Deliver(envelope);
				}
			}

			private void Dispatch(Action action)
			{
				if(Volatile.Read(ref _active) != 0)
					_dispatch(() =>
					{
						if(Volatile.Read(ref _active) != 0)
							action();
					});
			}

			private void Send(byte[] route, string command, params string[] frames)
			{
				try
				{
					_router.SendMoreFrame(route).SendMoreFrame(command);
					for(var index = 0; index < frames.Length; index++)
					{
						if(index == frames.Length - 1)
							_router.SendFrame(frames[index]);
						else
							_router.SendMoreFrame(frames[index]);
					}

					if(frames.Length == 0)
						_router.SendFrameEmpty();
				}
				catch(NetMQException) { }
			}
			#endregion

			#region 消息封装
			private static TimeSpan GetExpiry(DurableEnvelope envelope)
			{
				if(envelope.Expiration == default)
					return default;

				var expiry = envelope.Expiration - DateTime.UtcNow;
				return expiry > TimeSpan.Zero ? expiry : TimeSpan.FromTicks(1);
			}

			private static Message GetStorageMessage(DurableEnvelope envelope) => new(
				envelope.Identifier,
				envelope.Topic,
				JsonSerializer.SerializeToUtf8Bytes(new DurablePayload
				{
					Version = Protocol.Version,
					Compression = envelope.Compression,
					Data = envelope.Data,
					Expiration = envelope.Expiration,
				}))
			{
				Identity = envelope.Producer,
				Tags = envelope.Tags,
				Timestamp = envelope.Timestamp,
			};

			private static bool TryDeserialize(Message message, out DurableEnvelope envelope)
			{
				try
				{
					var payload = JsonSerializer.Deserialize<DurablePayload>(message.Data);
					if(payload?.Data == null || !string.Equals(payload.Version, Protocol.Version, StringComparison.Ordinal) ||
					   (!string.IsNullOrEmpty(payload.Compression) && !IO.Compression.Compressor.IsSupported(payload.Compression)) ||
					   string.IsNullOrWhiteSpace(message.Identifier) || message.Topic == null || string.IsNullOrWhiteSpace(message.Identity))
					{
						envelope = null;
						return false;
					}

					envelope = new DurableEnvelope(message.Identifier, message.Topic, message.Identity, message.Tags, payload.Compression, payload.Data, message.Timestamp, payload.Expiration);
					return true;
				}
				catch(JsonException)
				{
					envelope = null;
					return false;
				}
			}
			#endregion

			#region 资源释放
			public async ValueTask DisposeAsync()
			{
				Volatile.Write(ref _active, 0);
				if(_worker != null)
					await _worker.DisposeAsync();
			}
			#endregion

			#region 嵌套类型
			private enum RemovalReason
			{
				None,
				Acknowledged,
				Expired,
			}

			private sealed class Registration(byte[] route, string session, string identifier, string topic, DateTime lastSeen)
			{
				public byte[] Route = route;
				public readonly string Session = session;
				public readonly string Identifier = identifier;
				public readonly string Topic = topic;
				public DateTime LastSeen = lastSeen;
			}

			private sealed class Acceptance
			{
				private readonly List<byte[]> _routes = [];

				public Acceptance(DurableEnvelope envelope, byte[] route)
				{
					this.Envelope = envelope;
					this.Add(route);
				}

				public DurableEnvelope Envelope { get; }
				public IReadOnlyList<byte[]> Routes => _routes;

				public void Add(byte[] route)
				{
					foreach(var current in _routes)
					{
						if(current.AsSpan().SequenceEqual(route))
							return;
					}

					_routes.Add(route);
				}
			}

			private sealed class DurableEnvelope(string identifier, string topic, string producer, string tags, string compression, byte[] data, DateTime timestamp, DateTime expiration)
			{
				public readonly string Identifier = identifier;
				public readonly string Topic = topic;
				public readonly string Producer = producer;
				public readonly string Tags = string.IsNullOrEmpty(tags) ? null : tags;
				public readonly string Compression = string.IsNullOrEmpty(compression) ? null : compression;
				public readonly byte[] Data = data;
				public readonly DateTime Timestamp = timestamp;
				public readonly DateTime Expiration = expiration;
				public int Attempt;
				public DateTime NextAttempt;
				public RemovalReason Removal;
				public bool RemovalInFlight;
				public DateTime NextStorageAttempt;
				public bool IsExpired => this.Expiration != default && this.Expiration <= DateTime.UtcNow;

				public bool Equals(DurableEnvelope other) => other != null &&
					string.Equals(this.Identifier, other.Identifier, StringComparison.Ordinal) &&
					string.Equals(this.Topic, other.Topic, StringComparison.Ordinal) &&
					string.Equals(this.Producer, other.Producer, StringComparison.Ordinal) &&
					string.Equals(this.Tags, other.Tags, StringComparison.Ordinal) &&
					string.Equals(this.Compression, other.Compression, StringComparison.OrdinalIgnoreCase) &&
					this.Timestamp == other.Timestamp && this.Expiration == other.Expiration &&
					this.Data.AsSpan().SequenceEqual(other.Data);
			}

			private sealed class DurablePayload
			{
				public string Version { get; set; }
				public string Compression { get; set; }
				public byte[] Data { get; set; }
				public DateTime Expiration { get; set; }
			}

			private sealed class StorageWorker : IAsyncDisposable
			{
				private int _disposed;
				private readonly Task _runner;
				private readonly Channel<Work> _channel;

				public StorageWorker(int capacity)
				{
					_channel = Channel.CreateBounded<Work>(new BoundedChannelOptions(capacity)
					{
						SingleReader = true,
						SingleWriter = true,
						FullMode = BoundedChannelFullMode.Wait,
					});
					_runner = Task.Run(this.ProcessAsync);
				}

				public bool TryExecute(Func<ValueTask> action, Action<Exception> completed)
				{
					ArgumentNullException.ThrowIfNull(action);
					ArgumentNullException.ThrowIfNull(completed);
					return Volatile.Read(ref _disposed) == 0 && _channel.Writer.TryWrite(new Work(action, completed));
				}

				private async Task ProcessAsync()
				{
					await foreach(var work in _channel.Reader.ReadAllAsync())
					{
						Exception exception = null;
						try { await work.Action().ConfigureAwait(false); }
						catch(Exception error) { exception = error; }

						try { work.Completed(exception); }
						catch { }
					}
				}

				public async ValueTask DisposeAsync()
				{
					if(Interlocked.Exchange(ref _disposed, 1) != 0)
						return;

					_channel.Writer.TryComplete();
					await _runner.ConfigureAwait(false);
				}

				private sealed class Work(Func<ValueTask> action, Action<Exception> completed)
				{
					public readonly Func<ValueTask> Action = action;
					public readonly Action<Exception> Completed = completed;
				}
			}
			#endregion
		}
	}
}
