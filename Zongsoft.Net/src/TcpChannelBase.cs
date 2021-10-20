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
using System.IO.Pipelines;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

using Pipelines.Sockets.Unofficial;

namespace Zongsoft.Net
{
	public abstract class TcpChannelBase<T> : IDisposable
	{
		#region 私有变量
		private readonly SemaphoreSlim _singleWriter = new SemaphoreSlim(1);
		#endregion

		#region 成员字段
		private IDuplexPipe _transport;
		#endregion

		#region 构造函数
		protected TcpChannelBase(IDuplexPipe transport, EndPoint address)
		{
			_transport = transport;
			this.Address = address;
		}
		#endregion

		#region 公共属性
		public EndPoint Address { get; }
		public bool IsClosed { get => _transport == null; }
		public long TotalBytesSent { get => _transport is IMeasuredDuplexPipe transport ? transport.TotalBytesSent : 0; }
		public long TotalBytesReceived { get => _transport is IMeasuredDuplexPipe transport ? transport.TotalBytesReceived : 0; }
		protected IDuplexPipe Transport { get => _transport; }
		#endregion

		#region 发送数据
		protected ValueTask WriteAsync(T package, CancellationToken cancellation = default)
		{
			async ValueTask AwaitFlushAndRelease(ValueTask flush, PipeWriter writer)
			{
				try { await flush; }
				finally { _singleWriter.Release(); }
			}

			// try to get the conch; if not, switch to async
			if(!_singleWriter.Wait(0))
				return WriteAsyncSlowPath(package, cancellation);

			bool release = true;

			try
			{
				var writer = _transport?.Output ?? throw new ObjectDisposedException(ToString());

				//this.WriteHeader(writer, payload.Length);
				//var result = writer.WriteAsync(payload, cancellation); // includes a flush

				var result = this.PackAsync(writer, package, cancellation);

				if(result.IsCompletedSuccessfully)
				{
					return default; // sync fast path
				}

				release = false;
				return AwaitFlushAndRelease(result, writer);
			}
			finally
			{
				if(release)
					_singleWriter.Release();
			}
		}

		private async ValueTask WriteAsyncSlowPath(T package, CancellationToken cancellation)
		{
			await _singleWriter.WaitAsync();

			try
			{
				var writer = _transport?.Output ?? throw new ObjectDisposedException(ToString());

				//WriteHeader(writer, payload.Length);
				//await writer.WriteAsync(payload, cancellation);

				await this.PackAsync(writer, package, cancellation);
			}
			finally
			{
				_singleWriter.Release();
			}
		}
		#endregion

		#region 协议解析
		protected abstract ValueTask PackAsync(PipeWriter writer, in T package, CancellationToken cancellation);
		protected abstract bool Unpack(ref ReadOnlySequence<byte> data, out T package);
		#endregion

		#region 接收消息
		protected async Task StartReceiveLoopAsync(CancellationToken cancellationToken = default)
		{
			var reader = _transport?.Input ?? throw new ObjectDisposedException(ToString());

			try
			{
				await this.OnStartReceiveLoopAsync();
				bool makingProgress = false;

				while(!cancellationToken.IsCancellationRequested)
				{
					if(!(makingProgress && reader.TryRead(out var readResult)))
						readResult = await reader.ReadAsync(cancellationToken);

					if(readResult.IsCanceled)
						break;

					var buffer = readResult.Buffer;

					makingProgress = false;
					while(this.Unpack(ref buffer, out var payload))
					{
						makingProgress = true;
						await this.OnReceiveAsync(in payload);
					}

					// record that we comsumed up to the (now updated) buffer.Start,
					// and tried to look at everything - hence buffer.End
					reader.AdvanceTo(buffer.Start, buffer.End);

					// exit the loop electively, or because we've consumed everything
					// that we can usefully consume
					if(!makingProgress && readResult.IsCompleted)
						break;
				}

				try { reader.Complete(); } catch { }
			}
			catch(Exception ex)
			{
				try { reader.Complete(ex); } catch { }
			}
			finally
			{
				try { await this.OnEndReceiveLoopAsync(); } catch { }
			}
		}

		protected abstract ValueTask OnReceiveAsync(in T payload);

		protected virtual ValueTask OnStartReceiveLoopAsync() => default;
		protected virtual ValueTask OnEndReceiveLoopAsync() => default;
		#endregion

		#region 关闭方法
		public void Dispose() => this.Close();
		public void Close(Exception exception = null)
		{
			var transport = Interlocked.Exchange(ref _transport, null);

			if(transport != null)
			{
				try { transport.Input.Complete(exception); } catch { }
				try { transport.Input.CancelPendingRead(); } catch { }
				try { transport.Output.Complete(exception); } catch { }
				try { transport.Output.CancelPendingFlush(); } catch { }

				if(transport is IDisposable disposable)
					try { disposable.Dispose(); } catch { }
			}

			try { _singleWriter.Dispose(); } catch { }
		}
		#endregion
	}
}
