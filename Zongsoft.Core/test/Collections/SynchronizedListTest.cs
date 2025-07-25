using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Collections.Tests;

public class SynchronizedListTest
{
	[Fact]
	public void Test()
	{
		const int COUNT = 10000;

		var list = new SynchronizedList<int>(COUNT);

		Parallel.For(0, COUNT, index =>
		{
			list.Add(index);

			if(index % 100 == 0)
			{
				foreach(var item in list)
				{
					Assert.True(item >= 0);
				}

				for(int i = 0; i < list.Count; i++)
				{
					Assert.True(list[i] >= 0);
				}
			}
		});

		Assert.Equal(COUNT, list.Count);
		var hashset = new HashSet<int>(list);
		Assert.Equal(COUNT, hashset.Count);
	}
}
