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
 * This file is part of Zongsoft.Net library.
 *
 * The Zongsoft.Net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Net library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Zongsoft.Net;

public partial class TcpClient<T>
{
	#region 嵌套类型
	//连接器拥有地址、当前连接和在途尝试；一次尝试只负责取得或清理自己的 Socket。
	private sealed class Connector : IAsyncDisposable
	{
		#region 成员字段
		private readonly TcpClient<T> _client;
		private readonly object _sync = new();
		private readonly TcpClientOptions _options;
		private readonly ConcurrentDictionary<long, TcpClientChannel<T>> _channels = new();
		private EndPoint _address;
		private TcpClientChannel<T> _channel;
		private Attempt _attempt;
		private long _generation;
		private bool _disposed;
		private long _bytesSent;
		private long _bytesReceived;
		private TaskCompletionSource _disposing;
		#endregion

		#region 构造函数
		/// <summary>创建客户端专有连接器，统一管理地址、建连和连接退场。</summary>
		/// <param name="client">连接所属客户端。</param>
		/// <param name="options">客户端设置快照。</param>
		public Connector(TcpClient<T> client, TcpClientOptions options)
		{
			_client = client;
			_options = options;
		}
		#endregion

		#region 公共属性
		/// <summary>获取或设置目标地址；更改地址会使在途建连失效并关闭原连接。</summary>
		public EndPoint Address
		{
			get
			{
				lock(_sync)
					return CloneAddress(_address);
			}
			set
			{
				var address = CloneAddress(value);
				Attempt attempt;
				TcpClientChannel<T> channel;

				lock(_sync)
				{
					ObjectDisposedException.ThrowIf(_disposed, this);
					if(Equals(_address, address))
						return;

					_address = address;
					_generation++;
					attempt = _attempt;
					channel = Interlocked.Exchange(ref _channel, null);
				}

				attempt?.Cancel();
				channel?.Abort(TcpCloseReason.Local, null);
			}
		}

		/// <summary>获取当前连接，尚未连接或已经退场时为空。</summary>
		public TcpClientChannel<T> Channel => Volatile.Read(ref _channel);
		/// <summary>获取全部连接累计完成 Socket 发送的字节数。</summary>
		public long TotalBytesSent => Interlocked.Read(ref _bytesSent) + GetBytes(_channels, true);
		/// <summary>获取全部连接累计从 Socket 接收的字节数。</summary>
		public long TotalBytesReceived => Interlocked.Read(ref _bytesReceived) + GetBytes(_channels, false);
		#endregion

		#region 连接方法
		/// <summary>显式建立连接；并发调用共用一个在途尝试。首次发起者的令牌控制该尝试，其余令牌只取消各自等待。</summary>
		/// <param name="address">可选目标地址；指定时更新 <see cref="Address" />。</param>
		/// <param name="cancellation">取消令牌。</param>
		/// <returns>已登记且开始接收的当前通道。</returns>
		public Task<TcpClientChannel<T>> ConnectAsync(EndPoint address = null, CancellationToken cancellation = default)
		{
			cancellation.ThrowIfCancellationRequested();
			if(address != null)
				this.Address = address;

			Attempt attempt;
			var start = false;

			lock(_sync)
			{
				ObjectDisposedException.ThrowIf(_disposed, this);

				if(_channel is { IsActive: true } channel)
					return Task.FromResult(channel);

				if(_attempt != null)
					attempt = _attempt;
				else
				{
					var endpoint = CloneAddress(_address) ?? throw new InvalidOperationException(Properties.Resources.Network_DestinationRequired_Message);
					attempt = _attempt = new(endpoint, _generation, cancellation, _options);
					start = true;
				}
			}

			if(!start && attempt.Generation != Volatile.Read(ref _generation))
				return ReconnectAsync(attempt, cancellation);

			if(start)
				_ = attempt.RunAsync(this);

			return cancellation.CanBeCanceled ? attempt.Completion.WaitAsync(cancellation) : attempt.Completion;

			async Task<TcpClientChannel<T>> ReconnectAsync(Attempt previous, CancellationToken token)
			{
				await previous.ObserveAsync().WaitAsync(token).ConfigureAwait(false);
				return await this.ConnectAsync(cancellation: token).ConfigureAwait(false);
			}
		}

