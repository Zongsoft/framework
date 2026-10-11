using System;
using System.IO;

using Xunit;

using Microsoft.Extensions.Configuration;

namespace Zongsoft.Plugins.Tests;

public class HostingConfigurationTests
{
	[Theory]
	[InlineData("HostFixture.option", "base")]
	[InlineData("HostFixture.development.option", "environment")]
	[InlineData("HostFixture.blue.option", "host")]
	[InlineData("HostFixture.blue.development.option", "host-environment")]
	[InlineData("HostFixture.alpha.option", "site")]
	[InlineData("HostFixture.alpha.development.option", "site-environment")]
	public void HostOptionLayersOverrideEarlierSources(string lastFile, string expected)
	{
		using var scope = new PluginTestScope();
		var builder = new Hosting.ApplicationBuilder(typeof(HostingConfigurationTests).Assembly.GetName().Name, [$"--contentRoot={scope.DirectoryPath}", "--environment=Development"]);
		using var configuration = builder.Configuration;

		builder.Environment.EnvironmentName = "Development";
		configuration.SetBasePath(scope.DirectoryPath);
		configuration["host"] = "Blue";
		configuration["site"] = "Alpha";

		var layers = new (string File, string Value)[]
		{
			("HostFixture.option", "base"),
			("HostFixture.development.option", "environment"),
			("HostFixture.blue.option", "host"),
			("HostFixture.blue.development.option", "host-environment"),
			("HostFixture.alpha.option", "site"),
			("HostFixture.alpha.development.option", "site-environment"),
		};

		foreach(var layer in layers)
		{
			File.WriteAllText(Path.Combine(scope.DirectoryPath, layer.File), $"<options><option path=\"/Fixture\"><settings value=\"{layer.Value}\" /></option></options>");
			if(layer.File == lastFile)
				break;
		}

		builder.LoadConfiguration("HostFixture");

		Assert.Equal(expected, configuration["Fixture:settings:value"]);
		Assert.Equal("Development", builder.Environment.EnvironmentName);
	}
}
