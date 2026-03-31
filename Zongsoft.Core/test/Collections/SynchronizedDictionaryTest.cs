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
	public void GetOrAdd()
	{
		const int COUNT = 1_0000;

		var keys = System.Linq.Enumerable.Range(0, COUNT).ToArray();
		Random.Shared.Shuffle(keys);

		var dictionary = new SynchronizedDictionary<int, string>(COUNT);
		for(int i = 0; i < COUNT; i++)
		{
			if(i % 2 == 1)
				dictionary[i] = $"Value#{i}";
		}

		Assert.Equal(COUNT / 2, dictionary.Count);

		Parallel.For(0, COUNT, index =>
		{
			var key = keys[index];
			var value = dictionary.GetOrAdd(key, key => $"Added#{key}");

			if(key % 2 == 0)
				Assert.Equal($"Added#{key}", value);
			else
				Assert.Equal($"Value#{key}", value);

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
	public async Task GetOrAddAsync()
	{
		const int COUNT = 1_0000;

		var keys = System.Linq.Enumerable.Range(0, COUNT).ToArray();
		Random.Shared.Shuffle(keys);

		var dictionary = new SynchronizedDictionary<int, string>(COUNT);
		for(int i = 0; i < COUNT; i++)
		{
			if(i % 2 == 1)
				dictionary[i] = $"Value#{i}";
		}

		Assert.Equal(COUNT / 2, dictionary.Count);

		await Parallel.ForAsync(0, COUNT, (index, _) =>
		{
			var key = keys[index];
			var value = dictionary.GetOrAdd(key, key => $"Added#{key}");

			if(key % 2 == 0)
				Assert.Equal($"Added#{key}", value);
			else
				Assert.Equal($"Value#{key}", value);

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
	public void AddOrUpdate()
	{
		const int COUNT = 1_0000;

		var keys = System.Linq.Enumerable.Range(0, COUNT).ToArray();
		Random.Shared.Shuffle(keys);

		var dictionary = new SynchronizedDictionary<int, string>(COUNT);
		for(int i = 0; i < COUNT; i++)
		{
			if(i % 2 == 1)
				dictionary[i] = $"Value#{i}";
		}

		Assert.Equal(COUNT / 2, dictionary.Count);

		Parallel.For(0, COUNT, index =>
		{
			var key = keys[index];
			var value = dictionary.AddOrUpdate(key, key => $"Added#{key}", (key, value) => $"Updated#{key}");

			if(key % 2 == 0)
				Assert.Equal($"Added#{key}", value);
			else
				Assert.Equal($"Updated#{key}", value);

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
	public async Task AddOrUpdateAsync()
	{
		const int COUNT = 1_0000;

		var keys = System.Linq.Enumerable.Range(0, COUNT).ToArray();
		Random.Shared.Shuffle(keys);

		var dictionary = new SynchronizedDictionary<int, string>(COUNT);
		for(int i = 0; i < COUNT; i++)
		{
			if(i % 2 == 1)
				dictionary[i] = $"Value#{i}";
		}

		Assert.Equal(COUNT / 2, dictionary.Count);

		await Parallel.ForAsync(0, COUNT, (index, _) =>
		{
			var key = keys[index];
			var value = dictionary.AddOrUpdate(key, key => $"Added#{key}", (key, value) => $"Updated#{key}");

			if(key % 2 == 0)
				Assert.Equal($"Added#{key}", value);
			else
				Assert.Equal($"Updated#{key}", value);

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
	public void TryUpdate()
	{
		const int COUNT = 1_0000;

		var keys = System.Linq.Enumerable.Range(0, COUNT).ToArray();
		Random.Shared.Shuffle(keys);

		var dictionary = new SynchronizedDictionary<int, string>(COUNT);
		for(int i = 0; i < COUNT; i++)
		{
			if(i % 2 == 1)
				dictionary[i] = $"Value#{i}";
		}

		Assert.Equal(COUNT / 2, dictionary.Count);

		Parallel.For(0, COUNT, index =>
		{
			var key = keys[index];
			var updated = dictionary.TryUpdate(key, (key, value) => $"Updated#{key}({value})");

			if(key % 2 == 0)
				Assert.False(updated);
			else
			{
				Assert.True(updated);
				Assert.StartsWith($"Updated#{key}", dictionary[key]);
			}

			if(index % 100 == 0)
			{
				foreach(var item in dictionary)
				{
					Assert.True(item.Key >= 0);
					Assert.True(dictionary.ContainsKey(item.Key));
				}
			}
		});

		Assert.Equal(COUNT / 2, dictionary.Count);
	}

	[Fact]
	public async Task TryUpdateAsync()
	{
		const int COUNT = 1_0000;

		var keys = System.Linq.Enumerable.Range(0, COUNT).ToArray();
		Random.Shared.Shuffle(keys);

		var dictionary = new SynchronizedDictionary<int, string>(COUNT);
		for(int i = 0; i < COUNT; i++)
		{
			if(i % 2 == 1)
				dictionary[i] = $"Value#{i}";
		}

		Assert.Equal(COUNT / 2, dictionary.Count);

		await Parallel.ForAsync(0, COUNT, (index, _) =>
		{
			var key = keys[index];
			var updated = dictionary.TryUpdate(key, (key, value) => $"Updated#{key}({value})");

			if(key % 2 == 0)
				Assert.False(updated);
			else
			{
				Assert.True(updated);
				Assert.StartsWith($"Updated#{key}", dictionary[key]);
			}

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

		Assert.Equal(COUNT / 2, dictionary.Count);
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

	[Fact]
	public async Task ChaosAsync()
	{
		const int COUNT = 1_0000;

		var dictionary = new SynchronizedDictionary<int, string>(COUNT);
		var tasks = new Task[]
		{
			Task.Factory.StartNew(Run, dictionary),
			Task.Factory.StartNew(Run, dictionary),
			Task.Factory.StartNew(Run, dictionary),
			Task.Factory.StartNew(Run, dictionary),
			Task.Factory.StartNew(Run, dictionary),
		};

		await Task.WhenAll(tasks);
		Assert.Equal(COUNT, dictionary.Count);

		static void Run(object state)
		{
			var index = 0;
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			var dictionary = (SynchronizedDictionary<int, string>)state;

			while(Hit(dictionary, index, stopwatch.Elapsed) < COUNT)
				index = index >= COUNT ? 0 : index + 1;

			stopwatch.Stop();
		}

		static int Hit(SynchronizedDictionary<int, string> dictionary, int key, TimeSpan duration)
		{
			switch(Random.Shared.Next() % 10)
			{
				case 0:
					if(duration.TotalSeconds < 1.0)
						dictionary.Remove(key);
					break;
				case 1:
				case 2:
					dictionary.TryAdd(key, $"Value#{key}");
					break;
				case 3:
				case 4:
					if(!dictionary.TryUpdate(key, $"Value.{key}"))
						dictionary.TryAdd(key, $"Value#{key}");
					break;
				default:
					dictionary.AddOrUpdate(key, key => $"Added:{key}", (key, value) => $"Updated:{key}");
					break;
			}

			return dictionary.Count;
		}
	}
}
