using System;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Tests;

namespace Zongsoft.Messaging.Tests;

public class MessageAsyncTest
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task AcknowledgeAsync_PendingValueTaskSource_IsConsumedOnce(bool acceptsDelay)
	{
		using var cancellation = new CancellationTokenSource();
		var source = new AsyncOperationSource();
		var expectedDelay = TimeSpan.FromSeconds(13);
		var actualDelay = TimeSpan.Zero;
		var actualCancellation = CancellationToken.None;
		var calls = 0;
		var message = acceptsDelay ?
			new Message("topic", [1], (delay, token) =>
			{
				calls++;
				actualDelay = delay;
				actualCancellation = token;
				return source.Operation;
			}) :
			new Message("topic", [1], token =>
			{
				calls++;
				actualCancellation = token;
				return source.Operation;
			});

		var operation = message.AcknowledgeAsync(expectedDelay, cancellation.Token);

		try
		{
			Assert.False(operation.IsCompleted);
			Assert.Equal(0, source.ConsumptionCount);
			Assert.Equal(1, calls);
			Assert.Equal(cancellation.Token, actualCancellation);
			Assert.Equal(acceptsDelay ? expectedDelay : TimeSpan.Zero, actualDelay);
		}
		finally
		{
			source.Complete();
		}

		await operation;
		Assert.Equal(1, source.ConsumptionCount);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task AcknowledgeAsync_SynchronousDelegate_ForwardsDelayAndPreservesException(bool acceptsDelay)
	{
		var delay = TimeSpan.FromSeconds(7);
		var actualDelay = TimeSpan.Zero;
		var calls = 0;
		var failure = new InvalidOperationException("confirmation failed");
		var message = acceptsDelay ?
			new Message("topic", [1], (TimeSpan value) =>
			{
				calls++;
				actualDelay = value;
				throw failure;
			}) :
			new Message("topic", [1], () =>
			{
				calls++;
				throw failure;
			});

		var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await message.AcknowledgeAsync(delay));

		Assert.Same(failure, exception);
		Assert.Equal(1, calls);
		Assert.Equal(acceptsDelay ? delay : TimeSpan.Zero, actualDelay);
	}

	[Fact]
	public async Task AcknowledgeAsync_PendingCancellation_PreservesTokenAndConsumesSource()
	{
		using var cancellation = new CancellationTokenSource();
		var source = new AsyncOperationSource();
		var message = new Message("topic", [1], (delay, token) => source.Operation);
		var operation = message.AcknowledgeAsync(cancellation.Token).AsTask();

		cancellation.Cancel();
		source.Fail(new OperationCanceledException(cancellation.Token));
		var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);

		Assert.Equal(cancellation.Token, exception.CancellationToken);
		Assert.Equal(1, source.ConsumptionCount);
	}

	[Fact]
	public async Task AcknowledgeAsync_DefaultDelayAndMissingDelegate_CompleteSuccessfully()
	{
		TimeSpan? actualDelay = null;
		var message = new Message("topic", [1], (TimeSpan delay) => actualDelay = delay);

		await message.AcknowledgeAsync();
		await Message.Empty.AcknowledgeAsync();

		Assert.Equal(TimeSpan.Zero, actualDelay);
	}
}
