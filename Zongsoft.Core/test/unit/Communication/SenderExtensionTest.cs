using System;
using System.Text;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Tests;

namespace Zongsoft.Communication.Tests;

public class SenderExtensionTest
{
	[Fact]
	public async Task SendAsync_ByteArraySlice_TransmitsOnlyRequestedBytes()
	{
		var sender = new RecordingSender();

		await sender.SendAsync(new byte[] { 0, 1, 2, 3 }, 1, 2);

		Assert.Equal(1, sender.CallCount);
		Assert.Equal(new byte[] { 1, 2 }, sender.Snapshot);
	}

	[Fact]
	public async Task SendAsync_Text_TransmitsOnlyEncodedUtf8Bytes()
	{
		const string TEXT = "中€";

		var expected = Encoding.UTF8.GetBytes(TEXT);
		var sender = new RecordingSender();

		await sender.SendAsync(TEXT, Encoding.UTF8);

		Assert.Equal(1, sender.CallCount);
		Assert.Equal(expected.Length, sender.Snapshot.Length);
		Assert.Equal(expected, sender.Snapshot);
	}

	[Fact]
	public async Task SendAsync_Text_KeepsBufferAliveUntilSenderCompletes()
	{
		const string TEXT = "异步发送";

		var expected = Encoding.UTF8.GetBytes(TEXT);
		var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var sender = new RecordingSender((_, _) => new ValueTask(completion.Task));
		var sending = sender.SendAsync(TEXT, Encoding.UTF8).AsTask();

		await sender.Invoked.Task.WaitAsync(TimeSpan.FromSeconds(5));

		try
		{
			Assert.False(sending.IsCompleted);
			Assert.Equal(expected, sender.Memory[..expected.Length].ToArray());
		}
		finally
		{
			completion.TrySetResult();
			await sending;
		}
	}

	[Fact]
	public async Task SendAsync_Canceled_PropagatesOriginalToken()
	{
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		var sender = new RecordingSender((_, token) => ValueTask.FromCanceled(token));

		var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
			sender.SendAsync("cancel", Encoding.UTF8, cancellation.Token).AsTask());

		Assert.Equal(cancellation.Token, sender.Cancellation);
		Assert.Equal(cancellation.Token, exception.CancellationToken);
	}

	[Fact]
	public async Task SendAsync_SenderFailure_PropagatesAndClearsEncodedBuffer()
	{
		var failure = new InvalidOperationException("send failed");
		var sender = new RecordingSender((_, _) => ValueTask.FromException(failure));

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
			sender.SendAsync("sensitive", Encoding.UTF8).AsTask());

		Assert.Same(failure, exception);
		Assert.False(sender.Memory.IsEmpty);
		Assert.True(sender.Memory.Span.IndexOfAnyExcept((byte)0) < 0, "The encoded buffer still contains sensitive bytes after it was returned.");
	}

	[Fact]
	public async Task ByteArrayOverloads_NullArguments_ThrowArgumentNullException()
	{
		ISender missing = null;
		var sender = new RecordingSender();

		var missingSender = await Assert.ThrowsAsync<ArgumentNullException>(() => missing.SendAsync([1, 2, 3]).AsTask());
		var missingData = await Assert.ThrowsAsync<ArgumentNullException>(() => SenderExtension.SendAsync(sender, (byte[])null).AsTask());

		Assert.Equal("sender", missingSender.ParamName);
		Assert.Equal("data", missingData.ParamName);
		Assert.Equal(0, sender.CallCount);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task SendAsync_OwnedMemory_WaitsAndDisposesOnce(bool fail)
	{
		var source = new AsyncOperationSource();
		var owner = new MemoryOwner([1, 2, 3]);
		var sender = new RecordingSender((_, _) => source.Operation);
		var failure = new InvalidOperationException("send failed");
		var sending = sender.SendAsync(owner).AsTask();

		try
		{
			Assert.False(sending.IsCompleted);
			Assert.Equal(0, owner.DisposeCount);
			Assert.Equal(0, source.ConsumptionCount);
			Assert.Equal(new byte[] { 1, 2, 3 }, sender.Memory.ToArray());
		}
		finally
		{
			if(fail)
				source.Fail(failure);
			else
				source.Complete();
		}

		if(fail)
		{
			var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => sending);
			Assert.Same(failure, exception);
		}
		else
		{
			await sending;
		}

		Assert.Equal(1, source.ConsumptionCount);
		Assert.Equal(1, owner.DisposeCount);
		Assert.Equal(new byte[] { 0, 0, 0 }, sender.Memory.ToArray());
	}

	[Fact]
	public async Task SendAsync_OwnedMemory_CancellationDisposesAndPreservesToken()
	{
		using var cancellation = new CancellationTokenSource();
		var owner = new MemoryOwner([1, 2, 3]);
		cancellation.Cancel();
		var sender = new RecordingSender((_, token) => ValueTask.FromCanceled(token));

		var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sender.SendAsync(owner, cancellation.Token).AsTask());

		Assert.Equal(cancellation.Token, exception.CancellationToken);
		Assert.Equal(cancellation.Token, sender.Cancellation);
		Assert.Equal(1, owner.DisposeCount);
	}

	private sealed class MemoryOwner(byte[] data) : IMemoryOwner<byte>
	{
		public int DisposeCount { get; private set; }
		public Memory<byte> Memory => data;

		public void Dispose()
		{
			this.DisposeCount++;
			Array.Clear(data);
		}
	}

	private sealed class RecordingSender(Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask> behavior = null) : ISender
	{
		private readonly Func<ReadOnlyMemory<byte>, CancellationToken, ValueTask> _behavior = behavior;
		private int _callCount;

		public int CallCount => _callCount;
		public byte[] Snapshot { get; private set; } = [];
		public ReadOnlyMemory<byte> Memory { get; private set; }
		public CancellationToken Cancellation { get; private set; }
		public TaskCompletionSource Invoked { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellation = default)
		{
			Interlocked.Increment(ref _callCount);
			this.Memory = data;
			this.Snapshot = data.ToArray();
			this.Cancellation = cancellation;
			this.Invoked.TrySetResult();
			return _behavior == null ? ValueTask.CompletedTask : _behavior(data, cancellation);
		}
	}
}
