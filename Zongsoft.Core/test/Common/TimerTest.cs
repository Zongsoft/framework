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
	#endregion

	#region 构造函数
	public TimerTest() => _timer = new Timer(TimeSpan.FromMilliseconds(10), this.OnTick);
	#endregion

	#region 测试方法
	[Fact]
	public void Test()
	{
		_timer.Start();
		SpinWait.SpinUntil(() => _count >= LIMIT, 2000);
		Assert.Equal(LIMIT, _count);
	}
	#endregion

	#region 时钟回调
	private ValueTask OnTick(object state, CancellationToken cancellation)
	{
		if(Interlocked.Increment(ref _count) >= LIMIT)
			_timer.Stop();

		return ValueTask.CompletedTask;
	}
	#endregion
}
