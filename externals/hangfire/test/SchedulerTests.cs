using System;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Services;
using Zongsoft.Scheduling;

namespace Zongsoft.Externals.Hangfire.Tests;

public class SchedulerTests
{
	[Fact]
	public void Cron_Match_RecognizesCronNameIgnoringCase()
	{
		var matchable = Assert.IsAssignableFrom<IMatchable>(Scheduler.Cron);
		var typed = Assert.IsAssignableFrom<IMatchable<string>>(Scheduler.Cron);

		Assert.True(matchable.Match("cRoN"));
		Assert.True(typed.Match("cRoN"));
		Assert.False(matchable.Match("Latency"));
		Assert.False(matchable.Match(1));
	}

	[Fact]
	public void Latency_Match_RecognizesLatencyNameIgnoringCase()
	{
		var matchable = Assert.IsAssignableFrom<IMatchable>(Scheduler.Latency);
		var typed = Assert.IsAssignableFrom<IMatchable<string>>(Scheduler.Latency);

		Assert.True(matchable.Match("lAtEnCy"));
		Assert.True(typed.Match("lAtEnCy"));
		Assert.False(matchable.Match("Cron"));
		Assert.False(matchable.Match(1));
	}

	[Fact]
	public async Task CronScheduleAsync_NullOptions_ThrowsArgumentNullException()
	{
		var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
			await Scheduler.Cron.ScheduleAsync("job", null));

		Assert.Equal("options", exception.ParamName);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	public async Task CronScheduleAsync_MissingExpression_ThrowsArgumentException(string expression)
	{
		var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
			await Scheduler.Cron.ScheduleAsync("job", new TriggerOptions.Cron(expression)));

		Assert.Contains("Cron expression", exception.Message, StringComparison.Ordinal);
	}

	[Fact]
	public async Task LatencyScheduleAsync_NullOptions_ThrowsArgumentNullException()
	{
		var exception = await Assert.ThrowsAsync<ArgumentNullException>(async () =>
			await Scheduler.Latency.ScheduleAsync("job", null));

		Assert.Equal("options", exception.ParamName);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	public async Task RescheduleAsync_EmptyIdentifier_ReturnsFalseForBothSchedulers(string identifier)
	{
		Assert.False(await Scheduler.Cron.RescheduleAsync(identifier));
		Assert.False(await Scheduler.Latency.RescheduleAsync(identifier));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	public async Task UnscheduleAsync_EmptyIdentifier_ReturnsFalseForBothSchedulers(string identifier)
	{
		Assert.False(await Scheduler.Cron.UnscheduleAsync(identifier));
		Assert.False(await Scheduler.Latency.UnscheduleAsync(identifier));
	}

	[Fact]
	public void Storage_SetNull_ThrowsArgumentNullException()
	{
		var exception = Assert.Throws<ArgumentNullException>(() =>
		{
			Scheduler.Storage = null;
		});

		Assert.Equal("value", exception.ParamName);
	}
}
