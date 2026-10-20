using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Plugins.Parsers;

namespace Zongsoft.Plugins.Tests;

public class ParserTests
{
	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData(" ")]
	public void ExpressionParserRejectsBlankInput(string text)
	{
		Assert.Null(ParserExpression.Parse(text));
		Assert.False(ParserExpression.TryParse(text, out var expression));
		Assert.Null(expression);
	}

	[Theory]
	[InlineData("}")]
	[InlineData("{{path:/Items}")]
	[InlineData("{pa|th:/Items}")]
	[InlineData("{pa\\th:/Items}")]
	public void ExpressionParserReportsInvalidTokens(string text)
	{
		Assert.Throws<ParserException>(() => ParserExpression.Parse(text));
		Assert.False(ParserExpression.TryParse(text, out var expression));
		Assert.Null(expression);
	}

	[Theory]
	[InlineData(null, false)]
	[InlineData("", false)]
	[InlineData("plain", false)]
	[InlineData("{path:}", false)]
	[InlineData("{path:/Items}", true)]
	[InlineData("prefix{static:Something}suffixed", true)]
	[InlineData("{ service : ~@Orders }", true)]
	public void CanParseRecognizesEmbeddedExpressions(string text, bool expected) => Assert.Equal(expected, Parser.CanParse(text));

	[Fact]
	public async Task PathParserResolvesNodesMembersAndFileLocations()
	{
		await using var scope = new PluginTestScope();
		scope.WritePlugin($"<extension path=\"/Items\"><object name=\"Parent\" type=\"{typeof(ObjectBuilderTests.ComplexObject).AssemblyQualifiedName}\" Text=\"fixture\"><expose name=\"Detail\" value=\"{{path:../@Detail}}\" /></object></extension>");
		scope.Tree.Load();
		var builtin = scope.Builtin("/Items/Parent");
		var value = Assert.IsType<ObjectBuilderTests.ComplexObject>(builtin.GetValue(ObtainMode.Auto));

		Assert.Same(value, Parser.Parse("{path:.}", builtin, null, null));
		Assert.Same(value.Detail, scope.Builtin("/Items/Parent/Detail").Value);
		Assert.Equal("fixture", Parser.Parse("{path:./@Text}", builtin, null, null));
		Assert.Equal("[fixture]", Parser.Parse("[{path:./@Text}]", builtin, null, null));
		Assert.Equal(builtin.Plugin.FilePath, Parser.Parse("{path:~}", builtin, null, null));
		Assert.Equal(Path.GetDirectoryName(builtin.Plugin.FilePath), Parser.Parse("{path:~/}", builtin, null, null));
		Assert.Equal(typeof(string), Parser.GetValueType("{path:~}", builtin));
		Assert.Equal(typeof(string), Parser.GetValueType("{path:./@Text}", builtin));
		Assert.Equal(typeof(int), Parser.GetValueType("{path:./@Detail.Number}", builtin));
		Assert.Throws<PluginException>(() => Parser.Parse("{path:/Missing}", builtin, null, null));
	}

	[Fact]
	public async Task PathObtainModeNeverAvoidsConstructionAndAlwaysRebuilds()
	{
		await using var scope = new PluginTestScope();
		scope.WritePlugin("<extension path=\"/Items\"><object name=\"Item\" type=\"System.Object\" /></extension>");
		scope.Tree.Load();
		var builtin = scope.Builtin("/Items/Item");

		Assert.Null(Parser.Parse("{path:.,Never}", builtin, null, null));
		Assert.False(builtin.IsBuilded);
		Assert.Equal(typeof(object), Parser.GetValueType("{path:.,Never}", builtin));
		Assert.False(builtin.IsBuilded);

		var original = Parser.Parse("{path:.,Auto}", builtin, null, null);
		var replacement = Parser.Parse("{path:.,Alway}", builtin, null, null);

		Assert.NotSame(original, replacement);
		Assert.Same(replacement, builtin.Value);
	}

	[Fact]
	public async Task StaticTypeAndObjectParsersPreserveValueTypes()
	{
		await using var scope = new PluginTestScope();
		scope.WritePlugin("<extension path=\"/Items\"><object name=\"Anchor\" type=\"System.Object\" /></extension>");
		scope.Tree.Load();
		var builtin = scope.Builtin("/Items/Anchor");
		var typeName = typeof(StaticValues).FullName;
		var assemblyName = typeof(StaticValues).Assembly.GetName().Name;

		Assert.Same(StaticValues.Value, Parser.Parse($"{{static:{typeName}.Value, {assemblyName}}}", builtin, null, null));
		Assert.Equal(31, Parser.Parse($"{{static:{typeName}.Value.Number, {assemblyName}}}", builtin, null, null));
		Assert.Null(Parser.Parse("{static:NULL}", builtin, null, null));
		Assert.Equal(typeof(int), Parser.Parse("{type:int}", builtin, null, null));
		Assert.Equal(typeof(Dictionary<string, int>), PluginUtility.GetType(typeof(Dictionary<string, int>).AssemblyQualifiedName, builtin));
		Assert.IsType<object>(Parser.Parse("{object:System.Object}", builtin, null, null));
		Assert.Throws<PluginException>(() => Parser.Parse($"{{static:{typeName}.Missing, {assemblyName}}}", builtin, null, null));
		Assert.Throws<PluginException>(() => Parser.Parse("{type:Missing.Type}", builtin, null, null));
		Assert.Throws<ArgumentException>(() => Parser.Parse("{static:MissingAssembly}", builtin, null, null));
	}

	[Fact]
	public async Task ParserDispatchFindsRegistrationsFromCustomNodesAndRejectsUnknownSchemes()
	{
		await using var scope = new PluginTestScope();
		scope.Tree.Load();
		var node = scope.Tree.EnsurePath("/Custom");

		Assert.Equal(typeof(string), Parser.Parse("{type:string}", node, null, null));
		Assert.Equal(" ", Parser.Parse(" ", node, null, null));
		Assert.Null(Parser.Parse(null, node, null, null));
		Assert.Throws<PluginException>(() => Parser.Parse("plain", node, null, null));
		Assert.Throws<PluginException>(() => Parser.Parse("{unknown:value}", node, null, null));
		Assert.Throws<ArgumentNullException>("node", () => Parser.Parse("{type:int}", (PluginTreeNode)null, null, null));
		Assert.Throws<ArgumentNullException>("builtin", () => Parser.Parse("{type:int}", (Builtin)null, null, null));
	}

	[Fact]
	public async Task ServiceParserUsesApplicationModuleAndMemberType()
	{
		var shared = new NamedService("shared");
		var local = new NamedService("local");
		await using var scope = new PluginTestScope(configure: services => services.AddSingleton(shared));
		using var moduleServices = new ServiceCollection().AddSingleton(local).BuildServiceProvider();
		scope.Application.Modules.Add(new TestModule("Orders", moduleServices));
		scope.WritePlugin("<extension path=\"/Workbench/Modules/Orders\"><object name=\"Anchor\" type=\"System.Object\" /></extension>");
		scope.Tree.Load();
		var builtin = scope.Builtin("/Workbench/Modules/Orders/Anchor");

		Assert.Same(scope.Application.Services, Parser.Parse("{service:@}", builtin, null, null));
		Assert.Same(moduleServices, Parser.Parse("{service:@Orders}", builtin, null, null));
		Assert.Same(local, Parser.Parse("{service:~}", builtin, null, typeof(NamedService)));
		Assert.Same(shared, Parser.Parse("{service:~@}", builtin, null, typeof(NamedService)));
		Assert.Same(local, Parser.Parse("{service:~@Orders}", builtin, null, typeof(NamedService)));
		Assert.Same(local, Parser.Parse("{service:local@Orders}", builtin, null, typeof(NamedService)));
		Assert.Same(local, Assert.Single(Assert.IsAssignableFrom<IEnumerable<object>>(Parser.Parse("{service:*@Orders}", builtin, null, typeof(NamedService)))));
		Assert.Same(moduleServices, Parser.Parse("{service:~@Orders}", builtin, null, typeof(IServiceProvider)));
		Assert.Null(Parser.Parse("{service:~@Missing}", builtin, null, typeof(NamedService)));
		Assert.Null(Parser.Parse("{service:~}", builtin, null, null));
		Assert.Null(Parser.Parse("{service:absent@Orders}", builtin, null, typeof(NamedService)));
	}

	[Fact]
	public async Task NamedServiceAliasesSupportMemberNavigation()
	{
		await using var scope = new PluginTestScope(configure: services => Services.ServiceCollectionExtension.Register(services, typeof(ParserTests).Assembly, null));
		scope.Tree.Load();
		var node = scope.Tree.EnsurePath("/Items");
		var service = scope.Application.Services.GetRequiredService<AliasedService>();

		Assert.Same(service, Parser.Parse("{service:PluginsTestAlias}", node, null, null));
		Assert.Equal("alias-fixture", Parser.Parse("{service:PluginsTestAlias.Label}", node, null, null));
		Assert.Null(Parser.Parse("{service:UnknownPluginsTestAlias}", node, null, null));
	}

	[Fact]
	public async Task OptionParserBindsTypedValuesAndMemberAccess()
	{
		await using var scope = new PluginTestScope(settings: new Dictionary<string, string>
		{
			["Fixture:Number"] = "19",
			["Fixture:Label"] = "configured",
		});
		scope.WritePlugin("<extension path=\"/Items\"><object name=\"Anchor\" type=\"System.Object\" /></extension>");
		scope.Tree.Load();
		var builtin = scope.Builtin("/Items/Anchor");

		var option = Assert.IsType<TestOptions>(Parser.Parse("{option:/Fixture}", builtin, null, typeof(TestOptions)));

		Assert.Equal(19, option.Number);
		Assert.Equal("configured", option.Label);
		Assert.Equal(19, Parser.Parse("{option:/Fixture@Number}", builtin, null, typeof(TestOptions)));
	}

	public sealed class StaticValues
	{
		public static readonly ObjectBuilderTests.SimpleObject Value = new() { Number = 31 };
	}

	public sealed class NamedService(string name)
	{
		public string Name { get; } = name;
	}

	[Services.Service("PluginsTestAlias")]
	public sealed class AliasedService
	{
		public string Label => "alias-fixture";
	}

	public sealed class TestOptions
	{
		public int Number { get; set; }
		public string Label { get; set; }
	}

	private sealed class TestModule(string name, IServiceProvider services) : Services.ApplicationModule(name)
	{
		public override IServiceProvider Services => services;
	}
}
