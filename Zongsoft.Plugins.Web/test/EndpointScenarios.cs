using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Web.Hosting;

using Xunit;

namespace Zongsoft.Plugins.Web.Tests;

internal static partial class EndpointScenarios
{
	public static Task RunAsync(string name, EndpointTestScope scope, string argument) => name switch
	{
		nameof(EndpointIntegrationTests.NonWebServiceScanningIgnoresEndpointDeclarations) => NonWebServiceScanningIgnoresEndpointDeclarations(),
		nameof(EndpointIntegrationTests.AutomaticIntegrationRejectsUnregisteredProtocols) => AutomaticIntegrationRejectsUnregisteredProtocols(scope),
		nameof(EndpointIntegrationTests.PluginRegistrationPrecedesEndpointSnapshot) => PluginRegistrationPrecedesEndpointSnapshot(scope),
		nameof(EndpointIntegrationTests.PluginImportsProduceOneEffectiveEndpoint) => PluginImportsProduceOneEffectiveEndpoint(scope),
		nameof(EndpointIntegrationTests.EqualPluginValuesAreAllowed) => EqualPluginValuesAreAllowed(scope),
		nameof(EndpointIntegrationTests.DifferentPluginValuesReportEntryOriginsWithoutValues) => DifferentPluginValuesReportEntryOriginsWithoutValues(scope),
		nameof(EndpointIntegrationTests.HostFileProviderSurvivesEndpointIntegration) => HostFileProviderSurvivesEndpointIntegration(scope),
		nameof(EndpointIntegrationTests.EmptyExplicitEndpointsRejectRequests) => EmptyExplicitEndpointsRejectRequests(scope),
		nameof(EndpointIntegrationTests.NativeBuilderUsesAddressSourcesWithoutEndpointServices) => NativeBuilderUsesAddressSourcesWithoutEndpointServices(scope, argument),
		nameof(EndpointIntegrationTests.PathfulEndpointsListenAndEnforceRanges) => PathfulEndpointsListenAndEnforceRanges(scope),
		nameof(EndpointIntegrationTests.FactoryPipelineGuardsInitializersStaticFilesAndRoutes) => FactoryPipelineGuardsInitializersStaticFilesAndRoutes(scope),
		nameof(EndpointIntegrationTests.AutomaticIntegrationInstallsRangeMiddleware) => AutomaticIntegrationInstallsRangeMiddleware(scope),
		nameof(EndpointIntegrationTests.IPv6SocketProjectionPreservesAddressFamily) => IPv6SocketProjectionPreservesAddressFamily(scope),
		nameof(EndpointIntegrationTests.NativeBuilderUsesLateUrls) => NativeBuilderUsesLateUrls(scope),
		nameof(EndpointIntegrationTests.ExtraCodeListenerStartsButRequestsAreRejected) => ExtraCodeListenerStartsButRequestsAreRejected(scope),
		nameof(EndpointIntegrationTests.NativeBuilderUsesCodeListeners) => NativeBuilderUsesCodeListeners(scope),
		nameof(EndpointIntegrationTests.NetworkChangesWarnOnceWithoutChangingSnapshot) => NetworkChangesWarnOnceWithoutChangingSnapshot(scope),
		nameof(EndpointIntegrationTests.DisposingHostReleasesUnlatchedMonitor) => DisposingHostReleasesUnlatchedMonitor(scope, argument),
		nameof(EndpointIntegrationTests.NewPluginConflictRequiresRestart) => NewPluginConflictRequiresRestart(scope),
		nameof(EndpointIntegrationTests.FinalProjectionOverridesEarlierPostConfiguration) => FinalProjectionOverridesEarlierPostConfiguration(scope),
		nameof(EndpointIntegrationTests.InitializerFailureDisposesApplication) => InitializerFailureDisposesApplication(scope),
		nameof(EndpointIntegrationTests.TransportFactoriesPreserveSelectionOwnershipAndCarrier) => TransportFactoriesPreserveSelectionOwnershipAndCarrier(scope, argument),
		nameof(EndpointIntegrationTests.MissingDeclarationsKeepNativePluginConfiguration) => MissingDeclarationsKeepNativePluginConfiguration(scope),
		nameof(EndpointIntegrationTests.MonitoringIsOptional) => MonitoringIsOptional(scope),
		nameof(EndpointIntegrationTests.CallerSelectedConfigurationSectionIsSupported) => CallerSelectedConfigurationSectionIsSupported(scope),
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

	private static WebApplication Application(EndpointTestScope scope, Action<Microsoft.AspNetCore.Builder.WebApplicationBuilder> configure = null, bool endpoints = true, bool monitoring = false) =>
		Zongsoft.Web.Application.Web(typeof(TestProgram).Assembly.GetName().Name, Arguments(scope), builder =>
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

	private static void CancelWorkbenchExit(WebApplication application) => application.Services.GetRequiredService<Zongsoft.Web.WebApplicationContext>().Workbench.Closing += (_, args) => args.Cancel = true;

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
