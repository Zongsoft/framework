using System;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Collections.Tests;

public class SynchronizedDictionaryTest
{
	[Fact]
	public void Test()
	{
		const int COUNT = 10000;
		var dictionary = new SynchronizedDictionary<int, string>(COUNT);

		Parallel.For(0, COUNT, index =>
		{
			dictionary[index] = index.ToString();

			if(index % 100 == 0)
			{
				foreach(var item in dictionary)
				{
					Assert.True(item.Key >= 0);
					Assert.True(dictionary.ContainsKey(item.Key));
				}
			}
		});

		Assert.Equal(COUNT, dictionary.Count);
	}

	[Fact]
	public async Task TestAsync()
	{
		const int COUNT = 10000;
		var dictionary = new SynchronizedDictionary<int, string>(COUNT);

		await Parallel.ForAsync(0, COUNT, (index, _) =>
		{
			dictionary[index] = index.ToString();

			if(index % 100 == 0)
			{
				foreach(var item in dictionary)
				{
					Assert.True(item.Key >= 0);
					Assert.True(dictionary.ContainsKey(item.Key));
				}
			}

			return ValueTask.CompletedTask;
		});

		Assert.Equal(COUNT, dictionary.Count);
	}
}
