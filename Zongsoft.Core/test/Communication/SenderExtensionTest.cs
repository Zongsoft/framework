using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Communication.Tests;

public class SenderExtensionTest
{
	[Fact]
	public void Send_Text_TransmitsOnlyEncodedUtf8Bytes()
	{
		const string text = "中€";
		var expected = Encoding.UTF8.GetBytes(text);
		var sender = new RecordingSender();

		sender.Send(text, Encoding.UTF8);

		Assert.Equal(1, sender.CallCount);
		Assert.Equal(expected.Length, sender.Snapshot.Length);
		Assert.Equal(expected, sender.Snapshot);
	}

	[Fact]
	public async Task SendAsync_Text_TransmitsOnlyEncodedUtf8Bytes()
	{
		const string text = "中€";
		var expected = Encoding.UTF8.GetBytes(text);
		var sender = new RecordingSender();

		await sender.SendAsync(text, Encoding.UTF8);

		Assert.Equal(1, sender.CallCount);
		Assert.Equal(expected.Length, sender.Snapshot.Length);
		Assert.Equal(expected, sender.Snapshot);
	}

	[Fact]
	public async Task SendAsync_Text_KeepsBufferAliveUntilSenderCompletes()
	{
		const string text = "异步发送";
		var expected = Encoding.UTF8.GetBytes(text);
		var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var sender = new RecordingSender((_, _) => new ValueTask(completion.Task));
		var sending = sender.SendAsync(text, Encoding.UTF8).AsTask();

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
	public void ByteArrayOverloads_NullArguments_ThrowArgumentNullException()
	{
		ISender missing = null;
		var sender = new RecordingSender();

		var missingSender = Assert.Throws<ArgumentNullException>(() => missing.Send([1, 2, 3]));
		var missingData = Assert.Throws<ArgumentNullException>(() => sender.Send((byte[])null));

		Assert.Equal("sender", missingSender.ParamName);
		Assert.Equal("data", missingData.ParamName);
		Assert.Equal(0, sender.CallCount);
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
