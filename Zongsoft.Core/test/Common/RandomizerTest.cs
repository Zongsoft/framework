using System;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Common.Tests;

public class RandomizerTest
{
	const int COUNT = 100;

	[Fact]
	public void TestGenerateSecret()
	{
		var hashset = new HashSet<string>();

		for(int length = 1; length <= COUNT; length++)
		{
			var secret = Randomizer.GenerateSecret(length);
			Assert.NotEmpty(secret);
			Assert.Equal(length, secret.Length);
			Assert.True(hashset.Add(secret));
		}
	}

	[Fact]
	public void TestGenerateString()
	{
		var hashset = new HashSet<string>(COUNT);

		for(int length = 1; length <= COUNT; length++)
		{
			var digits = Randomizer.GenerateString(length, true);

			Assert.NotEmpty(digits);
			Assert.Equal(length, digits.Length);
			Assert.All(digits, chr => Assert.True(char.IsDigit(chr)));

			var letters = Randomizer.GenerateString(length, false);
			Assert.NotEmpty(letters);
			Assert.Equal(length, letters.Length);

			Assert.True(hashset.Add($"DIGITS:{digits}"));
			Assert.True(hashset.Add($"LETTER:{letters}"));
		}
	}
}
