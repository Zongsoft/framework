using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Primitives;

using Zongsoft.Components;

using Xunit;

namespace Zongsoft.Caching.Tests;

public class MemoryCacheScannerTest
{
	private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Start_IdleCache_RaisesExpirationWithoutTouchingEntries(bool sliding)
	{
		using var cache = new MemoryCache(TimeSpan.FromSeconds(1));
		using var scanner = new MemoryCacheScanner(cache);
		var evicted = new TaskCompletionSource<CacheEvictedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
		cache.Evicted += (_, args) => evicted.TrySetResult(args);
		cache.SetValue("permanent", "retained");
		cache.SetValue("expiring", "value", sliding ? new MemoryCache.Expiration(TimeSpan.FromMilliseconds(1500)) : new MemoryCache.Expiration(DateTimeOffset.UtcNow.AddMilliseconds(1500)));

		scanner.Start();
		//不读取缓存条目，由定时器独立触发过期通知。
		var args = await evicted.Task.WaitAsync(_timeout);
		Assert.Equal("expiring", args.Key);
		Assert.Equal("value", args.Value);
		Assert.Equal(CacheEvictedReason.Expired, args.Reason);
		Assert.Equal(1, cache.Count);
		Assert.Equal("retained", cache.GetValue<string>("permanent"));
	}

	[Fact]
	public async Task Stop_ThenStart_ResumesPassiveTokenExpiration()
	{
		using var cache = new MemoryCache(TimeSpan.FromSeconds(1));
		using var scanner = new MemoryCacheScanner(cache);
		var token = new ScanToken();
		cache.SetValue("probe", "value", token);
		var scanned = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var evicted = new TaskCompletionSource<CacheEvictedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
		token.Callback = () => scanned.TrySetResult();
		cache.Evicted += (_, args) => evicted.TrySetResult(args);

		scanner.Start();
		await scanned.Task.WaitAsync(_timeout);
		scanner.Stop();
		//停止前已开始的检查仍返回未过期；后续检查才报告过期。
		token.Callback = token.Expire;
		await Task.Delay(TimeSpan.FromMilliseconds(2500));
		Assert.False(evicted.Task.IsCompleted);

		scanner.Start();
		var args = await evicted.Task.WaitAsync(_timeout);
		Assert.Equal(CacheEvictedReason.Depended, args.Reason);
		Assert.Equal(0, cache.Count);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Dispose_BlockedScan_ReturnsBeforeScanFinishes(bool supervised)
	{
		using var resume = new ManualResetEventSlim();
		using var superviser = supervised ? new Superviser<string>() : null;
		using var cache = supervised ? GetField<MemoryCache>(superviser, "_cache") : new MemoryCache(TimeSpan.FromSeconds(1));
		using var scanner = supervised ? GetField<MemoryCacheScanner>(superviser, "_scanner") : new MemoryCacheScanner(cache);
		scanner.Stop();
		var token = new ScanToken();
		cache.SetValue("probe", "value", token);
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		token.Callback = () =>
		{
			entered.TrySetResult();
			resume.Wait(_timeout);
			finished.TrySetResult();
		};
		Task disposal = null;

		try
		{
			scanner.Start();
			await entered.Task.WaitAsync(_timeout);
			disposal = Task.Run(() =>
			{
				if(supervised)
					superviser.Dispose();
				else
					scanner.Dispose();
			});

			//扫描仍被信号阻塞，释放调用必须先返回。
			await disposal.WaitAsync(TimeSpan.FromSeconds(3));
			Assert.False(finished.Task.IsCompleted);
			Assert.Throws<ObjectDisposedException>(scanner.Start);

			if(supervised)
			{
				Assert.True(superviser.IsDisposed);
				Assert.Null(GetField<MemoryCache>(superviser, "_cache"));
			}
			else
				Assert.Equal("value", cache.GetValue<string>("probe"));
		}
		finally
		{
			resume.Set();
			if(disposal != null)
				await disposal.WaitAsync(_timeout);

			await finished.Task.WaitAsync(_timeout);
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Dispose_FromScanCallback_DoesNotWaitForItself(bool supervised)
	{
		using var superviser = supervised ? new Superviser<string>() : null;
		using var cache = supervised ? GetField<MemoryCache>(superviser, "_cache") : new MemoryCache(TimeSpan.FromSeconds(1));
		using var scanner = supervised ? GetField<MemoryCacheScanner>(superviser, "_scanner") : new MemoryCacheScanner(cache);
		scanner.Stop();
		var token = new ScanToken();
		cache.SetValue("probe", "value", token);
		var returned = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		token.Callback = () =>
		{
			try
			{
				if(supervised)
					superviser.Dispose();
				else
					scanner.Dispose();

				returned.TrySetResult();
			}
			catch(Exception exception)
			{
				returned.TrySetException(exception);
			}
		};

		scanner.Start();
		await returned.Task.WaitAsync(_timeout);
		Assert.Throws<ObjectDisposedException>(scanner.Start);

		if(supervised)
			Assert.Null(GetField<MemoryCache>(superviser, "_cache"));
		else
			Assert.Equal("value", cache.GetValue<string>("probe"));
	}

	[Fact]
	public async Task StartStopDispose_ConcurrentCalls_LeaveScannerDisposed()
	{
		using var cache = new MemoryCache(TimeSpan.FromSeconds(1));
		using var scanner = new MemoryCacheScanner(cache);
		var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var workers = new Task[5];

		for(int index = 0; index < workers.Length - 1; index++)
		{
			workers[index] = Task.Run(async () =>
			{
				await start.Task;
				for(int iteration = 0; iteration < 100; iteration++)
				{
					try
					{
						scanner.Start();
					}
					catch(ObjectDisposedException)
					{
						//释放后不能重新启动。
					}

					scanner.Stop();
				}
			});
		}

		workers[^1] = Task.Run(async () =>
		{
			await start.Task;
			cache.Dispose();
			scanner.Dispose();
		});
		start.SetResult();
		await Task.WhenAll(workers).WaitAsync(_timeout);
		Assert.Throws<ObjectDisposedException>(scanner.Start);
		scanner.Stop();
		scanner.Dispose();
	}

	[Fact]
	public void Scan_DisposedCache_IsHarmless()
	{
		using var cache = new MemoryCache();
		var scan = typeof(MemoryCache).GetMethod("Scan", BindingFlags.Instance | BindingFlags.NonPublic).CreateDelegate<Action>(cache);
		cache.Dispose();
		scan();
	}

	private static TValue GetField<TValue>(object instance, string name) =>
		(TValue)instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(instance);

	private sealed class ScanToken : IChangeToken
	{
		private Action _callback;
		private bool _expired;
		public Action Callback { set => Volatile.Write(ref _callback, value); }
		public bool ActiveChangeCallbacks => false;
		public bool HasChanged
		{
			get
			{
				Interlocked.Exchange(ref _callback, null)?.Invoke();
				return Volatile.Read(ref _expired);
			}
		}

		public void Expire() => Volatile.Write(ref _expired, true);
		public IDisposable RegisterChangeCallback(Action<object> callback, object state) => throw new NotSupportedException();
	}
}