		/// <summary>停止接收新报文，限时等待当前处理和发送排空。再次发送会重新连接，不自动重放消息。</summary>
		/// <param name="cancellation">取消本调用方的等待。</param>
		/// <returns>当前连接全部退场的结果，超时会强制关闭传输并抛出异常。</returns>
		public async ValueTask DisconnectAsync(CancellationToken cancellation = default)
		{
			cancellation.ThrowIfCancellationRequested();
			Attempt attempt;

			lock(_sync)
			{
				_generation++;
				attempt = _attempt;
				Interlocked.Exchange(ref _channel, null);
			}

			attempt?.Cancel();
			var tasks = new List<Task>();

			if(attempt != null)
				tasks.Add(attempt.ObserveAsync());

			foreach(var entry in _channels)
			{
				entry.Value.RequestShutdown();
				tasks.Add(entry.Value.WaitAsync());
			}

			try
			{
				await Task.WhenAll(tasks)
					.WaitAsync(_options.ShutdownTimeout, _options.TimeProvider, cancellation)
					.ConfigureAwait(false);
			}
			catch(TimeoutException exception)
			{
				foreach(var entry in _channels)
					entry.Value.Abort(TcpCloseReason.ShutdownTimeout, exception);

				throw;
			}
		}
		#endregion

		#region 连接注销
		/// <summary>注销已经清理的连接登记，保留累计字节数。</summary>
		/// <param name="channel">已经退场的连接。</param>
		internal void Unregister(TcpClientChannel<T> channel)
		{
			TcpConnectionMonitor.Remove(channel);
			Interlocked.CompareExchange(ref _channel, null, channel);

			if(_channels.TryRemove(channel.Identifier, out _))
			{
				Interlocked.Add(ref _bytesSent, channel.TotalBytesSent);
				Interlocked.Add(ref _bytesReceived, channel.TotalBytesReceived);
			}
		}
		#endregion

		#region 私有方法
		/// <summary>复制支持的端点，防止调用方修改已发布的目标地址。</summary>
		/// <param name="address">目标端点，允许 <see langword="null" />。</param>
		/// <returns>独立端点，或 <see langword="null" />。</returns>
		private static EndPoint CloneAddress(EndPoint address) => address switch
		{
			null => null,
			IPEndPoint endpoint => new IPEndPoint(endpoint.Address, endpoint.Port),
			DnsEndPoint endpoint => new DnsEndPoint(endpoint.Host, endpoint.Port, endpoint.AddressFamily),
			_ => throw new ArgumentException(null, nameof(address)),
		};

		/// <summary>遍历当前连接并累计指定方向的 Socket 字节数。</summary>
		/// <param name="channels">当前仍由客户端拥有的连接集合。</param>
		/// <param name="sent">为 <see langword="true" /> 时累计发送量，否则累计接收量。</param>
		/// <returns>当前连接的累计字节数；并发变化时为近似快照。</returns>
		private static long GetBytes(ConcurrentDictionary<long, TcpClientChannel<T>> channels, bool sent)
		{
			var bytes = 0L;

			foreach(var entry in channels)
				bytes += sent ? entry.Value.TotalBytesSent : entry.Value.TotalBytesReceived;

			return bytes;
		}
		#endregion

		#region 释放资源
		/// <summary>取消建连并等待所有已拥有的连接清理；并发调用共享结果。</summary>
		/// <returns>释放完成结果。</returns>
		public ValueTask DisposeAsync()
		{
			var disposing = Volatile.Read(ref _disposing);

			if(disposing == null)
			{
				var candidate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
				disposing = Interlocked.CompareExchange(ref _disposing, candidate, null) ?? candidate;

				if(ReferenceEquals(disposing, candidate))
					_ = DisposeCoreAsync(candidate);
			}

			return new(disposing.Task);

			async Task DisposeCoreAsync(TaskCompletionSource completion)
			{
				lock(_sync)
					_disposed = true;

				try
				{
					await this.DisconnectAsync().ConfigureAwait(false);
					completion.TrySetResult();
				}
				catch(Exception exception) { completion.TrySetException(exception); }
			}
		}
		#endregion

		#region 嵌套类型
		//一次建连拥有目标地址、代次、取消源和完成信号；成功后将 Socket 所有权交给通道。
		private sealed class Attempt
		{
			#region 成员字段
			private readonly EndPoint _address;
			private readonly CancellationToken _caller;
			private readonly CancellationTokenSource _deadline;
			private readonly CancellationTokenSource _cancellation;
			private readonly TaskCompletionSource<TcpClientChannel<T>> _completion;
			#endregion

