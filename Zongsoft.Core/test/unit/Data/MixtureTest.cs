using System;
using System.Globalization;
using System.Text.Json;
using System.Collections.Generic;

using Zongsoft.Serialization.Json.Converters;

namespace Zongsoft.Data.Tests;

public class MixtureTest
{
	[Fact]
	public void ParsesEnumValuesAndTransformsConditions()
	{
		Assert.True(Mixture<DayOfWeek>.TryParse("Sunday", out var single));
		var condition = single.ToCondition("Day");

		Assert.Equal(ConditionOperator.Equal, condition.Operator);
		Assert.Equal(DayOfWeek.Sunday, condition.Value);

		Assert.True(Mixture<DayOfWeek>.TryParse("Sunday,Tuesday", out var multiple));
		condition = multiple.ToCondition("Day");

		Assert.Equal(ConditionOperator.In, condition.Operator);
		Assert.Equal(new[] { DayOfWeek.Sunday, DayOfWeek.Tuesday }, Assert.IsType<DayOfWeek[]>(condition.Value));

		var range = new Range<DayOfWeek>(DayOfWeek.Friday, DayOfWeek.Monday);
		range.Minimum = DayOfWeek.Saturday;

		Assert.Equal(DayOfWeek.Friday, range.Minimum);
		Assert.Equal(DayOfWeek.Saturday, range.Maximum);
		Assert.True(range.Contains(DayOfWeek.Friday));
		Assert.Equal(ConditionOperator.Between, range.ToCondition("Day").Operator);
	}

	[Theory]
	[InlineData("0", false)]
	[InlineData("\"Sunday\"", false)]
	[InlineData("\"0,2\"", true)]
	[InlineData("\"Sunday,Tuesday\"", true)]
	[InlineData("[0,2]", true)]
	[InlineData("[\"Sunday\",\"Tuesday\"]", true)]
	public void ReadsAndRoundTripsEnumJson(string json, bool multiple)
	{
		var options = new JsonSerializerOptions();
		options.Converters.Add(new MixtureConverterFactory());
		var value = JsonSerializer.Deserialize<Mixture<DayOfWeek>>(json, options);

		if(multiple)
			Assert.Equal(new[] { DayOfWeek.Sunday, DayOfWeek.Tuesday }, value.Array);
		else
			Assert.Equal(DayOfWeek.Sunday, value.Value);

		Assert.True(value.HasValue);

		var serialized = JsonSerializer.Serialize(value, options);
		var restored = JsonSerializer.Deserialize<Mixture<DayOfWeek>>(serialized, options);

		Assert.Equal(value, restored);
	}

	[Theory]
	[InlineData("0")]
	[InlineData("\"0\"")]
	[InlineData("\"1,2\"")]
	[InlineData("[1,2]")]
	[InlineData("\"1~2\"")]
	public void PreservesNumericJson(string json)
	{
		var options = new JsonSerializerOptions();
		options.Converters.Add(new MixtureConverterFactory());
		var value = JsonSerializer.Deserialize<Mixture<int>>(json, options);

		Assert.True(value.HasValue);
		Assert.Equal(value, JsonSerializer.Deserialize<Mixture<int>>(JsonSerializer.Serialize(value, options), options));
	}

	[Theory]
	[InlineData("A~C", false)]
	[InlineData("A,B,D,F", true)]
	public void ConvertsEnumCriteriaFromJson(string text, bool multiple)
	{
		var json = Serialization.Serializer.Json.Serialize(new { Kind = text });
		var criteria = Serialization.Serializer.Json.Deserialize<MixedCriteria>(json);
		var condition = Assert.IsType<Condition>(criteria.Transform());
		Assert.Equal("Kind", condition.Name);

		if(multiple)
		{
			Assert.Equal(ConditionOperator.In, condition.Operator);
			Assert.Equal(new[] { Kind.A, Kind.B, Kind.D, Kind.F }, Assert.IsType<Kind[]>(condition.Value));
		}
		else
		{
			Assert.Equal(ConditionOperator.Between, condition.Operator);
			var range = Assert.IsType<Range<Kind>>(condition.Value);
			Assert.Equal(Kind.A, range.Minimum);
			Assert.Equal(Kind.C, range.Maximum);
		}
	}

