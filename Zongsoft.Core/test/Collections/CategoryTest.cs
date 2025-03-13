using System;

using Xunit;

namespace Zongsoft.Collections.Tests
{
	public class CategoryTest
	{
		private Category _root;

		#region 构造函数
		public CategoryTest()
		{
			_root = new Category();

			var file = _root.Categories.Add("File");
			var edit = _root.Categories.Add("Edit");
			var help = _root.Categories.Add("Help");

			file.Categories.Add("Open");
			file.Categories.Add("Close");
			file.Categories.Add("Save");
			file.Categories.Add("SaveAs");
			file.Categories.Add("Recents").Categories.AddRange(
				new Category("Document-1"),
				new Category("Document-2")
			);
		}
		#endregion

		#region 测试方法
		[Fact]
		public void TestName()
		{
			Assert.True(_root.IsRoot());
			Assert.Equal("/", _root.Name);
			Assert.Equal(string.Empty, _root.Path);
			Assert.Equal("/", _root.FullPath);

			var category = new Category();
			Assert.True(_root.IsRoot());

			Assert.IsType<ArgumentException>(Record.Exception(() => _root.Categories.Add(category)));
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
			var found = _root.Find("File");
			Assert.NotNull(found);
			Assert.Equal("File", found.Name);
			Assert.Equal("/", found.Path);
			Assert.Equal("/File", found.FullPath);

			found = _root.Find("Edit");
			Assert.NotNull(found);
			Assert.Equal("Edit", found.Name);
			Assert.Equal("/", found.Path);
			Assert.Equal("/Edit", found.FullPath);

			found = _root.Find("Help");
			Assert.NotNull(found);
			Assert.Equal("Help", found.Name);
			Assert.Equal("/", found.Path);
			Assert.Equal("/Help", found.FullPath);

			found = _root.Find(" File / Save");
			Assert.NotNull(found);
			Assert.Equal("Save", found.Name);
			Assert.Equal("/File", found.Path);
			Assert.Equal("/File/Save", found.FullPath);

			Assert.NotNull(_root.Find(" File/ Recents"));
			Assert.NotNull(_root.Find(" File /Recents / Document-1"));

			Assert.NotNull(_root.Find(" /File/ Save"));
			Assert.NotNull(_root.Find("/ File  /Recents"));
			Assert.NotNull(_root.Find(" / File  /  Recents/Document-2"));

			found = _root.Find("File").Find(" Open");
			Assert.NotNull(found);
			Assert.Equal("Open", found.Name);
			Assert.Equal("/File", found.Path);
			Assert.Equal("/File/Open", found.FullPath);

			found = _root.Find("File").Find("./ Recents");
			Assert.NotNull(found);
			Assert.Equal("Recents", found.Name);
			Assert.Equal("/File", found.Path);
			Assert.Equal("/File/Recents", found.FullPath);

			found = _root.Find("File").Find("Recents").Find("Document-2  ");
			Assert.NotNull(found);
			Assert.Equal("Document-2", found.Name);
			Assert.Equal("/File/Recents", found.Path);
			Assert.Equal("/File/Recents/Document-2", found.FullPath);

			found = _root.Find("Edit").Find(".. / File / Save");
			Assert.NotNull(found);
			Assert.Equal("Save", found.Name);
			Assert.Equal("/File", found.Path);
			Assert.Equal("/File/Save", found.FullPath);
		}
		#endregion
	}
}
