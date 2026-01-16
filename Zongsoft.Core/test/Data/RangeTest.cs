using System;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Data.Tests;

public class RangeTest
{
	[Fact]
	public void Test()
	{
		object min, max;

		var age = new Range<byte>(100, 18);
		var ageText = age.ToString();

		var range = Range<int>.Parse(ageText);
		Assert.Equal(18, range.Minimum);
		Assert.Equal(100, range.Maximum);

		range = 127;
		Assert.Equal(127, range.Minimum);
		Assert.Equal(127, range.Maximum);

		range = Range<int>.Parse("0 ");
		Assert.Equal(0, range.Minimum);
		Assert.Equal(0, range.Maximum);

		range = Range<int>.Parse("1~1");
		Assert.Equal(1, range.Minimum);
		Assert.Equal(1, range.Maximum);

		range = Range<int>.Parse(" 520 ");
		Assert.Equal(520, range.Minimum);
		Assert.Equal(520, range.Maximum);

		range = Range<int>.Parse("  * ~  520  ");
		Assert.Null(range.Minimum);
		Assert.Equal(520, range.Maximum);

		range = Range<int>.Parse(" ( ? ~  520 ) ");
		Assert.Null(range.Minimum);
		Assert.Equal(520, range.Maximum);

		Assert.True(Range.IsRange(age, out min, out max));
		Assert.Equal(18, (byte)min);
		Assert.Equal(100, (byte)max);

		var birthdate = new Range<DateTime>(new DateTime(1979, 6, 9), null);
		var birthdateText = birthdate.ToString().TrimStart('(').TrimEnd(')');

		var duration = Range<DateTime>.Parse(birthdateText);
		Assert.Equal(new DateTime(1979, 6, 9), duration.Minimum);
		Assert.Null(duration.Maximum);

		Assert.True(Range.IsRange(birthdate, out min, out max));
		Assert.Equal(new DateTime(1979, 6, 9), (DateTime)min);
		Assert.Null(max);
	}

	[Fact]
	public void TestToday()
	{
		var today = Range<DateTime>.Parse("today()");

		Assert.NotNull(today.Minimum);
		Assert.NotNull(today.Maximum);
		Assert.Equal(DateTime.Today, today.Minimum.Value);
		Assert.Equal(DateTime.Today, today.Maximum.Value.Date);
		Assert.Equal(23, today.Maximum.Value.Hour);
		Assert.Equal(59, today.Maximum.Value.Minute);
		Assert.Equal(59, today.Maximum.Value.Second);
	}

	[Fact]
	public void TestYesterday()
	{
		var yesterday = Range<DateTime>.Parse("Yesterday");

		Assert.NotNull(yesterday.Minimum);
		Assert.NotNull(yesterday.Maximum);
		Assert.Equal(DateTime.Today.AddDays(-1), yesterday.Minimum.Value);
		Assert.Equal(DateTime.Today.AddDays(-1), yesterday.Maximum.Value.Date);
		Assert.Equal(23, yesterday.Maximum.Value.Hour);
		Assert.Equal(59, yesterday.Maximum.Value.Minute);
		Assert.Equal(59, yesterday.Maximum.Value.Second);
	}

	[Fact]
	public void TestThisWeek()
	{
		var thisweek = Range<DateTime>.Parse("ThisWeek ( ) ");

		Assert.NotNull(thisweek.Minimum);
		Assert.NotNull(thisweek.Maximum);
		Assert.Equal(0, thisweek.Minimum.Value.Hour);
		Assert.Equal(0, thisweek.Minimum.Value.Minute);
		Assert.Equal(0, thisweek.Minimum.Value.Second);
		Assert.Equal(23, thisweek.Maximum.Value.Hour);
		Assert.Equal(59, thisweek.Maximum.Value.Minute);
		Assert.Equal(59, thisweek.Maximum.Value.Second);
	}

