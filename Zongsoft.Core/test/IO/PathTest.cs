using System;

using Xunit;

namespace Zongsoft.IO.Tests
{
	public class PathTest
	{
		[Fact]
		public void TestParse()
		{
			var text = @"zfs.local: / data  / images /  1/ year   /   month-day / [1]123.jpg";
			var path = PathToken.Parse(text);

			Assert.Equal("zfs.local", path.Scheme);
			Assert.Equal("/data/images/1/year/month-day/[1]123.jpg", path.FullPath);
			Assert.Equal("/data/images/1/year/month-day/", path.GetDirectory());
			Assert.Equal("[1]123.jpg", path.FileName);

			Assert.True(PathToken.TryParse("/images/avatar/large/steve.jpg", out path));
			Assert.Null(path.Scheme);
			Assert.True(path.IsFile);
			Assert.False(path.IsDirectory);
			Assert.Equal(4, path.Segments.Length);
			Assert.Equal("steve.jpg", path.FileName);

			Assert.True(PathToken.TryParse("zs:", out path));
			Assert.Equal("zs", path.Scheme);
			Assert.Equal(PathAnchor.None, path.Anchor);
			Assert.True(path.IsDirectory);
			Assert.False(path.IsFile);

			Assert.True(PathToken.TryParse("zs: / ", out path));
			Assert.Equal("zs", path.Scheme);
			Assert.Equal(PathAnchor.Root, path.Anchor);
			Assert.True(path.IsDirectory);
			Assert.False(path.IsFile);
			Assert.Equal("/", path.FullPath);
			Assert.Equal("zs:/", path.Url);
			Assert.Null(path.Segments);

			Assert.True(PathToken.TryParse("../directory/", out path));
			Assert.True(string.IsNullOrEmpty(path.Scheme));
			Assert.Equal(PathAnchor.Parent, path.Anchor);
			Assert.True(path.IsDirectory);
			Assert.False(path.IsFile);
			Assert.Equal("../directory/", path.FullPath);
			Assert.Equal("../directory/", path.Url);
			Assert.Equal(2, path.Segments.Length);
			Assert.Equal("directory", path.Segments[0]);
			Assert.True(string.IsNullOrEmpty(path.Segments[1]));

			path = PathToken.Parse(@"D:\dir1\dir2/dir3/fileName.ext");
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
