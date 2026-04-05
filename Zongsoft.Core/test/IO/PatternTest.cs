using System;

using Xunit;

namespace Zongsoft.IO.Tests;

public class PatternTest
{
	[Fact]
	public void Match()
	{
		var pattern = new Pattern();
		Assert.True(pattern.IsEmpty);
		Assert.Equal(pattern, default);
		Assert.Equal(pattern, new Pattern("*"));

		Assert.True(pattern.Match(null));
		Assert.True(pattern.Match(string.Empty));
		Assert.True(pattern.Match("_"));
		Assert.True(pattern.Match("ext"));
		Assert.True(pattern.Match(Guid.NewGuid().ToString()));
	}

	[Fact]
	public void MatchEndsWithAsterisk()
	{
		const string PATTERN = "*.ext";

		var pattern = new Pattern(PATTERN);
		Assert.False(pattern.IsEmpty);
		Assert.Equal(PATTERN, pattern);
		Assert.False(pattern.Match(null));
		Assert.False(pattern.Match(string.Empty));
		Assert.False(pattern.Match("_"));
		Assert.False(pattern.Match("ext"));
		Assert.False(pattern.Match(".exe"));
		Assert.False(pattern.Match("abc.exe"));
		Assert.False(pattern.Match(@"D:\_"));
		Assert.False(pattern.Match(@"D:\ext"));
		Assert.False(pattern.Match(@"D:\.exe"));
		Assert.False(pattern.Match(@"D:\abc.exe"));
		Assert.False(pattern.Match(@"D:\dir\abc.exe"));

		Assert.True(pattern.Match(".ext"));
		Assert.True(pattern.Match("abc.ext"));
		Assert.True(pattern.Match(@"D:\.ext"));
		Assert.True(pattern.Match(@"D:\abc.ext"));
		Assert.True(pattern.Match(@"D:\dir\abc.ext"));
	}

	[Fact]
	public void MatchStartsWithAsterisk()
	{
		const string PATTERN = "app.*";

		var pattern = new Pattern(PATTERN);
		Assert.False(pattern.IsEmpty);
		Assert.Equal(PATTERN, pattern);
		Assert.False(pattern.Match(null));
		Assert.False(pattern.Match(string.Empty));
		Assert.False(pattern.Match("_"));
		Assert.False(pattern.Match("abc"));
		Assert.False(pattern.Match("app"));
		Assert.False(pattern.Match(@"D:\_"));
		Assert.False(pattern.Match(@"D:\abc"));
		Assert.False(pattern.Match(@"D:\app"));
		Assert.False(pattern.Match(@"D:\dir\abc"));
		Assert.False(pattern.Match(@"D:\dir\app"));

		Assert.True(pattern.Match("app."));
		Assert.True(pattern.Match("app.ext"));
		Assert.True(pattern.Match(System.IO.Path.GetFileName(@"D:\app.")));
		Assert.True(pattern.Match(System.IO.Path.GetFileName(@"D:\app.ext")));
		Assert.True(pattern.Match(System.IO.Path.GetFileName(@"D:\dir\app.")));
		Assert.True(pattern.Match(System.IO.Path.GetFileName(@"D:\dir\app.ext")));
	}

