using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using StackExchange.Redis;

using Xunit;

using Zongsoft.Messaging;
using Zongsoft.Components;
using Zongsoft.Collections;

using Global= Zongsoft.Externals.Redis.Tests.Global;
using RedisTestUtility = Zongsoft.Externals.Redis.Tests.RedisTestUtility;

namespace Zongsoft.Externals.Redis.Messaging.Tests;

public class RedisQueueAsyncTests
{
	private const string REDIS_UNAVAILABLE = "Redis is unavailable at 127.0.0.1:6379.";

	[Fact]
	public async Task CompressedPayloadRoundTripsThroughRedisStream()
	{
		if(!Global.IsTestingEnabled)
			return;

		Assert.SkipUnless(Global.IsAvailable(), REDIS_UNAVAILABLE);
		var identity = Guid.NewGuid().ToString("N");
		var name = $"tests-{identity}";
		var topic = $"compression-{identity}";
		var key = RedisTestUtility.GetQueueKey(name, topic);
		var payload = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Repeat((byte)'A', 16 * 1024));
		using var administration = ConnectionMultiplexer.Connect($"{Global.Server},password={Global.Password}");
		var database = administration.GetDatabase();
		await using var queue = RedisTestUtility.CreateQueue(name, $"group-{identity}", $"client-{identity}");
		var handler = new CaptureHandler();
		var subscriber = await queue.SubscribeAsync(topic, handler);

