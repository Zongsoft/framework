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
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Net;

public abstract partial class TcpChannelBase<T>
{
	#region 发送报文
	/// <summary>编码一帧并等待本次 <c>Flush</c> 的背压。发送前后均不取得报文所有权。</summary>
	/// <param name="package">发送完成前必须保持稳定的报文。</param>
	/// <param name="cancellation">发送取消令牌；写入后取消会终止连接，不能据此判断对端是否收到。</param>
	/// <returns>本地提交结果。</returns>
	/// <exception cref="TcpException">容量不足时错误码为 <see cref="TcpError.SendRejected" />；非法帧或关闭连接返回相应错误码。</exception>
	public ValueTask SendAsync(T package, CancellationToken cancellation = default) => _sender.SendAsync(package, cancellation);
	#endregion

	#region 嵌套类型
	//独占输出写入权，统一维护接纳额度、提交进度和发送完成信号。
	private sealed class Sender : IDisposable
	{
		#region 成员字段
		private readonly TcpChannelBase<T> _channel;
		private readonly SemaphoreSlim _writer = new(1, 1);
		private readonly FrameWriter _encoder;
		private readonly object _sync = new();
		private TaskCompletionSource _completion;
		private int _count;
		private long _reserved;
		private long _committed;
		private long _frames;
		private long _waits;
		private long _lastProgress;
		private long _sentBytes;
		private bool _stopped;
		#endregion

		#region 构造函数
		public Sender(TcpChannelBase<T> channel)
		{
			_channel = channel;
			_encoder = new(channel._transport.Output);
			_lastProgress = channel._options.TimeProvider.GetTimestamp();
		}
		#endregion

		#region 公共属性
		/// <summary>获取已经接纳且尚未退场的发送操作数。</summary>
		public int Count => Volatile.Read(ref _count);
		/// <summary>获取成功编码并完成 <c>Flush</c> 的报文数。</summary>
		public long Frames => Interlocked.Read(ref _frames);
		/// <summary>获取等待唯一写入权的次数。</summary>
		public long Waits => Interlocked.Read(ref _waits);
		/// <summary>获取预留及尚未完成 Socket 发送的字节数。</summary>
		public long Bytes
		{
			get
			{
				lock(_sync)
					return _channel.IsClosed && _count == 0 ? 0 : Math.Max(0, _reserved + _committed - _channel._transport.BytesSent);
			}
		}
		#endregion

		#region 发送方法
		/// <summary>预留容量，独占编码和提交输出，并在退场时归还操作额度。</summary>
		/// <param name="package">发送完成前保持稳定的报文。</param>
		/// <param name="cancellation">本次发送的取消令牌。</param>
		/// <returns>本地提交操作。</returns>
		public async ValueTask SendAsync(T package, CancellationToken cancellation)
		{
			cancellation.ThrowIfCancellationRequested();
			var options = _channel._options;
			var length = _channel._packetizer.GetLength(in package);

			if(length < 0 || length > options.MaximumFrameLength)
				throw new TcpException(TcpError.InvalidFrame);

			Reserve();
			var acquired = false;
			var committed = false;
			var encoding = false;

			try
			{
				if(!_writer.Wait(0, cancellation))
				{
					if(options.EnableDiagnostics)
						Interlocked.Increment(ref _waits);

					await _writer.WaitAsync(cancellation).ConfigureAwait(false);
				}

				acquired = true;
				cancellation.ThrowIfCancellationRequested();
				if(_channel.IsClosing)
					throw new TcpException(TcpError.ConnectionClosed);

				_encoder.Reset(length);
				encoding = true;
				_channel._packetizer.Pack(_encoder, in package);

				if(_encoder.Written != length)
					throw new TcpException(TcpError.InvalidFrame);

				lock(_sync)
				{
					_reserved -= length;
					_committed += length;
				}

				committed = true;
				var result = await _channel._transport.Output.FlushAsync(cancellation).ConfigureAwait(false);
				if(result.IsCanceled)
					throw new OperationCanceledException(cancellation.IsCancellationRequested ? cancellation : _channel._lifetime.Token);
				if(result.IsCompleted)
					throw new TcpException(TcpError.ConnectionClosed);

				Interlocked.Increment(ref _frames);
			}
			catch(Exception exception)
			{
				if(committed || encoding && _encoder.Written > 0)
					_channel.Abort(GetCloseReason(exception), exception);

				throw;
			}
			finally
			{
				if(acquired)
					_writer.Release();

				Release();
			}

			void Reserve()
			{
				lock(_sync)
				{
					if(_stopped || _channel.IsClosing)
						throw new TcpException(TcpError.ConnectionClosed);

					var outstanding = Math.Max(0, _committed - _channel._transport.BytesSent);
					if(_count >= options.MaximumPendingSends || length > options.MaximumPendingSendBytes - _reserved - outstanding)
						throw new TcpException(TcpError.SendRejected);

					if(_count == 0 && outstanding == 0)
						Volatile.Write(ref _lastProgress, options.TimeProvider.GetTimestamp());

					_count++;
					_reserved += length;
				}
			}

			void Release()
			{
				TaskCompletionSource completion = null;

				lock(_sync)
				{
					if(!committed)
						_reserved -= length;

					if(--_count == 0 && _stopped)
						completion = _completion;
				}

				completion?.TrySetResult();
			}
		}
		#endregion

