using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Plugins.Web.IntegrationTests;

[Trait("Category", "Integration")]
public class EndpointIntegrationTests
{
	[Fact]
	public Task NonWebServiceScanningIgnoresEndpointDeclarations() => EndpointTestScope.RunAsync(nameof(NonWebServiceScanningIgnoresEndpointDeclarations));

	[Fact]
	public Task AutomaticIntegrationRejectsUnregisteredProtocols() => EndpointTestScope.RunAsync(nameof(AutomaticIntegrationRejectsUnregisteredProtocols));

	[Fact]
	public Task CallerSelectedConfigurationSectionIsSupported() => EndpointTestScope.RunAsync(nameof(CallerSelectedConfigurationSectionIsSupported));

	[Fact]
	public Task MonitoringIsOptional() => EndpointTestScope.RunAsync(nameof(MonitoringIsOptional));

	[Fact]
	public Task MissingDeclarationsKeepNativePluginConfiguration() => EndpointTestScope.RunAsync(nameof(MissingDeclarationsKeepNativePluginConfiguration));

	[Fact]
	public Task PluginRegistrationPrecedesEndpointSnapshot() => EndpointTestScope.RunAsync(nameof(PluginRegistrationPrecedesEndpointSnapshot));

	[Fact]
	public Task PluginImportsProduceOneEffectiveEndpoint() => EndpointTestScope.RunAsync(nameof(PluginImportsProduceOneEffectiveEndpoint));

	[Fact]
	public Task EqualPluginValuesAreAllowed() => EndpointTestScope.RunAsync(nameof(EqualPluginValuesAreAllowed));

	[Fact]
	public Task DifferentPluginValuesReportEntryOriginsWithoutValues() => EndpointTestScope.RunAsync(nameof(DifferentPluginValuesReportEntryOriginsWithoutValues));

	[Theory]
	[InlineData("same")]
	[InlineData("distinct")]
	[InlineData("missing")]
	public Task WebFactoryLoadsHostConfigurationBeforeWebConfiguration(string configuration) => EndpointTestScope.RunAsync(nameof(WebFactoryLoadsHostConfigurationBeforeWebConfiguration), configuration);

	[Fact]
	public Task HostFileProviderSurvivesEndpointIntegration() => EndpointTestScope.RunAsync(nameof(HostFileProviderSurvivesEndpointIntegration));

	[Fact]
	public Task EmptyExplicitEndpointsRejectRequests() => EndpointTestScope.RunAsync(nameof(EmptyExplicitEndpointsRejectRequests));

	[Theory]
	[InlineData("urls")]
	[InlineData("http_ports")]
	[InlineData("https_ports")]
	public Task NativeBuilderUsesAddressSourcesWithoutEndpointServices(string key) => EndpointTestScope.RunAsync(nameof(NativeBuilderUsesAddressSourcesWithoutEndpointServices), key);

	[Fact]
	public Task PathfulEndpointsListenAndEnforceRanges() => EndpointTestScope.RunAsync(nameof(PathfulEndpointsListenAndEnforceRanges));

	[Fact]
	public Task FactoryPipelineGuardsInitializersStaticFilesAndRoutes() => EndpointTestScope.RunAsync(nameof(FactoryPipelineGuardsInitializersStaticFilesAndRoutes));

	[Fact]
	public Task AutomaticIntegrationInstallsRangeMiddleware() => EndpointTestScope.RunAsync(nameof(AutomaticIntegrationInstallsRangeMiddleware));

	[Fact]
	public Task IPv6SocketProjectionPreservesAddressFamily() => EndpointTestScope.RunAsync(nameof(IPv6SocketProjectionPreservesAddressFamily));

	[Fact]
	public Task NativeBuilderUsesLateUrls() => EndpointTestScope.RunAsync(nameof(NativeBuilderUsesLateUrls));

	[Fact]
	public Task ExtraCodeListenerStartsButRequestsAreRejected() => EndpointTestScope.RunAsync(nameof(ExtraCodeListenerStartsButRequestsAreRejected));

	[Fact]
	public Task NativeBuilderUsesCodeListeners() => EndpointTestScope.RunAsync(nameof(NativeBuilderUsesCodeListeners));

	[Fact]
	public Task NetworkChangesWarnOnceWithoutChangingSnapshot() => EndpointTestScope.RunAsync(nameof(NetworkChangesWarnOnceWithoutChangingSnapshot));

	[Theory]
	[InlineData("stop")]
	[InlineData("dispose")]
	public Task DisposingHostReleasesUnlatchedMonitor(string lifecycle) => EndpointTestScope.RunAsync(nameof(DisposingHostReleasesUnlatchedMonitor), lifecycle);

	[Fact]
	public Task NewPluginConflictRequiresRestart() => EndpointTestScope.RunAsync(nameof(NewPluginConflictRequiresRestart));

	[Fact]
	public Task FinalProjectionOverridesEarlierPostConfiguration() => EndpointTestScope.RunAsync(nameof(FinalProjectionOverridesEarlierPostConfiguration));

	[Fact]
	public Task InitializerFailureDisposesApplication() => EndpointTestScope.RunAsync(nameof(InitializerFailureDisposesApplication));

	[Theory]
	[InlineData("instance")]
	[InlineData("factory")]
	[InlineData("type")]
	public Task TransportFactoriesPreserveSelectionOwnershipAndCarrier(string registration) => EndpointTestScope.RunAsync(nameof(TransportFactoriesPreserveSelectionOwnershipAndCarrier), registration);
}
