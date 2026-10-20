using System;
using System.Collections;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Collections.Tests;

public class CollectionTest
{
	[Fact]
	public void TestGenericCollection()
	{
		object list = new List<int>();

		Assert.True(CollectionUtility.TryAdd(list, 1));
		Assert.NotEmpty((ICollection<int>)list);
		Assert.Equal(1, ((IList<int>)list)[0]);

		Assert.True(CollectionUtility.TryAdd(list, 2));
		Assert.Equal(2, ((IList<int>)list).Count);
		Assert.Equal(1, ((IList<int>)list)[0]);
		Assert.Equal(2, ((IList<int>)list)[1]);

		Assert.True(CollectionUtility.TryRemove(list, 1));
		Assert.Single((IList<int>)list);
		Assert.True(CollectionUtility.TryRemove(list, 2));
		Assert.Empty((IList<int>)list);
	}

	[Fact]
	public void TestClassicCollection()
	{
		object list = new ArrayList();

		Assert.True(CollectionUtility.TryAdd(list, 1));
		Assert.NotEmpty((ICollection)list);
		Assert.Equal(1, ((IList)list)[0]);

		Assert.True(CollectionUtility.TryAdd(list, 2));
		Assert.Equal(2, ((IList)list).Count);
		Assert.Equal(1, ((IList)list)[0]);
		Assert.Equal(2, ((IList)list)[1]);

		Assert.True(CollectionUtility.TryRemove(list, 1));
		Assert.Single((IList)list);
		Assert.True(CollectionUtility.TryRemove(list, 2));
		Assert.Empty((IList)list);
	}
}
