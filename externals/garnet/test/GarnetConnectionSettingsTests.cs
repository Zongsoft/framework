/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.com>
 *
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Externals.Garnet library.
 *
 * The Zongsoft.Externals.Garnet is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Garnet is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Garnet library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

using Xunit;
using global::Garnet.server.Auth.Settings;

using Zongsoft.Externals.Garnet.Configuration;

namespace Zongsoft.Externals.Garnet.Tests;

public class GarnetConnectionSettingsTests
{
	[Fact]
	public void DriverDescribesRequiredAddressWithoutSupplyingDefaults()
	{
		var driver = GarnetConnectionSettingsDriver.Instance;
		var settings = driver.GetSettings("cache", null);

		Assert.Equal("Garnet", driver.Name);
		Assert.IsType<GarnetConnectionSettings>(settings);
		Assert.Same(driver, settings.Driver);
		Assert.Equal("cache", settings.Name);

		var address = driver.Descriptors[nameof(GarnetConnectionSettings.Address)];
		Assert.True(address.Required);
		Assert.Equal(typeof(string), address.Type);
		Assert.False(address.HasDefaultValue(out _));

		Assert.Equal(typeof(GarnetAuthenticationMode?), driver.Descriptors[nameof(GarnetConnectionSettings.AuthenticationMode)].Type);
		Assert.Equal(typeof(bool?), driver.Descriptors[nameof(GarnetConnectionSettings.EnableTLS)].Type);
		Assert.Equal(typeof(TimeSpan?), driver.Descriptors[nameof(GarnetConnectionSettings.CertificateRefreshFrequency)].Type);
		Assert.Equal(typeof(TimeSpan?), driver.Descriptors[nameof(GarnetConnectionSettings.CommitFrequency)].Type);
		Assert.All(driver.Descriptors, descriptor => Assert.False(descriptor.HasDefaultValue(out _)));

		Assert.Null(settings.Address);
		Assert.Null(settings.AuthenticationMode);
		Assert.Null(settings.Password);
		Assert.Null(settings.EnableTLS);
		Assert.Null(settings.CertificateFileName);
		Assert.Null(settings.CertificatePassword);
		Assert.Null(settings.ClientCertificateRequired);
		Assert.Null(settings.IssuerCertificatePath);
		Assert.Null(settings.CertificateRefreshFrequency);
		Assert.Null(settings.EnableAOF);
		Assert.Null(settings.CommitFrequency);
		Assert.Null(settings.EnableLua);
		Assert.Null(settings.Recover);
		Assert.Null(settings.CheckpointDirectory);
		Assert.Null(settings.QuietMode);
		Assert.Empty(settings);
	}

	[Fact]
	public void NativeOptionsPopulateTypedPropertiesAndPreserveExtensions()
	{
		var settings = GarnetConnectionSettingsDriver.Instance.GetSettings("cache",
			"address=[::1]:6380;auth=Password;password=test-value;tls=true;cert-file-name=~/server.pfx;" +
			"cert-password=test-certificate;client-certificate-required=false;issuer-certificate-path=~/issuer.crt;" +
			"cert-refresh-freq=45;aof=true;aof-commit-freq=1500;lua=false;recover=true;checkpointdir=~/checkpoints;q=false;" +
			"memory=32m;custom-option=custom-value");

		Assert.Equal("[::1]:6380", settings.Address);
		Assert.Equal(GarnetAuthenticationMode.Password, settings.AuthenticationMode);
		Assert.Equal("test-value", settings.Password);
		Assert.True(settings.EnableTLS);
		Assert.Equal("~/server.pfx", settings.CertificateFileName);
		Assert.Equal("test-certificate", settings.CertificatePassword);
		Assert.False(settings.ClientCertificateRequired);
		Assert.Equal("~/issuer.crt", settings.IssuerCertificatePath);
		Assert.Equal(TimeSpan.FromSeconds(45), settings.CertificateRefreshFrequency);
		Assert.True(settings.EnableAOF);
		Assert.Equal(TimeSpan.FromMilliseconds(1500), settings.CommitFrequency);
		Assert.False(settings.EnableLua);
		Assert.True(settings.Recover);
		Assert.Equal("~/checkpoints", settings.CheckpointDirectory);
		Assert.False(settings.QuietMode);

		Assert.Equal("32m", settings["LogMemorySize"]);
		Assert.Equal("custom-value", settings["custom-option"]);
		Assert.Equal(17, settings.Count);
	}

