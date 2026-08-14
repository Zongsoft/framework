using System;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Common.Tests;

public class TimerTest
{
	#region 常量定义
	private const int LIMIT = 10;
	#endregion

	#region 私有变量
	private int _count;
	private readonly Timer _timer;
	private readonly TaskCompletionSource _completion;
	#endregion

	#region 构造函数
	public TimerTest()
	{
		_timer = new Timer(TimeSpan.FromMilliseconds(1), this.OnTick);
		_completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
	}
	#endregion

	#region 测试方法
	[Fact]
	public async Task Test()
	{
		Assert.False(_timer.IsRunning);
		_timer.Start(TestContext.Current.CancellationToken);
		Assert.True(_timer.IsRunning);

		await _completion.Task.WaitAsync(TimeSpan.FromSeconds(30), TestContext.Current.CancellationToken);
		Assert.Equal(LIMIT, _count);
		Assert.False(_timer.IsRunning);
	}
	#endregion

	#region 时钟回调
	private ValueTask OnTick(object state, CancellationToken cancellation)
	{
		if(Interlocked.Increment(ref _count) >= LIMIT)
		{
			_timer.Stop();
			_completion.TrySetResult();
		}

		return ValueTask.CompletedTask;
	}
	#endregion
}
