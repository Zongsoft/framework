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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Buffers;

using BenchmarkDotNet.Attributes;

using Zongsoft.Communication;

namespace Zongsoft.Net.Benchmarks;

/// <summary>测量原始字节流的借用式解码与复用缓冲编码。</summary>
[MemoryDiagnoser]
public class HeadlessPacketizerBenchmarks
{
	private readonly PacketizerBase<ReadOnlySequence<byte>> _packetizer = TcpClient.Headless.Packetizer;
	private ReadOnlySequence<byte> _payload;
	private ArrayBufferWriter<byte> _writer;

	/// <summary>获取或设置正文长度。</summary>
	[Params(128, 65536)]
	public int PayloadLength { get; set; }

	/// <summary>在测量前创建数据和目标缓冲。</summary>
	[GlobalSetup]
	public void Setup()
	{
		_payload = new ReadOnlySequence<byte>(new byte[this.PayloadLength]);
		_writer = new ArrayBufferWriter<byte>(this.PayloadLength);
	}

	/// <summary>编码一段原始流数据，包含写入位置复位。</summary>
	/// <returns>写入的字节数。</returns>
	[Benchmark]
	public int Pack()
	{
		_writer.ResetWrittenCount();
		_packetizer.Pack(_writer, _payload);
		return _writer.WrittenCount;
	}

	/// <summary>解码借用序列，不复制正文。</summary>
	/// <returns>成功解码长度，失败时为负数。</returns>
	[Benchmark]
	public long Unpack()
	{
		var input = _payload;
		return _packetizer.Unpack(ref input, out var package) ? package.Length : -1;
	}
}