	[Fact]
	public void PropertyAndIndexerWritesShareAliasesAndSurviveTextRoundtrip()
	{
		var settings = GarnetConnectionSettingsDriver.Instance.GetSettings("cache", "address=127.0.0.1:6380;tls=false;memory=16m;custom-option=kept");
		settings.EnableTLS = true;
		settings.CertificateRefreshFrequency = TimeSpan.Zero;
		settings.AuthenticationMode = GarnetAuthenticationMode.Password;
		settings.CertificateFileName = "~/first.pfx";
		settings.CertificatePassword = "first-certificate";
		settings.CheckpointDirectory = "~/first-checkpoints";
		Assert.Equal("~/first.pfx", settings["cert-file-name"]);
		Assert.Equal("first-certificate", settings["cert-password"]);
		Assert.Equal("~/first-checkpoints", settings["checkpointdir"]);

		settings["CertificateFileName"] = "~/second.pfx";
		settings["CertificatePassword"] = "second-certificate";
		settings["CheckpointDirectory"] = "~/second-checkpoints";
		settings["LogMemorySize"] = "64m";

		Assert.True(settings.GetValue<bool>("tls"));
		Assert.Equal("00:00:00", settings["cert-refresh-freq"]);
		Assert.Equal("Password", settings["auth"]);
		Assert.Equal("~/second.pfx", settings["cert-file-name"]);
		Assert.Equal("second-certificate", settings["cert-password"]);
		Assert.Equal("~/second-checkpoints", settings["checkpointdir"]);
		Assert.Equal("64m", settings["memory"]);

		settings["tls"] = "false";
		settings.EnableLua = false;
		settings.CommitFrequency = TimeSpan.FromMilliseconds(-1);
		var restored = GarnetConnectionSettingsDriver.Instance.GetSettings("restored", settings.Value);

		Assert.Equal("restored", restored.Name);
		Assert.Equal("127.0.0.1:6380", restored.Address);
		Assert.False(restored.EnableTLS);
		Assert.False(restored.EnableLua);
		Assert.Equal(TimeSpan.FromMilliseconds(-1), restored.CommitFrequency);
		Assert.Equal(TimeSpan.Zero, restored.CertificateRefreshFrequency);
		Assert.Equal(GarnetAuthenticationMode.Password, restored.AuthenticationMode);
		Assert.Equal("~/second.pfx", restored.CertificateFileName);
		Assert.Equal("second-certificate", restored.CertificatePassword);
		Assert.Equal("~/second-checkpoints", restored.CheckpointDirectory);
		Assert.Equal("~/second.pfx", restored["cert-file-name"]);
		Assert.Equal("second-certificate", restored["cert-password"]);
		Assert.Equal("~/second-checkpoints", restored["checkpointdir"]);
		Assert.Equal("64m", restored["memory"]);
		Assert.Equal("kept", restored["custom-option"]);

		settings.EnableTLS = null;
		Assert.False(settings.Contains("tls"));
		Assert.Null(settings.EnableTLS);
		Assert.False(GarnetConnectionSettingsDriver.Instance.GetSettings("cleared", settings.Value).Contains("tls"));
	}

	[Theory]
	[InlineData("tls=false;EnableTLS=true;quiet=false;q=true;memory=16m;LogMemorySize=32m", true, true, "32m")]
	[InlineData("EnableTLS=true;tls=false;QuietMode=true;quiet=false;LogMemorySize=32m;memory=16m", false, false, "16m")]
	public void LastAliasValueWinsInInputOrder(string text, bool tls, bool quiet, string memory)
	{
		var settings = GarnetConnectionSettingsDriver.Instance.GetSettings("cache", text);

		Assert.Equal(tls, settings.EnableTLS);
		Assert.Equal(quiet, settings.QuietMode);
		Assert.Equal(memory, settings["memory"]);
		Assert.Equal(3, settings.Count);

		//触发重新生成 Value，防止只验证保留的原始文本。
		settings.Address = "127.0.0.1:6380";
		var restored = GarnetConnectionSettingsDriver.Instance.GetSettings("copy", settings.Value);

		Assert.Equal(tls, restored.EnableTLS);
		Assert.Equal(quiet, restored.QuietMode);
		Assert.Equal(memory, restored["LogMemorySize"]);
		Assert.Equal(4, restored.Count);
	}

