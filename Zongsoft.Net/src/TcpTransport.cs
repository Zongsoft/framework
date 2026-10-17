/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
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
using System.Buffers;
using System.Net.Sockets;
using System.IO.Pipelines;
using System.Threading;

using Pipelines.Sockets.Unofficial;

namespace Zongsoft.Net;

//SocketConnection 的唯一所有者。Pipe 两端分别由通道接收者和发送者完成。
internal sealed class TcpTransport : IDisposable
{
	#region 成员字段
	private static long _identity;
	private readonly SocketConnection _connection;
	private int _disposed;
	#endregion

	#region 构造函数
	internal TcpTransport(Socket socket, TcpConnectionOptions options)
	{
		try
		{
			socket.NoDelay = options.NoDelay;
			this.LocalEndPoint = socket.LocalEndPoint;
			this.RemoteEndPoint = socket.RemoteEndPoint;

			var send = CreateOptions(options.SendPauseThreshold, options.SendResumeThreshold);
			//完整帧不能被接收者提前消费；有界地提高阈值以保证最大帧不会被中途背压截住。
			var receive = CreateOptions(Math.Max(options.ReceivePauseThreshold, (long)options.MaximumBufferedReceiveBytes + 1), options.ReceiveResumeThreshold);
			_connection = SocketConnection.Create(socket, send, receive, options.ZeroLengthReads ? SocketConnectionOptions.ZeroLengthReads : SocketConnectionOptions.None);
		}
		catch
		{
			socket.Dispose();
			throw;
		}

		static PipeOptions CreateOptions(long pause, long resume) => new(
			MemoryPool<byte>.Shared,
			PipeScheduler.ThreadPool,
			PipeScheduler.ThreadPool,
			pauseWriterThreshold: pause,
			resumeWriterThreshold: resume,
			useSynchronizationContext: false);
	}
	#endregion

	#region 内部属性
	internal EndPoint LocalEndPoint { get; }
	internal EndPoint RemoteEndPoint { get; }
	internal PipeReader Input => _connection.Input;
	internal PipeWriter Output => _connection.Output;
	internal long BytesSent => ((IMeasuredDuplexPipe)_connection).TotalBytesSent;
	internal long BytesReceived => ((IMeasuredDuplexPipe)_connection).TotalBytesReceived;
	#endregion

	#region 私有方法
	internal static long GetIdentity() => Interlocked.Increment(ref _identity);
	#endregion

	#region 释放资源
	public void Dispose()
	{
		if(Interlocked.Exchange(ref _disposed, 1) == 0)
			_connection.Dispose();
	}
	#endregion
}
