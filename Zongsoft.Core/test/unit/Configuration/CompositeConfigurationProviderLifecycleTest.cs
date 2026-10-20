using System;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;

using Xunit;

namespace Zongsoft.Configuration.Tests;

public class CompositeConfigurationProviderLifecycleTest
{
	[Fact]
	public async Task DisposeAsync_PendingChild_SharesCompletionAndPrefersAsyncCleanup()
	{
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var pending = new AsyncProvider(() => new(release.Task));
		var dual = new DualProvider();
		var synchronous = new SyncProvider();
		var composite = new CompositeConfigurationProvider([synchronous, dual, pending]);
		var returned = new TaskCompletionSource<Task>(TaskCreationOptions.RunContinuationsAsynchronously);
		var invocation = Task.Run(() => returned.SetResult(composite.DisposeAsync().AsTask()));
		Task[] disposals = [];

		try
		{
			var first = await returned.Task.WaitAsync(TimeSpan.FromSeconds(5));
			disposals = [first, .. Enumerable.Range(0, 15).Select(_ => composite.DisposeAsync().AsTask())];

			Assert.All(disposals, disposal => Assert.Same(disposals[0], disposal));
			Assert.All(disposals, disposal => Assert.False(disposal.IsCompleted));
			Assert.Equal(1, pending.Disposals);
			Assert.Equal(0, dual.AsyncDisposals);
			Assert.Equal(0, synchronous.Disposals);
			Assert.Throws<InvalidOperationException>(() => composite.Dispose());
		}
		finally
		{
			release.TrySetResult();
			await invocation.WaitAsync(TimeSpan.FromSeconds(5));
		}

		await Task.WhenAll(disposals).WaitAsync(TimeSpan.FromSeconds(5));
		await composite.DisposeAsync();

		Assert.Equal(1, pending.Disposals);
		Assert.Equal(1, dual.AsyncDisposals);
		Assert.Equal(0, dual.SyncDisposals);
		Assert.Equal(1, synchronous.Disposals);
		Assert.Empty(composite.Providers);
	}

	[Fact]
	public async Task DisposeAsync_ChildFailures_CleansEveryProviderAndSharesFailure()
	{
		var firstFailure = new InvalidOperationException();
		var secondFailure = new ArgumentException();
		var first = new AsyncProvider(() => ValueTask.FromException(firstFailure));
		var second = new SyncProvider(() => throw secondFailure);
		var remaining = new AsyncProvider();
		var composite = new CompositeConfigurationProvider([remaining, second, first]);

		var firstDisposal = composite.DisposeAsync().AsTask();
		var secondDisposal = composite.DisposeAsync().AsTask();
		Assert.Same(firstDisposal, secondDisposal);
		var observed = await Assert.ThrowsAsync<AggregateException>(() => firstDisposal);
		var repeated = await Assert.ThrowsAsync<AggregateException>(() => secondDisposal);

		Assert.Same(observed, repeated);
		Assert.Equal([firstFailure, secondFailure], observed.InnerExceptions);
		Assert.Equal(1, first.Disposals);
		Assert.Equal(1, second.Disposals);
		Assert.Equal(1, remaining.Disposals);
		Assert.Empty(composite.Providers);
	}

	[Fact]
	public async Task Dispose_AsyncOnlyChild_RejectsBeforeReleasingAnyProvider()
	{
		var asynchronous = new AsyncProvider();
		var synchronous = new SyncProvider();
		var composite = new CompositeConfigurationProvider([asynchronous, synchronous]);

		Assert.Throws<InvalidOperationException>(() => composite.Dispose());
		Assert.Equal(0, asynchronous.Disposals);
		Assert.Equal(0, synchronous.Disposals);
		Assert.Equal(2, composite.Providers.Count());

		await composite.DisposeAsync();

		Assert.Equal(1, asynchronous.Disposals);
		Assert.Equal(1, synchronous.Disposals);
		Assert.Empty(composite.Providers);
	}

	[Fact]
	public async Task Dispose_SynchronousChildren_AreReleasedOnceAcrossBothEntryPoints()
	{
		var first = new SyncProvider();
		var dual = new DualProvider();
		var composite = new CompositeConfigurationProvider([first, dual]);

		composite.Dispose();
		composite.Dispose();
		await composite.DisposeAsync();

		Assert.Equal(1, first.Disposals);
		Assert.Equal(1, dual.SyncDisposals);
		Assert.Equal(0, dual.AsyncDisposals);
		Assert.Empty(composite.Providers);
	}

	[Fact]
	public void Dispose_SynchronousFailure_StillCleansRemainingProviders()
	{
		var failure = new InvalidOperationException();
		var failed = new SyncProvider(() => throw failure);
		var remaining = new SyncProvider();
		var composite = new CompositeConfigurationProvider([remaining, failed]);

		var observed = Assert.Throws<AggregateException>(() => composite.Dispose());

		Assert.Same(failure, Assert.Single(observed.InnerExceptions));
		Assert.Equal(1, failed.Disposals);
		Assert.Equal(1, remaining.Disposals);
		Assert.Empty(composite.Providers);
	}

	private sealed class AsyncProvider(Func<ValueTask> dispose = null) : ConfigurationProvider, IAsyncDisposable
	{
		public int Disposals { get; private set; }

		public ValueTask DisposeAsync()
		{
			this.Disposals++;
			return dispose?.Invoke() ?? ValueTask.CompletedTask;
		}
	}

	private sealed class SyncProvider(Action dispose = null) : ConfigurationProvider, IDisposable
	{
		public int Disposals { get; private set; }

		public void Dispose()
		{
			this.Disposals++;
			dispose?.Invoke();
		}
	}

	private sealed class DualProvider : ConfigurationProvider, IDisposable, IAsyncDisposable
	{
		public int SyncDisposals { get; private set; }
		public int AsyncDisposals { get; private set; }
		public void Dispose() => this.SyncDisposals++;

		public ValueTask DisposeAsync()
		{
			this.AsyncDisposals++;
			return ValueTask.CompletedTask;
		}
	}
}
