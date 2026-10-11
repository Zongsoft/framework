using System;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Security.Claims;

using Xunit;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Web;

namespace Zongsoft.Plugins.Tests;

[Trait("Category", "Integration")]
public class WebIntegrationTests
{
	[Fact]
	public void WebContextDiscoversPluginAssembliesAndInitializesOnce()
	{
		using var scope = new PluginTestScope();
		scope.WriteFile("Feature.plugin", "<plugin name=\"Feature\"><manifest><dependencies><dependency name=\"Main\" /></dependencies><assemblies><assembly name=\"Zongsoft.Plugins.Tests\" /></assemblies></manifest></plugin>");
		var manager = new ApplicationPartManager();
		manager.FeatureProviders.Add(new Microsoft.AspNetCore.Mvc.Controllers.ControllerFeatureProvider());
		var services = new ServiceCollection();
		services.AddSingleton(manager);
		services.AddSingleton<IApplicationFeatureProvider, ControllerFeatureProvider>();
		services.AddHttpContextAccessor();
		var provider = new Services.ServiceProviderFactory().CreateServiceProvider(services);
		using var serviceLifetime = (IDisposable)provider;
		using var application = new WebApplicationContext(provider, scope.Options);

		Assert.True(application.Initialize());
		var count = manager.ApplicationParts.Count;
		Assert.False(application.Initialize());

		Assert.Equal(count, manager.ApplicationParts.Count);
		Assert.Equal("Web", application.ApplicationType);
		Assert.Contains(manager.ApplicationParts, part => part is AssemblyPart assembly && assembly.Assembly == typeof(WebIntegrationTests).Assembly);
		Assert.Contains(manager.ApplicationParts, part => part is AssemblyPart assembly && assembly.Assembly == typeof(WebApplicationContext).Assembly);
		Assert.Contains(manager.FeatureProviders, feature => feature is ControllerFeatureProvider);
		Assert.DoesNotContain(manager.FeatureProviders, feature => feature.GetType() == typeof(Microsoft.AspNetCore.Mvc.Controllers.ControllerFeatureProvider));
		var controllers = new Microsoft.AspNetCore.Mvc.Controllers.ControllerFeature();
		manager.PopulateFeature(controllers);
		Assert.Contains(typeof(FixtureController).GetTypeInfo(), controllers.Controllers);
		Assert.Equal(WorkbenchStatus.None, application.Workbench.Status);
	}

	[Fact]
	public void RequestPrincipalAndRouteValuesFollowCurrentHttpContext()
	{
		using var scope = new PluginTestScope();
		var accessor = new HttpContextAccessor();
		var services = new ServiceCollection();
		services.AddSingleton<IHttpContextAccessor>(accessor);
		var provider = new Services.ServiceProviderFactory().CreateServiceProvider(services);
		using var serviceLifetime = (IDisposable)provider;
		using var application = new WebApplicationContext(provider, scope.Options);
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
	public void PluginControllerReportsMetadataWithoutBuildingValues()
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin("<extension path=\"/Items\"><object name=\"Item\" type=\"System.Object\" /></extension>");
		scope.Application.Initialize();
		var controller = new Web.Controllers.PluginController();
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
	public void ApplicationConventionPublishesControllerDescriptors()
	{
		using var scope = new PluginTestScope();
		var application = new ApplicationModel();
		application.Controllers.Add(new ControllerModel(typeof(FixtureController).GetTypeInfo(), []) { ControllerName = "Fixture" });

		new ApplicationConvention().Apply(application);

		Assert.True(scope.Application.Properties.TryGetValue(typeof(ControllerServiceDescriptorCollection), out var descriptors));
		Assert.IsType<ControllerServiceDescriptorCollection>(descriptors);
	}

	public sealed class FixtureController : ControllerBase;
}