	[Theory]
	[InlineData("2024-02-01~2024-02-29", "2024-02-01", "2024-02-29", false)]
	[InlineData("month(2024,2)", "2024-02-01", "2024-02-29", true)]
	[InlineData("day(2024,2,29)", "2024-02-29", "2024-02-29", true)]
	[InlineData("year(2024)", "2024-01-01", "2024-12-31", true)]
	public void ConvertsTemporalCriteriaFromJson(string text, string minimum, string maximum, bool endOfDay)
	{
		var first = DateTime.Parse(minimum);
		var last = DateTime.Parse(maximum);

		if(endOfDay)
			last = last.AddHours(23).AddMinutes(59).AddSeconds(59).AddMilliseconds(999);

		var json = Serialization.Serializer.Json.Serialize(new { Timestamp = text, Offset = text, Date = text });
		var criteria = Serialization.Serializer.Json.Deserialize<MixedCriteria>(json);
		Assert.Null(criteria.Timestamp.Array);
		Assert.Null(criteria.Offset.Array);
		Assert.Null(criteria.Date.Array);
		Assert.Equal(first, criteria.Timestamp.Range.Minimum);
		Assert.Equal(last, criteria.Timestamp.Range.Maximum);
		Assert.Equal(new DateTimeOffset(first), criteria.Offset.Range.Minimum);
		Assert.Equal(new DateTimeOffset(last), criteria.Offset.Range.Maximum);
		Assert.Equal(DateOnly.FromDateTime(first), criteria.Date.Range.Minimum);
		Assert.Equal(DateOnly.FromDateTime(last), criteria.Date.Range.Maximum);

		var conditions = Assert.IsType<ConditionCollection>(criteria.Transform());
		Assert.Equal(3, conditions.Count);
		Assert.True(conditions.Match("Timestamp", condition => Assert.Equal(ConditionOperator.Between, condition.Operator)));
		Assert.True(conditions.Match("Offset", condition => Assert.Equal(ConditionOperator.Between, condition.Operator)));
		Assert.True(conditions.Match("Date", condition => Assert.Equal(first.Date == last.Date ? ConditionOperator.Equal : ConditionOperator.Between, condition.Operator)));
	}

	[Fact]
	public void PreservesExplicitOffsetsAndFractionalSeconds()
	{
		const string TEXT = "2024-02-01T00:00:00.1234567+08:00~2024-02-29T23:59:59.7654321-05:00";
		var json = Serialization.Serializer.Json.Serialize(TEXT);
		var mixture = Serialization.Serializer.Json.Deserialize<Mixture<DateTimeOffset>>(json);
		var first = DateTimeOffset.Parse("2024-02-01T00:00:00.1234567+08:00");
		var last = DateTimeOffset.Parse("2024-02-29T23:59:59.7654321-05:00");
		Assert.Equal(first, mixture.Range.Minimum);
		Assert.Equal(last, mixture.Range.Maximum);
		Assert.Equal(first.Offset, mixture.Range.Minimum.Value.Offset);
		Assert.Equal(last.Offset, mixture.Range.Maximum.Value.Offset);
	}

	[Theory]
	[InlineData("\"2024-02-01,2024-02-29\"")]
	[InlineData("[\"2024-02-01\",\"2024-02-29\"]")]
	public void ReadsTemporalSelections(string json)
	{
		var options = new JsonSerializerOptions();
		options.Converters.Add(new MixtureConverterFactory());
		var timestamps = JsonSerializer.Deserialize<Mixture<DateTime>>(json, options);
		var offsets = JsonSerializer.Deserialize<Mixture<DateTimeOffset>>(json, options);
		var dates = JsonSerializer.Deserialize<Mixture<DateOnly>>(json, options);
		Assert.Equal(new[] { new DateTime(2024, 2, 1), new DateTime(2024, 2, 29) }, timestamps.Array);
		Assert.Equal(new[] { new DateTimeOffset(new DateTime(2024, 2, 1)), new DateTimeOffset(new DateTime(2024, 2, 29)) }, offsets.Array);
		Assert.Equal(new[] { new DateOnly(2024, 2, 1), new DateOnly(2024, 2, 29) }, dates.Array);
		Assert.Equal(ConditionOperator.In, timestamps.ToCondition("Timestamp").Operator);
		Assert.Equal(ConditionOperator.In, offsets.ToCondition("Offset").Operator);
		Assert.Equal(ConditionOperator.In, dates.ToCondition("Date").Operator);
	}

