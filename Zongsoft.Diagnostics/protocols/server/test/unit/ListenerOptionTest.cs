using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

using Xunit;

namespace Zongsoft.Diagnostics.Protocols.Server.Tests;

public class ListenerOptionTest
{
	[Fact]
	public void FixtureBindingsDescribeActualListener()
	{
		var document = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "fixtures", "Zongsoft.Diagnostics.Protocols.Server.option"));
		var instruction = Assert.Single(document.DescendantNodes().OfType<XProcessingInstruction>(), node => node.Target == "zongsoft.tools.binding");
		var binding = XElement.Parse($"<binding {instruction.Data}/>");
		Assert.Equal("Kestrel/Endpoints/telemetry", (string)binding.Attribute("path"));
		Assert.Equal("${this.Url}", (string)binding.Attribute("template"));
		Assert.Equal("${this.Protocols}", (string)binding.Attribute("protocols"));
		Assert.Equal("true", (string)binding.Attribute("required"));
		var endpoint = Assert.Single(document.Descendants("telemetry"));
		Assert.Equal("http://*:4317/opentelemetry", (string)endpoint.Attribute("url"));
		Assert.Equal("grpc+http2", (string)endpoint.Attribute("protocols"));
	}
}
