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
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

using Zongsoft.Collections;
using Zongsoft.Components;

using Xunit;

namespace Zongsoft.Communication.Tests;

public class ListenerDispatchTest
{
	[Fact]
	public async Task MissingHandlerIsNoOpAndChecksCancellation()
	{
		await using var listener = new ProbeListener();
		await ((IListener<int>)listener).HandleAsync(1);

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ((IListener<int>)listener).HandleAsync(1, new(true)).AsTask());
	}

	[Fact]
	public async Task DispatchConsumesSynchronousValueTaskExactlyOnce()
	{
		var handler = new ProbeHandler();

		await using(var listener = new ProbeListener { Handler = handler })
		{
			await ((IListener<int>)listener).HandleAsync(7);
			Assert.Equal(1, handler.Consumptions);
			Assert.Equal(7, handler.Argument);
		}

		Assert.False(handler.IsDisposed);
	}

	[Fact]
	public async Task ParameterlessHandlerPreservesNullAndExplicitIdentity()
	{
		var handler = new ArgumentHandler();
		await handler.HandleAsync(1);
		Assert.Null(handler.Parameters);

		var parameters = Parameters.Parameter("name", 42);
		await handler.HandleAsync(2, parameters);
		Assert.Same(parameters, handler.Parameters);

		await ((IHandler)handler).HandleAsync(3);
		Assert.Null(handler.Parameters);
	}

	[Fact]
	public async Task ParameterlessDispatchDoesNotAllocateAnEmptyContainer()
	{
		var handler = new ArgumentHandler();
		for(var i = 0; i < 100; i++)
			await handler.HandleAsync(i);

		var before = GC.GetAllocatedBytesForCurrentThread();
		for(var i = 0; i < 1000; i++)
			await handler.HandleAsync(i);

		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
		Assert.Equal(0, allocated);
	}

	private sealed class ProbeListener() : ListenerBase<int>("Probe")
	{
		protected override Task OnStartAsync(string[] args, CancellationToken cancellation) => Task.CompletedTask;
		protected override Task OnStopAsync(string[] args, CancellationToken cancellation) => Task.CompletedTask;
	}

	private sealed class ArgumentHandler : HandlerBase<int>
	{
		public Parameters Parameters { get; private set; }
		protected override ValueTask OnHandleAsync(int argument, Parameters parameters, CancellationToken cancellation)
		{
			this.Parameters = parameters;
			return ValueTask.CompletedTask;
		}
	}

	private sealed class ProbeHandler : HandlerBase<int>, IValueTaskSource, IDisposable
	{
		public int Consumptions { get; private set; }
		public int Argument { get; private set; }
		public bool IsDisposed { get; private set; }

		protected override ValueTask OnHandleAsync(int argument, Parameters parameters, CancellationToken cancellation)
		{
			this.Argument = argument;
			return new(this, 0);
		}

		public void GetResult(short token) => this.Consumptions++;
		public ValueTaskSourceStatus GetStatus(short token) => ValueTaskSourceStatus.Succeeded;
		public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags) => throw new NotSupportedException();
		public void Dispose() => this.IsDisposed = true;
	}
}
