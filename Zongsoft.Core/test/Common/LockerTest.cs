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
	public async Task LockAsync1()
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
				await using(await locker.LockAsync())
				{
					count++;
				}
			}
		}
	}

	[Fact]
	public async Task LockAsync2()
	{
		const int TIMES = 50;

		var count = 0;
		var locker = new Locker();

		await Parallel.ForAsync(0, TIMES, async (_, cancellation) =>
		{
			for(int i = 0; i < COUNT; i++)
			{
				await using(await locker.LockAsync(cancellation))
				{
					count++;
				}
			}
		});

		Assert.Equal(COUNT * TIMES, count);
	}
}
