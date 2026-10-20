using System;
using System.Linq;
using System.Text.Json;
using System.Reflection;
using System.Threading.Tasks;
using System.Security.Claims;

using Xunit;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Web;

namespace Zongsoft.Plugins.Web.Tests;

public class WebComponentTests
{
	[Fact]
	public async Task WebContextDiscoversPluginAssembliesAndInitializesOnce()
	{
		var manager = new ApplicationPartManager();
		manager.FeatureProviders.Add(new Microsoft.AspNetCore.Mvc.Controllers.ControllerFeatureProvider());
		await using var scope = new WebTestScope(services =>
		{
			services.AddSingleton(manager);
			services.AddSingleton<IApplicationFeatureProvider, ControllerFeatureProvider>();
		});
		scope.WriteFile("Feature.plugin", $"<plugin name=\"Feature\"><manifest><dependencies><dependency name=\"Main\" /></dependencies><assemblies><assembly name=\"{typeof(WebComponentTests).Assembly.GetName().Name}\" /></assemblies></manifest></plugin>");
		var application = scope.Application;

		Assert.True(application.Initialize());
		var count = manager.ApplicationParts.Count;
		Assert.False(application.Initialize());

		Assert.Equal(count, manager.ApplicationParts.Count);
		Assert.Equal("Web", application.ApplicationType);
		Assert.Contains(manager.ApplicationParts, part => part is AssemblyPart assembly && assembly.Assembly == typeof(WebComponentTests).Assembly);
		Assert.Contains(manager.ApplicationParts, part => part is AssemblyPart assembly && assembly.Assembly == typeof(WebApplicationContext).Assembly);
		Assert.Contains(manager.FeatureProviders, feature => feature is ControllerFeatureProvider);
		Assert.DoesNotContain(manager.FeatureProviders, feature => feature.GetType() == typeof(Microsoft.AspNetCore.Mvc.Controllers.ControllerFeatureProvider));
		var controllers = new Microsoft.AspNetCore.Mvc.Controllers.ControllerFeature();
		manager.PopulateFeature(controllers);
		Assert.Contains(typeof(FixtureController).GetTypeInfo(), controllers.Controllers);
		Assert.Equal(WorkbenchStatus.None, application.Workbench.Status);
	}

	[Fact]
	public async Task RequestPrincipalAndRouteValuesFollowCurrentHttpContext()
	{
		var accessor = new HttpContextAccessor();
		await using var scope = new WebTestScope(services => services.AddSingleton<IHttpContextAccessor>(accessor));
		var application = scope.Application;
		Assert.Null(application.HttpContext);
		Assert.Null(application.Principal);
		var session = application.Session;

		try
		{
			var first = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity("fixture")) };
			first.Request.RouteValues["id"] = "first";
			accessor.HttpContext = first;

			Assert.Same(first, application.HttpContext);
			Assert.Same(first.User, application.Principal);
			Assert.Equal("first", session["id"]);

			var second = new DefaultHttpContext();
			second.Request.RouteValues["id"] = "second";
			accessor.HttpContext = second;

			Assert.Same(second.User, application.Principal);
			Assert.Equal("second", session["id"]);
			Assert.Same(session, application.Session);
			Assert.False(session.TryGetValue("missing", out _));
		}
		finally
		{
			accessor.HttpContext = null;
		}
	}

	[Fact]
	public async Task PluginControllerReportsMetadataWithoutBuildingValues()
	{
		await using var scope = new WebTestScope();
		scope.WritePlugin("<extension path=\"/Items\"><object name=\"Item\" type=\"System.Object\" /></extension>");
		scope.Application.Initialize();
		var controller = new Controllers.PluginController();
		var builtin = scope.Builtin("/Items/Item");

		var node = JsonSerializer.SerializeToElement(controller.Get("/Items/Item"));
		var plugins = JsonSerializer.SerializeToElement(controller.Get(null));

		Assert.Equal("Item", node.GetProperty("Name").GetString());
		Assert.Equal("/Items", node.GetProperty("Path").GetString());
		Assert.Equal(JsonValueKind.Null, node.GetProperty("Value").ValueKind);
		Assert.False(builtin.IsBuilded);
		Assert.Equal(2, plugins.GetArrayLength());
		Assert.All(plugins.EnumerateArray(), plugin => Assert.DoesNotContain(scope.DirectoryPath, plugin.GetProperty("FilePath").GetString()));
		Assert.IsType<NotFoundResult>(controller.Get("/Missing"));
	}

	[Fact]
	public async Task ApplicationConventionPublishesControllerDescriptors()
	{
		await using var scope = new WebTestScope();
		var application = new ApplicationModel();
		application.Controllers.Add(new ControllerModel(typeof(FixtureController).GetTypeInfo(), []) { ControllerName = "Fixture" });

		new ApplicationConvention().Apply(application);

		Assert.True(scope.Application.Properties.TryGetValue(typeof(ControllerServiceDescriptorCollection), out var descriptors));
		Assert.IsType<ControllerServiceDescriptorCollection>(descriptors);
	}

	public sealed class FixtureController : ControllerBase;

	private sealed class WebTestScope : IAsyncDisposable
	{
		private readonly PluginTestDirectory _files = new();
		private readonly ConfigurationManager _configuration = new();
		private readonly IAsyncDisposable _services;

		public WebTestScope(Action<IServiceCollection> configure = null)
		{
			var services = new ServiceCollection();
			services.AddSingleton<IConfiguration>(_configuration);
			services.AddSingleton<ApplicationPartManager>();
			services.AddHttpContextAccessor();
			configure?.Invoke(services);
			var provider = new Services.ServiceProviderFactory().CreateServiceProvider(services);
			_services = (IAsyncDisposable)provider;
			this.Application = new WebApplicationContext(provider, new PluginOptions(_configuration, _files.DirectoryPath, "Development"));
		}

		public string DirectoryPath => _files.DirectoryPath;
		public WebApplicationContext Application { get; }
		public void WriteFile(string path, string content) => _files.Write($"plugins/{path}", content);
		public void WritePlugin(string content) => this.WriteFile("Feature.plugin", $"<plugin name=\"Feature\"><manifest><dependencies><dependency name=\"Main\" /></dependencies></manifest>{content}</plugin>");
		public Builtin Builtin(string path) => (Builtin)this.Application.PluginTree.Find(path).Value;

		public async ValueTask DisposeAsync()
		{
			try
			{
				await this.Application.DisposeAsync();
				await _services.DisposeAsync();
				((IDisposable)_configuration).Dispose();
			}
			finally
			{
				_files.Dispose();
			}
		}
	}
}