		#region 进度检查
		/// <summary>检查已接纳发送是否持续缺少 Socket 发送进展。</summary>
		/// <param name="now">当前单调时间戳。</param>
		/// <param name="delay">相对于发送期限的检测延迟。</param>
		/// <returns>发送是否已经超时。</returns>
		public bool CheckTimeout(long now, out TimeSpan delay)
		{
			var sent = _channel._transport.BytesSent;
			if(sent != _sentBytes)
			{
				_sentBytes = sent;
				Volatile.Write(ref _lastProgress, now);
			}

			delay = TimeSpan.Zero;
			if(this.Count == 0 && this.Bytes == 0)
				return false;

			var options = _channel._options;
			delay = options.TimeProvider.GetElapsedTime(Volatile.Read(ref _lastProgress), now) - options.SendTimeout;
			return delay >= TimeSpan.Zero;
		}
		#endregion

		#region 结束方法
		/// <summary>禁止新发送并等待已接纳的发送调用退场。</summary>
		/// <returns>所有已接纳发送的共享完成任务。</returns>
		public Task Stop()
		{
			lock(_sync)
			{
				_stopped = true;
				if(_count == 0)
					return Task.CompletedTask;

				_completion ??= new(TaskCreationOptions.RunContinuationsAsynchronously);
				return _completion.Task;
			}
		}

		/// <summary>限时等待已接纳发送及其提交的字节完成 Socket 发送。</summary>
		/// <returns>发送排空操作。</returns>
		public async Task DrainAsync()
		{
			var options = _channel._options;
			using var deadline = new CancellationTokenSource(options.ShutdownTimeout, options.TimeProvider);

			try
			{
				await this.Stop().WaitAsync(deadline.Token).ConfigureAwait(false);
				var target = Interlocked.Read(ref _committed);

				while(_channel._transport.BytesSent < target)
				{
					deadline.Token.ThrowIfCancellationRequested();
					await Task.Delay(TimeSpan.FromMilliseconds(10), options.TimeProvider, deadline.Token).ConfigureAwait(false);
				}
			}
			catch(OperationCanceledException) when(deadline.IsCancellationRequested)
			{
				var exception = new TimeoutException(Properties.Resources.Network_Timeout_Message);
				_channel.RecordFailure(TcpCloseReason.ShutdownTimeout, exception);
				throw exception;
			}
		}

		/// <summary>取得唯一写入权后完成输出管道。</summary>
		/// <param name="exception">通道结束时记录的首个故障。</param>
		/// <returns>输出管道清理操作。</returns>
		public async ValueTask CompleteAsync(Exception exception)
		{
			await _writer.WaitAsync().ConfigureAwait(false);

			try { await _channel._transport.Output.CompleteAsync(exception).ConfigureAwait(false); }
			finally { _writer.Release(); }
		}

		/// <summary>在全部发送退场后释放写入协调资源。</summary>
		public void Dispose() => _writer.Dispose();
		#endregion

		#region 嵌套类型
		private sealed class FrameWriter(IBufferWriter<byte> writer) : IBufferWriter<byte>
		{
			private long _length;
			private int _available;
			public long Written { get; private set; }

			public void Reset(long length)
			{
				_length = length;
				_available = 0;
				this.Written = 0;
			}

			public void Advance(int count)
			{
				if(count < 0 || count > _available)
					throw new TcpException(TcpError.InvalidFrame);

				writer.Advance(count);
				this.Written += count;
				_available = 0;
			}

			public Memory<byte> GetMemory(int sizeHint = 0)
			{
				var remaining = this.GetRemaining(sizeHint);
				var memory = writer.GetMemory(sizeHint);
				_available = (int)Math.Min(memory.Length, remaining);

				//零提示仍返回非空缓冲，但 Advance 只能提交声明长度以内的字节
				return memory[..Math.Max(1, _available)];
			}

			public Span<byte> GetSpan(int sizeHint = 0)
			{
				var remaining = this.GetRemaining(sizeHint);
				var span = writer.GetSpan(sizeHint);
				_available = (int)Math.Min(span.Length, remaining);

				return span[..Math.Max(1, _available)];
			}

			private long GetRemaining(int sizeHint)
			{
				var remaining = _length - this.Written;
				if(sizeHint < 0 || sizeHint > remaining)
					throw new TcpException(TcpError.InvalidFrame);

				return remaining;
			}
		}
		#endregion
	}
	#endregion
}