	[Fact]
	public void MatchEndsWithSingle()
	{
		const string PATTERN = "*.?";

		var pattern = new Pattern(PATTERN);
		Assert.False(pattern.IsEmpty);
		Assert.Equal(PATTERN, pattern);
		Assert.False(pattern.Match(null));
		Assert.False(pattern.Match(string.Empty));
		Assert.False(pattern.Match("_"));
		Assert.False(pattern.Match("ext"));
		Assert.False(pattern.Match(".ext"));
		Assert.False(pattern.Match("abc.ext"));
		Assert.False(pattern.Match(@"D:\_"));
		Assert.False(pattern.Match(@"D:\ext"));
		Assert.False(pattern.Match(@"D:\.ext"));
		Assert.False(pattern.Match(@"D:\abc.ext"));
		Assert.False(pattern.Match(@"D:\dir\abc.ext"));

		Assert.True(pattern.Match("._"));
		Assert.True(pattern.Match(".a"));
		Assert.True(pattern.Match("abc._"));
		Assert.True(pattern.Match("abc.b"));
		Assert.True(pattern.Match(@"D:\._"));
		Assert.True(pattern.Match(@"D:\.c"));
		Assert.True(pattern.Match(@"D:\abc._"));
		Assert.True(pattern.Match(@"D:\abc.d"));
		Assert.True(pattern.Match(@"D:\dir\abc._"));
		Assert.True(pattern.Match(@"D:\dir\abc.e"));
	}

	[Fact]
	public void MatchStartsWithSingle()
	{
		const string PATTERN = "app.?";

		var pattern = new Pattern(PATTERN);
		Assert.False(pattern.IsEmpty);
		Assert.Equal(PATTERN, pattern);
		Assert.False(pattern.Match(null));
		Assert.False(pattern.Match(string.Empty));
		Assert.False(pattern.Match("_"));
		Assert.False(pattern.Match("abc"));
		Assert.False(pattern.Match("app"));
		Assert.False(pattern.Match(@"D:\_"));
		Assert.False(pattern.Match(@"D:\abc"));
		Assert.False(pattern.Match(@"D:\app"));
		Assert.False(pattern.Match(@"D:\dir\abc"));
		Assert.False(pattern.Match(@"D:\dir\app"));

		Assert.False(pattern.Match("app."));
		Assert.False(pattern.Match("app.ext"));
		Assert.False(pattern.Match(@"D:\app."));
		Assert.False(pattern.Match(@"D:\app.ext"));
		Assert.False(pattern.Match(@"D:\dir\app."));
		Assert.False(pattern.Match(@"D:\dir\app.ext"));

		Assert.True(pattern.Match("app.a"));
		Assert.True(pattern.Match("app._"));
		Assert.True(pattern.Match(System.IO.Path.GetFileName(@"D:\app.a")));
		Assert.True(pattern.Match(System.IO.Path.GetFileName(@"D:\app.b")));
		Assert.True(pattern.Match(System.IO.Path.GetFileName(@"D:\dir\app.c")));
		Assert.True(pattern.Match(System.IO.Path.GetFileName(@"D:\dir\app.d")));
	}

	[Fact]
	public void MatchEndsWithMany()
	{
		const string PATTERN = "*.??";

		var pattern = new Pattern(PATTERN);
		Assert.False(pattern.IsEmpty);
		Assert.Equal(PATTERN, pattern);
		Assert.False(pattern.Match(null));
		Assert.False(pattern.Match(string.Empty));
		Assert.False(pattern.Match("_"));
		Assert.False(pattern.Match("ext"));
		Assert.False(pattern.Match(".ext"));
		Assert.False(pattern.Match("abc.ext"));
		Assert.False(pattern.Match(@"D:\_"));
		Assert.False(pattern.Match(@"D:\ext"));
		Assert.False(pattern.Match(@"D:\.ext"));
		Assert.False(pattern.Match(@"D:\abc.ext"));
		Assert.False(pattern.Match(@"D:\dir\abc.ext"));

		Assert.True(pattern.Match(".__"));
		Assert.True(pattern.Match(".aa"));
		Assert.True(pattern.Match("abc.__"));
		Assert.True(pattern.Match("abc.bb"));
		Assert.True(pattern.Match(@"D:\.__"));
		Assert.True(pattern.Match(@"D:\.cc"));
		Assert.True(pattern.Match(@"D:\abc.__"));
		Assert.True(pattern.Match(@"D:\abc.12"));
		Assert.True(pattern.Match(@"D:\dir\abc.__"));
		Assert.True(pattern.Match(@"D:\dir\abc.12"));
	}

