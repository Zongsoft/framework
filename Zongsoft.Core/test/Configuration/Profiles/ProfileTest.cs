using System;
using System.Linq;

using Zongsoft.Configuration.Profiles;

using Xunit;
using System.Collections.Generic;

namespace Zongsoft.Configuration.Tests;

public class ProfileTest
{
	public static Profile GetProfile1() => Profile.Load("Configuration/Profiles/Profile-1.ini");
	public static Profile GetProfile2() => Profile.Load("Configuration/Profiles/Profile-2.ini");
	public static Profile GetProfile3() => Profile.Load("Configuration/Profiles/Profile-3.ini");

	[Fact]
	public void TestLoad1() => TestProfile1(GetProfile1());
	private static void TestProfile1(Profile profile)
	{
		Assert.NotNull(profile);
		Assert.NotEmpty(profile.Items);

		Assert.Single(profile.Sections);
		Assert.True(profile.Sections.TryGetValue("plugins", out var section));
		Assert.NotNull(section);
		Assert.Equal("plugins", section.Name);
		Assert.Empty(section.Entries);

		Assert.Single(section.Sections);
		Assert.True(section.Sections.TryGetValue("zongsoft", out section));
		Assert.NotNull(section);
		Assert.Equal("zongsoft", section.Name);
		Assert.Empty(section.Entries);

		Assert.Single(section.Sections);
		Assert.True(section.Sections.TryGetValue("data", out section));
		Assert.NotNull(section);
		Assert.Equal("data", section.Name);
		Assert.Empty(section.Sections);
		Assert.Single(section.Entries);

		Assert.True(section.Entries.TryGetValue("nuget:Zongsoft.Data", out var entry));
		Assert.NotNull(entry);
		Assert.Equal("nuget:Zongsoft.Data", entry.Name);
		Assert.Null(entry.Value);
	}

	[Fact]
	public void TestLoad2() => TestProfile2(GetProfile2());
	private static void TestProfile2(Profile profile)
	{
		Assert.NotNull(profile);
		Assert.NotEmpty(profile.Items);

		Assert.Single(profile.Sections);
		Assert.True(profile.Sections.TryGetValue("plugins", out var section));
		Assert.NotNull(section);
		Assert.Equal("plugins", section.Name);
		Assert.Empty(section.Entries);

		Assert.Single(section.Sections);
		Assert.True(section.Sections.TryGetValue("zongsoft", out section));
		Assert.NotNull(section);
		Assert.Equal("zongsoft", section.Name);
		Assert.Empty(section.Entries);

		Assert.Single(section.Sections);
		Assert.True(section.Sections.TryGetValue("data", out section));
		Assert.NotNull(section);
		Assert.Equal("data", section.Name);
		Assert.Equal(2, section.Sections.Count);
		Assert.Empty(section.Entries);

		Assert.True(section.Sections.TryGetValue("mysql", out var mysql));
		Assert.NotNull(mysql);
		Assert.Equal("mysql", mysql.Name);
		Assert.Empty(mysql.Sections);
		Assert.Single(mysql.Entries);

		Assert.True(mysql.Entries.TryGetValue("nuget:Zongsoft.Data.MySql", out var entry));
		Assert.NotNull(entry);
		Assert.Equal("nuget:Zongsoft.Data.MySql", entry.Name);
		Assert.Null(entry.Value);

		Assert.True(section.Sections.TryGetValue("postgres", out var postgres));
		Assert.NotNull(postgres);
		Assert.Equal("postgres", postgres.Name);
		Assert.Empty(postgres.Sections);
		Assert.Single(postgres.Entries);

		Assert.True(postgres.Entries.TryGetValue("nuget:Zongsoft.Data.Postgres", out entry));
		Assert.NotNull(entry);
		Assert.Equal("nuget:Zongsoft.Data.Postgres", entry.Name);
		Assert.Null(entry.Value);
	}

	[Fact]
	public void TestLoad3() => TestProfile3(GetProfile3());
	private static void TestProfile3(Profile profile)
	{
		Assert.NotNull(profile);
		Assert.NotEmpty(profile.Items);

		var item = profile.Items.FirstOrDefault();

		Assert.NotNull(item);
		Assert.Equal(ProfileItemType.Entry, item.ItemType);
		Assert.IsType<ProfileEntry>(item);

		var entry = (ProfileEntry)item;
		Assert.Equal("../mime", entry.Name);
		Assert.Null(entry.Value);

		Assert.True(profile.Sections.TryGetValue("plugins", out var section));
		Assert.NotNull(section);
		Assert.NotEmpty(section.Entries);
		Assert.Equal("nuget:Zongsoft.Plugins/plugins/Main.plugin", section.Entries.First().Name);

		var comment = Get(profile.Comments, 0);
		Assert.NotNull(comment);
		Assert.IsType<ProfileDirective>(comment);

		var directive = (ProfileDirective)comment;
		Assert.Equal("import", directive.Name);
		Assert.Equal("./Profile-1.option", directive.Argument);

		comment = Get(profile.Comments, 1);
		Assert.NotNull(comment);
		Assert.IsType<ProfileDirective>(comment);

		directive = (ProfileDirective)comment;
		Assert.Equal("import", directive.Name);
		Assert.Equal("./Profile-2.option", directive.Argument);
	}

	#region 私有方法
	private static TElement Get<TElement>(ICollection<TElement> collection, int index)
	{
		if(collection == null || index < 0)
			return default;

		int current = 0;

		foreach(var element in collection)
		{
			if(index == current++)
				return element;
		}

		return default;
	}
	#endregion
}
