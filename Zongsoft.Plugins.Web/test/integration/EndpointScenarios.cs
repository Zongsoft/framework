using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Web.Hosting;

using Xunit;

namespace Zongsoft.Plugins.Web.IntegrationTests;

internal static partial class EndpointScenarios
{
	public static Task RunAsync(string name, EndpointTestScope scope, string argument) => name switch
	{
		nameof(WebHostLifecycleTest.WebHostConstructionInitializesPluginServices) => WebHostConstructionInitializesPluginServices(scope),
		nameof(EndpointIntegrationTest.NonWebServiceScanningIgnoresEndpointDeclarations) => NonWebServiceScanningIgnoresEndpointDeclarations(),
		nameof(EndpointIntegrationTest.AutomaticIntegrationRejectsUnregisteredProtocols) => AutomaticIntegrationRejectsUnregisteredProtocols(scope),
		nameof(EndpointIntegrationTest.PluginRegistrationPrecedesEndpointSnapshot) => PluginRegistrationPrecedesEndpointSnapshot(scope),
		nameof(EndpointIntegrationTest.PluginImportsProduceOneEffectiveEndpoint) => PluginImportsProduceOneEffectiveEndpoint(scope),
		nameof(EndpointIntegrationTest.EqualPluginValuesAreAllowed) => EqualPluginValuesAreAllowed(scope),
		nameof(EndpointIntegrationTest.DifferentPluginValuesReportEntryOriginsWithoutValues) => DifferentPluginValuesReportEntryOriginsWithoutValues(scope),
		nameof(EndpointIntegrationTest.WebFactoryLoadsHostConfigurationBeforeWebConfiguration) => WebFactoryLoadsHostConfigurationBeforeWebConfiguration(scope, argument),
		nameof(EndpointIntegrationTest.HostFileProviderSurvivesEndpointIntegration) => HostFileProviderSurvivesEndpointIntegration(scope),
		nameof(EndpointIntegrationTest.EmptyExplicitEndpointsRejectRequests) => EmptyExplicitEndpointsRejectRequests(scope),
		nameof(EndpointIntegrationTest.NativeBuilderUsesAddressSourcesWithoutEndpointServices) => NativeBuilderUsesAddressSourcesWithoutEndpointServices(scope, argument),
		nameof(EndpointIntegrationTest.PathfulEndpointsListenAndEnforceRanges) => PathfulEndpointsListenAndEnforceRanges(scope),
		nameof(EndpointIntegrationTest.FactoryPipelineGuardsInitializersStaticFilesAndRoutes) => FactoryPipelineGuardsInitializersStaticFilesAndRoutes(scope),
		nameof(EndpointIntegrationTest.AutomaticIntegrationInstallsRangeMiddleware) => AutomaticIntegrationInstallsRangeMiddleware(scope),
		nameof(EndpointIntegrationTest.IPv6SocketProjectionPreservesAddressFamily) => IPv6SocketProjectionPreservesAddressFamily(scope),
		nameof(EndpointIntegrationTest.NativeBuilderUsesLateUrls) => NativeBuilderUsesLateUrls(scope),
		nameof(EndpointIntegrationTest.ExtraCodeListenerStartsButRequestsAreRejected) => ExtraCodeListenerStartsButRequestsAreRejected(scope),
		nameof(EndpointIntegrationTest.NativeBuilderUsesCodeListeners) => NativeBuilderUsesCodeListeners(scope),
		nameof(EndpointIntegrationTest.NetworkChangesWarnOnceWithoutChangingSnapshot) => NetworkChangesWarnOnceWithoutChangingSnapshot(scope),
		nameof(EndpointIntegrationTest.DisposingHostReleasesUnlatchedMonitor) => DisposingHostReleasesUnlatchedMonitor(scope, argument),
		nameof(EndpointIntegrationTest.NewPluginConflictRequiresRestart) => NewPluginConflictRequiresRestart(scope),
		nameof(EndpointIntegrationTest.FinalProjectionOverridesEarlierPostConfiguration) => FinalProjectionOverridesEarlierPostConfiguration(scope),
		nameof(EndpointIntegrationTest.InitializerFailureDisposesApplication) => InitializerFailureDisposesApplication(scope),
		nameof(EndpointIntegrationTest.TransportFactoriesPreserveSelectionOwnershipAndCarrier) => TransportFactoriesPreserveSelectionOwnershipAndCarrier(scope, argument),
		nameof(EndpointIntegrationTest.MissingDeclarationsKeepNativePluginConfiguration) => MissingDeclarationsKeepNativePluginConfiguration(scope),
		nameof(EndpointIntegrationTest.MonitoringIsOptional) => MonitoringIsOptional(scope),
		nameof(EndpointIntegrationTest.CallerSelectedConfigurationSectionIsSupported) => CallerSelectedConfigurationSectionIsSupported(scope),
		_ => throw new ArgumentOutOfRangeException(nameof(name)),
	};