			#region 构造函数
			/// <summary>建立一次建连尝试的不可变目标与取消期限。</summary>
			/// <param name="address">复制后的目标端点。</param>
			/// <param name="generation">所属客户端的地址代次。</param>
			/// <param name="caller">首次建连调用方的取消令牌。</param>
			/// <param name="options">固定的客户端设置。</param>
			public Attempt(EndPoint address, long generation, CancellationToken caller, TcpClientOptions options)
			{
				_address = address;
				this.Generation = generation;
				_caller = caller;
				_deadline = new(options.ConnectTimeout, options.TimeProvider);
				_completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
				_cancellation = CancellationTokenSource.CreateLinkedTokenSource(caller, _deadline.Token);
			}
			#endregion

			#region 公共属性
			/// <summary>获取目标地址的代次。</summary>
			public long Generation { get; }
			/// <summary>获取所有建连等待者共享的完成任务。</summary>
			public Task<TcpClientChannel<T>> Completion => _completion.Task;
			#endregion

			#region 建连流程
			/// <summary>创建连接，验证代次后发布通道，并向所有等待者报告结果。</summary>
			/// <param name="owner">拥有本次尝试和最终通道的连接器。</param>
			/// <returns>建连及失败清理任务；建连结果通过 <see cref="Completion" /> 发布。</returns>
			public async Task RunAsync(Connector owner)
			{
				Socket socket = null;
				TcpClientChannel<T> channel = null;
				Exception error = null;

				try
				{
					socket = _address.AddressFamily == AddressFamily.Unspecified ?
						new Socket(SocketType.Stream, ProtocolType.Tcp) :
						new Socket(_address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

					await socket.ConnectAsync(_address, _cancellation.Token).ConfigureAwait(false);
					_cancellation.Token.ThrowIfCancellationRequested();

					var transport = new TcpTransport(socket, owner._options);
					socket = null;
					channel = new(owner._client, transport, owner._client.Packetizer, owner._options);

					lock(owner._sync)
					{
						_cancellation.Token.ThrowIfCancellationRequested();
						if(owner._disposed || this.Generation != owner._generation)
							throw new OperationCanceledException();

						owner._channels.TryAdd(channel.Identifier, channel);
						Volatile.Write(ref owner._channel, channel);
					}

					TcpConnectionMonitor.Add(channel);
					channel.Start();
				}
				catch(Exception exception)
				{
					if(_deadline.IsCancellationRequested && !_caller.IsCancellationRequested)
						error = new TimeoutException(Properties.Resources.Network_Timeout_Message, exception);
					else if(_cancellation.IsCancellationRequested)
						error = new OperationCanceledException(null, exception, _cancellation.Token);
					else
						error = exception;

					if(channel != null)
					{
						try { await channel.DisposeAsync().ConfigureAwait(false); }
						catch(Exception cleanup) { error = new AggregateException(error, cleanup); }
					}
				}
				finally
				{
					socket?.Dispose();

					lock(owner._sync)
					{
						if(ReferenceEquals(owner._attempt, this))
							owner._attempt = null;
					}

					_cancellation.Dispose();
					_deadline.Dispose();
				}

				if(error == null)
					_completion.TrySetResult(channel);
				else if(error is OperationCanceledException canceled)
					_completion.TrySetCanceled(canceled.CancellationToken);
				else
				{
					_completion.TrySetException(error);
					_ = this.Completion.Exception;
				}
			}
			#endregion

			#region 退出协调
			/// <summary>取消本次尝试，允许与取消源释放竞争。</summary>
			public void Cancel()
			{
				try { _cancellation.Cancel(); }
				catch(ObjectDisposedException) { }
			}

			/// <summary>等待本次尝试退场，失败仍由建连调用方观察。</summary>
			/// <returns>尝试完成的等待任务，不传播建连故障。</returns>
			public async Task ObserveAsync()
			{
				//断开与地址切换只等待尝试退场，建连调用方仍通过 Completion 观察原始结果。
				try { await this.Completion.ConfigureAwait(false); }
				catch { }
			}
			#endregion
		}
		#endregion
	}
	#endregion
}
