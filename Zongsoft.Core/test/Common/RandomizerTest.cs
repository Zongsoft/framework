using System;

using Xunit;

namespace Zongsoft.Common.Tests;

public class RandomizerTest
{
	[Fact]
	public void TestGenerateString()
	{
		for(int length = 1; length <= 100; length++)
		{
			var digits = Randomizer.GenerateString(length, true);

			Assert.NotEmpty(digits);
			Assert.Equal(length, digits.Length);
			Assert.All(digits, chr => Assert.True(char.IsDigit(chr)));

			var letters = Randomizer.GenerateString(length, false);
			Assert.NotEmpty(letters);
			Assert.Equal(length, letters.Length);
		}
	}
}