	private static Zongsoft.Web.WebApplicationBuilder Builder(EndpointTestScope scope, Action<Microsoft.AspNetCore.Builder.WebApplicationBuilder> configure = null, bool endpoints = true, bool monitoring = false) =>
		new(typeof(TestProgram).Assembly.GetName().Name, Arguments(scope), builder =>
		{
			Configure(builder);
			configure?.Invoke(builder);
			if(endpoints)
				builder.Services.AddWebEndpoints(builder.Configuration);
			if(monitoring)
				builder.Services.AddWebEndpointMonitoring().AddWebEndpointMonitoring();
		});

	private static ValueTask<WebApplication> ApplicationAsync(EndpointTestScope scope, Action<Microsoft.AspNetCore.Builder.WebApplicationBuilder> configure = null, bool endpoints = true, bool monitoring = false) =>
		Zongsoft.Web.Application.WebAsync(typeof(TestProgram).Assembly.GetName().Name, Arguments(scope), builder =>
		{
			Configure(builder);
			configure?.Invoke(builder);
			if(endpoints)
				builder.Services.AddWebEndpoints(builder.Configuration);
			if(monitoring)
				builder.Services.AddWebEndpointMonitoring().AddWebEndpointMonitoring();
		});

	private static string[] Arguments(EndpointTestScope scope) => [$"--contentRoot={scope.DirectoryPath}", "--environment=Development"];

	private static void Configure(Microsoft.AspNetCore.Builder.WebApplicationBuilder builder)
	{
		builder.Logging.ClearProviders();
		//清除执行机器上可能存在的端口简写，具体冲突由各用例显式设置。
		builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
		{
			["urls"] = "",
			["http_ports"] = "",
			["https_ports"] = "",
		});
	}

	private static void ConfigureEndpoint(IConfiguration configuration, int port, string path = "/api", string name = "api")
	{
		configuration[$"Kestrel:Endpoints:{name}:Url"] = $"http://127.0.0.1:{port}{path}";
		configuration[$"Kestrel:Endpoints:{name}:Protocols"] = "http1";
	}


	private static void Failure(Exception exception, string reason, string key = null)
	{
		Assert.IsType<InvalidOperationException>(exception);
		Assert.Equal(reason, exception.Data["Reason"]);
		if(key != null)
			Assert.Equal(key, exception.Data["Key"]);
	}

}

//由真实服务扫描器发现，配置未启用时不参与其他宿主场景。
public sealed class TestEndpointRegistration : Zongsoft.Services.IServiceRegistration
{
	public void Register(IServiceCollection services, IConfiguration configuration)
	{
		if(configuration["Fixture:rescan:enabled"] == "true")
		{
			Assert.Single(services, service => service.ServiceType == typeof(WebEndpointCollection));
			Zongsoft.Services.ServiceCollectionExtension.Register(services, typeof(Zongsoft.Web.WebApplicationBuilder).Assembly, configuration);
			Assert.Single(services, service => service.ServiceType == typeof(WebEndpointCollection));
		}

		if(configuration["Fixture:registration:enabled"] != "true")
			return;

		Assert.Equal("custom", configuration["Kestrel:Endpoints:api:Protocols"]);
		Assert.Contains(services, service => service.ServiceType == typeof(WebEndpointCollection));
		services.AddWebEndpointProtocol(new WebEndpointProtocol("custom", "http1"));
	}
}
