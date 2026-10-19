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
	private Acceptor _acceptor;
	private Task _shutdown = Task.CompletedTask;
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
			_broadcaster.Start();

			_acceptor = new(this, listener);
			_acceptor.Start();
			return Task.CompletedTask;
		}
		catch
		{
			listener.Dispose();
			throw;
		}
	}

	/// <summary>以同一期限等待受理、通道最终通知及广播退场。</summary>
	/// <param name="args">生命周期操作参数。</param>
	/// <param name="cancellation">取消令牌；仅取消等待的入口不影响已经开始的资源清理。</param>
	/// <returns>全部运行资源的退出结果；超时强制关闭传输，尚未完成的受理与通道仍由服务持有。</returns>
	protected override async Task OnStopAsync(string[] args, CancellationToken cancellation)
	{
		var broadcasting = _broadcaster.Stop();
		var acceptor = _acceptor;
		var accepting = acceptor?.StopAsync() ?? Task.CompletedTask;
		var previous = _shutdown;
		var stopping = _shutdown = StopAsync();

		try
		{
			await stopping.WaitAsync(_options.ShutdownTimeout, _options.TimeProvider, cancellation).ConfigureAwait(false);
		}
		catch(TimeoutException exception)
		{
			foreach(var channel in this.Channels.GetPending())
				channel.Abort(TcpCloseReason.ShutdownTimeout, exception);

			_ = ObserveAsync(stopping);
			throw;
		}
		finally
		{
			//等待超时不等于受理已经退场；重试必须继续等待同一个受理任务。
			if(accepting.IsCompleted && ReferenceEquals(_acceptor, acceptor))
				_acceptor = null;

			if(stopping.IsCompleted)
				_shutdown = Task.CompletedTask;
		}

		async Task StopAsync()
		{
			await Task.Yield();

			//重试先等待前次清理退场，避免迟到的清理触及重启后的连接。
			try { await previous.ConfigureAwait(false); }
			catch { }

			try { await accepting.ConfigureAwait(false); }
			catch { } //受理故障仍交给下方的 WhenAll 汇总，不能跳过通道清理。

			var channels = this.Channels.GetPending();
			var tasks = new List<Task>(channels.Count + 2) { broadcasting, accepting };

			foreach(var channel in channels)
				tasks.Add(channel.ShutdownAsync());

			await Task.WhenAll(tasks).ConfigureAwait(false);
		}

		static async Task ObserveAsync(Task task)
		{
			//超时后继续清理；通道保留自己的故障，受理登记保留到实际退出。
			try { await task.ConfigureAwait(false); }
			catch { }
		}
	}
	#endregion

	#region 嵌套类型
	//每次启动拥有独立的监听 Socket、取消源及受理任务，不随停止等待超时而丢失。
	private sealed class Acceptor(TcpServer<T> server, Socket listener)
	{
		private readonly CancellationTokenSource _cancellation = new();
		private Task _accepting = Task.CompletedTask;
		private Task _stopping;
		private Exception _error;

		public void Start()
		{
			_accepting = AcceptAsync(_cancellation.Token);

			async Task AcceptAsync(CancellationToken cancellation)
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

						if(Interlocked.Increment(ref server._admitted) > server._options.MaximumConnections)
						{
							Interlocked.Decrement(ref server._admitted);
							Interlocked.Increment(ref server._rejected);
							socket.Dispose();
							TcpDiagnostics.Reject(true, "accept");
							continue;
						}

						await InitializeAsync(socket, cancellation).ConfigureAwait(false);
					}
				}
				catch(OperationCanceledException) when(cancellation.IsCancellationRequested) { }
				catch(ObjectDisposedException) when(cancellation.IsCancellationRequested) { }
				catch(SocketException) when(cancellation.IsCancellationRequested) { }
				catch(Exception exception)
				{
					_error = exception;
					TcpDiagnostics.Report(server, "accept", exception);
				}
			}

			async Task InitializeAsync(Socket socket, CancellationToken cancellation)
			{
				TcpServerChannel<T> channel = null;
				var registered = false;

				try
				{
					var transport = new TcpTransport(socket, server._options);
					channel = new(server, transport, server.Packetizer, server._options);
					cancellation.ThrowIfCancellationRequested();

					if(!(registered = server.Channels.Add(channel)))
						throw new InvalidOperationException();

					Interlocked.Increment(ref server._accepted);
					TcpConnectionMonitor.Add(channel);
					channel.Start();
				}
				catch(Exception exception)
				{
					try
					{
						if(channel == null)
							socket.Dispose();
						else
							await channel.DisposeAsync().ConfigureAwait(false);
					}
					catch(Exception cleanup) { exception = new AggregateException(exception, cleanup); }
					finally
					{
						if(!registered)
							Interlocked.Decrement(ref server._admitted);
					}

					if(exception is OperationCanceledException && cancellation.IsCancellationRequested)
						return;

					TcpDiagnostics.Report(server, "initialize", exception);
				}
			}
		}

		public Task StopAsync()
		{
			//由 WorkerBase 串行调用，后续停止重试复用同一清理任务。
			return _stopping ??= StopCoreAsync();

			async Task StopCoreAsync()
			{
				var cancelling = _cancellation.CancelAsync();
				listener.Dispose();

				try { await Task.WhenAll(cancelling, _accepting).ConfigureAwait(false); }
				finally { _cancellation.Dispose(); }

				if(_error != null)
					throw new TcpException(TcpError.ConnectionClosed, null, _error);
			}
		}
	}
	#endregion
}
