using System;
using System.Globalization;

namespace Zongsoft.Data.Tests;

public class PagingTest
{
	[Fact]
	public void Disabled()
	{
		var page = Paging.Disabled;
		Assert.NotNull(page);
		Assert.False(page.IsPaged());
		Assert.False(page.IsLimited());

		Assert.Throws<InvalidOperationException>(() => page.Size = 1);
		Assert.Throws<InvalidOperationException>(() => page.Index = 1);
	}

	[Fact]
	public void Paged()
	{
		var page = Paging.Page(10);
		Assert.NotNull(page);
		Assert.False(page.IsLimited());
		Assert.True(page.IsPaged(out var index, out var size));
		Assert.Equal(10, index);
		Assert.Equal(20, size);

		page = Paging.Page(20, 100);
		Assert.NotNull(page);
		Assert.False(page.IsLimited());
		Assert.True(page.IsPaged(out index, out size));
		Assert.Equal(20, index);
		Assert.Equal(100, size);
	}

	[Fact]
	public void Limited()
	{
		var page = Paging.Limit(100);
		Assert.NotNull(page);
		Assert.False(page.IsPaged());
		Assert.True(page.IsLimited(out var limit, out var offset));
		Assert.Equal(100, limit);
		Assert.Equal(0, offset);

		page = Paging.Limit(200, 10);
		Assert.NotNull(page);
		Assert.False(page.IsPaged());
		Assert.True(page.IsLimited(out limit, out offset));
		Assert.Equal(200, limit);
		Assert.Equal(10, offset);
	}

	[Fact]
	public void ToLimited()
	{
		var page = Paging.First();
		Assert.NotNull(page);
		Assert.True(page.IsPaged());
		Assert.False(page.IsLimited());
		Assert.NotSame(page, Paging.Disabled);

		var limited = page.ToLimited();
		Assert.NotNull(limited);
		Assert.False(limited.IsPaged());
		Assert.True(limited.IsLimited(out var count, out var offset));
		Assert.NotSame(limited, Paging.Disabled);

		Assert.Equal(20, count);
		Assert.Equal(0, offset);
		Assert.Equal(20, limited.Size);
		Assert.Equal(0, limited.Index);
	}

	[Fact]
	public void ConvertDisabled()
	{
		var page1 = Zongsoft.Common.Convert.ConvertValue<Paging>("*");
		Assert.NotNull(page1);
		Assert.False(page1.IsPaged(out _, out _));
		Assert.False(page1.IsLimited(out _, out _));

		var page2 = Zongsoft.Common.Convert.ConvertValue<Paging>("0");
		Assert.NotNull(page2);
		Assert.False(page2.IsPaged(out _, out _));
		Assert.False(page2.IsLimited(out _, out _));

		Assert.Same(page1, page2);
	}

	[Fact]
	public void ConvertPaged()
	{
		var page = Zongsoft.Common.Convert.ConvertValue<Paging>("5|25");
		Assert.NotNull(page);
		Assert.False(page.IsLimited(out _, out _));
		Assert.True(page.IsPaged(out var index, out var size));
		Assert.Equal(5, index);
		Assert.Equal(25, size);
	}

	[Fact]
	public void ConvertLimited()
	{
		var page = Zongsoft.Common.Convert.ConvertValue<Paging>("100@50");
		Assert.NotNull(page);
		Assert.False(page.IsPaged(out _, out _));
		Assert.True(page.IsLimited(out var count, out var offset));
		Assert.Equal(100, count);
		Assert.Equal(50, offset);
	}

	[Fact]
	public void ConvertResulted()
	{
		var page = Zongsoft.Common.Convert.ConvertValue<Paging>("1/11(220)");
		Assert.NotNull(page);
		Assert.False(page.IsEmpty);
		Assert.False(page.IsLimited(out _, out _));
		Assert.True(page.IsPaged(out var index, out var size));
		Assert.Equal(1, index);
		Assert.Equal(20, size);
		Assert.Equal(1, page.Index);
		Assert.Equal(11, page.Count);
		Assert.Equal(220, page.Total);
	}

	[Theory]
	[InlineData(null, 0, 0, -1, 0)]
	[InlineData("", 0, 0, -1, 0)]
	[InlineData("*", 0, 0, -1, 0)]
	[InlineData("0", 0, 0, -1, 0)]
	[InlineData("disabled", 0, 0, -1, 0)]
	[InlineData("5", 5, 20, -1, 0)]
	[InlineData("5|25", 5, 25, -1, 0)]
	[InlineData("100@50", 0, 100, -1, 50)]
	[InlineData("1/11(220)", 1, 20, 220, 0)]
	public void ParsesPagingThroughStringAndSpanInterfaces(string text, int index, int size, long total, long offset)
	{
		var expected = Paging.Parse(text.AsSpan());
		Assert.Equal(index, expected.Index);
		Assert.Equal(size, expected.Size);
		Assert.Equal(total, expected.Total);
		Assert.Equal(expected, Paging.Parse(text));
		Assert.True(Paging.TryParse(text, out var fromString));
		Assert.Equal(expected, fromString);
		AssertInterfaceParsing(text, expected);

		if(index == 0 && size > 0)
		{
			Assert.True(expected.IsLimited(out var count, out var actualOffset));
			Assert.Equal(size, count);
			Assert.Equal(offset, actualOffset);
		}
	}

	[Theory]
	[InlineData("invalid")]
	[InlineData("1|")]
	[InlineData("1/0(220)")]
	[InlineData("1/1(9223372036854775807)")]
	public void RejectsInvalidPagingThroughInterfaces(string text)
	{
		Assert.False(TryParseString<Paging>(text, out var fromString));
		Assert.False(TryParseSpan<Paging>(text.AsSpan(), out var fromSpan));
		Assert.Null(fromString);
		Assert.Null(fromSpan);
		Assert.Throws<ArgumentException>(() => ParseString<Paging>(text));
		Assert.Throws<ArgumentException>(() => ParseSpan<Paging>(text.AsSpan()));
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
