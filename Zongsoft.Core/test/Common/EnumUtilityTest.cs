using System;

using Xunit;

using Zongsoft.Tests;

namespace Zongsoft.Common.Tests;

public class EnumUtilityTest
{
	[Fact]
	public void TestGetEnumEntry()
	{
		var entry = EnumUtility.GetEnumEntry(Gender.Female);

		Assert.Equal("Female", entry.Name);
		Assert.Equal(Gender.Female, entry.Value); //注意：entry.Value 为枚举类型
		Assert.True(entry.HasAlias("F"));

		entry = EnumUtility.GetEnumEntry(Gender.Male, true);

		Assert.Equal("Male", entry.Name);
		Assert.Equal((byte)1, entry.Value); //注意：entry.Value 为枚举项的基元类型
		Assert.True(entry.HasAlias("M"));
	}

	[Fact]
	public void TestGetEnumEntries()
	{
		var entries = EnumUtility.GetEnumEntries(typeof(Gender), true);

		Assert.Equal(2, entries.Length);
		Assert.Contains(entries, entry => entry.Name == "Male");
		Assert.Contains(entries, entry => entry.Name == "Female");

		entries = EnumUtility.GetEnumEntries(typeof(Nullable<Gender>), true, null, "<Unknown>");

		Assert.Equal(3, entries.Length);
		Assert.Equal("", entries[0].Name);
		Assert.Null(entries[0].Value);
		Assert.Equal("<Unknown>", entries[0].Description);

		Assert.Contains(entries, entry => entry.Name == "Male");
		Assert.Contains(entries, entry => entry.Name == "Female");
	}
}
