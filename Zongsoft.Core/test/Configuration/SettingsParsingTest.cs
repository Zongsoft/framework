using System;

using Xunit;

namespace Zongsoft.Configuration.Tests;

public class SettingsParsingTest
{
	[Theory]
	[InlineData("=")]
	[InlineData("Good=1;=")]
	[InlineData("Password==")]
	[InlineData("Password==abc==")]
	[InlineData("Password='")]
	[InlineData("Password='abc")]
	[InlineData("Password='abc\\")]
	[InlineData("Password='abc\\'")]
	[InlineData("Password='abc'x")]
	[InlineData("Password='abc'x ")]
	[InlineData("Password=\"")]
	[InlineData("Password=\"abc")]
	[InlineData("Password=\"abc\\")]
	[InlineData("Password=\"abc\\\"")]
	[InlineData("Password=\"abc\"x")]
	[InlineData("Good=1;Password='abc")]
	public void InvalidText_IsRejectedByEveryTextEntrypoint(string text)
	{
		Assert.False(Settings.TryParse(text, out var unnamed));
		Assert.Null(unnamed);
		Assert.False(Settings.TryParse("profile", text, out var named));
		Assert.Null(named);

		Assert.Throws<ArgumentException>(() => Settings.Parse(text));
		Assert.Throws<ArgumentException>(() => Settings.Parse("profile", text));
		Assert.Throws<ArgumentException>(() => new Settings(text));
		Assert.Throws<ArgumentException>(() => new Settings("profile", text));
		Assert.Throws<ArgumentException>(() => new ConnectionSettings(text));
	}

	[Theory]
	[InlineData(false, "Password==")]
	[InlineData(true, "Password==")]
	[InlineData(false, "Password='abc")]
	[InlineData(true, "Password='abc")]
	[InlineData(false, "Password='abc\\")]
	[InlineData(true, "Password='abc\\")]
	[InlineData(false, "Password='abc'x")]
	[InlineData(true, "Password='abc'x")]
	public void InvalidValueReplacement_PreservesThePreviousState(bool connection, string text)
	{
		Settings settings = connection ? new ConnectionSettings("Password=original;Port=1234") : new Settings("Password=original;Port=1234");
		settings["Added"] = "retained";
		var original = settings.Value;

		Assert.Throws<ArgumentException>(() => settings.Value = "New=discarded;" + text);

		Assert.Equal(original, settings.Value);
		Assert.Equal(3, settings.Count);
		Assert.Equal("original", settings["Password"]);
		Assert.Equal("1234", settings["Port"]);
		Assert.Equal("retained", settings["Added"]);
		Assert.False(settings.ContainsKey("New"));
	}

	[Theory]
	[InlineData("Password", null)]
	[InlineData("Password=", null)]
	[InlineData("Password=   ", null)]
	[InlineData("Password=''", "")]
	[InlineData("Password=\"\"", "")]
	[InlineData("Password='=abc=='", "=abc==")]
	[InlineData("Password=\"=abc==\"", "=abc==")]
	[InlineData("Password=a=b", "a=b")]
	[InlineData("Password='abc\\\\'", "abc\\")]
	[InlineData("Password='abc\\''", "abc'")]
	[InlineData("Password=\"abc\\\"\"", "abc\"")]
	[InlineData("Password='abc'; ", "abc")]
	[InlineData("Password='a\\tb\\r\\nc'", "a\tb\r\nc")]
	public void CompleteValues_AreAcceptedAndRoundTrip(string text, string expected)
	{
		Assert.True(Settings.TryParse(text, out var settings));
		Assert.Single(settings);
		Assert.True(settings.ContainsKey("Password"));
		Assert.Equal(expected, settings["Password"]);
		Assert.Equal(expected, new ConnectionSettings(text)["Password"]);

		//强制序列化，避免只验证解析器返回的原文缓存
		ISettings copy = new Settings();
		copy.Add("Password", expected);

		Assert.True(Settings.TryParse(copy.Value, out var restored));
		Assert.Single(restored);
		Assert.True(restored.ContainsKey("Password"));
		Assert.Equal(expected ?? string.Empty, restored["Password"] ?? string.Empty);
	}
}
