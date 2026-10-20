using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Web.Hosting;

using Xunit;

namespace Zongsoft.Plugins.Web.IntegrationTests;

internal static partial class EndpointScenarios
{
	private static async Task WebHostConstructionInitializesPluginServices(EndpointTestScope scope)
	{
		scope.Plugin("Feature", EndpointTestScope.Endpoint("api", "http://127.0.0.1:18081/"));
		scope.Write("plugins/Feature.plugin", $"<plugin name=\"Feature\"><manifest><dependencies><dependency name=\"Main\" /></dependencies></manifest><extension path=\"/Workspace/Environment/Services\"><object name=\"FixtureService\" type=\"{typeof(WebHostLifecycleTests.ProbeService).AssemblyQualifiedName}\" /></extension></plugin>");
		await using var application = await ApplicationAsync(scope, endpoints: false);
		var context = application.Services.GetRequiredService<Zongsoft.Web.WebApplicationContext>();

		Assert.Equal("Web", context.ApplicationType);
		Assert.Same(context, application.Services.GetRequiredService<PluginApplicationContext>());
		Assert.Same(context, application.Services.GetRequiredService<Services.IApplicationContext>());
		Assert.False(context.Initialize());
		Assert.Equal(WorkbenchStatus.None, context.Workbench.Status);
		Assert.IsType<Zongsoft.Web.ControllerActivator>(application.Services.GetRequiredService<Microsoft.AspNetCore.Mvc.Controllers.IControllerActivator>());
		Assert.Same(application.Services.GetRequiredService<WebHostLifecycleTests.ProbeService>(), context.PluginTree.Find("/Workspace/Environment/Services/FixtureService").UnwrapValue(ObtainMode.Never));
	}

	private static async Task PluginRegistrationPrecedesEndpointSnapshot(EndpointTestScope scope)
	{
		scope.Plugin("Feature", EndpointTestScope.Endpoint("api", "http://127.0.0.1:18081/api", "custom") + "<?import path='Feature-registration.option'?>");
		scope.Write("plugins/Feature-registration.option", "<options><option path=\"Fixture\"><registration enabled=\"true\" /></option></options>");
		await using var application = await Builder(scope, endpoints: false).BuildAsync();
		var endpoint = Assert.Single(application.Services.GetRequiredService<WebEndpointCollection>());
		Assert.Equal("api", endpoint.Name);
		Assert.Equal("custom+http1", Assert.Single(endpoint.Protocols).ToString());
		Assert.Equal("custom", application.Configuration["Kestrel:Endpoints:api:Protocols"]);
	}

	private static async Task PluginImportsProduceOneEffectiveEndpoint(EndpointTestScope scope)
	{
		scope.Plugin("Feature", EndpointTestScope.Endpoint("api", "http://127.0.0.1:18081/old") + "<?import path='Feature-site.option'?>");
		scope.Write("plugins/Feature-site.option", EndpointTestScope.Endpoint("api", "http://127.0.0.1:18082/final"));
		await using var application = await Builder(scope, endpoints: false).BuildAsync();
		var endpoint = Assert.Single(application.Services.GetRequiredService<WebEndpointCollection>());
		Assert.Equal(18082, endpoint.Address.Port);
		Assert.Equal("/final", endpoint.Address.Path);
		Assert.Equal("http://127.0.0.1:18082/final", application.Configuration["Kestrel:Endpoints:api:Url"]);
	}

	private static async Task EqualPluginValuesAreAllowed(EndpointTestScope scope)
	{
		var option = EndpointTestScope.Endpoint("api", "http://127.0.0.1:18081/shared");
		scope.Plugin("Alpha", option);
		scope.Plugin("Beta", option);
		await using var application = await Builder(scope, endpoints: false).BuildAsync();
		Assert.Equal("/shared", Assert.Single(application.Services.GetRequiredService<WebEndpointCollection>()).Address.Path);
	}

