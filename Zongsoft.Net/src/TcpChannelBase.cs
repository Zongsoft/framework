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

using Zongsoft.Communication;

namespace Zongsoft.Net
{
	public abstract class TcpChannelBase<T> : ChannelBase, ISender, ISender<T>
	{
		#region 私有变量
		private bool _initialized;
		private readonly SemaphoreSlim _singleWriter;
		#endregion

		#region 成员字段
		private IDuplexPipe _transport;
		#endregion

		#region 构造函数
		protected TcpChannelBase(IDuplexPipe transport, EndPoint address)
		{
			_transport = transport;
			this.Address = address;
			_singleWriter = new SemaphoreSlim(1);
		}
		#endregion

		#region 公共属性
		public EndPoint Address { get; }
		public long TotalBytesSent => _transport is IMeasuredDuplexPipe transport ? transport.TotalBytesSent : 0;
		public long TotalBytesReceived => _transport is IMeasuredDuplexPipe transport ? transport.TotalBytesReceived : 0;
		protected IDuplexPipe Transport => _transport;
		#endregion

		#region 初始化器
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private void Initialize(in T package)
		{
			if(_initialized)
				return;

			lock(this)
			{
				if(Volatile.Read(ref _initialized))
					return;

				if(this.OnInitialize(package))
					Volatile.Write(ref _initialized, true);
			}
		}

		protected virtual bool OnInitialize(in T package) => true;
		#endregion

		#region 发送数据
		public async ValueTask SendAsync(T package, CancellationToken cancellation = default)
		{
			if(!_singleWriter.Wait(0, cancellation))
			{
				await this.SendSlowAsync(package, cancellation);
				return;
			}

			bool release = true;

			try
			{
				var writer = _transport?.Output ?? throw new ObjectDisposedException(ToString());
				this.Pack(writer, package);
				await writer.FlushAsync(cancellation);
			}
			finally
			{
				if(release)
					_singleWriter.Release();
			}
		}

		private async ValueTask SendSlowAsync(T package, CancellationToken cancellation)
		{
			await _singleWriter.WaitAsync(cancellation);

			try
			{
				var writer = _transport?.Output ?? throw new ObjectDisposedException(this.GetType().FullName);
				this.Pack(writer, package);
			}
			finally
			{
				_singleWriter.Release();
			}
		}

		public ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellation = default)
		{
			async ValueTask AwaitFlushAndRelease(ValueTask<FlushResult> flush)
			{
				try { await flush; }
				finally { _singleWriter.Release(); }
			}

			if(!_singleWriter.Wait(0, cancellation))
				return this.SendSlowAsync(data, cancellation);

			bool release = true;

			try
			{
				var writer = _transport?.Output ?? throw new ObjectDisposedException(this.GetType().FullName);
				var result = writer.WriteAsync(data, cancellation);

				if(result.IsCompletedSuccessfully)
					return default;

				release = false;
				return AwaitFlushAndRelease(result);
			}
			finally
			{
				if(release)
					_singleWriter.Release();
			}
		}

		private async ValueTask SendSlowAsync(ReadOnlyMemory<byte> data, CancellationToken cancellation)
		{
			await _singleWriter.WaitAsync();

			try
			{
				var writer = _transport?.Output ?? throw new ObjectDisposedException(this.GetType().FullName);
				await writer.WriteAsync(data, cancellation);
			}
			finally
			{
				_singleWriter.Release();
			}
		}

		protected virtual ValueTask<FlushResult> OnSendAsync(PipeWriter writer, ReadOnlyMemory<byte> data, CancellationToken cancellation) => writer.WriteAsync(data, cancellation);
		#endregion

		#region 协议解析
		protected abstract void Pack(PipeWriter writer, in T package);
		protected abstract bool Unpack(ref ReadOnlySequence<byte> data, out T package);
		#endregion

		#region 接收消息
		protected async Task ReceiveAsync(CancellationToken cancellation = default)
		{
			var reader = _transport?.Input ?? throw new ObjectDisposedException(ToString());

			try
			{
				//开启异步接收
				this.OnReceiving();

				bool unpacked = false;

				while(!cancellation.IsCancellationRequested)
				{
					if(!(unpacked && reader.TryRead(out var result)))
						result = await reader.ReadAsync(cancellation);

					if(result.IsCanceled)
						break;

					var buffer = result.Buffer;

					unpacked = false;
					while(this.Unpack(ref buffer, out var package))
					{
						unpacked = true;

						if(!_initialized)
							this.Initialize(package);

						await this.OnReceiveAsync(in package);
					}

					reader.AdvanceTo(buffer.Start, buffer.End);

					if(!unpacked && result.IsCompleted)
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
				try { await this.CloseAsync(cancellation); } catch { }
			}
		}

		protected abstract ValueTask OnReceiveAsync(in T package);
		protected virtual void OnReceiving() { }
		#endregion

		#region 关闭方法
		protected override async ValueTask OnCloseAsync(CancellationToken cancellation)
		{
			Volatile.Write(ref _initialized, false);
			var transport = Interlocked.Exchange(ref _transport, null);

			if(transport != null)
			{
				try { transport.Input.Complete(); } catch { }
				try { transport.Input.CancelPendingRead(); } catch { }
				try { transport.Output.Complete(); } catch { }
				try { transport.Output.CancelPendingFlush(); } catch { }

				switch(transport)
				{
					case IAsyncDisposable disposable:
						await disposable.DisposeAsync();
						break;
					case IDisposable disposable:
						disposable.Dispose();
						break;
				}
			}

			try { _singleWriter.Dispose(); } catch { }
		}
		#endregion
	}
}
