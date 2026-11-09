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
using System.Threading;
using System.Threading.Tasks;

using BenchmarkDotNet.Attributes;

using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Net.Benchmarks;

/// <summary>对比泛型处理器省略参数和显式空参数的派发成本。</summary>
[MemoryDiagnoser]
public class HandlerBenchmarks
{
	private readonly PayloadHandler _handler = new();
	private readonly ReadOnlySequence<byte> _payload = new(new byte[128]);

	/// <summary>调用显式空参数重载。</summary>
	/// <returns>本次处理操作。</returns>
	[Benchmark(Baseline = true)]
	public ValueTask ExplicitNullAsync() => _handler.HandleAsync(_payload, null);

	/// <summary>调用无附加参数重载。</summary>
	/// <returns>本次处理操作。</returns>
	[Benchmark]
	public ValueTask WithoutParametersAsync() => _handler.HandleAsync(_payload);

	private sealed class PayloadHandler : HandlerBase<ReadOnlySequence<byte>>
	{
		public long Length { get; private set; }
		public Parameters Parameters { get; private set; }

		protected override ValueTask OnHandleAsync(ReadOnlySequence<byte> argument, Parameters parameters, CancellationToken cancellation)
		{
			this.Length = argument.Length;
			this.Parameters = parameters;
			return ValueTask.CompletedTask;
		}
	}
}
