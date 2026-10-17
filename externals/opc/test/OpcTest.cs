using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Xunit;

namespace Zongsoft.Externals.Opc.Tests;

public class OpcTest
{
	[Fact]
	public void OpcServer_Urls_UsesAllConfiguredAddresses()
	{
		var server = new OpcServer
		{
			Urls = "opc.tcp://127.0.0.1:4840/ua/server, opc.tcp://[::1]:4841/ua/other",
		};

		Assert.Equal(["opc.tcp://127.0.0.1:4840/ua/server", "opc.tcp://[::1]:4841/ua/other"], server.Options.Urls);
	}

	[Fact]
	public void OpcServer_Discovery_RemainsSeparateFromListenerUrls()
	{
		var server = new OpcServer
		{
			Urls = "opc.tcp://127.0.0.1:4840/ua/server",
			Discovery = "opc.tcp://registry.example:4840/registration",
		};

		Assert.Equal("opc.tcp://registry.example:4840/registration", server.Options.Discovery);
		Assert.Equal(["opc.tcp://127.0.0.1:4840/ua/server"], server.Options.Urls);
	}

	[Theory]
	[InlineData("")]
	[InlineData("opc.tcp://localhost/ua")]
	[InlineData("https://localhost:4840/ua")]
	[InlineData("opc.tcp://localhost:4840/ua,,opc.tcp://localhost:4841/ua")]
	public void OpcServer_Urls_RejectsMissingInvalidOrEmptyAddresses(string urls)
	{
		var server = new OpcServer();

		Assert.Throws<ArgumentException>(() => server.Urls = urls);
	}

	[Fact]
	public void OpcDaemonArtifacts_UseTheSameEnabledUrlAndDiscoveryOptions()
	{
		var plugin = LoadXml("Zongsoft.Externals.Opc-daemon.plugin");
		var option = LoadXml("Zongsoft.Externals.Opc-daemon.option");
		var server = plugin.Descendants("object").Single(element => (string)element.Attribute("name") == "Server");
		var binding = option.Nodes().OfType<XProcessingInstruction>().Single(instruction => instruction.Target == "zongsoft.tools.binding");

		Assert.Equal("{option:/Externals/Opc/Server/Enabled}", (string)server.Attribute("Enabled"));
		Assert.Equal("{option:/Externals/Opc/Server/Urls}", (string)server.Attribute("Urls"));
		Assert.Equal("{option:/Externals/Opc/Server/Discovery}", (string)server.Attribute("Discovery"));
		Assert.Contains("path=\"Externals/Opc/Server\"", binding.Data);
		Assert.Contains("filter=\"enabled:true\"", binding.Data);
		Assert.Contains("template=\"${this.urls}\"", binding.Data);
		Assert.Contains("protocols=\"opc.tcp\"", binding.Data);
		Assert.Contains("required=\"true\"", binding.Data);
		Assert.Contains(plugin.Descendants("object").Where(element => element.Attribute("value") != null), element => (string)element.Attribute("value") == "{path:/Workbench/Externals/Opc/Server}");
	}

	private static XDocument LoadXml(string fileName) => XDocument.Load(Path.Combine(AppContext.BaseDirectory, fileName), LoadOptions.PreserveWhitespace);
}
