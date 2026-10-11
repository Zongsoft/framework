using Xunit;

using Zongsoft.Plugins.Parsers;

namespace Zongsoft.Plugins.Tests;

public class ResourceParserTests
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public void ResourceLookupSearchesManifestAssembliesAndDependencies(bool dependency)
	{
		using var scope = new PluginTestScope();
		scope.WriteFile("Resources.plugin", "<plugin name=\"Resources\"><manifest><dependencies><dependency name=\"Main\" /></dependencies><assemblies><assembly name=\"Zongsoft.Plugins.Tests\" /></assemblies></manifest><extension path=\"/Resources\"><object name=\"Anchor\" type=\"System.Object\" /></extension></plugin>");
		scope.WriteFile("Consumer.plugin", "<plugin name=\"Consumer\"><manifest><dependencies><dependency name=\"Main\" /><dependency name=\"Resources\" /></dependencies></manifest><extension path=\"/Consumer\"><object name=\"Anchor\" type=\"System.Object\" /></extension></plugin>");
		scope.Tree.Load();
		var builtin = scope.Builtin(dependency ? "/Consumer/Anchor" : "/Resources/Anchor");

		//资源是测试自有数据，不依赖框架资源文案或当前语言。
		Assert.Equal("fixture-payload-731", Parser.Parse("{resource:FixturePayload}", builtin, null, null));
		Assert.Equal("MissingFixturePayload", Parser.Parse("{resource:MissingFixturePayload}", builtin, null, null));
		Assert.False(builtin.IsBuilded);
	}
}