	[Fact]
	public void TestAgo()
	{
		var just = DateTime.Now;
		var year1 = Range<DateTime>.Parse("ago(1y)");

		Assert.Null(year1.Minimum);
		Assert.NotNull(year1.Maximum);
		Assert.Equal(DateTime.Today.AddYears(-1), year1.Maximum.Value.Date);
		Assert.InRange(year1.Maximum.Value.TimeOfDay, just.TimeOfDay, DateTime.Now.TimeOfDay);

		var month2 = Range<DateTime>.Parse(" ago ( 2M ) ");

		Assert.Null(month2.Minimum);
		Assert.NotNull(month2.Maximum);
		Assert.Equal(DateTime.Today.AddMonths(-2), month2.Maximum.Value.Date);
		Assert.InRange(month2.Maximum.Value.TimeOfDay, just.TimeOfDay, DateTime.Now.TimeOfDay);

		var day3 = Range<DateTime>.Parse(" ago( 3d ) ");

		Assert.Null(day3.Minimum);
		Assert.NotNull(day3.Maximum);
		Assert.Equal(DateTime.Today.AddDays(-3), day3.Maximum.Value.Date);
		Assert.InRange(day3.Maximum.Value.TimeOfDay, just.TimeOfDay, DateTime.Now.TimeOfDay);

		var hours3 = Range<DateTime>.Parse(" ago( 4h ) ");

		Assert.Null(hours3.Minimum);
		Assert.NotNull(hours3.Maximum);
		Assert.InRange(hours3.Maximum.Value, just.AddHours(-4), DateTime.Now.AddHours(-4));

		var minutes5 = Range<DateTime>.Parse(" ago (5m) ");

		Assert.Null(minutes5.Minimum);
		Assert.NotNull(minutes5.Maximum);
		Assert.InRange(minutes5.Maximum.Value, just.AddMinutes(-5), DateTime.Now.AddMinutes(-5));

		var seconds10 = Range<DateTime>.Parse(" ago(10s) ");

		Assert.Null(seconds10.Minimum);
		Assert.NotNull(seconds10.Maximum);
		Assert.InRange(seconds10.Maximum.Value, just.AddSeconds(-10), DateTime.Now.AddSeconds(-10));
	}

	[Fact]
	public void TestLast()
	{
		var just = DateTime.Now;
		var year1 = Range<DateTime>.Parse("last(1y)");

		Assert.NotNull(year1.Minimum);
		Assert.NotNull(year1.Maximum);
		Assert.InRange(year1.Minimum.Value, just.AddYears(-1), DateTime.Now.AddYears(-1));
		Assert.InRange(year1.Maximum.Value, just, DateTime.Now);

		var month2 = Range<DateTime>.Parse(" last ( 2M   ) ");

		Assert.NotNull(month2.Minimum);
		Assert.NotNull(month2.Maximum);
		Assert.InRange(month2.Minimum.Value, just.AddMonths(-2), DateTime.Now.AddMonths(-2));
		Assert.InRange(month2.Maximum.Value, just, DateTime.Now);

		var day13 = Range<DateTime>.Parse(" last ( 13D)  ");

		Assert.NotNull(day13.Minimum);
		Assert.NotNull(day13.Maximum);
		Assert.InRange(day13.Minimum.Value, just.AddDays(-13), DateTime.Now.AddDays(-13));
		Assert.InRange(day13.Maximum.Value, just, DateTime.Now);

		var hours15 = Range<DateTime>.Parse(" last ( 15h)  ");

		Assert.NotNull(hours15.Minimum);
		Assert.NotNull(hours15.Maximum);
		Assert.InRange(hours15.Minimum.Value, just.AddHours(-15), DateTime.Now.AddHours(-15));
		Assert.InRange(hours15.Maximum.Value, just, DateTime.Now);

		var minutes35 = Range<DateTime>.Parse(" last ( 35m)");

		Assert.NotNull(minutes35.Minimum);
		Assert.NotNull(minutes35.Maximum);
		Assert.InRange(minutes35.Minimum.Value, just.AddMinutes(-35), DateTime.Now.AddMinutes(-35));
		Assert.InRange(minutes35.Maximum.Value, just, DateTime.Now);

		var seconds152 = Range<DateTime>.Parse("last( 152s)");

		Assert.NotNull(seconds152.Minimum);
		Assert.NotNull(seconds152.Maximum);
		Assert.InRange(seconds152.Minimum.Value, just.AddSeconds(-152), DateTime.Now.AddSeconds(-152));
		Assert.InRange(seconds152.Maximum.Value, just, DateTime.Now);
	}

