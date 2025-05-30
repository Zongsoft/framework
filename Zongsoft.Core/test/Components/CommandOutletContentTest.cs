using System;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Components.Tests;

public class CommandOutletContentTest
{
	[Fact]
	public void TestEmpty()
	{
		var first = CommandOutletContent.Create();
		Assert.Null(first.Text);
		Assert.Null(first.Next);
		Assert.NotNull(first.Last);
		Assert.NotNull(first.First);
		Assert.Null(first.Previous);
		Assert.True(first.IsEmpty);

		var second = first.Append(string.Empty);
		Assert.Null(second.Next);
		Assert.NotNull(second.Last);
		Assert.NotNull(second.First);
		Assert.Null(second.Previous);
		Assert.True(first.IsEmpty);
		Assert.True(second.IsEmpty);

		var third = second.Append(string.Empty);
		Assert.Null(third.Next);
		Assert.NotNull(third.Last);
		Assert.NotNull(third.First);
		Assert.Null(third.Previous);
		Assert.True(first.IsEmpty);
		Assert.True(second.IsEmpty);
		Assert.True(third.IsEmpty);

		var fourth = third.Append(string.Empty);
		Assert.Null(fourth.Next);
		Assert.NotNull(fourth.Last);
		Assert.NotNull(fourth.First);
		Assert.Null(fourth.Previous);
		Assert.True(first.IsEmpty);
		Assert.True(second.IsEmpty);
		Assert.True(third.IsEmpty);
		Assert.True(fourth.IsEmpty);
	}

	[Fact]
	public void TestNonempty()
	{
		var first = CommandOutletContent.Create();
		Assert.Null(first.Text);
		Assert.Null(first.Next);
		Assert.NotNull(first.Last);
		Assert.NotNull(first.First);
		Assert.Null(first.Previous);
		Assert.True(first.IsEmpty);

		var second = first.Append("A");
		Assert.NotEmpty(second.Text);
		Assert.Equal("A", second.Text);
		Assert.Null(second.Color);
		Assert.Null(second.Next);
		Assert.NotNull(second.Last);
		Assert.NotNull(second.First);
		Assert.NotNull(second.Previous);
		Assert.False(first.IsEmpty);
		Assert.False(second.IsEmpty);

		var third = second.Append(CommandOutletColor.Red, "B");
		Assert.NotEmpty(third.Text);
		Assert.Equal("B", third.Text);
		Assert.Equal(CommandOutletColor.Red, third.Color);
		Assert.Null(third.Next);
		Assert.NotNull(third.Last);
		Assert.NotNull(third.First);
		Assert.NotNull(third.Previous);
		Assert.False(first.IsEmpty);
		Assert.False(second.IsEmpty);
		Assert.False(third.IsEmpty);

		var fourth = third.Append(CommandOutletColor.Blue, "C");
		Assert.NotEmpty(fourth.Text);
		Assert.Equal("C", fourth.Text);
		Assert.Equal(CommandOutletColor.Blue, fourth.Color);
		Assert.Null(fourth.Next);
		Assert.NotNull(fourth.Last);
		Assert.NotNull(fourth.First);
		Assert.NotNull(fourth.Previous);
		Assert.False(first.IsEmpty);
		Assert.False(second.IsEmpty);
		Assert.False(third.IsEmpty);
		Assert.False(fourth.IsEmpty);
	}
}
