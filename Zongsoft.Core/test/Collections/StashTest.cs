using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Collections.Tests;

public class StashTest
{
	[Fact]
	public void Test()
	{
		var stash = new Stash<string>(Access, TimeSpan.FromSeconds(100), 0);
		Parallel.For(0, 10000, index => stash.Put($"Value#{index}"));
		Assert.Equal(10000, stash.Count);
	}

	private static void Access(IEnumerable<string> items)
	{
	}
}
