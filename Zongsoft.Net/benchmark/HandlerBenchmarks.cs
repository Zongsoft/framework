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
