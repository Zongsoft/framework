using System;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Common.Tests;

public class RandomGeneratorTest
{
	[Fact]
	public void TestGenerateString()
	{
		var data = new string[100];

		for(int i = 0; i < data.Length; i++)
		{
			for(int j = 1; j <= 128; j++)
			{
				data[i] = Randomizer.GenerateString(j);
				Assert.True(!string.IsNullOrEmpty(data[i]) && data[i].Length == j);
			}
		}
	}
}
