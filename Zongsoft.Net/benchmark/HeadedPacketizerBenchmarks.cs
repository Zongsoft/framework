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

using BenchmarkDotNet.Attributes;

using Zongsoft.Communication;

namespace Zongsoft.Net.Benchmarks;

/// <summary>测量带长度前缀报文的编码与单段、跨段解码。</summary>
[MemoryDiagnoser]
public class HeadedPacketizerBenchmarks
{
	private readonly PacketizerBase<ReadOnlySequence<byte>> _packetizer = TcpClient.Headed.Packetizer;
	private ArrayBufferWriter<byte> _writer;
	private ReadOnlySequence<byte> _payload;
	private ReadOnlySequence<byte> _single;
	private ReadOnlySequence<byte> _segmented;

	/// <summary>获取或设置正文长度。</summary>
	[Params(0, 32, 128, 512, 1024, 4096, 16384, 65536)]
	public int PayloadLength { get; set; }

	/// <summary>准备并验证报文，所有输入缓冲均在测量之前创建。</summary>
	[GlobalSetup]
	public void Setup()
	{
		var payload = new byte[this.PayloadLength];
		new Random(42).NextBytes(payload);
		_payload = new ReadOnlySequence<byte>(payload);
		_writer = new ArrayBufferWriter<byte>(this.PayloadLength + 4);
		_packetizer.Pack(_writer, _payload);

		var frame = _writer.WrittenMemory.ToArray();
		_single = new ReadOnlySequence<byte>(frame);
		var head = new Segment(frame.AsMemory(0, 2));
		var tail = head.Append(frame.AsMemory(2));
		_segmented = new ReadOnlySequence<byte>(head, 0, tail, tail.Memory.Length);

		var input = _segmented;
		if(!_packetizer.Unpack(ref input, out var result) || !input.IsEmpty || !result.ToArray().AsSpan().SequenceEqual(payload))
			throw new InvalidOperationException();
	}

	/// <summary>向复用缓冲编码一帧，包含缓冲写入位置复位的成本。</summary>
	/// <returns>编码后的线协议字节数。</returns>
	[Benchmark]
	public int Pack()
	{
		_writer.ResetWrittenCount();
		_packetizer.Pack(_writer, _payload);
		return _writer.WrittenCount;
	}

	/// <summary>解码单段报文。</summary>
	/// <returns>正文长度；数据不足时为负数。</returns>
	[Benchmark]
	public long UnpackSingle()
	{
		var input = _single;
		return _packetizer.Unpack(ref input, out var package) ? package.Length : -1;
	}

	/// <summary>解码包头跨两个内存段的报文。</summary>
	/// <returns>正文长度；数据不足时为负数。</returns>
	[Benchmark]
	public long UnpackSegmented()
	{
		var input = _segmented;
		return _packetizer.Unpack(ref input, out var package) ? package.Length : -1;
	}

	private sealed class Segment : ReadOnlySequenceSegment<byte>
	{
		public Segment(ReadOnlyMemory<byte> memory) => this.Memory = memory;

		public Segment Append(ReadOnlyMemory<byte> memory)
		{
			var segment = new Segment(memory) { RunningIndex = this.RunningIndex + this.Memory.Length };
			this.Next = segment;
			return segment;
		}
	}
}
