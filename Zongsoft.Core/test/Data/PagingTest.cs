using System;

using Xunit;

namespace Zongsoft.Data.Tests;

public class PagingTest
{
	[Fact]
	public void Disabled()
	{
		var page = Paging.Disabled;
		Assert.NotNull(page);
		Assert.False(page.IsPaged(out _, out _));
		Assert.False(page.IsLimited(out _, out _));

		Assert.Throws<InvalidOperationException>(() => page.Size = 1);
		Assert.Throws<InvalidOperationException>(() => page.Index = 1);
	}

	[Fact]
	public void Paged()
	{
		var page = Paging.Page(10);
		Assert.NotNull(page);
		Assert.False(page.IsLimited(out _, out _));
		Assert.True(page.IsPaged(out var index, out var size));
		Assert.Equal(10, index);
		Assert.Equal(20, size);

		page = Paging.Page(20, 100);
		Assert.NotNull(page);
		Assert.False(page.IsLimited(out _, out _));
		Assert.True(page.IsPaged(out index, out size));
		Assert.Equal(20, index);
		Assert.Equal(100, size);
	}

	[Fact]
	public void Limited()
	{
		var page = Paging.Limit(100);
		Assert.NotNull(page);
		Assert.False(page.IsPaged(out _, out _));
		Assert.True(page.IsLimited(out var limit, out var offset));
		Assert.Equal(100, limit);
		Assert.Equal(0, offset);

		page = Paging.Limit(200, 10);
		Assert.NotNull(page);
		Assert.False(page.IsPaged(out _, out _));
		Assert.True(page.IsLimited(out limit, out offset));
		Assert.Equal(200, limit);
		Assert.Equal(10, offset);
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
}