	[Theory]
	[InlineData("\"\"")]
	[InlineData("\" , , \"")]
	[InlineData("\"?\"")]
	[InlineData("\"(?~?)\"")]
	[InlineData("[]")]
	public void PreservesEmptyJson(string json)
	{
		Assert.True(Serialization.Serializer.Json.Deserialize<Mixture<int>>(json).IsEmpty);
		Assert.True(Serialization.Serializer.Json.Deserialize<Mixture<DateOnly>>(json).IsEmpty);
		Assert.True(Serialization.Serializer.Json.Deserialize<Mixture<DateTimeOffset>>(json).IsEmpty);
	}

	[Theory]
	[InlineData("month(2024,13)")]
	[InlineData("day(2023,2,29)")]
	[InlineData("month(2024,2")]
	[InlineData("2024-02-01,invalid,2024-02-29")]
	public void RejectsInvalidTemporalJson(string text)
	{
		var json = Serialization.Serializer.Json.Serialize(text);
		Assert.Throws<JsonException>(() => Serialization.Serializer.Json.Deserialize<Mixture<DateTime>>(json));
		Assert.Throws<JsonException>(() => Serialization.Serializer.Json.Deserialize<Mixture<DateTimeOffset>>(json));
		Assert.Throws<JsonException>(() => Serialization.Serializer.Json.Deserialize<Mixture<DateOnly>>(json));
	}

	[Fact]
	public void PreservesSelectionsWhenIgnoringDefaultJsonValues()
	{
		var options = new JsonSerializerOptions
		{
			DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault,
		};
		options.Converters.Add(new MixtureConverterFactory());
		var json = JsonSerializer.Serialize(new { Items = new Mixture<int>(new[] { 1, 2 }) }, options);
		using var document = JsonDocument.Parse(json);
		Assert.True(document.RootElement.TryGetProperty("Items", out var property));
		Assert.Equal("1,2", property.GetString());
	}

	public enum Kind : byte
	{
		A,
		B,
		C,
		D,
		E,
		F,
	}

	public abstract class MixedCriteria : CriteriaBase
	{
		public abstract Mixture<Kind> Kind { get; set; }
		public abstract Mixture<DateTime> Timestamp { get; set; }
		public abstract Mixture<DateTimeOffset> Offset { get; set; }
		public abstract Mixture<DateOnly> Date { get; set; }
	}

	[Fact]
	public void RejectsUnorderedParsedRangesAndAcceptsSelections()
	{
		Assert.False(Range<Unordered>.TryParse("1~2", out var range));
		Assert.True(range.IsEmpty);
		Assert.False(Mixture<Unordered>.TryParse("1~2", out var mixture));
		Assert.True(mixture.IsEmpty);
		Assert.True(Mixture<Unordered>.TryParse("1,2", out mixture));
		Assert.Equal(new[] { new Unordered(1), new Unordered(2) }, mixture.Array);
		Assert.Equal(ConditionOperator.In, mixture.ToCondition("Items").Operator);
	}

	[Fact]
	public void SupportsUnorderedMixtureArraysAndValueEquality()
	{
		var first = new Mixture<Unordered>(new[] { new Unordered(1), new Unordered(2) });
		var equal = new Mixture<Unordered>(new[] { new Unordered(1), new Unordered(2) });
		var different = new Mixture<Unordered>(new[] { new Unordered(1), new Unordered(3) });
		var condition = first.ToCondition("Items");

		Assert.True(first.HasValue);
		Assert.Equal(ConditionOperator.In, condition.Operator);
		Assert.Equal("Items", condition.Name);
		Assert.Equal(first.Array, Assert.IsType<Unordered[]>(condition.Value));
		Assert.True(first.Equals(equal));
		Assert.Equal(first, equal);
		Assert.Equal(first.GetHashCode(), equal.GetHashCode());
		Assert.NotEqual(first, different);
		Assert.False(default(Mixture<Unordered>).Equals(first));
		Assert.False(first.Equals(default(Mixture<Unordered>)));
	}