		try
		{
			var options = new MessageEnqueueOptions { Compression = new MessageCompression("GZip", 1) };
			await queue.ProduceAsync(topic, "kind:compressed", payload, options);
			var message = await handler.Completion.Task.WaitAsync(TimeSpan.FromSeconds(10));
			var entry = Assert.Single(await database.StreamRangeAsync(key));

			Assert.Equal(payload, message.Data);
			Assert.Equal("kind:compressed", message.Tags);
			Assert.Equal("GZip", (string)entry.GetMessageCompression());
			Assert.True(((byte[])entry.GetMessageData()).Length < payload.Length);
			await message.AcknowledgeAsync();
		}
		finally
		{
			await subscriber.DisposeAsync();
			await database.KeyDeleteAsync(key);
		}
	}

	[Fact]
	public async Task EquivalentQueues_ShareConnectionAndReleaseItAfterLastAsyncDispose()
	{
		if(!Global.IsTestingEnabled)
			return;

		Assert.SkipUnless(Global.IsAvailable(), REDIS_UNAVAILABLE);

		var identity = Guid.NewGuid().ToString("N");
		var name = $"tests-{identity}";
		var topic = $"shared-{identity}";
		var key = RedisTestUtility.GetQueueKey(name, topic);
		var group = $"group-{identity}";
		var client = $"client-{identity}";

		using var administration = ConnectionMultiplexer.Connect($"{Global.Server},password={Global.Password}");
		var database = administration.GetDatabase();
		var first = RedisTestUtility.CreateQueue(name, group, client);
		var second = RedisTestUtility.CreateQueue(name, group, client);
		var connection = first.Database.Multiplexer;

		try
		{
			Assert.Same(connection, second.Database.Multiplexer);

			await first.DisposeAsync();
			Assert.True(connection.IsConnected);
			Assert.True(await second.Database.PingAsync() >= TimeSpan.Zero);

			var identifier = await second.ProduceAsync(topic, Encoding.UTF8.GetBytes("still-owned"));
			Assert.False(string.IsNullOrEmpty(identifier));

			await second.DisposeAsync();
			Assert.False(connection.IsConnected);
		}
		finally
		{
			await first.DisposeAsync();
			await second.DisposeAsync();
			await database.KeyDeleteAsync(key);
		}
	}

	[Fact]
	public async Task BlockedReceive_DisposeAsyncCancelsPromptlyAndStopsDelivery()
	{
		if(!Global.IsTestingEnabled)
			return;

		Assert.SkipUnless(Global.IsAvailable(), REDIS_UNAVAILABLE);

		var identity = Guid.NewGuid().ToString("N");
		var name = $"tests-{identity}";
		var topic = $"cancel-{identity}";
		var key = RedisTestUtility.GetQueueKey(name, topic);

		using var administration = ConnectionMultiplexer.Connect($"{Global.Server},password={Global.Password}");
		var database = administration.GetDatabase();
		await using var queue = RedisTestUtility.CreateQueue(name, $"group-{identity}", $"client-{identity}");
		var handler = new CountingHandler();
		var subscriber = await queue.SubscribeAsync(topic, handler);

		try
		{
			await Task.Delay(100);
			await subscriber.DisposeAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2));
			Assert.Empty(queue.Subscribers);

			await queue.ProduceAsync(topic, Encoding.UTF8.GetBytes("after-close"));
			await Task.Delay(250);
			Assert.Equal(0, handler.Count);
		}
		finally
		{
			await subscriber.DisposeAsync();
			await database.KeyDeleteAsync(key);
		}
	}

	[Fact]
	public async Task AsyncHandler_IsAwaitedAndMessagesAreDeliveredSerially()
	{
		if(!Global.IsTestingEnabled)
			return;

		Assert.SkipUnless(Global.IsAvailable(), REDIS_UNAVAILABLE);

		var identity = Guid.NewGuid().ToString("N");
		var name = $"tests-{identity}";
		var topic = $"serial-{identity}";
		var key = RedisTestUtility.GetQueueKey(name, topic);

		using var administration = ConnectionMultiplexer.Connect($"{Global.Server},password={Global.Password}");
		var database = administration.GetDatabase();
		await using var queue = RedisTestUtility.CreateQueue(name, $"group-{identity}", $"client-{identity}");
		var handler = new SerialHandler();
		var subscriber = await queue.SubscribeAsync(topic, handler);

		try
		{
			await queue.ProduceAsync(topic, Encoding.UTF8.GetBytes("first"));
			await queue.ProduceAsync(topic, Encoding.UTF8.GetBytes("second"));
			await handler.FirstStarted.WaitAsync(TimeSpan.FromSeconds(10));

			await Task.Delay(200);
			Assert.Equal(1, handler.Count);
			Assert.Equal(1, handler.MaximumConcurrency);

			handler.ReleaseFirst();
			await handler.BothCompleted.WaitAsync(TimeSpan.FromSeconds(10));
			Assert.Equal(2, handler.Count);
			Assert.Equal(1, handler.MaximumConcurrency);
		}
		finally
		{
			handler.ReleaseFirst();
			await subscriber.DisposeAsync();
			await database.KeyDeleteAsync(key);
		}
	}

	private sealed class CountingHandler : HandlerBase<Message>
	{
		private int _count;
		public int Count => _count;

		protected override ValueTask OnHandleAsync(Message message, Parameters parameters, CancellationToken cancellation)
		{
			Interlocked.Increment(ref _count);
			return ValueTask.CompletedTask;
		}
	}

	private sealed class CaptureHandler : HandlerBase<Message>
	{
		public TaskCompletionSource<Message> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		protected override ValueTask OnHandleAsync(Message message, Parameters parameters, CancellationToken cancellation)
		{
			this.Completion.TrySetResult(message);
			return ValueTask.CompletedTask;
		}
	}

	private sealed class SerialHandler : HandlerBase<Message>
	{
		private readonly TaskCompletionSource<bool> _firstStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private readonly TaskCompletionSource<bool> _releaseFirst = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private readonly TaskCompletionSource<bool> _bothCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private int _active;
		private int _count;
		private int _maximumConcurrency;

		public int Count => Volatile.Read(ref _count);
		public int MaximumConcurrency => Volatile.Read(ref _maximumConcurrency);
		public Task FirstStarted => _firstStarted.Task;
		public Task BothCompleted => _bothCompleted.Task;

		public void ReleaseFirst() => _releaseFirst.TrySetResult(true);

		protected override async ValueTask OnHandleAsync(Message message, Parameters parameters, CancellationToken cancellation)
		{
			var active = Interlocked.Increment(ref _active);
			InterlockedExtensions.Max(ref _maximumConcurrency, active);
			var count = Interlocked.Increment(ref _count);

			try
			{
				if(count == 1)
				{
					_firstStarted.TrySetResult(true);
					await _releaseFirst.Task.WaitAsync(cancellation);
				}

				await message.AcknowledgeAsync(cancellation);
				if(count == 2)
					_bothCompleted.TrySetResult(true);
			}
			finally
			{
				Interlocked.Decrement(ref _active);
			}
		}
	}

	private static class InterlockedExtensions
	{
		public static void Max(ref int location, int value)
		{
			var current = Volatile.Read(ref location);
			while(current < value)
			{
				var previous = Interlocked.CompareExchange(ref location, value, current);
				if(previous == current)
					return;

				current = previous;
			}
		}
	}
}
