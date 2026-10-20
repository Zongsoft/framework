using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Caching;

using Xunit;

namespace Zongsoft.Components.Tests;

public class SuperviserDisposalTest
{
	private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

	[Fact]
	public async Task Dispose_EvictionCompletesBeforeClear_KeepsCacheAlive()
	{
		using var resumeCallback = new ManualResetEventSlim();
		using var resumeDisposal = new ManualResetEventSlim();
		var callbackEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var callbackFinished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var disposalEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

		using var superviser = new DisposingSuperviser(() =>
		{
			disposalEntered.TrySetResult();
			Assert.True(resumeDisposal.Wait(_timeout));
		});

		var cache = GetCache(superviser);
		cache.Evicted += (_, _) => callbackFinished.TrySetResult();
		superviser.Unsupervised += (_, _) =>
		{
			callbackEntered.TrySetResult();
			Assert.True(resumeCallback.Wait(_timeout));
		};
		superviser.Supervise("S", new CallbackSupervisable());

		Task disposal = null;

		try
		{
			Assert.True(superviser.Unsupervise("S"));
			await callbackEntered.Task.WaitAsync(_timeout);
			disposal = Task.Run(superviser.Dispose);
			await disposalEntered.Task.WaitAsync(_timeout);
			Assert.True(superviser.IsDisposing);

			//让逐出回调完整返回，同时保持 Dispose 停在清理缓存之前。
			resumeCallback.Set();
			await callbackFinished.Task.WaitAsync(_timeout);
			Assert.Same(cache, GetCache(superviser));
			Assert.False(cache.Contains("S"));
		}
		finally
		{
			resumeCallback.Set();
			resumeDisposal.Set();
			if(disposal != null)
				await disposal.WaitAsync(_timeout);
		}

		Assert.True(superviser.IsDisposed);
		Assert.Null(GetCache(superviser));
	}

	[Fact]
	public async Task Dispose_PendingEvictions_KeepsCacheUntilAllCallbacksFinish()
	{
		using var resumeCallback = new ManualResetEventSlim();
		var callbackEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var slowFinished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var fastFinished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

		var notifications = 0;
		using var superviser = new Superviser<string>();

		var cache = GetCache(superviser);
		cache.Evicted += (_, args) =>
		{
			if(Equals(args.Key, "slow"))
				slowFinished.TrySetResult();
			else
				fastFinished.TrySetResult();
		};
		superviser.Supervise("slow", new CallbackSupervisable(() =>
		{
			callbackEntered.TrySetResult();
			Assert.True(resumeCallback.Wait(_timeout));
		}));
		superviser.Supervise("fast", new CallbackSupervisable(() => Interlocked.Increment(ref notifications)));

		try
		{
			Assert.True(superviser.Unsupervise("slow"));
			await callbackEntered.Task.WaitAsync(_timeout);
			superviser.Dispose();
			await fastFinished.Task.WaitAsync(_timeout);

			Assert.True(superviser.IsDisposed);
			Assert.Equal(0, superviser.Count);
			Assert.Equal(1, Volatile.Read(ref notifications));
			Assert.Same(cache, GetCache(superviser));
			Assert.False(cache.Contains("fast"));
		}
		finally
		{
			resumeCallback.Set();
			await slowFinished.Task.WaitAsync(_timeout);
		}

		Assert.Null(GetCache(superviser));
	}

	[Fact]
	public void Dispose_EmptySuperviser_ReleasesCache()
	{
		var superviser = new Superviser<string>();
		superviser.Dispose();
		superviser.Dispose();

		Assert.True(superviser.IsDisposed);
		Assert.False(superviser.IsDisposing);
		Assert.Equal(0, superviser.Count);
		Assert.Null(GetCache(superviser));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Dispose_FromEvictionCallback_ReleasesCacheEvenIfCallbackThrows(bool throws)
	{
		using var superviser = new Superviser<string>();
		var callbackFinished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		superviser.Supervise("S", new CallbackSupervisable(() =>
		{
			superviser.Dispose();
			callbackFinished.TrySetResult();
			if(throws)
				throw new InvalidOperationException("The notification failed.");
		}));

		Assert.True(superviser.Unsupervise("S"));
		await callbackFinished.Task.WaitAsync(_timeout);
		Assert.True(SpinWait.SpinUntil(() => GetCache(superviser) == null, _timeout));
		Assert.True(superviser.IsDisposed);
	}

	//只在测试中读取资源所有权，不扩大生产成员的可见性。
	private static MemoryCache GetCache(Superviser<string> superviser) =>
		(MemoryCache)typeof(Superviser<string>).GetField("_cache", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(superviser);

	private sealed class DisposingSuperviser(Action callback) : Superviser<string>
	{
		protected override void Dispose(bool disposing)
		{
			if(disposing)
				callback();

			base.Dispose(disposing);
		}
	}

	private sealed class CallbackSupervisable(Action callback = null) : Supervisable<string>
	{
		protected override void OnUnsupervised(ISuperviser<string> superviser, SupervisableReason reason) => callback?.Invoke();
	}
}
