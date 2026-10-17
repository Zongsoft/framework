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
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;

namespace Zongsoft.Net.Benchmarks;

/// <summary>测量已连接通道无网络负载的发送协调快路径，不能代表实际传输吞吐。</summary>
[MemoryDiagnoser]
[ThreadingDiagnoser]
public class SendBenchmarks
{
	private TcpServer<ReadOnlySequence<byte>> _server;
	private TcpClient<ReadOnlySequence<byte>> _client;
	private TcpClientChannel<ReadOnlySequence<byte>> _channel;
	private ReadOnlySequence<byte> _payload;

	/// <summary>在测量前建立真实回环连接。</summary>
	/// <returns>连接准备任务。</returns>
	[GlobalSetup]
	public async Task SetupAsync()
	{
		_server = TcpServer.Headless;
		_server.Address = new IPEndPoint(IPAddress.Loopback, 0);
		await _server.StartAsync([]);
		_client = TcpClient.Headless;
		_channel = await _client.ConnectAsync(_server.Address);
		_payload = ReadOnlySequence<byte>.Empty;
	}

	/// <summary>发送零字节 Headless 数据，覆盖额度、单写者和同步 Flush 路径，不产生网络帧。</summary>
	/// <returns>发送操作；运行器恰好消费一次。</returns>
	[Benchmark]
	public ValueTask EmptyHeadlessAsync() => _channel.SendAsync(_payload);

	/// <summary>在测量后等待连接与服务清理。</summary>
	/// <returns>清理任务。</returns>
	[GlobalCleanup]
	public async Task CleanupAsync()
	{
		await _client.DisposeAsync();
		await _server.DisposeAsync();
	}
}
