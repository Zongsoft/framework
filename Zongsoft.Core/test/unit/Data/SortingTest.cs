using System;
using System.Globalization;

namespace Zongsoft.Data.Tests;

public class SortingTest
{
	[Fact]
	public void Test()
	{
		var sorting1 = Sorting.Ascending("Name");

		Assert.Equal(SortingMode.Ascending, sorting1.Mode);
		Assert.Equal("Name", sorting1.Name);

		var sorting2 = Sorting.Descending("Name");

		Assert.Equal(SortingMode.Descending, sorting2.Mode);
		Assert.Equal("Name", sorting2.Name);

		var sortings1 = sorting1 + sorting2;
		Assert.Single(sortings1);

		Assert.Equal(SortingMode.Descending, sortings1[0].Mode);
		Assert.Equal("Name", sortings1[0].Name);

		var sortings2 = sortings1 + Sorting.Ascending("PhoneNumber");
		Assert.Equal(2, sortings2.Length);

		Assert.Equal(SortingMode.Ascending, sortings2[1].Mode);
		Assert.Equal("PhoneNumber", sortings2[1].Name);
	}

	[Theory]
	[InlineData("Name", "Name", SortingMode.Ascending)]
	[InlineData("+Name", "Name", SortingMode.Ascending)]
	[InlineData("-Name", "Name", SortingMode.Descending)]
	[InlineData("~Name", "Name", SortingMode.Descending)]
	[InlineData(" \t- \tName\t ", "Name", SortingMode.Descending)]
	public void ParsesSortingThroughStringAndSpanInterfaces(string text, string name, SortingMode mode)
	{
		var expected = new Sorting(name, mode);
		Assert.Equal(expected, Sorting.Parse(text));
		Assert.Equal(expected, Sorting.Parse(text.AsSpan()));
		Assert.True(Sorting.TryParse(text.AsSpan(), out var direct));
		Assert.Equal(expected, direct);
		AssertInterfaceParsing(text, expected);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" ")]
	[InlineData("+")]
	[InlineData("-")]
	[InlineData("~")]
	[InlineData("Name Other")]
	public void RejectsInvalidSortingThroughInterfaces(string text)
	{
		Assert.False(TryParseString<Sorting>(text, out var fromString));
		Assert.False(TryParseSpan<Sorting>(text.AsSpan(), out var fromSpan));
		Assert.Equal(default, fromString);
		Assert.Equal(default, fromSpan);
		Assert.False(Sorting.TryParse(text.AsSpan(), out var direct));
		Assert.Equal(default, direct);
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
