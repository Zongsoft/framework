using System;

using Xunit;

namespace Zongsoft.IO.Tests
{
	public class PathTest
	{
		[Fact]
		public void TestParse()
		{
			var root = Path.Parse("/");
			Assert.Equal(PathAnchor.Root, root.Anchor);
			Assert.Equal("/", root.FullPath);
			Assert.Equal("/", root.Url);
			Assert.True(root.IsDirectory);
			Assert.False(root.IsFile);
			Assert.False(root.HasSegments);

			var text = @"zfs.local: / data  / images /  1/ year   /   month-day / [1]123.jpg";
			var path = Path.Parse(text);

			Assert.Equal("zfs.local", path.Scheme);
			Assert.Equal("/data/images/1/year/month-day/[1]123.jpg", path.FullPath);
			Assert.Equal("/data/images/1/year/month-day/", path.GetDirectory());
			Assert.Equal("[1]123.jpg", path.FileName);

			Assert.True(Path.TryParse("/images/avatar/large/steve.jpg", out path));
			Assert.Null(path.Scheme);
			Assert.True(path.IsFile);
			Assert.False(path.IsDirectory);
			Assert.Equal(4, path.Segments.Length);
			Assert.Equal("steve.jpg", path.FileName);

			Assert.True(Path.TryParse("zs:", out path));
			Assert.Equal("zs", path.Scheme);
			Assert.Equal(PathAnchor.None, path.Anchor);
			Assert.True(path.IsDirectory);
			Assert.False(path.IsFile);

			Assert.True(Path.TryParse("zs: / ", out path));
			Assert.Equal("zs", path.Scheme);
			Assert.Equal(PathAnchor.Root, path.Anchor);
			Assert.True(path.IsDirectory);
			Assert.False(path.IsFile);
			Assert.Equal("/", path.FullPath);
			Assert.Equal("/", path.GetDirectory());
			Assert.Equal("zs:/", path.Url);
			Assert.Equal("zs:/", path.GetDirectoryUrl());
			Assert.Null(path.Segments);

			Assert.True(Path.TryParse("zs: / dir1/dir2/ ", out path));
			Assert.Equal("zs", path.Scheme);
			Assert.Equal(PathAnchor.Root, path.Anchor);
			Assert.True(path.IsDirectory);
			Assert.False(path.IsFile);
			Assert.Equal("/dir1/dir2/", path.FullPath);
			Assert.Equal("/dir1/dir2/", path.GetDirectory());
			Assert.Equal("zs:/dir1/dir2/", path.Url);
			Assert.Equal("zs:/dir1/dir2/", path.GetDirectoryUrl());
			Assert.Equal(3, path.Segments.Length);
			Assert.Equal("dir1", path.Segments[0]);
			Assert.Equal("dir2", path.Segments[1]);
			Assert.Equal("", path.Segments[2]);

			Assert.True(Path.TryParse("../directory/", out path));
			Assert.True(string.IsNullOrEmpty(path.Scheme));
			Assert.Equal(PathAnchor.Parent, path.Anchor);
			Assert.True(path.IsDirectory);
			Assert.False(path.IsFile);
			Assert.Equal("../directory/", path.FullPath);
			Assert.Equal("../directory/", path.Url);
			Assert.Equal(2, path.Segments.Length);
			Assert.Equal("directory", path.Segments[0]);
			Assert.True(string.IsNullOrEmpty(path.Segments[1]));

			path = Path.Parse(@"D:\dir1\ dir2 / / dir3 /fileName.ext");
			Assert.Equal(LocalFileSystem.Instance.Scheme, path.Scheme);
			Assert.Equal(PathAnchor.Root, path.Anchor);
			Assert.True(path.IsFile);
			Assert.False(path.IsDirectory);
			Assert.Equal("/D/dir1/dir2/dir3/fileName.ext", path.FullPath);
			Assert.Equal("/D/dir1/dir2/dir3/", path.GetDirectory());
			Assert.Equal(5, path.Segments.Length);
			Assert.Equal("fileName.ext", path.FileName);
		}

		[Fact]
		public void TestGetUrl()
		{
			var url = FileSystem.GetUrl("1");
			Assert.NotNull(url);
			Assert.Equal("1", url);

			url = FileSystem.GetUrl("12");
			Assert.NotNull(url);
			Assert.Equal("12", url);
		}

		[Fact]
		public void TestCombine()
		{
			var baseDirectory = "zfs.local:/data/images/";
			var selfDirectory = "./bin";
			var parentDirectory = "../bin/Debug";

			Assert.Equal("zfs.local:/data/images/bin", Path.Combine(baseDirectory, selfDirectory));
			Assert.Equal("zfs.local:/data/bin/Debug", Path.Combine(baseDirectory, parentDirectory));
			Assert.Equal("zfs.local:/data/images/bin/Debug", Path.Combine(baseDirectory, selfDirectory, parentDirectory));
			Assert.Equal("/root", Path.Combine(baseDirectory, "/root"));
			Assert.Equal("/root/", Path.Combine(baseDirectory, selfDirectory, parentDirectory, "/root/"));

			Assert.Equal(@"D:/data/images/avatars/001.jpg", Path.Combine(@"D:\data\images\", "avatars/001.jpg"));
			Assert.Equal(@"D:/data/images/avatars/001.jpg", Path.Combine(@"D:\data\images\", ". /avatars / 001.jpg"));
			Assert.Equal(@"D:/data/avatars/001.jpg", Path.Combine(@"D:\data\images\", "../avatars / 001.jpg"));
			Assert.Equal(@"/avatars/001.jpg", Path.Combine(@"D:\data\images\", "/avatars/001.jpg"));
			Assert.Equal(@"/final.ext", Path.Combine(@"D:\data\images\", "avatars / 001.jpg", " / final.ext"));
			Assert.Equal(@"/final.ext/tail", Path.Combine(@"D:\data\images\", "avatars / 001.jpg", " / final.ext \t ", "tail  "));
		}
	}
}
