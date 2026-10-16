using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Configuration.Xml;

using Xunit;

namespace Zongsoft.Configuration.Tests;

public class XmlConfigurationInstructionTest
{
	[Fact]
	public void Read_AllLegalPositionsPreserveMetadataAndMappedConfiguration()
	{
		var xml = string.Join("\r\n", [
			"<?xml version=\"1.0\"?>",
			"<?zongsoft.tools.binding path=\"Kestrel/Endpoints\" url='Url' ?>",
			"<!-- <?ignored id='comment'?> -->",
			"<configuration>",
			"\t<?zongsoft.tools.binding id=\"root\"?>",
			"\t<option path=\"/Kestrel\">",
			"\t\t<endpoints>",
			"\t\t\t<endpoint endpoint.name=\"http\" url=\"http://*:8069\">",
			"\t\t\t\t<?zongsoft.tools.binding id=\"nested\"?>",
			"\t\t\t</endpoint>",
			"\t\t\t<?zongsoft.tools.binding id=\"sibling\"?>",
			"\t\t\t<endpoint endpoint.name=\"https\" url=\"https://*:8070\" />",
			"\t\t\t<?zongsoft.tools.binding id=\"after-empty\"?>",
			"\t\t</endpoints>",
			"\t\t<values><![CDATA[<?ignored id='cdata'?>]]></values>",
			"\t</option>",
			"</configuration>",
			"<?zongsoft.tools.binding id=\"after\"?>",
		]);
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));

		var data = XmlStreamConfigurationProvider.Read(stream, out var instructions, "app.option");

		Assert.Equal(["zongsoft.tools.binding", "zongsoft.tools.binding", "zongsoft.tools.binding", "zongsoft.tools.binding", "zongsoft.tools.binding", "zongsoft.tools.binding"], instructions.Select(instruction => instruction.Name));
		Assert.Equal([
			"path=\"Kestrel/Endpoints\" url='Url' ", "id=\"root\"", "id=\"nested\"",
			"id=\"sibling\"", "id=\"after-empty\"", "id=\"after\"",
		], instructions.Select(instruction => instruction.Data));
		Assert.Equal([
			string.Empty, "/configuration", "/configuration/option/endpoints/endpoint",
			"/configuration/option/endpoints", "/configuration/option/endpoints", string.Empty,
		], instructions.Select(instruction => instruction.Path));
		Assert.All(instructions, instruction => Assert.Equal("app.option", instruction.FilePath));
		Assert.Equal([2, 5, 9, 11, 13, 18], instructions.Select(instruction => instruction.LineNumber));
		Assert.Equal([3, 4, 7, 6, 6, 3], instructions.Select(instruction => instruction.LinePosition));
		Assert.Equal("http", data["Kestrel:endpoints:http:name"]);
		Assert.Equal("http://*:8069", data["Kestrel:endpoints:http:url"]);
		Assert.Equal("https://*:8070", data["Kestrel:endpoints:https:url"]);
		Assert.Equal("<?ignored id='cdata'?>", data["Kestrel:values:[<?ignored id='cdata'?>]"]);
		Assert.True(stream.CanRead);
	}

	[Fact]
	public void Read_QualifiedElementNamesUsePhysicalParentPaths()
	{
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes(
			"<c:configuration xmlns:c='urn:test'><c:option path='/Logical'><c:Parent><c:Child child.name='mapped'><?MiXeD data?></c:Child><?next?></c:Parent></c:option><?root?></c:configuration>"));

		var data = XmlStreamConfigurationProvider.Read(stream, out var instructions);

		Assert.Equal(["MiXeD", "next", "root"], instructions.Select(instruction => instruction.Name));
		Assert.Equal([
			"/c:configuration/c:option/c:Parent/c:Child", "/c:configuration/c:option/c:Parent", "/c:configuration",
		], instructions.Select(instruction => instruction.Path));
		Assert.Equal("mapped", data["Logical:Parent:mapped:name"]);
		Assert.All(instructions, instruction => Assert.Equal(string.Empty, instruction.FilePath));
	}

	[Theory]
	[InlineData("configuration")]
	[InlineData("options")]
	public void Read_EmptyRootStillCollectsFollowingInstructions(string root)
	{
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes($"<?before?><{root} /><?after?>"));

		var data = XmlStreamConfigurationProvider.Read(stream, out var instructions);

		Assert.Empty(data);
		Assert.Equal(["before", "after"], instructions.Select(instruction => instruction.Name));
		Assert.All(instructions, instruction => Assert.Equal(string.Empty, instruction.Path));
	}

	[Fact]
	public void Read_DataUsesXmlReaderWhitespaceAndNewlineNormalization()
	{
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes("<?custom \t value='a'\r\nnext='b'\r third='c' ?><configuration />"));

		XmlStreamConfigurationProvider.Read(stream, out var instructions);

		var instruction = Assert.Single(instructions);
		Assert.Equal("value='a'\nnext='b'\n third='c' ", instruction.Data);
		Assert.Equal(1, instruction.LineNumber);
		Assert.Equal(3, instruction.LinePosition);
		Assert.Equal("b", instruction.GetAttributes()["NEXT"]);
	}

	[Theory]
	[InlineData("<items></items>", "<items><?custom anything?></items>", "Example:items:[]", "")]
	[InlineData("<items value='present'></items>", "<items value='present'><?custom?></items>", "Example:items:value", "present")]
	[InlineData("<items>alpha</items>", "<items><?custom?>alpha<?custom?></items>", "Example:items:[alpha]", "alpha")]
	[InlineData("<items><child /></items>", "<items><child /><?custom?></items>", "Example:items:child:[]", "")]
	public void Read_InstructionsDoNotChangeEmptyElementOrTextMapping(string original, string annotated, string key, string value)
	{
		using var originalStream = new MemoryStream(Encoding.UTF8.GetBytes($"<configuration><option path='/Example'>{original}</option></configuration>"));
		using var annotatedStream = new MemoryStream(Encoding.UTF8.GetBytes($"<configuration><option path='/Example'>{annotated}</option></configuration>"));
		var expected = XmlStreamConfigurationProvider.Read(originalStream);

		var actual = XmlStreamConfigurationProvider.Read(annotatedStream, out var instructions);

		Assert.Equal(value, actual[key]);
		Assert.Equal(expected.OrderBy(entry => entry.Key), actual.OrderBy(entry => entry.Key));
		Assert.NotEmpty(instructions);
		Assert.True(originalStream.CanRead);
		Assert.True(annotatedStream.CanRead);
	}

	[Fact]
	public void Attributes_XmlSyntaxIsParsedWithoutChangingData()
	{
		const string DATA = "Path=\"Kestrel/Endpoints\" url='Url' empty=\"\" amp=\"a&amp;b\" once=\"&amp;amp;\" file=\"C:\\configs\\common.option\" quote=\"&quot;&apos;\" numeric=\"&#65;&#x42;\" whitespace=\"a\tb\nc\" escaped=\"a&#x9;b&#xA;c\" \t";
		var instruction = ReadInstruction(DATA);

		var attributes = instruction.GetAttributes();

		Assert.Equal(10, attributes.Count);
		Assert.Equal("Kestrel/Endpoints", attributes["PATH"]);
		Assert.Equal("Url", attributes["URL"]);
		Assert.Equal(string.Empty, attributes["empty"]);
		Assert.Equal("a&b", attributes["amp"]);
		Assert.Equal("&amp;", attributes["once"]);
		Assert.Equal("C:\\configs\\common.option", attributes["FILE"]);
		Assert.Equal("\"'", attributes["quote"]);
		Assert.Equal("AB", attributes["numeric"]);
		Assert.Equal("a b c", attributes["whitespace"]);
		Assert.Equal("a\tb\nc", attributes["escaped"]);
		Assert.True(instruction.TryGetAttributes(out var cached));
		Assert.Same(attributes, cached);
		Assert.Equal(DATA, instruction.Data);
	}

	[Theory]
	[InlineData("")]
	[InlineData(" \t\n")]
	public void Attributes_EmptyDataReturnsEmptyDictionary(string data)
	{
		var instruction = ReadInstruction(data);

		Assert.True(instruction.TryGetAttributes(out var attributes));
		Assert.Empty(attributes);
		Assert.Same(attributes, instruction.GetAttributes());
	}

	[Theory]
	[InlineData("some free-form text")]
	[InlineData("path=Kestrel/Endpoints")]
	[InlineData("path='a' path='b'")]
	[InlineData("Path='a' pATH='b'")]
	[InlineData("path='a' trailing")]
	[InlineData("path='a'next='b'")]
	[InlineData("path='unterminated")]
	[InlineData("value='&unknown;'")]
	[InlineData("value='a<b'")]
	[InlineData("value='ok'/><other key='injected'")]
	[InlineData("value='ok'/><!-- injected -->")]
	[InlineData("value='ok'><!-- injected --><other")]
	[InlineData("value='ok'><other /></instruction><instruction")]
	[InlineData("value='ok' / ")]
	public void Attributes_InvalidSyntaxReturnsNoPartialResult(string data)
	{
		var instruction = ReadInstruction(data);

		Assert.False(instruction.TryGetAttributes(out var attributes));
		Assert.Null(attributes);
		Assert.Throws<FormatException>(() => instruction.GetAttributes());
		Assert.False(instruction.TryGetAttributes(out attributes));
		Assert.Null(attributes);
		Assert.Equal(data, instruction.Data);
		Assert.Equal("custom", instruction.Name);
	}

	[Fact]
	public async Task Attributes_ConcurrentReadsShareImmutableResult()
	{
		var instruction = ReadInstruction("Path='Kestrel/Endpoints' url='Url'");

		var results = await Task.WhenAll(Enumerable.Range(0, 32).Select(_ => Task.Run(instruction.GetAttributes)));

		var attributes = results[0];
		Assert.All(results, result =>
		{
			Assert.Same(attributes, result);
			Assert.Equal("Kestrel/Endpoints", result["path"]);
			Assert.Equal("Url", result["URL"]);
		});

		if(attributes is IDictionary<string, string> dictionary)
		{
			Assert.True(dictionary.IsReadOnly);
			Assert.Throws<NotSupportedException>(() => dictionary.Add("injected", "value"));
			Assert.Throws<NotSupportedException>(() => dictionary["Path"] = "changed");
		}

		Assert.Equal(2, instruction.GetAttributes().Count);
		Assert.Equal("Kestrel/Endpoints", instruction.GetAttributes()["PATH"]);
	}

	private static XmlConfigurationInstruction ReadInstruction(string data)
	{
		using var stream = new MemoryStream(Encoding.UTF8.GetBytes($"<?custom {data}?><configuration />"));
		var configuration = XmlStreamConfigurationProvider.Read(stream, out var instructions);
		Assert.Empty(configuration);
		return Assert.Single(instructions);
	}
}
