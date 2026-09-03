using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Common.Tests;

public class LockerTest
{
	const int COUNT = 500;
	private const string DoubleDisposeProbeEnvironment = "ZONGSOFT_LOCKER_DOUBLE_DISPOSE_PROBE";

	[Fact]
	public async Task LockAsync_CanceledWhileWaiting_DoesNotSplitFutureLock()
	{
		await using var locker = new Locker();
		var holderEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var releaseHolder = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		Task holder;

		using(ExecutionContext.SuppressFlow())
		{
			holder = Task.Run(async () =>
			{
				await using(await locker.LockAsync())
				{
					holderEntered.TrySetResult();
					await releaseHolder.Task;
				}
			});
		}

		await holderEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		using var cancellation = new CancellationTokenSource();
		var waiting = locker.LockAsync(cancellation.Token).AsTask();
		Assert.False(waiting.IsCompleted);
		cancellation.Cancel();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);

		releaseHolder.TrySetResult();
		await holder.WaitAsync(TimeSpan.FromSeconds(5));

		var held = locker.Lock();
		var disposing = locker.DisposeAsync().AsTask();
		var disposedBeforeRelease = disposing.IsCompleted;
		held.Dispose();
		await disposing.WaitAsync(TimeSpan.FromSeconds(5));

		Assert.False(disposedBeforeRelease, "A canceled waiter left the active lock outside the topmost semaphore chain.");
	}

	[Fact]
	public async Task Releaser_DisposedTwice_ChildProcessPreservesMutualExclusion()
	{
		if(string.Equals(Environment.GetEnvironmentVariable(DoubleDisposeProbeEnvironment), "1", StringComparison.Ordinal))
		{
			await RunDoubleDisposeProbeAsync();
			return;
		}

		var start = new ProcessStartInfo("dotnet")
		{
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardError = true,
			RedirectStandardOutput = true,
		};

		start.ArgumentList.Add(typeof(LockerTest).Assembly.Location);
		start.ArgumentList.Add("-method");
		start.ArgumentList.Add($"{typeof(LockerTest).FullName}.{nameof(Releaser_DisposedTwice_ChildProcessPreservesMutualExclusion)}");
		start.ArgumentList.Add("-parallel");
		start.ArgumentList.Add("none");
		start.Environment[DoubleDisposeProbeEnvironment] = "1";

		using var process = Process.Start(start);
		Assert.NotNull(process);

		var standardOutput = process.StandardOutput.ReadToEndAsync();
		var standardError = process.StandardError.ReadToEndAsync();
		using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(20));

		try
		{
			await process.WaitForExitAsync(cancellation.Token);
		}
		catch(OperationCanceledException)
		{
			process.Kill(true);
			throw new TimeoutException("The isolated Locker double-dispose probe did not exit within 20 seconds.");
		}

		var output = await standardOutput;
		var error = await standardError;
		Assert.True(process.ExitCode == 0, $"The isolated Locker probe exited with code {process.ExitCode}.{Environment.NewLine}{output}{Environment.NewLine}{error}");
	}

	[Fact]
	public async Task DisposeAsync_WhileHeld_WaitsThenRejectsNewLocks()
	{
		var locker = new Locker();
		var held = locker.Lock();
		var disposing = locker.DisposeAsync().AsTask();
		var concurrentDisposing = locker.DisposeAsync().AsTask();

		Assert.False(disposing.IsCompleted);
		Assert.Same(disposing, concurrentDisposing);
		held.Dispose();
		await disposing.WaitAsync(TimeSpan.FromSeconds(5));
		await locker.DisposeAsync();

		Assert.Throws<ObjectDisposedException>(() => locker.Lock());
		await Assert.ThrowsAsync<ObjectDisposedException>(() => locker.LockAsync().AsTask());
	}

	private static async Task RunDoubleDisposeProbeAsync()
	{
		var locker = new Locker();
		var releaser = locker.Lock();
		releaser.Dispose();
		releaser.Dispose();

		var held = locker.Lock();
		var disposing = locker.DisposeAsync().AsTask();
		var disposedBeforeRelease = disposing.IsCompleted;
		held.Dispose();
		await disposing.WaitAsync(TimeSpan.FromSeconds(5));

		Assert.False(disposedBeforeRelease, "Disposing a releaser twice increased the semaphore capacity.");
	}

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
