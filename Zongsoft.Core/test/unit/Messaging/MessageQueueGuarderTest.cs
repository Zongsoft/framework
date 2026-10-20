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
using System.Collections.Generic;

using Xunit;

using Zongsoft.Components;

namespace Zongsoft.Messaging.Tests;

public class MessageQueueGuarderTest
{
	[Fact]
	public async Task FailedDisposalDoesNotSkipOtherConsumersAndCanBeRetried()
	{
		await using var queue = new Queue();
		await using var guarder = new MessageQueueGuarder { Queue = queue, Options = new() };
		await guarder.StartAsync(["first", "second"]);
		queue.Created[0].Fail = true;

		await Assert.ThrowsAsync<InvalidOperationException>(() => guarder.StopAsync());
		Assert.Equal(WorkerState.Stopping, guarder.State);
		Assert.Equal(1, queue.Created[0].Calls);
		Assert.Equal(1, queue.Created[1].Calls);
		Assert.False(queue.Created[0].IsDisposed);
		Assert.True(queue.Created[1].IsDisposed);

		queue.Created[0].Fail = false;
		await guarder.StopAsync();
		Assert.Equal(WorkerState.Stopped, guarder.State);
		Assert.True(queue.Created[0].IsDisposed);
		Assert.Equal(2, queue.Created[0].Calls);
		Assert.Equal(1, queue.Created[1].Calls);
	}

	[Fact]
	public async Task FailedSubscriptionCleansPreviouslyCreatedConsumers()
	{
		await using var queue = new Queue { FailSecond = true };
		await using var guarder = new MessageQueueGuarder { Queue = queue, Options = new() };
		await Assert.ThrowsAsync<InvalidOperationException>(() => guarder.StartAsync(["first", "second"]));
		Assert.Equal(WorkerState.Stopped, guarder.State);
		Assert.True(Assert.Single(queue.Created).IsDisposed);
	}

	private sealed class Queue() : MessageQueueBase<Consumer>("Tests")
	{
		public bool FailSecond { get; init; }
		public List<Consumer> Created { get; } = [];

		protected override ValueTask<string> OnProduceAsync(string topic, string tags, ReadOnlyMemory<byte> data, MessageEnqueueOptions options, CancellationToken cancellation) => ValueTask.FromResult(string.Empty);
		protected override ValueTask<bool> OnSubscribeAsync(Consumer subscriber, CancellationToken cancellation) => ValueTask.FromResult(true);
		protected override ValueTask<Consumer> CreateSubscriberAsync(string topic, string tags, IHandler<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation)
		{
			if(this.FailSecond && this.Created.Count > 0)
				throw new InvalidOperationException();

			var consumer = new Consumer(topic);
			this.Created.Add(consumer);
			return ValueTask.FromResult(consumer);
		}
	}

	private sealed class Consumer(string topic) : IMessageConsumer
	{
		public event EventHandler Closed;
		public event EventHandler Closing;
		public string Topic => topic;
		public string[] Tags => [];
		public IHandler<Message> Handler => null;
		public MessageSubscribeOptions Options => null;
		public bool IsClosed { get; private set; }
		public bool IsDisposed { get; private set; }
		public bool Fail { get; set; }
		public int Calls { get; private set; }

		public ValueTask UnsubscribeAsync(CancellationToken cancellation = default) => this.CloseAsync(cancellation);
		public ValueTask CloseAsync(CancellationToken cancellation = default)
		{
			this.Closing?.Invoke(this, EventArgs.Empty);
			this.IsClosed = true;
			this.Closed?.Invoke(this, EventArgs.Empty);
			return ValueTask.CompletedTask;
		}

		public ValueTask DisposeAsync()
		{
			this.Calls++;

			if(this.Fail)
				throw new InvalidOperationException();

			this.IsDisposed = true;
			return this.CloseAsync();
		}
	}
}
