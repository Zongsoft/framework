using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Collections.Tests;

public class SynchronizedDictionaryTest
{
	[Fact]
	public void Set()
	{
		const int COUNT = 1_0000;
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
	public async Task SetAsync()
	{
		const int COUNT = 1_0000;
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

	[Fact]
	public void TryAdd()
	{
		const int COUNT = 1_0000;
		var dictionary = new SynchronizedDictionary<int, string>(COUNT);

		Parallel.For(0, COUNT, index =>
		{
			Assert.True(dictionary.TryAdd(index, $"Value#{index}"));

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

		Parallel.For(0, COUNT, index =>
		{
			Assert.False(dictionary.TryAdd(index, $"Value#{index}"));

			if(index % 100 == 0)
			{
				foreach(var item in dictionary)
				{
					Assert.True(item.Key >= 0);
					Assert.True(dictionary.ContainsKey(item.Key));
				}
			}
		});
	}

	[Fact]
	public async Task TryAddAsync()
	{
		const int COUNT = 1_0000;
		var dictionary = new SynchronizedDictionary<int, string>(COUNT);

		await Parallel.ForAsync(0, COUNT, (index, _) =>
		{
			Assert.True(dictionary.TryAdd(index, $"Value#{index}"));

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

		await Parallel.ForAsync(0, COUNT, (index, _) =>
		{
			Assert.False(dictionary.TryAdd(index, $"Value#{index}"));

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
	}

	[Fact]
	public void Remove()
	{
		const int COUNT = 1_0000;

		var keys = System.Linq.Enumerable.Range(0, COUNT).ToArray();
		Random.Shared.Shuffle(keys);

		var dictionary = new SynchronizedDictionary<int, string>(COUNT);
		for(int i = 0; i < COUNT; i++)
			dictionary[i] = $"Value#{i}";

		Assert.Equal(COUNT, dictionary.Count);

		Parallel.For(0, COUNT, index =>
		{
			Assert.True(dictionary.Remove(keys[index]));
			Assert.False(dictionary.ContainsKey(keys[index]));

			if(index % 100 == 0)
			{
				foreach(var item in dictionary)
				{
					Assert.True(item.Key >= 0);
					Assert.True(dictionary.ContainsKey(item.Key));
				}
			}
		});

		Assert.Empty(dictionary);
	}

	[Fact]
	public async Task RemoveAsync()
	{
		const int COUNT = 1_0000;

		var keys = System.Linq.Enumerable.Range(0, COUNT).ToArray();
		Random.Shared.Shuffle(keys);

		var dictionary = new SynchronizedDictionary<int, string>(COUNT);
		for(int i = 0; i < COUNT; i++)
			dictionary[i] = $"Value#{i}";

		Assert.Equal(COUNT, dictionary.Count);

		await Parallel.ForAsync(0, COUNT, (index, _) =>
		{
			Assert.True(dictionary.Remove(keys[index]));
			Assert.False(dictionary.ContainsKey(keys[index]));

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

		Assert.Empty(dictionary);
	}
}
