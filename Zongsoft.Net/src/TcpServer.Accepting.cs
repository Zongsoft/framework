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

namespace Zongsoft.Net;

public partial class TcpServer<T>
{
	#region 成员字段
	private Socket _listener;
	private CancellationTokenSource _acceptCancellation;
	private Task _accepting;
	private Exception _acceptError;
	private int _admitted;
	#endregion

	#region 服务启停
	/// <summary>绑定官方 Socket 并启动唯一受理任务，绑定失败直接传播。</summary>
	/// <param name="args">生命周期操作参数。</param>
	/// <param name="cancellation">取消令牌；仅取消等待的入口不影响已经开始的资源清理。</param>
	/// <returns>监听初始化结果。</returns>
	protected override Task OnStartAsync(string[] args, CancellationToken cancellation)
	{
		cancellation.ThrowIfCancellationRequested();
		var address = this.Address;

		if(args is { Length: > 0 })
		{
			if(args.Length == 1 && int.TryParse(args[0], out var port))
				address = new(address.Address, port);
			else if(args.Length == 2 && IPAddress.TryParse(args[0], out var ip) && int.TryParse(args[1], out port))
				address = new(ip, port);
			else
				throw new ArgumentException(null, nameof(args));
		}

		var listener = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

		try
		{
			if(address.AddressFamily == AddressFamily.InterNetworkV6)
				listener.DualMode = false;

			listener.Bind(address);
			listener.Listen(_options.Backlog);

			cancellation.ThrowIfCancellationRequested();

			Volatile.Write(ref _address, (IPEndPoint)listener.LocalEndPoint);
			_acceptError = null;

			_broadcaster.Start();

			_acceptCancellation = new();
			_listener = listener;
			_accepting = AcceptAsync(listener, _acceptCancellation.Token);

			return Task.CompletedTask;
		}
		catch
		{
			listener.Dispose();
			throw;
		}

		async Task AcceptAsync(Socket listener, CancellationToken cancellation)
		{
			try
			{
				while(!cancellation.IsCancellationRequested)
				{
					var socket = await listener.AcceptAsync(cancellation).ConfigureAwait(false);

					if(cancellation.IsCancellationRequested)
					{
						socket.Dispose();
						break;
					}

					if(Interlocked.Increment(ref _admitted) > _options.MaximumConnections)
					{
						Interlocked.Decrement(ref _admitted);
						Interlocked.Increment(ref _rejected);
						socket.Dispose();
						continue;
					}

					TcpServerChannel<T> channel = null;

					try
					{
						var transport = new TcpTransport(socket, _options);
						channel = new(this, transport, this.Packetizer, _options);

						if(!this.Channels.Add(channel))
							throw new InvalidOperationException();

						Interlocked.Increment(ref _accepted);
						TcpConnectionMonitor.Add(channel);

						channel.Start();
					}
					catch(Exception exception)
					{
						if(channel == null)
						{
							socket.Dispose();
							Interlocked.Decrement(ref _admitted);
						}
						else
							await channel.DisposeAsync().ConfigureAwait(false);

						Diagnostics.Logging.GetLogging(this).Error(exception);
					}
				}
			}
			catch(OperationCanceledException) when(cancellation.IsCancellationRequested) { }
			catch(ObjectDisposedException) when(cancellation.IsCancellationRequested) { }
			catch(SocketException) when(cancellation.IsCancellationRequested) { }
			catch(Exception exception) { _acceptError = exception; }
		}
	}

	/// <summary>停止受理及广播接纳，限时等待通道和广播退场。</summary>
	/// <param name="args">生命周期操作参数。</param>
	/// <param name="cancellation">取消令牌；仅取消等待的入口不影响已经开始的资源清理。</param>
	/// <returns>全部运行资源的退出结果；超时强制关闭传输并保留尚在运行的处理器登记。</returns>
	protected override async Task OnStopAsync(string[] args, CancellationToken cancellation)
	{
		var broadcasting = _broadcaster.Stop();

		var source = Interlocked.Exchange(ref _acceptCancellation, null);
		if(source != null)
			await source.CancelAsync().ConfigureAwait(false);

		Interlocked.Exchange(ref _listener, null)?.Dispose();

		var accepting = Interlocked.Exchange(ref _accepting, null);
		if(accepting != null)
			await accepting.ConfigureAwait(false);

		source?.Dispose();
		var tasks = new List<Task>(this.Channels.Count + 1) { broadcasting };

		foreach(var channel in this.Channels)
		{
			channel.RequestShutdown();
			tasks.Add(channel.WaitAsync());
		}

		try
		{
			await Task.WhenAll(tasks)
				.WaitAsync(_options.ShutdownTimeout, _options.TimeProvider, cancellation)
				.ConfigureAwait(false);
		}
		catch(TimeoutException exception)
		{
			foreach(var channel in this.Channels)
				channel.Abort(TcpCloseReason.ShutdownTimeout, exception);

			throw;
		}

		var error = Interlocked.Exchange(ref _acceptError, null);

		if(error != null)
			throw new TcpException(TcpError.ConnectionClosed, null, error);
	}
	#endregion
}
