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
using System.Net.Sockets;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace Zongsoft.Net;

public abstract partial class TcpChannelBase<T>
{
	#region 嵌套类型
	//独占输入缓冲及分帧进度，处理器退场之前不会推进或释放借用内存。
	private sealed class Receiver(TcpChannelBase<T> channel)
	{
		#region 成员字段
		private readonly TcpChannelBase<T> _channel = channel;
		private long _lastReceived = channel._options.TimeProvider.GetTimestamp();
		private long _receivedBytes;
		private long _assemblyStarted;
		private int _assembling;
		private int _stopRequested;
		private long _frames;
		private long _reads;
		private long _budgetExhaustions;
		private long _maximumFrames;
		#endregion

		#region 公共属性
		/// <summary>获取一个值，指示接收流程仍可开始处理新报文。</summary>
		public bool IsActive => !_channel.IsClosing && Volatile.Read(ref _stopRequested) == 0;
		/// <summary>获取成功解码的报文数。</summary>
		public long Frames => Interlocked.Read(ref _frames);
		/// <summary>获取完成处理的读取次数。</summary>
		public long Reads => Interlocked.Read(ref _reads);
		/// <summary>获取同步处理预算耗尽的次数。</summary>
		public long BudgetExhaustions => Interlocked.Read(ref _budgetExhaustions);
		/// <summary>获取单次读取中处理的最大帧数。</summary>
		public long MaximumFrames => Interlocked.Read(ref _maximumFrames);
		#endregion

		#region 接收方法
		/// <summary>停止开始新的报文处理，唤醒正在等待数据的读取。</summary>
		public void RequestShutdown()
		{
			Volatile.Write(ref _stopRequested, 1);
			_channel._transport.Input.CancelPendingRead();
		}

		/// <summary>逐帧处理借用报文，在业务处理完成后释放并推进管道。</summary>
		/// <returns>对端正常结束输入时返回 <see langword="true" />；本地停止接收时返回 <see langword="false" />。</returns>
		public async Task<bool> RunAsync()
		{
			var reader = _channel._transport.Input;
			var options = _channel._options;
			var packetizer = _channel._packetizer;
			var cancellation = _channel._lifetime.Token;

			while(this.IsActive)
			{
				ReadResult result;

				try { result = await reader.ReadAsync(cancellation).ConfigureAwait(false); }
				catch(SocketException exception) when(cancellation.IsCancellationRequested && exception.SocketErrorCode == SocketError.Shutdown)
				{
					//主动关闭 Socket 可能先以 Shutdown 结束读取；此处不包含报文处理和资源释放。
					return false;
				}

				if(await ProcessAsync(result).ConfigureAwait(false))
					return true;

				if(result.IsCanceled)
					break;
			}

			return false;

			//一次读取拥有缓冲，逐帧处理完成后统一推进；预算耗尽时不将剩余数据标为已检查。
			async ValueTask<bool> ProcessAsync(ReadResult result)
			{
				var buffer = result.Buffer;
				var examined = buffer.End;
				var frames = 0;
				var bytes = 0L;
				var readFrames = 0L;
				var yield = false;

				try
				{
					if(result.IsCanceled)
						return false;

					while(!buffer.IsEmpty && this.IsActive)
					{
						var length = buffer.Length;

						if(!TryUnpack(ref buffer, result.IsCompleted, out var package))
						{
							examined = buffer.End;
							break;
						}

						examined = buffer.Start;

						try
						{
							var consumed = length - buffer.Length;
							if(consumed <= 0 || consumed > options.MaximumFrameLength)
								throw new TcpException(TcpError.InvalidFrame);

							Volatile.Write(ref _assembling, 0);
							Interlocked.Increment(ref _frames);
							readFrames++;

							if(await DispatchAsync(package).ConfigureAwait(false))
							{
								frames = 0;
								bytes = 0;
							}

							frames++;
							bytes += consumed;
						}
						finally { packetizer.Release(in package); }

						examined = buffer.Start;
						if(frames >= options.ReceiveFrameBudget || bytes >= options.ReceiveByteBudget)
						{
							yield = !buffer.IsEmpty;

							if(options.EnableDiagnostics)
								Interlocked.Increment(ref _budgetExhaustions);

							break;
						}
					}
				}
				catch
				{
					//结束路径不能回退已检查位置，否则推进异常会掩盖原始报文故障。
					examined = result.Buffer.End;
					throw;
				}
				finally
				{
					reader.AdvanceTo(buffer.Start, examined);

					if(options.EnableDiagnostics)
					{
						Interlocked.Increment(ref _reads);
						Interlocked.Exchange(ref _maximumFrames, Math.Max(_maximumFrames, readFrames));
					}
				}

				if(yield)
					await Task.Yield();

				return result.IsCompleted && buffer.IsEmpty;

				bool TryUnpack(ref ReadOnlySequence<byte> data, bool completed, out T package)
				{
					var length = data.Length;
					if(packetizer.Unpack(ref data, out package))
						return true;

					if(data.Length != length || length > options.MaximumBufferedReceiveBytes)
						throw new TcpException(TcpError.InvalidFrame);
					if(completed)
						throw new TcpException(TcpError.IncompleteFrame);

					if(Volatile.Read(ref _assembling) == 0)
					{
						Volatile.Write(ref _assemblyStarted, options.TimeProvider.GetTimestamp());
						Volatile.Write(ref _assembling, 1);
					}

					return false;
				}

				//返回本次处理是否异步挂起，供调用方重置连续同步处理预算。
				async ValueTask<bool> DispatchAsync(T package)
				{
					try
					{
						var handling = _channel.OnReceiveAsync(new(_channel, package), cancellation);
						var asynchronous = !handling.IsCompletedSuccessfully;
						await handling.ConfigureAwait(false);

						return asynchronous;
					}
					catch(OperationCanceledException) when(cancellation.IsCancellationRequested) { throw; }
					catch(Exception exception)
					{
						_channel.RecordFailure(TcpCloseReason.HandlerError, exception);
						throw;
					}
				}
			}
		}
		#endregion

		#region 超时检查
		/// <summary>根据真实接收进度和首个残帧时间检查空闲及组包期限。</summary>
		/// <param name="now">当前单调时间戳。</param>
		/// <param name="delay">相对于相应期限的检测延迟。</param>
		/// <returns>超时原因；未超时返回 <see cref="TcpCloseReason.None" />。</returns>
		public TcpCloseReason CheckTimeout(long now, out TimeSpan delay)
		{
			var received = _channel._transport.BytesReceived;
			if(received != _receivedBytes)
			{
				_receivedBytes = received;
				_lastReceived = now;
			}

			var options = _channel._options;
			var clock = options.TimeProvider;
			delay = clock.GetElapsedTime(_lastReceived, now) - options.IdleTimeout;

			if(options.IdleTimeout > TimeSpan.Zero && delay >= TimeSpan.Zero)
				return TcpCloseReason.IdleTimeout;

			if(Volatile.Read(ref _assembling) != 0)
			{
				delay = clock.GetElapsedTime(Volatile.Read(ref _assemblyStarted), now) - options.FrameTimeout;
				if(delay >= TimeSpan.Zero)
					return TcpCloseReason.FrameTimeout;
			}

			delay = TimeSpan.Zero;
			return TcpCloseReason.None;
		}
		#endregion
	}
	#endregion
}