	[Fact]
	public void TestYear()
	{
		static void Verify(int year, Range<DateTime> range)
		{
			Assert.NotNull(range.Minimum);
			Assert.NotNull(range.Maximum);

			Assert.Equal(year, range.Minimum.Value.Year);
			Assert.Equal(1, range.Minimum.Value.Month);
			Assert.Equal(1, range.Minimum.Value.Day);
			Assert.Equal(0, range.Minimum.Value.Hour);
			Assert.Equal(0, range.Minimum.Value.Minute);
			Assert.Equal(0, range.Minimum.Value.Second);

			Assert.Equal(year, range.Maximum.Value.Year);
			Assert.Equal(12, range.Maximum.Value.Month);
			Assert.Equal(31, range.Maximum.Value.Day);
			Assert.Equal(23, range.Maximum.Value.Hour);
			Assert.Equal(59, range.Maximum.Value.Minute);
			Assert.Equal(59, range.Maximum.Value.Second);
		}

		Verify(2020, Range<DateTime>.Parse("year(2020)"));
		Verify(1979, Range<DateTime>.Parse("year (1979 ) "));
		Verify(2000, Range<DateTime>.Parse("year( 2000  ) "));
		Verify(2010, Range<DateTime>.Parse(" year ( 2010  ) "));
	}

	[Fact]
	public void TestMonth()
	{
		static void Verify(int year, int month, Range<DateTime> range)
		{
			Assert.NotNull(range.Minimum);
			Assert.NotNull(range.Maximum);

			Assert.Equal(year, range.Minimum.Value.Year);
			Assert.Equal(month, range.Minimum.Value.Month);
			Assert.Equal(1, range.Minimum.Value.Day);
			Assert.Equal(0, range.Minimum.Value.Hour);
			Assert.Equal(0, range.Minimum.Value.Minute);
			Assert.Equal(0, range.Minimum.Value.Second);

			Assert.Equal(year, range.Maximum.Value.Year);
			Assert.Equal(month, range.Maximum.Value.Month);
			Assert.Equal(DateTime.DaysInMonth(year, month), range.Maximum.Value.Day);
			Assert.Equal(23, range.Maximum.Value.Hour);
			Assert.Equal(59, range.Maximum.Value.Minute);
			Assert.Equal(59, range.Maximum.Value.Second);
		}

		Verify(2020, 1, Range<DateTime>.Parse("month(2020, 1)"));
		Verify(1979, 2, Range<DateTime>.Parse("month (1979,2 ) "));
		Verify(2000, 2, Range<DateTime>.Parse(" month( 2000 , 2 ) "));
		Verify(2010, 12, Range<DateTime>.Parse(" month ( 2010  ,12 ) "));
	}

	[Fact]
	public void TestConverter()
	{
		const string JSON = @"{""age"":""18~40"",""weight"":""50"",""timestamp"":""2020-6-1~2020-12-31""}";
		var data = Serialization.Serializer.Json.Deserialize<TestConditional>(JSON);

		Assert.NotNull(data);
		Assert.NotNull(data.Age);
		Assert.NotNull(data.Weight);
		Assert.NotNull(data.Timestamp);

		Assert.True(data.Birthday.IsEmpty);

		Assert.Equal(18, (int)data.Age.Value.Minimum);
		Assert.Equal(40, (int)data.Age.Value.Maximum);
		Assert.Equal(50, (int)data.Weight.Value.Minimum);
		Assert.Equal(50, (int)data.Weight.Value.Maximum);
		Assert.Equal(new DateTime(2020, 6, 1), data.Timestamp.Value.Minimum);
		Assert.Equal(new DateTime(2020, 12, 31), data.Timestamp.Value.Maximum);
	}
}

public class TestConditional
{
	public Range<byte>? Age { get; set; }
	public Range<ushort>? Weight { get; set; }
	public Range<DateTime>? Timestamp { get; set; }
	public Range<DateTime> Birthday { get; set; }
}
