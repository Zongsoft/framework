using System;
using System.Collections.Generic;

using Zongsoft.Common;
using Zongsoft.Tests;

using Xunit;

namespace Zongsoft.Common.Tests
{
	public class EnumUtilityTest
	{
		[Fact]
		public void TestGetEnumEntry()
		{
			var entry = EnumUtility.GetEnumEntry(Gender.Female);

			Assert.Equal("Female", entry.Name);
			Assert.Equal(Gender.Female, entry.Value); //注意：entry.Value 为枚举类型
			Assert.Equal("F", entry.Alias);
			Assert.Equal("女士", entry.Description);
			Assert.Equal("女士", EnumUtility.GetEnumDescription(Gender.Female));

			entry = EnumUtility.GetEnumEntry(Gender.Male, true);

			Assert.Equal("Male", entry.Name);
			Assert.Equal(1, entry.Value); //注意：entry.Value 为枚举项的基元类型
			Assert.Equal("M", entry.Alias);
			Assert.Equal("男士", entry.Description);
			Assert.Equal("男士", EnumUtility.GetEnumDescription(Gender.Male));
		}

		[Fact]
		public void TestGetEnumEntries()
		{
			var entries = EnumUtility.GetEnumEntries(typeof(Gender), true);

			Assert.Equal(2, entries.Length);
			Assert.Equal("Male", entries[1].Name);
			Assert.Equal("Female", entries[0].Name);

			entries = EnumUtility.GetEnumEntries(typeof(Nullable<Gender>), true, null, "<Unknown>");

			Assert.Equal(3, entries.Length);
			Assert.Equal("", entries[0].Name);
			Assert.Null(entries[0].Value);
			Assert.Equal("<Unknown>", entries[0].Description);

			Assert.Equal("Male", entries[2].Name);
			Assert.Equal("Female", entries[1].Name);
		}
	}
}
