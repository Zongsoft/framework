using System;
using System.Collections;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Collections.Tests;

public class DictionaryTest
{
	[Fact]
	public void TestGenericDictionary()
	{
		object dictionary = new Dictionary<string, int>();

		Assert.True(DictionaryUtility.TryAdd(dictionary, "K1", 1));
		Assert.NotEmpty((IDictionary<string, int>)dictionary);
		Assert.Equal(1, ((IDictionary<string, int>)dictionary)["K1"]);

		Assert.True(DictionaryUtility.TryAdd(dictionary, "K2", 2));
		Assert.Equal(2, ((IDictionary<string, int>)dictionary).Count);
		Assert.Equal(1, ((IDictionary<string, int>)dictionary)["K1"]);
		Assert.Equal(2, ((IDictionary<string, int>)dictionary)["K2"]);

		Assert.True(DictionaryUtility.TryRemove(dictionary, "K1"));
		Assert.Single((IDictionary<string, int>)dictionary);
		Assert.True(DictionaryUtility.TryRemove(dictionary, "K2"));
		Assert.Empty((IDictionary<string, int>)dictionary);
	}

	[Fact]
	public void TestClassicDictionary()
	{
		object dictionary = new Hashtable();

		Assert.True(DictionaryUtility.TryAdd(dictionary, "K1", 1));
		Assert.NotEmpty((IDictionary)dictionary);
		Assert.Equal(1, ((IDictionary)dictionary)["K1"]);

		Assert.True(DictionaryUtility.TryAdd(dictionary, "K2", 2));
		Assert.Equal(2, ((IDictionary)dictionary).Count);
		Assert.Equal(1, ((IDictionary)dictionary)["K1"]);
		Assert.Equal(2, ((IDictionary)dictionary)["K2"]);

		Assert.True(DictionaryUtility.TryRemove(dictionary, "K1"));
		Assert.Single((IDictionary)dictionary);
		Assert.True(DictionaryUtility.TryRemove(dictionary, "K2"));
		Assert.Empty(((IDictionary)dictionary));
	}
}