	[Fact]
	public void BuildsEnumSelectionConditionsThroughAllPublicOverloads()
	{
		var values = new[] { DayOfWeek.Monday, DayOfWeek.Friday };
		IEnumerable<DayOfWeek> sequence = new List<DayOfWeek>(values);
		var conditions = new[]
		{
			(Condition.In(nameof(SampleModel.Day), values), ConditionOperator.In),
			(Condition.In(nameof(SampleModel.Day), sequence), ConditionOperator.In),
			(Condition.NotIn(nameof(SampleModel.Day), values), ConditionOperator.NotIn),
			(Condition.NotIn(nameof(SampleModel.Day), sequence), ConditionOperator.NotIn),
			(Condition.Builder<SampleModel>.In(nameof(SampleModel.Day), values), ConditionOperator.In),
			(Condition.Builder<SampleModel>.In(nameof(SampleModel.Day), sequence), ConditionOperator.In),
			(Condition.Builder<SampleModel>.NotIn(nameof(SampleModel.Day), values), ConditionOperator.NotIn),
			(Condition.Builder<SampleModel>.NotIn(nameof(SampleModel.Day), sequence), ConditionOperator.NotIn),
			(Condition.Builder<SampleModel>.In(model => model.Day, values), ConditionOperator.In),
			(Condition.Builder<SampleModel>.In(model => model.Day, sequence), ConditionOperator.In),
			(Condition.Builder<SampleModel>.NotIn(model => model.Day, values), ConditionOperator.NotIn),
			(Condition.Builder<SampleModel>.NotIn(model => model.Day, sequence), ConditionOperator.NotIn),
		};

		foreach(var (condition, operation) in conditions)
		{
			Assert.Equal(nameof(SampleModel.Day), condition.Name);
			Assert.Equal(operation, condition.Operator);
			Assert.Equal(values, Assert.IsAssignableFrom<IEnumerable<DayOfWeek>>(condition.Value));
		}
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
	[InlineData("A~C", false)]
	[InlineData("A,B,D,F", true)]
	[InlineData(" A , , B , D , F , ", true)]
	public void ParsesEnumMixturesThroughInterfaces(string text, bool multiple)
	{
		var expected = multiple ?
			new Mixture<Kind>(new[] { Kind.A, Kind.B, Kind.D, Kind.F }) :
			new Mixture<Kind>(new Range<Kind>(Kind.A, Kind.C));

		Assert.Equal(expected, Mixture<Kind>.Parse(text));
		Assert.Equal(expected, Mixture<Kind>.Parse(text.AsSpan()));
		AssertInterfaceParsing(text, expected);
	}

	[Theory]
	[InlineData("month(2024,2)", 1, 29)]
	[InlineData("day(2024,2,29)", 29, 29)]
	public void PreservesTemporalExpressionCommasInMixtures(string text, int firstDay, int lastDay)
	{
		var first = new DateTime(2024, 2, firstDay);
		var last = new DateTime(2024, 2, lastDay, 23, 59, 59, 999);
		var dates = new Mixture<DateOnly>(new Range<DateOnly>(DateOnly.FromDateTime(first), DateOnly.FromDateTime(last)));
		var timestamps = new Mixture<DateTime>(new Range<DateTime>(first, last));
		var offsets = new Mixture<DateTimeOffset>(new Range<DateTimeOffset>(new DateTimeOffset(first), new DateTimeOffset(last)));

		AssertInterfaceParsing(text, dates);
		AssertInterfaceParsing(text, timestamps);
		AssertInterfaceParsing(text, offsets);
		Assert.Equal(dates, Mixture<DateOnly>.Parse(text.AsSpan()));
		Assert.Equal(timestamps, Mixture<DateTime>.Parse(text.AsSpan()));
		Assert.Equal(offsets, Mixture<DateTimeOffset>.Parse(text.AsSpan()));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData("A,invalid,B")]
	[InlineData("A~invalid")]
	[InlineData("month(2024,2")]
	public void RejectsInvalidMixturesThroughInterfaces(string text)
	{
		Assert.False(TryParseString<Mixture<Kind>>(text, out var fromString));
		Assert.False(TryParseSpan<Mixture<Kind>>(text.AsSpan(), out var fromSpan));
		Assert.True(fromString.IsEmpty);
		Assert.True(fromSpan.IsEmpty);
		Assert.False(Mixture<Kind>.TryParse(text.AsSpan(), out var direct));
		Assert.True(direct.IsEmpty);

		if(text == null)
			Assert.Throws<ArgumentNullException>(() => ParseString<Mixture<Kind>>(text));
		else
			Assert.Throws<FormatException>(() => ParseString<Mixture<Kind>>(text));

		Assert.Throws<FormatException>(() => ParseSpan<Mixture<Kind>>(text.AsSpan()));
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
