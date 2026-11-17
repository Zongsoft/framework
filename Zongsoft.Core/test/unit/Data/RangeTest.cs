using System;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

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
		Verify(2020, Range<DateTime>.Parse("year(2020)"));
		Verify(1979, Range<DateTime>.Parse("year (1979 ) "));
		Verify(2000, Range<DateTime>.Parse("year( 2000  ) "));
		Verify(2010, Range<DateTime>.Parse(" year ( 2010  ) "));

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
	}

	[Fact]
	public void TestMonth()
	{
		Verify(2020, 1, Range<DateTime>.Parse("month(2020, 1)"));
		Verify(1979, 2, Range<DateTime>.Parse("month (1979,2 ) "));
		Verify(2000, 2, Range<DateTime>.Parse(" month( 2000 , 2 ) "));
		Verify(2010, 12, Range<DateTime>.Parse(" month ( 2010  ,12 ) "));

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

	[Theory]
	[InlineData(2024, 2, 29)]
	[InlineData(2000, 12, 31)]
	[InlineData(1, 1, 1)]
	[InlineData(9999, 12, 31)]
	public void TestDateOnlyFactories(int year, int month, int day)
	{
		var date = new DateOnly(year, month, day);
		var single = Range.Timing.Day(date);
		Assert.Equal(date, single.Minimum);
		Assert.Equal(date, single.Maximum);
		Assert.True(single.Contains(date));
		Assert.Equal(single, Range.Timing.Day<DateOnly>(year, month, day));
		Assert.Equal(single, Range.Timing.Day<DateOnly>(date.ToDateTime(TimeOnly.MinValue)));

		var monthly = Range.Timing.Month<DateOnly>(year, month);
		Assert.Equal(new DateOnly(year, month, 1), monthly.Minimum);
		Assert.Equal(new DateOnly(year, month, DateTime.DaysInMonth(year, month)), monthly.Maximum);

		var yearly = Range.Timing.Year<DateOnly>(year);
		Assert.Equal(new DateOnly(year, 1, 1), yearly.Minimum);
		Assert.Equal(new DateOnly(year, 12, 31), yearly.Maximum);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(480)]
	[InlineData(-330)]
	[InlineData(840)]
	public void TestDateTimeOffsetDay(int minutes)
	{
		var offset = TimeSpan.FromMinutes(minutes);
		var date = new DateTimeOffset(2024, 2, 29, 12, 30, 45, offset);
		var range = Range.Timing.Day(date);
		Assert.Equal(new DateTimeOffset(2024, 2, 29, 0, 0, 0, offset), range.Minimum);
		Assert.Equal(new DateTimeOffset(2024, 2, 29, 23, 59, 59, 999, offset), range.Maximum);
		Assert.Equal(offset, range.Minimum.Value.Offset);
		Assert.Equal(offset, range.Maximum.Value.Offset);
		Assert.True(range.Contains(date.ToUniversalTime()));
	}

	[Theory]
	[InlineData(2024, 2, 29)]
	[InlineData(2025, 2, 28)]
	[InlineData(2026, 12, 31)]
	public void TestDateTimeOffsetFactories(int year, int month, int day)
	{
		AssertOffsetRange(Range.Timing.Day(year, month, day), Range.Timing.Day<DateTimeOffset>(year, month, day));
		AssertOffsetRange(Range.Timing.Day(new DateTime(year, month, day)), Range.Timing.Day<DateTimeOffset>(new DateTime(year, month, day)));
		AssertOffsetRange(Range.Timing.Month(year, month), Range.Timing.Month<DateTimeOffset>(year, month));
		AssertOffsetRange(Range.Timing.Year(year), Range.Timing.Year<DateTimeOffset>(year));

		Assert.Equal(Range.Timing.Day(year, month, day), Range.Timing.Day<DateTime>(year, month, day));
		Assert.Equal(Range.Timing.Month(year, month), Range.Timing.Month<DateTime>(year, month));
		Assert.Equal(Range.Timing.Year(year), Range.Timing.Year<DateTime>(year));
	}

	[Theory]
	[InlineData(DateTimeKind.Unspecified)]
	[InlineData(DateTimeKind.Local)]
	[InlineData(DateTimeKind.Utc)]
	public void TestDateTimeKind(DateTimeKind kind)
	{
		var date = new DateTime(2024, 2, 29, 12, 30, 45, kind);
		var maximum = new DateTime(2024, 2, 29, 23, 59, 59, 999, kind);
		var range = Range.Timing.Day<DateTimeOffset>(date);
		Assert.Equal(new DateTimeOffset(date.Date), range.Minimum);
		Assert.Equal(new DateTimeOffset(maximum), range.Maximum);
		Assert.Equal(new DateTimeOffset(date.Date).Offset, range.Minimum.Value.Offset);
		Assert.Equal(new DateTimeOffset(maximum).Offset, range.Maximum.Value.Offset);
		Assert.Equal(Range.Timing.Day(date), Range.Timing.Day<DateTime>(date));
	}

	[Theory]
	[InlineData("today")]
	[InlineData("tomorrow")]
	[InlineData("yesterday")]
	[InlineData("thisweek")]
	[InlineData("lastweek")]
	[InlineData("thismonth")]
	[InlineData("lastmonth")]
	[InlineData("thisyear")]
	[InlineData("lastyear")]
	public void TestCalendarExpressions(string name)
	{
		var first = Range<DateTime>.Parse(name);
		var dates = GetCalendarRange<DateOnly>(name);
		var offsets = GetCalendarRange<DateTimeOffset>(name);
		var timestamps = GetCalendarRange<DateTime>(name);
		var parsedDates = Range<DateOnly>.Parse($" {name.ToUpperInvariant()} ( ) ");
		var parsedOffsets = Range<DateTimeOffset>.Parse($" {name.ToUpperInvariant()} ( ) ");
		Assert.True(Range.TryParse<DateOnly>(name, out var triedDates));
		Assert.True(Range.TryParse<DateTimeOffset>(name, out var triedOffsets));
		var last = Range<DateTime>.Parse(name);

		AssertDateRange(first, last, dates);
		AssertDateRange(first, last, parsedDates);
		AssertDateRange(first, last, triedDates);
		Assert.Contains(timestamps, new[] { first, last });
		AssertCalendarOffsetRange(first, last, offsets);
		AssertCalendarOffsetRange(first, last, parsedOffsets);
		AssertCalendarOffsetRange(first, last, triedOffsets);
	}

	[Theory]
	[InlineData("year(2024)", 2024, 1, 1, 2024, 12, 31)]
	[InlineData(" Month ( 2024 , 2 ) ", 2024, 2, 1, 2024, 2, 29)]
	[InlineData("day(2024,2,29)", 2024, 2, 29, 2024, 2, 29)]
	public void TestCalendarArguments(string text, int firstYear, int firstMonth, int firstDay, int lastYear, int lastMonth, int lastDay)
	{
		var minimum = new DateTime(firstYear, firstMonth, firstDay);
		var maximum = new DateTime(lastYear, lastMonth, lastDay, 23, 59, 59, 999);
		var expected = new Range<DateTime>(minimum, maximum);
		Assert.Equal(expected, Range<DateTime>.Parse(text));
		Assert.True(Range<DateTime>.TryParse(text, out var timestamps));
		Assert.Equal(expected, timestamps);

		AssertDateRange(expected, expected, Range.Parse<DateOnly>(text));
		Assert.True(Range<DateOnly>.TryParse(text, out var dates));
		AssertDateRange(expected, expected, dates);
		AssertOffsetRange(expected, Range.Parse<DateTimeOffset>(text));
		Assert.True(Range<DateTimeOffset>.TryParse(text, out var offsets));
		AssertOffsetRange(expected, offsets);
		var json = Serialization.Serializer.Json.Serialize(text);
		AssertDateRange(expected, expected, Serialization.Serializer.Json.Deserialize<Range<DateOnly>>(json));
		AssertOffsetRange(expected, Serialization.Serializer.Json.Deserialize<Range<DateTimeOffset>>(json));
	}

	[Theory]
	[InlineData(0, 'y')]
	[InlineData(1, 'Y')]
	[InlineData(0, 'M')]
	[InlineData(2, 'M')]
	[InlineData(0, 'd')]
	[InlineData(3, 'D')]
	[InlineData(0, 'h')]
	[InlineData(4, 'H')]
	[InlineData(0, 'm')]
	[InlineData(5, 'm')]
	[InlineData(0, 's')]
	[InlineData(10, 'S')]
	public void TestRelativeExpressions(int number, char unit)
	{
		var before = DateTime.Now;
		var agoDates = Range.Timing.Ago<DateOnly>(number, unit);
		var agoOffsets = Range.Timing.Ago<DateTimeOffset>(number, unit);
		var agoTimestamps = Range.Timing.Ago<DateTime>(number, unit);
		var lastDates = Range.Timing.Last<DateOnly>(number, unit);
		var lastOffsets = Range.Timing.Last<DateTimeOffset>(number, unit);
		var lastTimestamps = Range.Timing.Last<DateTime>(number, unit);
		var parsedAgoDates = Range<DateOnly>.Parse($" ago ( {number}{unit} ) ");
		var parsedLastOffsets = Range<DateTimeOffset>.Parse($" last ( {number}{unit} ) ");
		Assert.True(Range<DateTimeOffset>.TryParse($"ago({number}{unit})", out var triedAgoOffsets));
		Assert.True(Range<DateOnly>.TryParse($"last({number}{unit})", out var triedLastDates));
		var after = DateTime.Now;

		var first = Shift(before, number, unit);
		var last = Shift(after, number, unit);
		Assert.Null(agoDates.Minimum);
		Assert.Null(agoOffsets.Minimum);
		Assert.Null(agoTimestamps.Minimum);
		Assert.Null(parsedAgoDates.Minimum);
		Assert.Null(triedAgoOffsets.Minimum);
		Assert.InRange(agoDates.Maximum.Value, DateOnly.FromDateTime(first), DateOnly.FromDateTime(last));
		Assert.InRange(parsedAgoDates.Maximum.Value, DateOnly.FromDateTime(first), DateOnly.FromDateTime(last));
		Assert.InRange(agoOffsets.Maximum.Value, new DateTimeOffset(first), new DateTimeOffset(last));
		Assert.InRange(triedAgoOffsets.Maximum.Value, new DateTimeOffset(first), new DateTimeOffset(last));
		Assert.InRange(agoTimestamps.Maximum.Value, first, last);

		first = number == 0 ? Start(before, unit) : first;
		last = number == 0 ? Start(after, unit) : last;
		Assert.InRange(lastDates.Minimum.Value, DateOnly.FromDateTime(first), DateOnly.FromDateTime(last));
		Assert.InRange(triedLastDates.Minimum.Value, DateOnly.FromDateTime(first), DateOnly.FromDateTime(last));
		Assert.InRange(lastDates.Maximum.Value, DateOnly.FromDateTime(before), DateOnly.FromDateTime(after));
		Assert.InRange(triedLastDates.Maximum.Value, DateOnly.FromDateTime(before), DateOnly.FromDateTime(after));
		Assert.InRange(lastOffsets.Minimum.Value, new DateTimeOffset(first), new DateTimeOffset(last));
		Assert.InRange(parsedLastOffsets.Minimum.Value, new DateTimeOffset(first), new DateTimeOffset(last));
		Assert.InRange(lastOffsets.Maximum.Value, new DateTimeOffset(before), new DateTimeOffset(after));
		Assert.InRange(parsedLastOffsets.Maximum.Value, new DateTimeOffset(before), new DateTimeOffset(after));
		Assert.InRange(lastTimestamps.Minimum.Value, first, last);
		Assert.InRange(lastTimestamps.Maximum.Value, before, after);
	}

	[Theory]
	[InlineData("2024-02-01~2024-02-29", "2024-02-01", "2024-02-29")]
	[InlineData("(2024-02-29~2024-02-01)", "2024-02-01", "2024-02-29")]
	[InlineData("2024-02-29", "2024-02-29", "2024-02-29")]
	[InlineData("?~2024-02-29", null, "2024-02-29")]
	[InlineData("2024-02-01~*", "2024-02-01", null)]
	public void TestDateOnlyLiterals(string text, string minimum, string maximum)
	{
		var range = Range<DateOnly>.Parse(text);
		Assert.Equal(minimum == null ? (DateOnly?)null : DateOnly.Parse(minimum), range.Minimum);
		Assert.Equal(maximum == null ? (DateOnly?)null : DateOnly.Parse(maximum), range.Maximum);
		Assert.True(Range<DateOnly>.TryParse(text, out var parsed));
		Assert.Equal(range, parsed);
		Assert.True(Range<DateOnly>.TryParse(range.ToString(), out var roundtrip));
		Assert.Equal(range, roundtrip);
		Assert.Equal(range, Range.Create(typeof(DateOnly), minimum, maximum));
		Assert.Equal(range, Serialization.Serializer.Json.Deserialize<Range<DateOnly>>(Serialization.Serializer.Json.Serialize(range)));
	}

	[Theory]
	[InlineData("2024-02-01T00:00:00+08:00~2024-02-29T23:59:59-05:00", "2024-02-01T00:00:00+08:00", "2024-02-29T23:59:59-05:00")]
	[InlineData("2024-02-01T00:00:00-05:00~2024-02-01T00:00:00+08:00", "2024-02-01T00:00:00+08:00", "2024-02-01T00:00:00-05:00")]
	[InlineData("2024-02-29T12:00:00Z", "2024-02-29T12:00:00Z", "2024-02-29T12:00:00Z")]
	[InlineData("?~2024-02-29T12:00:00+08:00", null, "2024-02-29T12:00:00+08:00")]
	[InlineData("2024-02-01T00:00:00+08:00~?", "2024-02-01T00:00:00+08:00", null)]
	public void TestDateTimeOffsetLiterals(string text, string minimum, string maximum)
	{
		var range = Range<DateTimeOffset>.Parse(text);
		var first = minimum == null ? (DateTimeOffset?)null : DateTimeOffset.Parse(minimum);
		var last = maximum == null ? (DateTimeOffset?)null : DateTimeOffset.Parse(maximum);
		Assert.Equal(first, range.Minimum);
		Assert.Equal(last, range.Maximum);
		Assert.Equal(first?.Offset, range.Minimum?.Offset);
		Assert.Equal(last?.Offset, range.Maximum?.Offset);
		Assert.True(Range<DateTimeOffset>.TryParse(text, out var parsed));
		Assert.Equal(range, parsed);
		Assert.True(Range<DateTimeOffset>.TryParse(range.ToString(), out var roundtrip));
		Assert.Equal(range, roundtrip);
		Assert.Equal(range, Range.Create(typeof(DateTimeOffset), minimum, maximum));
		Assert.Equal(range, Serialization.Serializer.Json.Deserialize<Range<DateTimeOffset>>(Serialization.Serializer.Json.Serialize(range)));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData("unknown()")]
	[InlineData("today(1)")]
	[InlineData("today()x")]
	[InlineData("year()")]
	[InlineData("year(2024,1)")]
	[InlineData("year(2024y)")]
	[InlineData("year(0)")]
	[InlineData("year(10000)")]
	[InlineData("year(999999999999999999999999)")]
	[InlineData("month(2024)")]
	[InlineData("month(2024,13)")]
	[InlineData("day(2023,2,29)")]
	[InlineData("day(2024,2,29")]
	[InlineData("ago()")]
	[InlineData("ago(0)")]
	[InlineData("ago(1)")]
	[InlineData("ago(1q)")]
	[InlineData("ago(2147483647y)")]
	[InlineData("last(1,2)")]
	[InlineData("last(0)")]
	[InlineData("last(1)")]
	[InlineData("last(2147483647M)")]
	public void TestInvalidExpressions(string text)
	{
		Assert.False(Range<DateTime>.TryParse(text, out var timestamps));
		Assert.False(Range<DateOnly>.TryParse(text, out var dates));
		Assert.False(Range<DateTimeOffset>.TryParse(text, out var offsets));
		Assert.True(timestamps.IsEmpty);
		Assert.True(dates.IsEmpty);
		Assert.True(offsets.IsEmpty);
		Assert.ThrowsAny<ArgumentException>(() => Range<DateTime>.Parse(text));
		Assert.ThrowsAny<ArgumentException>(() => Range<DateOnly>.Parse(text));
		Assert.ThrowsAny<ArgumentException>(() => Range<DateTimeOffset>.Parse(text));
	}

	[Fact]
	public void TestUnsupportedTimingType()
	{
		Assert.Throws<NotSupportedException>(() => Range.Timing.Today<int>());
	}

	private static Range<T> GetCalendarRange<T>(string name) where T : struct, IComparable<T> => name switch
	{
		"today" => Range.Timing.Today<T>(),
		"tomorrow" => Range.Timing.Tomorrow<T>(),
		"yesterday" => Range.Timing.Yesterday<T>(),
		"thisweek" => Range.Timing.ThisWeek<T>(),
		"lastweek" => Range.Timing.LastWeek<T>(),
		"thismonth" => Range.Timing.ThisMonth<T>(),
		"lastmonth" => Range.Timing.LastMonth<T>(),
		"thisyear" => Range.Timing.ThisYear<T>(),
		"lastyear" => Range.Timing.LastYear<T>(),
		_ => throw new ArgumentOutOfRangeException(nameof(name)),
	};

	private static DateTime Shift(DateTime value, int number, char unit) => unit switch
	{
		'Y' or 'y' => value.AddYears(-number),
		'M' => value.AddMonths(-number),
		'D' or 'd' => value.AddDays(-number),
		'H' or 'h' => value.AddHours(-number),
		'm' => value.AddMinutes(-number),
		'S' or 's' => value.AddSeconds(-number),
		_ => throw new ArgumentOutOfRangeException(nameof(unit)),
	};

	private static DateTime Start(DateTime value, char unit) => unit switch
	{
		'Y' or 'y' => new(value.Year, 1, 1),
		'M' => new(value.Year, value.Month, 1),
		'D' or 'd' => value.Date,
		'H' or 'h' => new(value.Year, value.Month, value.Day, value.Hour, 0, 0),
		'm' => new(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0),
		'S' or 's' => value,
		_ => throw new ArgumentOutOfRangeException(nameof(unit)),
	};

	private static void AssertDateRange(Range<DateTime> first, Range<DateTime> last, Range<DateOnly> actual)
	{
		Assert.Contains(actual, new[]
		{
			new Range<DateOnly>(DateOnly.FromDateTime(first.Minimum.Value), DateOnly.FromDateTime(first.Maximum.Value)),
			new Range<DateOnly>(DateOnly.FromDateTime(last.Minimum.Value), DateOnly.FromDateTime(last.Maximum.Value)),
		});
	}

	private static void AssertOffsetRange(Range<DateTime> expected, Range<DateTimeOffset> actual)
	{
		Assert.Equal(new DateTimeOffset(expected.Minimum.Value), actual.Minimum);
		Assert.Equal(new DateTimeOffset(expected.Maximum.Value), actual.Maximum);
		Assert.Equal(expected.Minimum.Value, actual.Minimum.Value.DateTime);
		Assert.Equal(expected.Maximum.Value, actual.Maximum.Value.DateTime);
		Assert.Equal(TimeZoneInfo.Local.GetUtcOffset(expected.Minimum.Value), actual.Minimum.Value.Offset);
		Assert.Equal(TimeZoneInfo.Local.GetUtcOffset(expected.Maximum.Value), actual.Maximum.Value.Offset);
	}

	private static void AssertCalendarOffsetRange(Range<DateTime> first, Range<DateTime> last, Range<DateTimeOffset> actual)
	{
		Assert.Contains(new Range<DateTime>(actual.Minimum.Value.DateTime, actual.Maximum.Value.DateTime), new[] { first, last });
		Assert.Equal(TimeZoneInfo.Local.GetUtcOffset(actual.Minimum.Value.DateTime), actual.Minimum.Value.Offset);
		Assert.Equal(TimeZoneInfo.Local.GetUtcOffset(actual.Maximum.Value.DateTime), actual.Maximum.Value.Offset);
	}

	[Theory]
	[InlineData("18~", 18, null)]
	[InlineData("18~?", 18, null)]
	[InlineData("~40", null, 40)]
	[InlineData("( 18 ~ 40 )", 18, 40)]
	[InlineData("( 18 )", 18, 18)]
	public void ParsesOpenBoundariesAndTrimmedParentheses(string text, int? minimum, int? maximum)
	{
		Assert.True(Range<int>.TryParse(text, out var range));
		Assert.Equal(minimum, range.Minimum);
		Assert.Equal(maximum, range.Maximum);
		Assert.Equal(range, Range<int>.Parse(text.AsSpan()));
	}

	[Theory]
	[InlineData("*")]
	[InlineData("?")]
	public void ParsesStandaloneWildcardsAsEmptyRanges(string text)
	{
		Assert.True(Range<int>.TryParse(text, out var range));
		Assert.True(range.IsEmpty);
		Assert.False(range.HasValue);
		Assert.True(Range<int>.Parse(text.AsSpan()).IsEmpty);
	}

	[Theory]
	[InlineData("YeAr(2024)", 1, 1, 12, 31)]
	[InlineData(" MoNtH ( 2024 , 2 ) ", 2, 1, 2, 29)]
	[InlineData("DaY(2024,2,29)", 2, 29, 2, 29)]
	public void ParsesMixedCaseCalendarFunctions(string text, int firstMonth, int firstDay, int lastMonth, int lastDay)
	{
		var minimum = new DateTime(2024, firstMonth, firstDay);
		var maximum = new DateTime(2024, lastMonth, lastDay, 23, 59, 59, 999);
		var wrapped = "prefix" + text + "suffix";
		var timestamps = Range<DateTime>.Parse(wrapped.AsSpan(6, text.Length));
		var dates = Range<DateOnly>.Parse(text);
		var offsets = Range<DateTimeOffset>.Parse(text);

		Assert.Equal(minimum, timestamps.Minimum);
		Assert.Equal(maximum, timestamps.Maximum);
		Assert.Equal(DateOnly.FromDateTime(minimum), dates.Minimum);
		Assert.Equal(DateOnly.FromDateTime(maximum), dates.Maximum);
		Assert.Equal(minimum, offsets.Minimum.Value.DateTime);
		Assert.Equal(maximum, offsets.Maximum.Value.DateTime);
		Assert.Equal(TimeZoneInfo.Local.GetUtcOffset(minimum), offsets.Minimum.Value.Offset);
		Assert.Equal(TimeZoneInfo.Local.GetUtcOffset(maximum), offsets.Maximum.Value.Offset);
	}

	[Theory]
	[InlineData("ToDaY", "today")]
	[InlineData("ToMoRrOw()", "tomorrow()")]
	[InlineData("YeStErDaY ( )", "yesterday()")]
	[InlineData("ThIsWeEk", "thisweek")]
	[InlineData("LaStWeEk()", "lastweek()")]
	[InlineData("ThIsMoNtH", "thismonth")]
	[InlineData("LaStMoNtH()", "lastmonth()")]
	[InlineData("ThIsYeAr", "thisyear")]
	[InlineData("LaStYeAr()", "lastyear()")]
	public void ParsesMixedCaseParameterlessFunctions(string text, string canonical)
	{
		AssertCaseInsensitive<DateTime>(text, canonical);
		AssertCaseInsensitive<DateOnly>(text, canonical);
		AssertCaseInsensitive<DateTimeOffset>(text, canonical);
	}

	[Theory]
	[InlineData("1*x")]
	[InlineData("1~2*x")]
	[InlineData("1?x")]
	[InlineData("1~2?x")]
	[InlineData("18~40)")]
	[InlineData("((18~40))")]
	[InlineData("18~~40")]
	[InlineData("18~40~60")]
	[InlineData("*18~40")]
	[InlineData("18~?40")]
	[InlineData("18~40?")]
	[InlineData("*?")]
	public void RejectsMalformedRangeText(string text)
	{
		Assert.False(Range<int>.TryParse(text, out var fromString));
		Assert.False(Range<int>.TryParse(text.AsSpan(), out var fromSpan));
		Assert.True(fromString.IsEmpty);
		Assert.True(fromSpan.IsEmpty);
		Assert.ThrowsAny<ArgumentException>(() => Range<int>.Parse(text));
		Assert.ThrowsAny<ArgumentException>(() => Range<int>.Parse(text.AsSpan()));
	}

	[Theory]
	[InlineData("AgO(2147483647s)", true)]
	[InlineData("LaSt(2147483647S)", false)]
	public void AcceptsLargeRepresentableSecondIntervals(string text, bool ago)
	{
		var before = DateTime.Now;
		Assert.True(Range<DateTime>.TryParse(text, out var timestamps));
		Assert.True(Range<DateOnly>.TryParse(text, out var dates));
		Assert.True(Range<DateTimeOffset>.TryParse(text, out var offsets));
		var after = DateTime.Now;
		var first = before.AddSeconds(-int.MaxValue);
		var last = after.AddSeconds(-int.MaxValue);

		if(ago)
		{
			Assert.Null(timestamps.Minimum);
			Assert.Null(dates.Minimum);
			Assert.Null(offsets.Minimum);
			Assert.InRange(timestamps.Maximum.Value, first, last);
			Assert.InRange(dates.Maximum.Value, DateOnly.FromDateTime(first), DateOnly.FromDateTime(last));
			Assert.InRange(offsets.Maximum.Value.DateTime, first, last);
		}
		else
		{
			Assert.InRange(timestamps.Minimum.Value, first, last);
			Assert.InRange(dates.Minimum.Value, DateOnly.FromDateTime(first), DateOnly.FromDateTime(last));
			Assert.InRange(offsets.Minimum.Value.DateTime, first, last);
			Assert.InRange(timestamps.Maximum.Value, before, after);
			Assert.InRange(dates.Maximum.Value, DateOnly.FromDateTime(before), DateOnly.FromDateTime(after));
			Assert.InRange(offsets.Maximum.Value.DateTime, before, after);
		}
	}

	[Theory]
	[InlineData("YeAr(1)", 1, 1, 1, 1, 12, 31)]
	[InlineData("YeAr(9999)", 9999, 1, 1, 9999, 12, 31)]
	[InlineData("MoNtH(1,1)", 1, 1, 1, 1, 1, 31)]
	[InlineData("DaY(9999,12,31)", 9999, 12, 31, 9999, 12, 31)]
	public void ChecksOffsetEndpointsAgainstTheUtcRepresentationRange(string text, int firstYear, int firstMonth, int firstDay, int lastYear, int lastMonth, int lastDay)
	{
		var minimum = new DateTime(firstYear, firstMonth, firstDay);
		var maximum = new DateTime(lastYear, lastMonth, lastDay, 23, 59, 59, 999);
		var minimumOffset = TimeZoneInfo.Local.GetUtcOffset(minimum);
		var maximumOffset = TimeZoneInfo.Local.GetUtcOffset(maximum);
		var representable = IsRepresentable(minimum, minimumOffset) && IsRepresentable(maximum, maximumOffset);

		Assert.Equal(representable, Range<DateTimeOffset>.TryParse(text, out var range));

		if(representable)
		{
			Assert.Equal(minimum, range.Minimum.Value.DateTime);
			Assert.Equal(maximum, range.Maximum.Value.DateTime);
			Assert.Equal(minimumOffset, range.Minimum.Value.Offset);
			Assert.Equal(maximumOffset, range.Maximum.Value.Offset);
		}
		else
			Assert.True(range.IsEmpty);
	}

	[Fact]
	public void RejectsInvalidTemporalExpressionsWithoutThrowingCaughtExceptions()
	{
		string[] expressions =
		[
			"year(0)", "year(10000)", "year(2147483647)", "year(99999999999999999999)",
			"month(2024,0)", "month(2024,13)", "month(0,1)",
			"day(2023,2,29)", "day(2024,2,0)", "day(2024,2,30)", "day(2024,13,1)", "day(0,1,1)",
			"ago()", "ago(0)", "ago(1)", "ago(1q)",
			"last()", "last(0)", "last(1)", "last(1q)",
			"ago(2147483647y)", "ago(2147483647M)", "ago(2147483647d)", "ago(2147483647h)", "ago(2147483647m)",
			"last(2147483647y)", "last(2147483647M)", "last(2147483647d)", "last(2147483647h)", "last(2147483647m)",
			"ago(99999999999999999999s)", "last(99999999999999999999S)",
			"unknown()", "today(1)", "day(2024,2)", "year(2024y)",
		];

		Assert.True(Range<DateTime>.TryParse("year(2024)", out _));
		Assert.True(Range<DateOnly>.TryParse("year(2024)", out _));
		Assert.True(Range<DateTimeOffset>.TryParse("year(2024)", out _));
		AssertInvalid<DateTime>("unknown()");
		AssertInvalid<DateOnly>("unknown()");
		AssertInvalid<DateTimeOffset>("unknown()");

		var thread = Environment.CurrentManagedThreadId;
		var exceptions = 0;
		EventHandler<FirstChanceExceptionEventArgs> handler = (_, args) =>
		{
			if(Environment.CurrentManagedThreadId == thread &&
				(args.Exception is ArgumentException or OverflowException) &&
				IsRangeParsingException(args.Exception))
				exceptions++;
		};

		AppDomain.CurrentDomain.FirstChanceException += handler;

		try
		{
			foreach(var expression in expressions)
			{
				AssertInvalid<DateTime>(expression);
				AssertInvalid<DateOnly>(expression);
				AssertInvalid<DateTimeOffset>(expression);
			}

			Range<DateTimeOffset>.TryParse("year(1)", out _);
			Range<DateTimeOffset>.TryParse("year(9999)", out _);
		}
		finally
		{
			AppDomain.CurrentDomain.FirstChanceException -= handler;
		}

		Assert.Equal(0, exceptions);
	}

	private static void AssertCaseInsensitive<T>(string text, string canonical) where T : struct
	{
		var first = Range<T>.Parse(canonical);
		Assert.True(Range<T>.TryParse(text, out var actual));
		var last = Range<T>.Parse(canonical);
		Assert.Contains(actual, new[] { first, last });
	}

	private static void AssertInvalid<T>(string text) where T : struct
	{
		Assert.False(Range<T>.TryParse(text, out var range));
		Assert.True(range.IsEmpty);
	}

	private static bool IsRepresentable(DateTime value, TimeSpan offset)
	{
		var utcTicks = value.Ticks - offset.Ticks;
		return utcTicks >= DateTime.MinValue.Ticks && utcTicks <= DateTime.MaxValue.Ticks;
	}

	private static bool IsRangeParsingException(Exception exception)
	{
		var type = exception.TargetSite?.DeclaringType?.FullName;
		return HasRangeParserFrame(type) || HasRangeParserFrame(exception.StackTrace) || HasRangeParserFrame(Environment.StackTrace);
	}

	private static bool HasRangeParserFrame(string text) => text != null &&
		(text.Contains("Zongsoft.Data.Range", StringComparison.Ordinal) || text.Contains("Zongsoft.Data.DateTimeRangeParser", StringComparison.Ordinal));

	[Fact]
	public void UsesGenericComparisonForRangeOperations()
	{
		AssertRangeOperations(
			new GenericComparable(2), new GenericComparable(5),
			new GenericComparable(1), new GenericComparable(8));
	}

	[Fact]
	public void UsesNonGenericComparisonForRangeOperations()
	{
		AssertRangeOperations(
			new NonGenericComparable(2), new NonGenericComparable(5),
			new NonGenericComparable(1), new NonGenericComparable(8));
	}

	[Fact]
	public void RejectsRangeEndpointsWithoutComparisonSupport()
	{
		Assert.Throws<ArgumentException>(() => new Range<Unordered>(new Unordered(2), new Unordered(1)));
	}

	[Fact]
	public void ComparesEnumRangeEndpointsByTheirValues()
	{
		var range = new Range<DayOfWeek>(DayOfWeek.Friday, DayOfWeek.Monday);
		Assert.Equal(DayOfWeek.Monday, range.Minimum);
		Assert.Equal(DayOfWeek.Friday, range.Maximum);
		Assert.True(range.Contains(DayOfWeek.Wednesday));
		Assert.False(range.Contains(DayOfWeek.Sunday));

		var condition = range.ToCondition("Day");
		Assert.Equal(ConditionOperator.Between, condition.Operator);
		Assert.Equal(range, Assert.IsType<Range<DayOfWeek>>(condition.Value));
	}

	[Fact]
	public void BuildsEnumRangeConditionsThroughAllBuilderOverloads()
	{
		var range = new Range<DayOfWeek>(DayOfWeek.Friday, DayOfWeek.Monday);
		var conditions = new[]
		{
			Condition.Builder<SampleModel>.Between(nameof(SampleModel.Day), range),
			Condition.Builder<SampleModel>.Between(nameof(SampleModel.Day), DayOfWeek.Friday, DayOfWeek.Monday),
			Condition.Builder<SampleModel>.Between(nameof(SampleModel.Day), (DayOfWeek?)DayOfWeek.Friday, (DayOfWeek?)DayOfWeek.Monday),
			Condition.Builder<SampleModel>.Between(model => model.Day, range),
			Condition.Builder<SampleModel>.Between(model => model.Day, DayOfWeek.Friday, DayOfWeek.Monday),
			Condition.Builder<SampleModel>.Between(model => model.Day, (DayOfWeek?)DayOfWeek.Friday, (DayOfWeek?)DayOfWeek.Monday),
		};

		foreach(var condition in conditions)
		{
			Assert.Equal(nameof(SampleModel.Day), condition.Name);
			Assert.Equal(ConditionOperator.Between, condition.Operator);
			Assert.Equal(range, Assert.IsType<Range<DayOfWeek>>(condition.Value));
		}
	}

	private static void AssertRangeOperations<T>(T minimum, T maximum, T below, T above) where T : struct
	{
		var range = new Range<T>(maximum, minimum);
		Assert.Equal(minimum, range.Minimum);
		Assert.Equal(maximum, range.Maximum);
		Assert.True(range.Contains(minimum));
		Assert.True(range.Contains(maximum));
		Assert.False(range.Contains(below));
		Assert.False(range.Contains(above));

		range.Minimum = above;
		Assert.Equal(maximum, range.Minimum);
		Assert.Equal(above, range.Maximum);
		Assert.True(range.Contains(above));

		range.Maximum = below;
		Assert.Equal(below, range.Minimum);
		Assert.Equal(maximum, range.Maximum);
		Assert.True(range.Contains(below));
		Assert.False(range.Contains(above));

		var condition = range.ToCondition("Value");
		Assert.Equal("Value", condition.Name);
		Assert.Equal(ConditionOperator.Between, condition.Operator);
		Assert.Equal(range, Assert.IsType<Range<T>>(condition.Value));
	}

	private readonly struct GenericComparable(int value) : IComparable<GenericComparable>
	{
		public int Value { get; } = value;
		public int CompareTo(GenericComparable other) => this.Value.CompareTo(other.Value);
	}

	private readonly struct NonGenericComparable(int value) : IComparable
	{
		public int Value { get; } = value;
		public int CompareTo(object other) => this.Value.CompareTo(((NonGenericComparable)other).Value);
	}

	private readonly struct Unordered(int value)
	{
		public int Value { get; } = value;

		public static bool TryParse(string text, out Unordered result)
		{
			result = default;

			if(!int.TryParse(text, out var number))
				return false;

			result = new Unordered(number);
			return true;
		}
	}

	private sealed class SampleModel
	{
		public DayOfWeek Day { get; set; }
	}

	[Theory]
	[InlineData("18~40", 18, 40)]
	[InlineData("(40~18)", 18, 40)]
	[InlineData("?~40", null, 40)]
	[InlineData("18~*", 18, null)]
	[InlineData("18", 18, 18)]
	public void ParsesRangeThroughStringAndSpanInterfaces(string text, int? minimum, int? maximum)
	{
		var expected = new Range<int>(minimum, maximum);
		Assert.Equal(expected, Range<int>.Parse(text));
		Assert.Equal(expected, Range<int>.Parse(text.AsSpan()));
		Assert.True(Range<int>.TryParse(text, out var fromString));
		Assert.True(Range<int>.TryParse(text.AsSpan(), out var fromSpan));
		Assert.Equal(expected, fromString);
		Assert.Equal(expected, fromSpan);
		AssertInterfaceParsing(text, expected);
	}

	[Theory]
	[InlineData("2024-02-01~2024-02-29", 1, 29)]
	[InlineData("month(2024,2)", 1, 29)]
	[InlineData("day(2024,2,29)", 29, 29)]
	public void ParsesDateOnlyRangesThroughInterfaces(string text, int firstDay, int lastDay)
	{
		var expected = new Range<DateOnly>(new DateOnly(2024, 2, firstDay), new DateOnly(2024, 2, lastDay));
		AssertInterfaceParsing(text, expected);
		Assert.Equal(expected, Range<DateOnly>.Parse(text.AsSpan()));
	}

	[Fact]
	public void ParsesOffsetRangesThroughInterfaces()
	{
		const string TEXT = "2024-02-01T00:00:00+08:00~2024-02-29T23:59:59-05:00";
		var first = new DateTimeOffset(2024, 2, 1, 0, 0, 0, TimeSpan.FromHours(8));
		var last = new DateTimeOffset(2024, 2, 29, 23, 59, 59, TimeSpan.FromHours(-5));
		var expected = new Range<DateTimeOffset>(first, last);
		AssertInterfaceParsing(TEXT, expected);

		var parsed = ParseSpan<Range<DateTimeOffset>>(TEXT.AsSpan());
		Assert.Equal(first.Offset, parsed.Minimum.Value.Offset);
		Assert.Equal(last.Offset, parsed.Maximum.Value.Offset);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("(18~40")]
	[InlineData("invalid")]
	public void RejectsInvalidRangeThroughTryParseInterfaces(string text)
	{
		Assert.False(TryParseString<Range<int>>(text, out var fromString));
		Assert.False(TryParseSpan<Range<int>>(text.AsSpan(), out var fromSpan));
		Assert.True(fromString.IsEmpty);
		Assert.True(fromSpan.IsEmpty);
		Assert.False(Range<int>.TryParse(text.AsSpan(), out var direct));
		Assert.True(direct.IsEmpty);
	}

	[Fact]
	public void PreservesRangeParseEmptyExceptions()
	{
		Assert.Throws<ArgumentNullException>(() => ParseString<Range<int>>(null));
		Assert.Throws<ArgumentNullException>(() => ParseString<Range<int>>(string.Empty));
		Assert.Throws<ArgumentNullException>(() => ParseSpan<Range<int>>(ReadOnlySpan<char>.Empty));
	}

	private static void AssertInterfaceParsing<T>(string text, T expected) where T : ISpanParsable<T>
	{
		Assert.Equal(expected, ParseString<T>(text));
		Assert.True(TryParseString<T>(text, out var fromString));
		Assert.Equal(expected, fromString);

		var decorated = "!" + text + "!";
		var span = decorated.AsSpan(1, text?.Length ?? 0);
		Assert.Equal(expected, ParseSpan<T>(span));
		Assert.True(TryParseSpan<T>(span, out var fromSpan));
		Assert.Equal(expected, fromSpan);
	}

	private static T ParseString<T>(string text) where T : IParsable<T> => T.Parse(text, CultureInfo.InvariantCulture);
	private static T ParseSpan<T>(ReadOnlySpan<char> text) where T : ISpanParsable<T> => T.Parse(text, CultureInfo.InvariantCulture);
	private static bool TryParseString<T>(string text, out T result) where T : IParsable<T> => T.TryParse(text, CultureInfo.InvariantCulture, out result);
	private static bool TryParseSpan<T>(ReadOnlySpan<char> text, out T result) where T : ISpanParsable<T> => T.TryParse(text, CultureInfo.InvariantCulture, out result);
}

public class TestConditional
{
	public Range<byte>? Age { get; set; }
	public Range<ushort>? Weight { get; set; }
	public Range<DateTime>? Timestamp { get; set; }
	public Range<DateTime> Birthday { get; set; }
}
