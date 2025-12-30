using System;

using Xunit;

namespace Zongsoft.Common.Tests;

public class TimeSpanTest
{
	[Fact]
	public void Clamp()
	{
		var minimum = TimeSpan.FromHours(1);
		var maximum = TimeSpan.FromHours(12);

		var duration = TimeSpanUtility.Clamp(new TimeSpan(0, 2, 30, 59), minimum, maximum);
		Assert.Equal(new TimeSpan(0, 2, 30, 59), duration);

		duration = TimeSpanUtility.Clamp(TimeSpan.Zero, minimum, maximum);
		Assert.Equal(minimum, duration);
		duration = TimeSpanUtility.Clamp(TimeSpan.MinValue, minimum, maximum);
		Assert.Equal(minimum, duration);
		duration = TimeSpanUtility.Clamp(new TimeSpan(0, 0, 30, 40), minimum, maximum);
		Assert.Equal(minimum, duration);

		duration = TimeSpanUtility.Clamp(TimeSpan.MaxValue, minimum, maximum);
		Assert.Equal(maximum, duration);
		duration = TimeSpanUtility.Clamp(new TimeSpan(1, 0, 0, 1), minimum, maximum);
		Assert.Equal(maximum, duration);
	}

	[Fact]
	public void TryParse()
	{
		Assert.True(TimeSpanUtility.TryParse(null, out var result));
		Assert.Equal(TimeSpan.Zero, result);
		Assert.True(TimeSpanUtility.TryParse("", out result));
		Assert.Equal(TimeSpan.Zero, result);
		Assert.True(TimeSpanUtility.TryParse("0", out result));
		Assert.Equal(TimeSpan.Zero, result);
		Assert.True(TimeSpanUtility.TryParse("00", out result));
		Assert.Equal(TimeSpan.Zero, result);

		Assert.True(TimeSpanUtility.TryParse("1d", out result));
		Assert.Equal(new TimeSpan(1, 0, 0, 0), result);
		Assert.True(TimeSpanUtility.TryParse("1D", out result));
		Assert.Equal(new TimeSpan(1, 0, 0, 0), result);
		Assert.True(TimeSpanUtility.TryParse(".5d", out result));
		Assert.Equal(new TimeSpan(0, 12, 0, 0), result);
		Assert.True(TimeSpanUtility.TryParse("0.5D", out result));
		Assert.Equal(new TimeSpan(0, 12, 0, 0), result);

		Assert.True(TimeSpanUtility.TryParse("1h", out result));
		Assert.Equal(new TimeSpan(0, 1, 0, 0), result);
		Assert.True(TimeSpanUtility.TryParse("1H", out result));
		Assert.Equal(new TimeSpan(0, 1, 0, 0), result);
		Assert.True(TimeSpanUtility.TryParse(".1h", out result));
		Assert.Equal(new TimeSpan(0, 0, 6, 0), result);
		Assert.True(TimeSpanUtility.TryParse(".1H", out result));
		Assert.Equal(new TimeSpan(0, 0, 6, 0), result);

		Assert.True(TimeSpanUtility.TryParse("1m", out result));
		Assert.Equal(new TimeSpan(0, 0, 1, 0), result);
		Assert.True(TimeSpanUtility.TryParse("1M", out result));
		Assert.Equal(new TimeSpan(0, 0, 1, 0), result);
		Assert.True(TimeSpanUtility.TryParse(".5m", out result));
		Assert.Equal(new TimeSpan(0, 0, 0, 30), result);
		Assert.True(TimeSpanUtility.TryParse(".5M", out result));
		Assert.Equal(new TimeSpan(0, 0, 0, 30), result);

		Assert.True(TimeSpanUtility.TryParse("1s", out result));
		Assert.Equal(new TimeSpan(0, 0, 0, 1), result);
		Assert.True(TimeSpanUtility.TryParse("1S", out result));
		Assert.Equal(new TimeSpan(0, 0, 0, 1), result);
		Assert.True(TimeSpanUtility.TryParse("0.5s", out result));
		Assert.Equal(new TimeSpan(0, 0, 0, 0, 500), result);
		Assert.True(TimeSpanUtility.TryParse("0.5S", out result));
		Assert.Equal(new TimeSpan(0, 0, 0, 0, 500), result);

		Assert.True(TimeSpanUtility.TryParse("1ms", out result));
		Assert.Equal(new TimeSpan(0, 0, 0, 0, 1), result);
		Assert.True(TimeSpanUtility.TryParse("1MS", out result));
		Assert.Equal(new TimeSpan(0, 0, 0, 0, 1), result);

		Assert.True(TimeSpanUtility.TryParse("1.02:03:04", out result));
		Assert.Equal(new TimeSpan(1, 2, 3, 4), result);
		Assert.True(TimeSpanUtility.TryParse("2:03:04", out result));
		Assert.Equal(new TimeSpan(0, 2, 3, 4), result);
		Assert.True(TimeSpanUtility.TryParse("3:4", out result));
		Assert.Equal(new TimeSpan(0, 3, 4, 0), result);
		Assert.False(TimeSpanUtility.TryParse("invalid", out _));
	}
}
