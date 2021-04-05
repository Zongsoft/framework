using System;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Data.Tests
{
	public class CriteriaParserTest
	{
		[Fact]
		public void TestParse()
		{
			var succeed = CriteriaParser.TryParse("  \t", out var result);

			Assert.False(succeed);
			Assert.Null(result);

			succeed = CriteriaParser.TryParse("checked", out result);

			Assert.True(succeed);
			Assert.Single(result);
			Assert.Equal("checked", result[0].Key);
			Assert.Null(result[0].Value);

			this.TestParse1();
			this.TestParse2();
		}

		[Fact]
		private void TestParse1()
		{
			string TEXT = @"k1:v1";
			var succeed = CriteriaParser.TryParse(TEXT.AsSpan(), out var result);
			AssertResult(result, "k1", "v1");

			TEXT = @"k1.k2:v1";
			succeed = CriteriaParser.TryParse(TEXT.AsSpan(), out result);
			AssertResult(result, "k1.k2", "v1");

			TEXT = @" k1: v1 +";
			succeed = CriteriaParser.TryParse(TEXT.AsSpan(), out result);
			AssertResult(result, "k1", "v1");

			TEXT = @" k1 : v 1 + ";
			succeed = CriteriaParser.TryParse(TEXT.AsSpan(), out result);
			AssertResult(result, "k1", "v 1");

			TEXT = @" k1 : v: 1 + ";
			succeed = CriteriaParser.TryParse(TEXT.AsSpan(), out result);
			AssertResult(result, "k1", "v: 1");

			TEXT = @" k1 : v:\+ 1 + ";
			succeed = CriteriaParser.TryParse(TEXT.AsSpan(), out result);
			AssertResult(result, "k1", "v:+ 1");

			static void AssertResult(KeyValuePair<string, string>[] result, string key, string value)
			{
				Assert.NotEmpty(result);
				Assert.Single(result);

				Assert.Equal(key, result[0].Key);
				Assert.Equal(value, result[0].Value);
			}
		}

		[Fact]
		private void TestParse2()
		{
			string TEXT = @" +  + k1:v1+++checked + ++ ";
			var succeed = CriteriaParser.TryParse(TEXT.AsSpan(), out var result);
			AssertResult(result);

			TEXT = @" k1: v1 +checked  ";
			succeed = CriteriaParser.TryParse(TEXT.AsSpan(), out result);
			AssertResult(result);

			TEXT = @" k1 : v1 + checked  ";
			succeed = CriteriaParser.TryParse(TEXT.AsSpan(), out result);
			AssertResult(result);

			static void AssertResult(KeyValuePair<string, string>[] result)
			{
				Assert.NotEmpty(result);
				Assert.Equal(2, result.Length);

				Assert.Equal("k1", result[0].Key);
				Assert.Equal("v1", result[0].Value);
				Assert.Equal("checked", result[1].Key);
				Assert.Null(result[1].Value);
			}
		}
	}
}
