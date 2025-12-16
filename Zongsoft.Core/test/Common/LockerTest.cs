using System;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Common.Tests;

public class LockerTest
{
	const int COUNT = 500;

	[Fact]
	public void Lock()
	{
		var count = 0;
		var locker = new Locker();

		Parallel.For(0, COUNT, i =>
		{
			using(locker.Lock())
			{
				count++;
			}
		});

		Assert.Equal(COUNT, count);
	}

	[Fact]
	public void TryLock()
	{
		var count = 0;
		var failure = 0;
		var success = 0;
		var locker = new Locker();

		Parallel.For(0, COUNT, i =>
		{
			if(locker.TryLock(() => count++, TimeSpan.FromMilliseconds(100)))
				Interlocked.Increment(ref success);
			else
				Interlocked.Increment(ref failure);
		});

		Assert.Equal(success, count);
		Assert.Equal(COUNT, success + failure);
	}

	[Fact]
	public async Task LockAsync()
	{
		var count = 0;
		var locker = new Locker();

		var tasks = new Task[]
		{
			IncreaseAsync(),
			IncreaseAsync(),
			IncreaseAsync(),
			IncreaseAsync(),
			IncreaseAsync(),
			IncreaseAsync(),
			IncreaseAsync(),
			IncreaseAsync(),
			IncreaseAsync(),
			IncreaseAsync(),
		};

		//确保所有任务都已执行完毕
		await Task.WhenAll(tasks);

		Assert.Equal(COUNT * tasks.Length, count);

		async Task IncreaseAsync()
		{
			for(int i = 0; i < COUNT; i++)
			{
				using(await locker.LockAsync())
				{
					count++;
				}
			}
		}
	}

	[Fact]
	public async Task TryLockAsync()
	{
		var count = 0;
		var locker = new Locker();

		var tasks = new Task[]
		{
			IncreaseAsync(),
			IncreaseAsyncWithTimeout(),
			IncreaseAsync(),
			IncreaseAsyncWithTimeout(),
			IncreaseAsync(),
			IncreaseAsyncWithTimeout(),
			IncreaseAsync(),
			IncreaseAsyncWithTimeout(),
			IncreaseAsync(),
			IncreaseAsyncWithTimeout(),
		};

		//确保所有任务都已执行完毕
		await Task.WhenAll(tasks);

		Assert.Equal(COUNT * tasks.Length, count);

		async Task IncreaseAsync()
		{
			for(int i = 0; i < COUNT; i++)
			{
				await locker.TryLockAsync(() =>
				{
					count++;
				}, CancellationToken.None);
			}
		}

		async Task IncreaseAsyncWithTimeout()
		{
			for(int i = 0; i < COUNT; i++)
			{
				await locker.TryLockAsync(() =>
				{
					count++;
				}, TimeSpan.FromMilliseconds(1000));
			}
		}
	}
}
