using System;

using Xunit;

namespace Zongsoft.Collections.Tests;

public class CategoryTest
{
	#region 测试方法
	[Fact]
	public void TestName()
	{
		var root = Initialize();

		Assert.True(root.IsRoot());
		Assert.Equal("/", root.Name);
		Assert.Equal(string.Empty, root.Path);
		Assert.Equal("/", root.FullPath);

		var category = new Category();
		Assert.True(root.IsRoot());

		Assert.IsType<ArgumentException>(Record.Exception(() => root.Categories.Add(category)));
		Assert.IsType<ArgumentException>(Record.Exception(() => new Category("/")));
		Assert.IsType<ArgumentException>(Record.Exception(() => new Category("ABC\\")));
		Assert.IsType<ArgumentException>(Record.Exception(() => new Category("ABC/DEF")));
		Assert.IsType<ArgumentNullException>(Record.Exception(() => new Category(string.Empty)));
		Assert.IsType<ArgumentNullException>(Record.Exception(() => new Category(" ")));
		Assert.IsType<ArgumentNullException>(Record.Exception(() => new Category("\t")));
		Assert.IsType<ArgumentNullException>(Record.Exception(() => new Category("\n")));
		Assert.IsType<ArgumentNullException>(Record.Exception(() => new Category("\r")));
		Assert.IsType<ArgumentNullException>(Record.Exception(() => new Category(Environment.NewLine)));
	}

	[Fact]
	public void TestFind()
	{
		var root = Initialize();

		var found = root.Find("File");
		Assert.NotNull(found);
		Assert.Equal("File", found.Name);
		Assert.Equal("/", found.Path);
		Assert.Equal("/File", found.FullPath);

		found = root.Find("Edit");
		Assert.NotNull(found);
		Assert.Equal("Edit", found.Name);
		Assert.Equal("/", found.Path);
		Assert.Equal("/Edit", found.FullPath);

		found = root.Find("Help");
		Assert.NotNull(found);
		Assert.Equal("Help", found.Name);
		Assert.Equal("/", found.Path);
		Assert.Equal("/Help", found.FullPath);

		found = root.Find(" File / Save");
		Assert.NotNull(found);
		Assert.Equal("Save", found.Name);
		Assert.Equal("/File", found.Path);
		Assert.Equal("/File/Save", found.FullPath);

		Assert.NotNull(root.Find(" File/ Recents"));
		Assert.NotNull(root.Find(" File /Recents / Document-1"));

		Assert.NotNull(root.Find(" /File/ Save"));
		Assert.NotNull(root.Find("/ File  /Recents"));
		Assert.NotNull(root.Find(" / File  /  Recents/Document-2"));

		found = root.Find("File").Find(" Open");
		Assert.NotNull(found);
		Assert.Equal("Open", found.Name);
		Assert.Equal("/File", found.Path);
		Assert.Equal("/File/Open", found.FullPath);

		found = root.Find("File").Find("./ Recents");
		Assert.NotNull(found);
		Assert.Equal("Recents", found.Name);
		Assert.Equal("/File", found.Path);
		Assert.Equal("/File/Recents", found.FullPath);

		found = root.Find("File").Find("Recents").Find("Document-2  ");
		Assert.NotNull(found);
		Assert.Equal("Document-2", found.Name);
		Assert.Equal("/File/Recents", found.Path);
		Assert.Equal("/File/Recents/Document-2", found.FullPath);

		found = root.Find("Edit").Find(".. / File / Save");
		Assert.NotNull(found);
		Assert.Equal("Save", found.Name);
		Assert.Equal("/File", found.Path);
		Assert.Equal("/File/Save", found.FullPath);
	}

	[Fact]
	public void TestOrdinal()
	{
		const int COUNT = 100;

		var root = new Category();

		for(int i = 0; i < COUNT; i++)
		{
			var ordinal = Random.Shared.Next() % COUNT;
			root.Categories.Add(new Category($"A{(i + 1):000}") { Ordinal = ordinal });
		}

		for(int i = 1; i < root.Categories.Count; i++)
		{
			Assert.NotNull(root.Categories[i]);
			Assert.NotNull(root.Categories[i - 1]);
			Assert.True(root.Categories[i].Ordinal >= root.Categories[i - 1].Ordinal);
		}
	}
	#endregion

	#region 私有方法
	private static Category Initialize()
	{
		var root = new Category();

		var file = root.Categories.Add("File");
		var edit = root.Categories.Add("Edit");
		var help = root.Categories.Add("Help");

		file.Categories.Add("Open");
		file.Categories.Add("Close");
		file.Categories.Add("Save");
		file.Categories.Add("SaveAs");
		file.Categories.Add("Recents").Categories.AddRange(
			new Category("Document-1"),
			new Category("Document-2")
		);

		return root;
	}
	#endregion
}