	[Theory]
	[InlineData("aof-commit-freq=1500;cert-refresh-freq=45", 1500L, 45000L)]
	[InlineData("CommitFrequency=1500;CertificateRefreshFrequency=45", 1500L, 45000L)]
	[InlineData("aof-commit-freq=1500ms;cert-refresh-freq=2m", 1500L, 120000L)]
	[InlineData("CommitFrequency=1s;CertificateRefreshFrequency=30s", 1000L, 30000L)]
	[InlineData("aof-commit-freq=-1;cert-refresh-freq=0", -1L, 0L)]
	[InlineData("CommitFrequency=00:00:01.5000000;CertificateRefreshFrequency=00:02:00", 1500L, 120000L)]
	[InlineData("aof-commit-freq=2147483647;cert-refresh-freq=2147483647", 2147483647L, 2147483647000L)]
	public void DurationsUseNativeUnitsAndSupportTimeSpanText(string text, long commitMilliseconds, long refreshMilliseconds)
	{
		var settings = GarnetConnectionSettingsDriver.Instance.GetSettings("cache", text);
		var commit = TimeSpan.FromTicks(commitMilliseconds * TimeSpan.TicksPerMillisecond);
		var refresh = TimeSpan.FromTicks(refreshMilliseconds * TimeSpan.TicksPerMillisecond);
		Assert.Equal(commit, settings.CommitFrequency);
		Assert.Equal(refresh, settings.CertificateRefreshFrequency);
		Assert.Equal(commit, settings.GetValue<TimeSpan?>("aof-commit-freq"));
		Assert.Equal(refresh, settings.GetValue<TimeSpan?>("cert-refresh-freq"));

		settings.CommitFrequency = commit;
		settings.CertificateRefreshFrequency = refresh;
		Assert.Equal(commit.ToString("c"), settings["aof-commit-freq"]);
		Assert.Equal(refresh.ToString("c"), settings["cert-refresh-freq"]);
		var restored = GarnetConnectionSettingsDriver.Instance.GetSettings("copy", settings.Value);
		Assert.Equal(commit, restored.CommitFrequency);
		Assert.Equal(refresh, restored.CertificateRefreshFrequency);
		Assert.Equal(2, restored.Count);
	}

	[Theory]
	[InlineData("noauth", GarnetAuthenticationMode.NoAuth)]
	[InlineData("password", GarnetAuthenticationMode.Password)]
	[InlineData("aad", GarnetAuthenticationMode.Aad)]
	[InlineData("acl", GarnetAuthenticationMode.ACL)]
	[InlineData("aclwithaad", GarnetAuthenticationMode.AclWithAad)]
	public void AuthenticationModesSupportAliasesAndTextRoundtrip(string text, GarnetAuthenticationMode expected)
	{
		var settings = GarnetConnectionSettingsDriver.Instance.GetSettings("cache", $"auth={text}");
		Assert.Equal(expected, settings.AuthenticationMode);

		settings["AuthenticationMode"] = expected.ToString();
		Assert.Equal(expected, settings.GetValue<GarnetAuthenticationMode?>("auth"));
		Assert.Single(settings);

		settings.AuthenticationMode = expected;
		settings.Address = "127.0.0.1:6380";
		var restored = GarnetConnectionSettingsDriver.Instance.GetSettings("copy", settings.Value);
		Assert.Equal(expected, restored.AuthenticationMode);
		Assert.Equal(expected.ToString(), restored["auth"]);
		Assert.Equal(2, restored.Count);

		settings.AuthenticationMode = null;
		Assert.False(settings.Contains("auth"));
		Assert.Null(settings.AuthenticationMode);
		Assert.Null(GarnetConnectionSettingsDriver.Instance.GetSettings("cleared", settings.Value).AuthenticationMode);
	}

	[Fact]
	public void InvalidAuthenticationModeFailsTypedRead()
	{
		var settings = GarnetConnectionSettingsDriver.Instance.GetSettings("cache", "auth=not-a-mode");
		Assert.Throws<InvalidOperationException>(() => settings.AuthenticationMode);

		settings.AuthenticationMode = GarnetAuthenticationMode.NoAuth;
		Assert.Equal(GarnetAuthenticationMode.NoAuth, settings.AuthenticationMode);
		Assert.Equal("NoAuth", settings["auth"]);
	}

	[Fact]
	public void BareQuietRemainsAnExplicitFlagWithoutInjectingFalse()
	{
		var settings = GarnetConnectionSettingsDriver.Instance.GetSettings("cache", "quiet");

		Assert.True(settings.TryGetValue("q", out string value));
		Assert.Null(value);
		Assert.Null(settings.QuietMode);
		Assert.Single(settings);

		settings.Address = "127.0.0.1:6380";
		var restored = GarnetConnectionSettingsDriver.Instance.GetSettings("copy", settings.Value);

		Assert.True(restored.TryGetValue("QuietMode", out value));
		Assert.Null(value);
		Assert.Null(restored.QuietMode);
		Assert.Equal(2, restored.Count);
	}
}