	[Fact]
	public void MatchStartsWithMany()
	{
		const string PATTERN = "app.??";

		var pattern = new Pattern(PATTERN);
		Assert.False(pattern.IsEmpty);
		Assert.Equal(PATTERN, pattern);
		Assert.False(pattern.Match(null));
		Assert.False(pattern.Match(string.Empty));
		Assert.False(pattern.Match("_"));
		Assert.False(pattern.Match("abc"));
		Assert.False(pattern.Match("app"));
		Assert.False(pattern.Match(@"D:\_"));
		Assert.False(pattern.Match(@"D:\abc"));
		Assert.False(pattern.Match(@"D:\app"));
		Assert.False(pattern.Match(@"D:\dir\abc"));
		Assert.False(pattern.Match(@"D:\dir\app"));

		Assert.False(pattern.Match("app."));
		Assert.False(pattern.Match("app.ext"));
		Assert.False(pattern.Match(@"D:\app."));
		Assert.False(pattern.Match(@"D:\app.ext"));
		Assert.False(pattern.Match(@"D:\dir\app."));
		Assert.False(pattern.Match(@"D:\dir\app.ext"));

		Assert.True(pattern.Match("app.aa"));
		Assert.True(pattern.Match("app.__"));
		Assert.True(pattern.Match(System.IO.Path.GetFileName(@"D:\app.aa")));
		Assert.True(pattern.Match(System.IO.Path.GetFileName(@"D:\app.bb")));
		Assert.True(pattern.Match(System.IO.Path.GetFileName(@"D:\dir\app.12")));
		Assert.True(pattern.Match(System.IO.Path.GetFileName(@"D:\dir\app.34")));
	}

	[Fact]
	public void MatchContains()
	{
		const string PATTERN = "a??s.ex?";

		var pattern = new Pattern(PATTERN, true);
		Assert.False(pattern.IsEmpty);
		Assert.Equal(PATTERN, pattern);
		Assert.False(pattern.Match(null));
		Assert.False(pattern.Match(string.Empty));
		Assert.False(pattern.Match("_"));
		Assert.False(pattern.Match("app"));
		Assert.False(pattern.Match("app.e"));
		Assert.False(pattern.Match("app.ex"));
		Assert.False(pattern.Match("app.ext"));
		Assert.False(pattern.Match("apps.ex"));
		Assert.False(pattern.Match("apps.extension"));

		Assert.True(pattern.Match("abcs.exe"));
		Assert.True(pattern.Match("abcs.ext"));
		Assert.True(pattern.Match("apps.exe"));
		Assert.True(pattern.Match("apps.ext"));
		Assert.True(pattern.Match("abcs.EXE"));
		Assert.True(pattern.Match("abcs.Ext"));
		Assert.True(pattern.Match("Apps.exe"));
		Assert.True(pattern.Match("Apps.ext"));

		pattern = new Pattern(PATTERN, false);
		Assert.False(pattern.IsEmpty);
		Assert.Equal(PATTERN, pattern);
		Assert.False(pattern.Match(null));
		Assert.False(pattern.Match(string.Empty));
		Assert.False(pattern.Match("_"));
		Assert.False(pattern.Match("app"));
		Assert.False(pattern.Match("app.e"));
		Assert.False(pattern.Match("app.ex"));
		Assert.False(pattern.Match("app.ext"));
		Assert.False(pattern.Match("apps.ex"));
		Assert.False(pattern.Match("apps.extension"));

		Assert.True(pattern.Match("abcs.exe"));
		Assert.True(pattern.Match("abcs.ext"));
		Assert.True(pattern.Match("apps.exe"));
		Assert.True(pattern.Match("apps.ext"));
		Assert.False(pattern.Match("abcs.EXE"));
		Assert.False(pattern.Match("abcs.Ext"));
		Assert.False(pattern.Match("Apps.exe"));
		Assert.False(pattern.Match("Apps.ext"));
	}
}