	private static async Task DifferentPluginValuesReportEntryOriginsWithoutValues(EndpointTestScope scope)
	{
		var alpha = "http://127.0.0.1:18081/private-alpha";
		var beta = "http://127.0.0.1:18081/PRIVATE-ALPHA";
		scope.Plugin("Alpha", EndpointTestScope.Endpoint("api", beta) + "<?import path='Alpha-site.option'?>");
		scope.Write("plugins/Alpha-site.option", EndpointTestScope.Endpoint("api", alpha));
		scope.Plugin("Beta", EndpointTestScope.Endpoint("api", beta));
		var error = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
		{
			await using var application = await Builder(scope, endpoints: false).BuildAsync();
			application.Services.GetRequiredService<WebEndpointCollection>();
		});
		Failure(error, "ConflictingPluginConfiguration");
		Assert.Equal("Kestrel:Endpoints:api:Url", Assert.IsType<string>(error.Data["Key"]), ignoreCase: true);
		var sources = Assert.IsType<string[]>(error.Data["Sources"]);
		Assert.Equal(new[] { Path.Combine(scope.DirectoryPath, "plugins", "Alpha.option"), Path.Combine(scope.DirectoryPath, "plugins", "Beta.option") }.Order(StringComparer.OrdinalIgnoreCase), sources.Order(StringComparer.OrdinalIgnoreCase));
		Assert.DoesNotContain("private-alpha", error.ToString(), StringComparison.Ordinal);
		Assert.DoesNotContain("PRIVATE-ALPHA", error.ToString(), StringComparison.Ordinal);
		Assert.DoesNotContain(alpha, error.Data.Values.Cast<object>().Select(value => value?.ToString()));
		Assert.DoesNotContain(beta, error.Data.Values.Cast<object>().Select(value => value?.ToString()));
	}

	private static async Task WebFactoryLoadsHostConfigurationBeforeWebConfiguration(EndpointTestScope scope, string configuration)
	{
		var assemblyName = typeof(TestProgram).Assembly.GetName().Name;
		var applicationName = configuration == "distinct" ? typeof(Zongsoft.Web.WebApplicationBuilder).Assembly.GetName().Name : assemblyName;

		if(configuration == "distinct")
			scope.Write($"{applicationName}.option", "<options><option path=\"Fixture\"><settings application=\"application\" priority=\"application\" hostPriority=\"application\" /></option></options>");

		if(configuration != "missing")
			scope.Write($"{assemblyName}.option", "<options><option path=\"Fixture\"><settings host=\"host\" priority=\"host\" hostPriority=\"host\" /></option></options>");

		scope.Write("web.option", "<options><option path=\"Fixture\"><settings web=\"web\" priority=\"web\" /></option></options>");
		scope.Plugin("Feature", "<options><option path=\"Fixture\"><settings plugin=\"plugin\" /></option></options>");

		await using var application = await Zongsoft.Web.Application.WebAsync(applicationName, Arguments(scope), Configure);

		Assert.Equal(configuration == "distinct" ? "application" : null, application.Configuration["Fixture:settings:application"]);
		Assert.Equal(configuration == "missing" ? null : "host", application.Configuration["Fixture:settings:host"]);
		Assert.Equal(configuration == "missing" ? null : "host", application.Configuration["Fixture:settings:hostPriority"]);
		Assert.Equal("web", application.Configuration["Fixture:settings:web"]);
		Assert.Equal("web", application.Configuration["Fixture:settings:priority"]);
		Assert.Equal("plugin", application.Configuration["Fixture:settings:plugin"]);

		var files = ((IConfigurationRoot)application.Configuration).Providers.OfType<FileConfigurationProvider>();
		Assert.Single(files, provider => provider.Source.Path == $"{assemblyName}.option");
	}

	private static async Task HostFileProviderSurvivesEndpointIntegration(EndpointTestScope scope)
	{
		var files = new MemoryFileProvider(
			new("web.option", EndpointTestScope.Endpoint("api", "http://127.0.0.1:18081/main") + "<?import path='web-site.option'?>"),
			new("web-site.option", EndpointTestScope.Endpoint("api", "http://127.0.0.1:18082/site")));
		var builder = Builder(scope, endpoints: false);
		builder.Configuration.SetFileProvider(files);
		builder.LoadConfiguration("web");
		await using var application = await builder.BuildAsync();
		Assert.Same(files, builder.Configuration.GetFileProvider());
		Assert.Equal("http://127.0.0.1:18082/site", application.Configuration["Kestrel:Endpoints:api:Url"]);
		Assert.Equal("/site", Assert.Single(application.Services.GetRequiredService<WebEndpointCollection>()).Address.Path);
	}

	private static Task NonWebServiceScanningIgnoresEndpointDeclarations()
	{
		using var configuration = (ConfigurationRoot)new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
		{
			["Kestrel:Endpoints:api:Url"] = "http://127.0.0.1:18081/api",
			["Kestrel:Endpoints:api:Protocols"] = "http1",
		}).Build();
		var services = new ServiceCollection();
		Zongsoft.Services.ServiceCollectionExtension.Register(services, typeof(Zongsoft.Web.WebApplicationBuilder).Assembly, configuration);
		Assert.DoesNotContain(services, service => service.ServiceType == typeof(WebEndpointCollection));
		Assert.DoesNotContain(services, service => service.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService));
		Assert.DoesNotContain(services, service => service.ServiceType == typeof(Microsoft.AspNetCore.Hosting.IStartupFilter));
		Assert.Equal("http://127.0.0.1:18081/api", configuration["Kestrel:Endpoints:api:Url"]);
		return Task.CompletedTask;
	}

	private static async Task AutomaticIntegrationRejectsUnregisteredProtocols(EndpointTestScope scope)
	{
		var port = EndpointTestScope.Port();
		scope.Plugin("Feature", EndpointTestScope.Endpoint("api", $"http://127.0.0.1:{port}/api", "custom+http1"));
		await Assert.ThrowsAsync<InvalidOperationException>(async () =>
		{
			await using var application = await Builder(scope, endpoints: false).BuildAsync();
			await application.StartAsync();
		});
		AssertPortAvailable(port);
	}

	private sealed class MemoryFileProvider(params MemoryFile[] files) : IFileProvider, IDirectoryContents
	{
		public bool Exists => true;
		public IFileInfo GetFileInfo(string path) => files.FirstOrDefault(file => file.Name == path) is { } file ? file : new NotFoundFileInfo(path);
		public IDirectoryContents GetDirectoryContents(string path) => string.IsNullOrEmpty(path) || path == "/" ? this : NotFoundDirectoryContents.Singleton;
		public IChangeToken Watch(string filter) => NullChangeToken.Singleton;
		public IEnumerator<IFileInfo> GetEnumerator() => files.Cast<IFileInfo>().GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	}

	private sealed class MemoryFile(string name, string content) : IFileInfo
	{
		public bool Exists => true;
		public bool IsDirectory => false;
		public string Name => name;
		public string PhysicalPath => null;
		public long Length => Encoding.UTF8.GetByteCount(content);
		public DateTimeOffset LastModified => DateTimeOffset.UnixEpoch;
		public Stream CreateReadStream() => new MemoryStream(Encoding.UTF8.GetBytes(content));
	}
}
