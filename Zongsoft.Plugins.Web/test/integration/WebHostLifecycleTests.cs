using System.Threading.Tasks;
using Xunit;

namespace Zongsoft.Plugins.Web.IntegrationTests;

[Trait("Category", "Integration")]
public class WebHostLifecycleTests
{
	[Fact]
	public Task WebHostConstructionInitializesPluginServices() => EndpointTestScope.RunAsync(nameof(WebHostConstructionInitializesPluginServices));

	public sealed class ProbeService;
}
