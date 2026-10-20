using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Schema;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Plugins.Tests;

public class PluginSchemaTests
{
	private static readonly XmlSchemaSet _schemas = CreateSchemas();

	[Theory]
	[InlineData("Main.plugin")]
	[InlineData("Terminal.plugin")]
	public void ShippedManifestsValidateWithoutWarnings(string name)
	{
		Assert.Empty(Validate(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name))));
	}

	[Theory]
	[InlineData("")]
	[InlineData("<manifest><assemblies><assembly name=\"Zongsoft.Plugins\" /></assemblies></manifest>")]
	[InlineData("<manifest><assemblies /><dependencies /></manifest>")]
	[InlineData("<builders><builder name=\"fixture\" type=\"Zongsoft.Plugins.Builders.ObjectBuilder, Zongsoft.Plugins\" /></builders>")]
	[InlineData("<parsers><parser name=\"fixture\" type=\"Zongsoft.Plugins.Parsers.PluginPathParser, Zongsoft.Plugins\" /></parsers>")]
	public async Task PluginsWithoutExtensionsValidateAndLoad(string content)
	{
		var xml = $"<plugin name=\"Feature\">{content}</plugin>";
		Assert.Empty(Validate(xml));

		await using var scope = new PluginTestScope(false);
		scope.WriteFile("Feature.plugin", xml);
		scope.Tree.Load();

		Assert.Equal(PluginStatus.Loaded, Assert.Single(scope.Tree.Plugins).Status);
	}

	[Fact]
	public async Task OrderedSectionsValidateAndLoad()
	{
		var xml = """
			<plugin name="Feature">
				<manifest><dependencies><dependency name="Main" /></dependencies></manifest>
				<builders><builder name="fixture" type="Zongsoft.Plugins.Builders.ObjectBuilder, Zongsoft.Plugins" /></builders>
				<parsers><parser name="fixture" type="Zongsoft.Plugins.Parsers.PluginPathParser, Zongsoft.Plugins" /></parsers>
				<extension path="/Items"><object name="Value" type="System.Object" /></extension>
				<extension path="/Items"><object name="Other" type="System.Object" /></extension>
			</plugin>
			""";
		Assert.Empty(Validate(xml));

		await using var scope = new PluginTestScope();
		scope.WriteFile("Feature.plugin", xml);
		scope.Tree.Load();

		Assert.IsType<Builders.ObjectBuilder>(scope.Tree.Plugins["Feature"].GetBuilder("fixture"));
		Assert.IsType<Parsers.PluginPathParser>(scope.Tree.Plugins["Feature"].GetParser("fixture"));
		Assert.IsType<object>(scope.Builtin("/Items/Value").GetValue(ObtainMode.Auto));
		Assert.IsType<object>(scope.Builtin("/Items/Other").GetValue(ObtainMode.Auto));
	}

	[Theory]
	[InlineData("", true)]
	[InlineData("M", true)]
	[InlineData("B", true)]
	[InlineData("P", true)]
	[InlineData("BP", true)]
	[InlineData("MBPEEE", true)]
	[InlineData("BPMBP", false)]
	[InlineData("PBMEBPEP", false)]
	[InlineData("BEP", false)]
	[InlineData("MEE", true)]
	[InlineData("E", true)]
	[InlineData("MM", false)]
	[InlineData("MBPM", false)]
	[InlineData("EM", false)]
	[InlineData("MBEM", false)]
	[InlineData("EMEE", false)]
	[InlineData("EBPM", false)]
	[InlineData("BB", false)]
	[InlineData("PP", false)]
	[InlineData("BEB", false)]
	[InlineData("PEP", false)]
	public void TopLevelSectionsRespectMultiplicityAndOrder(string order, bool valid)
	{
		var issues = Validate(CreatePlugin(order));

		if(valid)
			Assert.Empty(issues);
		else
			Assert.Contains(issues, issue => issue.Severity == XmlSeverityType.Error);
	}

	[Fact]
	public void AllShortSectionCombinationsMatchTheTopLevelContract()
	{
		foreach(var order in GetSectionOrders(string.Empty, 5))
		{
			var valid = order.Count(element => element == 'M') <= 1 &&
				order.Count(element => element == 'B') <= 1 &&
				order.Count(element => element == 'P') <= 1 &&
				order.SequenceEqual(order.OrderBy(element => "MBPE".IndexOf(element)));

			Assert.True(valid == (Validate(CreatePlugin(order)).Count == 0), $"Unexpected schema result for section order '{order}'.");
		}
	}

	[Theory]
	[InlineData("builders", "builder", 0, false)]
	[InlineData("builders", "builder", 1, true)]
	[InlineData("builders", "builder", 2, true)]
	[InlineData("parsers", "parser", 0, false)]
	[InlineData("parsers", "parser", 1, true)]
	[InlineData("parsers", "parser", 2, true)]
	public void FoundationSectionsRequireAtLeastOneRegistration(string section, string element, int count, bool valid)
	{
		var registrations = string.Concat(Enumerable.Range(0, count).Select(index => $"<{element} name=\"fixture{index}\" type=\"System.Object\" />"));
		var issues = Validate($"<plugin name=\"Feature\"><{section}>{registrations}</{section}></plugin>");

		if(valid)
			Assert.Empty(issues);
		else
			Assert.Contains(issues, issue => issue.Severity == XmlSeverityType.Error);
	}

	[Theory]
	[InlineData("builders", "builder")]
	[InlineData("parsers", "parser")]
	public void FoundationSectionCannotRepeatEvenWithDifferentRegistrationNames(string section, string element)
	{
		var xml = $"<plugin name=\"Feature\"><{section}><{element} name=\"first\" type=\"System.Object\" /></{section}><{section}><{element} name=\"second\" type=\"System.Object\" /></{section}></plugin>";

		Assert.Contains(Validate(xml), issue => issue.Severity == XmlSeverityType.Error);
	}

	[Fact]
	public async Task NestedBuiltinsAndDynamicAttributesValidateAndBuild()
	{
		await using var scope = new PluginTestScope();
		scope.WritePlugin($$"""
			<extension path="/Items">
				<object name="Owner" type="{{typeof(CollectionOwner).AssemblyQualifiedName}}" Label="fixture">
					<expose name="Items" value="{path:../@Items}">
						<object name="Child" type="System.Object" position="^" />
					</expose>
				</object>
			</extension>
			""");
		Assert.Empty(Validate(File.ReadAllText(Path.Combine(scope.DirectoryPath, "plugins", "Feature.plugin"))));
		scope.Tree.Load();

		var owner = Assert.IsType<CollectionOwner>(scope.Builtin("/Items/Owner").GetValue(ObtainMode.Auto));
		Assert.Equal("fixture", owner.Label);
		Assert.Same(scope.Builtin("/Items/Owner/Items/Child").Value, Assert.Single(owner.Items));
	}

	[Theory]
	[InlineData("object", "<parameter name=\"count\" value=\"17\" />")]
	[InlineData("object", "<parameter type=\"System.Int32\" value=\"17\" />")]
	[InlineData("object", "<parameter name=\"count\" value=\"1\">17</parameter>")]
	[InlineData("expose", "<parameter name=\"count\" value=\"17\" />")]
	[InlineData("expose", "<parameter type=\"System.Int32\" value=\"17\" />")]
	[InlineData("expose", "<parameter name=\"count\" value=\"1\">17</parameter>")]
	public async Task ConstructorParametersCanBeNamedTypedOrTextual(string scheme, string parameter)
	{
		await using var scope = new PluginTestScope();
		scope.WritePlugin($"<extension path=\"/Items\"><{scheme} name=\"Value\" type=\"{typeof(ObjectBuilderTests.ConstructorObject).AssemblyQualifiedName}\"><{scheme}.constructor>{parameter}</{scheme}.constructor></{scheme}></extension>");
		Assert.Empty(Validate(File.ReadAllText(Path.Combine(scope.DirectoryPath, "plugins", "Feature.plugin"))));
		scope.Tree.Load();

		Assert.Equal(17, Assert.IsType<ObjectBuilderTests.ConstructorObject>(scope.Builtin("/Items/Value").GetValue(ObtainMode.Auto)).Count);
	}

	[Fact]
	public async Task ConstructorAllowsSeveralUnnamedParameters()
	{
		await using var scope = new PluginTestScope();
		scope.WritePlugin($"<extension path=\"/Items\"><object name=\"Value\" type=\"{typeof(Pair).AssemblyQualifiedName}\"><object.constructor><parameter type=\"System.Int32\" value=\"17\" /><parameter type=\"System.String\">fixture</parameter></object.constructor></object></extension>");
		Assert.Empty(Validate(File.ReadAllText(Path.Combine(scope.DirectoryPath, "plugins", "Feature.plugin"))));
		scope.Tree.Load();

		var value = Assert.IsType<Pair>(scope.Builtin("/Items/Value").GetValue(ObtainMode.Auto));
		Assert.Equal(17, value.Number);
		Assert.Equal("fixture", value.Text);
	}

	[Fact]
	public async Task EmptyConstructorCanFollowChildDeclarations()
	{
		await using var scope = new PluginTestScope();
		scope.WritePlugin("<extension path=\"/Items\"><object name=\"Values\" type=\"System.Collections.ArrayList\"><object type=\"System.Object\" /><object.constructor /></object></extension>");
		Assert.Empty(Validate(File.ReadAllText(Path.Combine(scope.DirectoryPath, "plugins", "Feature.plugin"))));
		scope.Tree.Load();

		Assert.Single(Assert.IsType<System.Collections.ArrayList>(scope.Builtin("/Items/Values").GetValue(ObtainMode.Auto)));
	}

	[Fact]
	public async Task DynamicMemberNamesProduceOnlySchemaAvailabilityWarnings()
	{
		await using var scope = new PluginTestScope();
		scope.WritePlugin($"<extension path=\"/Items\"><object name=\"Owner\" type=\"{typeof(ObjectBuilderTests.ComplexObject).AssemblyQualifiedName}\"><Owner.Child><object type=\"{typeof(ObjectBuilderTests.SimpleObject).AssemblyQualifiedName}\" Number=\"7\" /></Owner.Child></object></extension>");
		var xml = File.ReadAllText(Path.Combine(scope.DirectoryPath, "plugins", "Feature.plugin"));
		var warnings = Validate(xml);
		Assert.NotEmpty(warnings);
		Assert.All(warnings, warning => Assert.Equal(XmlSeverityType.Warning, warning.Severity));
		Assert.Empty(Validate(xml, false));
		scope.Tree.Load();

		Assert.Equal(7, Assert.IsType<ObjectBuilderTests.ComplexObject>(scope.Builtin("/Items/Owner").GetValue(ObtainMode.Auto)).Child.Number);
	}

	[Fact]
	public async Task CustomBuildersCanSupplyTheirOwnElementSchema()
	{
		await using var scope = new PluginTestScope();
		scope.WritePlugin("<builders><builder name=\"fixture\" type=\"Zongsoft.Plugins.Builders.ObjectBuilder, Zongsoft.Plugins\" /></builders><extension path=\"/Items\"><fixture name=\"Value\" type=\"System.Object\" /></extension>");
		var xml = File.ReadAllText(Path.Combine(scope.DirectoryPath, "plugins", "Feature.plugin"));
		var schema = "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\"><xs:element name=\"fixture\" type=\"BuiltinElementType\" /></xs:schema>";
		Assert.Empty(Validate(xml, true, schema));
		scope.Tree.Load();

		Assert.IsType<object>(scope.Builtin("/Items/Value").GetValue(ObtainMode.Auto));
	}

	[Fact]
	public async Task VersionComponentsDoNotHaveArbitraryDigitLimits()
	{
		var xml = "<plugin name=\"Feature\" version=\"10000.20000.300000.400000\" />";
		Assert.Empty(Validate(xml));

		await using var scope = new PluginTestScope(false);
		scope.WriteFile("Feature.plugin", xml);
		scope.Tree.Load();

		Assert.Equal(new Version(10000, 20000, 300000, 400000), Assert.Single(scope.Tree.Plugins).Manifest.Version);
	}

	[Theory]
	[InlineData("<plugin />")]
	[InlineData("<plugin name=\"Feature\"><unexpected /></plugin>")]
	[InlineData("<plugin name=\"Feature\"><manifest><unexpected /></manifest></plugin>")]
	[InlineData("<plugin name=\"Feature\"><builders><builder name=\"fixture\" /></builders></plugin>")]
	[InlineData("<plugin name=\"Feature\"><parsers><parser type=\"System.Object\" /></parsers></plugin>")]
	[InlineData("<plugin name=\"Feature\"><extension><object type=\"System.Object\" /></extension></plugin>")]
	[InlineData("<plugin name=\"Feature\"><builders><builder name=\"same\" type=\"System.Object\" /><builder name=\"same\" type=\"System.Object\" /></builders></plugin>")]
	[InlineData("<plugin name=\"Feature\"><extension path=\"/Items\"><object><object><object.constructor><unexpected /></object.constructor></object></object></extension></plugin>")]
	[InlineData("<plugin name=\"Feature\"><extension path=\"/Items\"><expose><expose.constructor><parameter><object /></parameter></expose.constructor></expose></extension></plugin>")]
	[InlineData("<plugin name=\"Feature\"><extension path=\"/Items\"><object><object.property><object /></object.property></object></extension></plugin>")]
	public void InvalidKnownStructuresStillProduceErrors(string xml)
	{
		Assert.Contains(Validate(xml), issue => issue.Severity == XmlSeverityType.Error);
	}

	private static string CreatePlugin(string order)
	{
		var content = string.Concat(order.Select((element, index) => element switch
		{
			'M' => "<manifest />",
			'B' => $"<builders><builder name=\"fixture{index}\" type=\"System.Object\" /></builders>",
			'P' => $"<parsers><parser name=\"fixture{index}\" type=\"System.Object\" /></parsers>",
			'E' => "<extension path=\"/Items\" />",
			_ => throw new ArgumentOutOfRangeException(nameof(order)),
		}));

		return $"<plugin name=\"Feature\">{content}</plugin>";
	}

	private static IEnumerable<string> GetSectionOrders(string prefix, int remaining)
	{
		yield return prefix;

		if(remaining > 0)
		{
			foreach(var element in "MBPE")
			{
				foreach(var order in GetSectionOrders(prefix + element, remaining - 1))
					yield return order;
			}
		}
	}

	private static XmlSchemaSet CreateSchemas(string additionalSchema = null)
	{
		var schemas = new XmlSchemaSet { XmlResolver = null };
		schemas.Add(null, Path.Combine(AppContext.BaseDirectory, "Fixtures", "Zongsoft.Plugins.xsd"));

		if(additionalSchema != null)
		{
			using var input = new StringReader(additionalSchema);
			using var schemaReader = XmlReader.Create(input);
			schemas.Add(null, schemaReader);
		}

		schemas.Compile();
		return schemas;
	}

	private static List<ValidationEventArgs> Validate(string xml, bool warnings = true, string additionalSchema = null)
	{
		var settings = new XmlReaderSettings
		{
			Schemas = additionalSchema == null ? _schemas : CreateSchemas(additionalSchema),
			XmlResolver = null,
			DtdProcessing = DtdProcessing.Prohibit,
			ValidationType = ValidationType.Schema,
			ValidationFlags = XmlSchemaValidationFlags.ProcessIdentityConstraints,
		};

		if(warnings)
			settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;

		var issues = new List<ValidationEventArgs>();
		settings.ValidationEventHandler += (_, args) => issues.Add(args);
		using var text = new StringReader(xml);
		using var reader = XmlReader.Create(text, settings);

		while(reader.Read())
			;

		return issues;
	}

	public sealed class CollectionOwner
	{
		public string Label { get; set; }
		public List<object> Items { get; } = new();
	}

	public sealed class Pair(int number, string text)
	{
		public int Number { get; } = number;
		public string Text { get; } = text;
	}
}
