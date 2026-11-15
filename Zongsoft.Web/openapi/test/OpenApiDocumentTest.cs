using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.DependencyInjection;

using YamlDotNet.RepresentationModel;

using Zongsoft.Configuration;
using Zongsoft.Services;

namespace Zongsoft.Web.OpenApi.Tests;

public class OpenApiDocumentTest
{
	[Fact]
	public async Task WritesTheSameTagTreeAndPathsInJsonAndYaml()
	{
		var cancellation = TestContext.Current.CancellationToken;
		var builder = WebApplication.CreateBuilder();
		using var options = new MemoryStream(Encoding.UTF8.GetBytes("""
			<options>
				<option path="/web/openapi">
					<environments>
						<environment environment.name="testing">
							<variable variable.name="host" default="localhost" />
						</environment>
					</environments>
				</option>
			</options>
			"""));
		builder.Configuration.AddOptionStream(options);
		builder.Host.UseServiceProviderFactory(new ServiceProviderFactory());
		builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
		builder.Services.AddControllers(options => options.Conventions.Add(new CaptureControllers()))
			.AddApplicationPart(typeof(CatalogController).Assembly)
			.ConfigureApplicationPartManager(parts => parts.FeatureProviders.Add(new ControllerFeatureProvider()));

		await using var app = builder.Build();
		await using var application = new TestApplication(app.Services);
		app.MapControllers();
		app.MapControllerRoute("default", "conventional/{controller}/{action}");
		app.UseOpenApi();
		await app.StartAsync(cancellation);

		using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
		var documents = await Task.WhenAll(
			client.GetStringAsync("/openapi/v1.yaml", cancellation),
			client.GetStringAsync("/openapi/v1.json", cancellation),
			client.GetStringAsync("/openapi/v1.yml", cancellation));
		Assert.Equal(documents[0], documents[2]);

		var json = JsonNode.Parse(documents[1]);
		var yaml = new YamlStream();
		using var reader = new StringReader(documents[0]);
		yaml.Load(reader);
		var root = Assert.IsType<YamlMappingNode>(Assert.Single(yaml.Documents).RootNode);
		Assert.Equal("3.2.1", json["openapi"].GetValue<string>());
		Assert.Equal("3.2.1", Scalar(root["openapi"]));
		Assert.Equal("3.2.0", json["info"]["version"].GetValue<string>());
		Assert.Equal("3.2.0", Scalar(Assert.IsType<YamlMappingNode>(root["info"])["version"]));
		Assert.Equal("/", Assert.Single(json["servers"].AsArray())["url"].GetValue<string>());

		var environment = json["x-scalar-environments"]["testing"];
		var variable = Assert.Single(environment["variables"].AsArray());
		Assert.Equal("host", variable["name"].GetValue<string>());
		Assert.Equal("localhost", variable["value"]["default"].GetValue<string>());

		var jsonTags = new List<(string Name, string Parent)>();

		foreach(var tag in json["tags"].AsArray())
		{
			var name = tag["name"].GetValue<string>();
			var parent = tag["parent"]?.GetValue<string>();
			jsonTags.Add((name, parent));
		}

		var yamlTags = new List<(string Name, string Parent)>();
		var yamlTagNodes = Assert.IsType<YamlSequenceNode>(root["tags"]);

		foreach(YamlMappingNode tag in yamlTagNodes.Children)
		{
			var name = Scalar(tag["name"]);
			var parent = tag.Children.TryGetValue(new YamlScalarNode("parent"), out var value) ? Scalar(value) : null;
			yamlTags.Add((name, parent));
		}

		var orderedTags = jsonTags.OrderBy(tag => tag.Name, StringComparer.Ordinal);
		Assert.Equal(jsonTags, yamlTags);
		Assert.Equal(orderedTags, jsonTags);
		Assert.Contains(("module/Catalog", null), jsonTags);
		Assert.Contains(("Catalog:Catalog", "module/Catalog"), jsonTags);
		Assert.Contains(("namespace/Catalog:Catalog.Inventory", "Catalog:Catalog"), jsonTags);
		Assert.Contains(("namespace/Catalog:Catalog.Inventory.Warehouses", "namespace/Catalog:Catalog.Inventory"), jsonTags);
		Assert.Contains(("Catalog:Catalog.Inventory.Warehouses.Items", "namespace/Catalog:Catalog.Inventory.Warehouses"), jsonTags);
		Assert.Contains(("namespace/Catalog:Catalog.Returns.Warehouses", "namespace/Catalog:Catalog.Returns"), jsonTags);
		Assert.All(json["tags"].AsArray(), tag => Assert.Equal("nav", tag["kind"].GetValue<string>()));

		var paths = json["paths"].AsObject();
		var yamlPaths = Assert.IsType<YamlMappingNode>(root["paths"]);
		var jsonPathNames = paths.Select(path => path.Key).ToArray();
		var yamlPathNames = yamlPaths.Children.Keys.Select(Scalar);
		Assert.Equal(jsonPathNames, yamlPathNames);
		Assert.Equal("Catalog:Catalog", paths["/catalog/inventory"]["get"]["tags"][0].GetValue<string>());
		Assert.Equal("Catalog:Catalog.ListInventory", paths["/catalog/inventory"]["get"]["operationId"].GetValue<string>());
		Assert.Equal("Catalog:Catalog.ListInventory.GET.%2Fcatalog%2Finventory-alias", paths["/catalog/inventory-alias"]["get"]["operationId"].GetValue<string>());
		Assert.Equal("Catalog:Catalog.Inventory.Warehouses.Items", paths["/catalog/warehouses/{id}"]["get"]["tags"][0].GetValue<string>());
		Assert.True(paths["/catalog/warehouses/{id}"]["get"]["parameters"][0]["required"].GetValue<bool>());
		Assert.NotNull(paths["/catalog/warehouses/{id}"]["get"]["responses"]["default"]["description"]);
		Assert.Contains("/returns/items", jsonPathNames);
		Assert.Contains("/conventional/Browse/List", jsonPathNames);
		Assert.Equal(documents[1], await client.GetStringAsync("/openapi/v1.json", cancellation));

		static string Scalar(YamlNode node) => Assert.IsType<YamlScalarNode>(node).Value;
	}

	[Fact]
	public async Task ScalarEnvironmentDocumentsUseTheirOwnServerAndVariables()
	{
		var cancellation = TestContext.Current.CancellationToken;
		var builder = WebApplication.CreateBuilder();
		using var options = new MemoryStream(Encoding.UTF8.GetBytes("""
			<options>
				<option path="/web/openapi">
					<authentication persisted="true">
						<authenticator authenticator.name="credential" kind="http" scheme="Credential" />
					</authentication>
					<environments>
						<environment environment.name="development" description="Dev profile" url="http://localhost:58100/">
							<variable variable.name="scenario" default="development-value" />
						</environment>
						<environment environment.name="demo-a" description="Demo profile" url="http://localhost:58101/gateway/api/">
							<variable variable.name="scenario" default="demo-value" />
						</environment>
						<environment environment.name="variables-only">
							<variable variable.name="scenario" default="local" />
						</environment>
					</environments>
				</option>
			</options>
			"""));
		builder.Configuration.AddOptionStream(options);
		builder.Host.UseServiceProviderFactory(new ServiceProviderFactory());
		builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
		builder.Services.AddControllers(options => options.Conventions.Add(new CaptureControllers()))
			.AddApplicationPart(typeof(CatalogController).Assembly);
		await using var app = builder.Build();
		await using var application = new TestApplication(app.Services);
		app.UsePathBase("/gateway");
		app.MapControllers();
		new WebInitializer().Initialize(app);
		await app.StartAsync(cancellation);

		using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
		var documents = await Task.WhenAll(
			client.GetStringAsync("/gateway/openapi/environments/development.json", cancellation),
			client.GetStringAsync("/gateway/openapi/environments/demo-a.json", cancellation),
			client.GetStringAsync("/gateway/openapi/environments/demo-a.yaml", cancellation),
			client.GetStringAsync("/gateway/openapi/v1.json", cancellation));
		var development = JsonNode.Parse(documents[0]);
		var demo = JsonNode.Parse(documents[1]);
		var manual = JsonNode.Parse(documents[3]);
		AssertEnvironment(development, "development", "http://localhost:58100", "development-value");
		AssertEnvironment(demo, "demo-a", "http://localhost:58101/gateway/api", "demo-value");
		Assert.True(JsonNode.DeepEquals(manual["paths"], development["paths"]));
		var credential = development["components"]["securitySchemes"]["credential"];
		Assert.Equal("apiKey", credential["type"].GetValue<string>());
		Assert.Equal("Authorization", credential["name"].GetValue<string>());
		Assert.Equal("header", credential["in"].GetValue<string>());
		Assert.True(JsonNode.DeepEquals(manual["tags"], demo["tags"]));
		Assert.Equal("/gateway", Assert.Single(manual["servers"].AsArray())["url"].GetValue<string>());
		Assert.NotNull(manual["x-scalar-environments"]["variables-only"]);
		Assert.Equal(documents[0], await client.GetStringAsync("/gateway/openapi/environments/DEVELOPMENT.json", cancellation));

		var yaml = new YamlStream();
		using var reader = new StringReader(documents[2]);
		yaml.Load(reader);
		var root = (YamlMappingNode)yaml.Documents[0].RootNode;
		var server = (YamlMappingNode)Assert.Single(((YamlSequenceNode)root["servers"]).Children);
		Assert.Equal("demo-a", ((YamlScalarNode)server["name"]).Value);
		Assert.Equal("http://localhost:58101/gateway/api", ((YamlScalarNode)server["url"]).Value);
		Assert.Equal("demo-a", ((YamlScalarNode)root["x-scalar-active-environment"]).Value);

		var scalar = await client.GetStringAsync("/gateway/scalar/", cancellation);
		Assert.Contains("\"sources\":[{\"title\":\"none\",\"url\":\"openapi/v1.json\",\"default\":true}", scalar);
		Assert.Contains("\"title\":\"development\",\"url\":\"openapi/environments/development.json\"", scalar);
		Assert.Contains("\"title\":\"demo-a\",\"url\":\"openapi/environments/demo-a.json\"", scalar);
		Assert.True(scalar.IndexOf("openapi/environments/development.json", StringComparison.Ordinal) <
		            scalar.IndexOf("openapi/environments/demo-a.json", StringComparison.Ordinal));
		Assert.Contains("\"persistAuth\":true", scalar);
		Assert.DoesNotContain("variables-only.json", scalar);

		using var unknown = await client.GetAsync("/gateway/openapi/environments/unknown.json", cancellation);
		Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
		using var variablesOnly = await client.GetAsync("/gateway/openapi/environments/variables-only.json", cancellation);
		Assert.Equal(HttpStatusCode.NotFound, variablesOnly.StatusCode);

		static void AssertEnvironment(JsonNode document, string name, string url, string scenario)
		{
			var server = Assert.Single(document["servers"].AsArray());
			Assert.Equal(name, server["name"].GetValue<string>());
			Assert.Equal(url, server["url"].GetValue<string>());
			Assert.Null(server["variables"]);
			Assert.Equal(name, document["x-scalar-active-environment"].GetValue<string>());
			var environment = Assert.Single(document["x-scalar-environments"].AsObject());
			Assert.Equal(name, environment.Key);
			var variable = Assert.Single(environment.Value["variables"].AsArray());
			Assert.Equal("scenario", variable["name"].GetValue<string>());
			Assert.Equal(scenario, variable["value"]["default"].GetValue<string>());
		}
	}

	[Fact]
	public async Task DefaultServersOfferSiteDropdown()
	{
		var cancellation = TestContext.Current.CancellationToken;
		var builder = WebApplication.CreateBuilder();
		using var options = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Zongsoft.Web.OpenApi.option"));
		builder.Configuration.AddOptionStream(options);
		builder.Host.UseServiceProviderFactory(new ServiceProviderFactory());
		builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
		builder.Services.AddControllers(options => options.Conventions.Add(new CaptureControllers()))
			.AddApplicationPart(typeof(CatalogController).Assembly);
		await using var app = builder.Build();
		await using var application = new TestApplication(app.Services);
		app.MapControllers();
		new WebInitializer().Initialize(app);
		await app.StartAsync(cancellation);
		using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
		var documents = await Task.WhenAll(
			client.GetStringAsync("/openapi/v1.json", cancellation),
			client.GetStringAsync("/openapi/v1.yaml", cancellation));
		var jsonRoot = JsonNode.Parse(documents[0]);
		var servers = jsonRoot["servers"].AsArray();
		var serverNames = servers.Select(server => server["name"].GetValue<string>());
		Assert.Equal(["current", "ports", "sites"], serverNames);
		string[] authenticators = ["credential", "bearer", "basic"];
		Assert.Equal(authenticators, jsonRoot["components"]["securitySchemes"].AsObject().Select(scheme => scheme.Key));
		Assert.Equal("/", servers[0]["url"].GetValue<string>());
		var scalar = await client.GetStringAsync("/scalar/", cancellation);
		Assert.Contains("\"title\":\"none\",\"url\":\"openapi/v1.json\",\"default\":true", scalar);
		Assert.Contains("\"title\":\"production-a\",\"url\":\"openapi/environments/production-a.json\"", scalar);
		Assert.Contains("\"title\":\"development-b\",\"url\":\"openapi/environments/development-b.json\"", scalar);
		Assert.Contains("\"title\":\"staging-c\",\"url\":\"openapi/environments/staging-c.json\"", scalar);
		Assert.DoesNotContain("openapi/environments/testing.json", scalar);

		var yaml = new YamlStream();
		using var reader = new StringReader(documents[1]);
		yaml.Load(reader);
		var yamlRoot = (YamlMappingNode)yaml.Documents[0].RootNode;
		var yamlSecuritySchemes = (YamlMappingNode)((YamlMappingNode)yamlRoot["components"])["securitySchemes"];
		Assert.Equal(authenticators, yamlSecuritySchemes.Children.Keys.Cast<YamlScalarNode>().Select(scheme => scheme.Value));
		var yamlServers = (YamlSequenceNode)yamlRoot["servers"];
		Assert.Equal(servers.Count, yamlServers.Children.Count);

		for(var index = 1; index < servers.Count; index++)
		{
			var name = servers[index]["name"].GetValue<string>();
			var url = name == "sites" ? "{scheme}://{site}.{host}:{port}" : "{scheme}://{host}:{port}";
			Assert.Equal(url, servers[index]["url"].GetValue<string>());

			string[] expected = name == "sites" ? ["scheme", "site", "host", "port"] : ["scheme", "host", "port"];
			var variables = servers[index]["variables"].AsObject();
			var variableNames = variables.Select(variable => variable.Key);
			Assert.Equal(expected, variableNames);

			var yamlServer = (YamlMappingNode)yamlServers.Children[index];
			Assert.Equal(name, ((YamlScalarNode)yamlServer["name"]).Value);
			Assert.Equal(url, ((YamlScalarNode)yamlServer["url"]).Value);
			var yamlVariables = (YamlMappingNode)yamlServer["variables"];
			var yamlVariableNames = yamlVariables.Children.Keys.Cast<YamlScalarNode>().Select(variable => variable.Value);
			Assert.Equal(expected, yamlVariableNames);
			Assert.Equal("http", variables["scheme"]["default"].GetValue<string>());
			var schemes = variables["scheme"]["enum"].AsArray().Select(value => value.GetValue<string>());
			Assert.Equal(["http", "https"], schemes);

			var yamlScheme = (YamlMappingNode)yamlVariables["scheme"];
			var yamlEnum = (YamlSequenceNode)yamlScheme["enum"];
			var yamlSchemes = yamlEnum.Children.Cast<YamlScalarNode>().Select(value => value.Value);
			Assert.Equal(["http", "https"], yamlSchemes);

			foreach(var variable in new[] { "host", "port" })
			{
				Assert.Null(variables[variable]["enum"]);
				Assert.False(((YamlMappingNode)yamlVariables[variable]).Children.ContainsKey(new YamlScalarNode("enum")));
			}

			if(name == "sites")
			{
				string[] expectedSites = ["api.a", "api.b", "api.c", "api.iot", "api.gateway"];
				var site = variables["site"];
				var siteNames = site["enum"].AsArray().Select(value => value.GetValue<string>());
				Assert.Equal("api.b", site["default"].GetValue<string>());
				Assert.Equal(expectedSites, siteNames);

				var yamlSite = (YamlMappingNode)yamlVariables["site"];
				var yamlSiteEnum = (YamlSequenceNode)yamlSite["enum"];
				var yamlSiteNames = yamlSiteEnum.Children.Cast<YamlScalarNode>().Select(value => value.Value);
				Assert.Equal("api.b", ((YamlScalarNode)yamlSite["default"]).Value);
				Assert.Equal(expectedSites, yamlSiteNames);
			}

			Assert.Equal(name == "sites" ? "zongsoft.com" : "localhost", variables["host"]["default"].GetValue<string>());
			Assert.Equal(name == "sites" ? "80" : "8069", variables["port"]["default"].GetValue<string>());
		}
	}

	[Theory]
	[InlineData(null)]
	[InlineData("/public/api")]
	[InlineData("https://api.example.test/base")]
	public async Task ServersSupportCurrentSiteCustomAddressesAndFreeVariables(string currentUrl)
	{
		var cancellation = TestContext.Current.CancellationToken;
		var builder = WebApplication.CreateBuilder();
		using var options = new MemoryStream(Encoding.UTF8.GetBytes($$"""
			<options>
				<option path="/web/openapi">
					<servers>
						<server server.name="z-custom" description="Custom test address" url="{baseUrl}">
							<variable variable.name="baseUrl" default="/" description="Full URL" />
						</server>
						<server server.name="host-port" url="{scheme}://{host}:{port}">
							<variable variable.name="port" default="8069" />
							<variable variable.name="host" default="localhost" />
							<variable variable.name="scheme" default="http"><value>http</value><value>https</value></variable>
						</server>
						<server server.name="CURRENT" url="{{currentUrl}}" description="Current test site" />
					</servers>
				</option>
			</options>
			"""));
		builder.Configuration.AddOptionStream(options);
		builder.Host.UseServiceProviderFactory(new ServiceProviderFactory());
		builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
		builder.Services.AddControllers(options => options.Conventions.Add(new CaptureControllers()))
			.AddApplicationPart(typeof(CatalogController).Assembly);

		await using var app = builder.Build();
		await using var application = new TestApplication(app.Services);
		app.UsePathBase("/gateway");
		app.UsePathBase("/edge");
		app.MapControllers();
		app.UseOpenApi();
		await app.StartAsync(cancellation);
		using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };

		foreach(var prefix in new[] { "/gateway", "", "/edge", "/gateway" })
		{
			var documents = await Task.WhenAll(
				client.GetStringAsync($"{prefix}/openapi/v1.json", cancellation),
				client.GetStringAsync($"{prefix}/openapi/v1.yaml", cancellation));
			var servers = JsonNode.Parse(documents[0])["servers"].AsArray();
			var serverNames = servers.Select(server => server["name"].GetValue<string>());
			Assert.Equal(["CURRENT", "z-custom", "host-port"], serverNames);
			Assert.Equal(currentUrl ?? (prefix.Length == 0 ? "/" : prefix), servers[0]["url"].GetValue<string>());
			Assert.Equal("Current test site", servers[0]["description"].GetValue<string>());
			Assert.Equal("{baseUrl}", servers[1]["url"].GetValue<string>());
			Assert.Equal("Custom test address", servers[1]["description"].GetValue<string>());
			Assert.Equal("Full URL", servers[1]["variables"]["baseUrl"]["description"].GetValue<string>());
			Assert.Equal("/", servers[1]["variables"]["baseUrl"]["default"].GetValue<string>());
			Assert.Null(servers[1]["variables"]["baseUrl"]["enum"]);
			Assert.Equal("host-port", servers[2]["description"].GetValue<string>());
			Assert.Equal("{scheme}://{host}:{port}", servers[2]["url"].GetValue<string>());
			var variableNames = servers[2]["variables"].AsObject().Select(variable => variable.Key);
			Assert.Equal(["scheme", "host", "port"], variableNames);
			var jsonScheme = servers[2]["variables"]["scheme"]["enum"].AsArray();
			var jsonSchemeValues = jsonScheme.Select(value => value.GetValue<string>());
			Assert.Equal(["http", "https"], jsonSchemeValues);
			Assert.Null(servers[2]["variables"]["host"]["enum"]);
			Assert.Null(servers[2]["variables"]["port"]["enum"]);

			var yaml = new YamlStream();
			using var reader = new StringReader(documents[1]);
			yaml.Load(reader);
			var yamlServers = Assert.IsType<YamlSequenceNode>(((YamlMappingNode)yaml.Documents[0].RootNode)["servers"]);
			Assert.Equal(servers.Count, yamlServers.Children.Count);

			for(var index = 0; index < servers.Count; index++)
			{
				var yamlServer = Assert.IsType<YamlMappingNode>(yamlServers.Children[index]);
				foreach(var field in new[] { "name", "url", "description" })
					Assert.Equal(servers[index][field].GetValue<string>(), ((YamlScalarNode)yamlServer[field]).Value);
			}

			var yamlVariables = (YamlMappingNode)((YamlMappingNode)yamlServers.Children[2])["variables"];
			Assert.False(((YamlMappingNode)yamlVariables["host"]).Children.ContainsKey(new YamlScalarNode("enum")));
			Assert.False(((YamlMappingNode)yamlVariables["port"]).Children.ContainsKey(new YamlScalarNode("enum")));
			var yamlScheme = (YamlMappingNode)yamlVariables["scheme"];
			var yamlSchemeValues = ((YamlSequenceNode)yamlScheme["enum"]).Children.Cast<YamlScalarNode>()
				.Select(value => value.Value);
			Assert.Equal(["http", "https"], yamlSchemeValues);
			Assert.Equal("inventory", await client.GetStringAsync($"{prefix}/catalog/inventory", cancellation));
		}
	}

	[Theory]
	[InlineData("ftp")]
	[InlineData("HTTP")]
	public async Task RejectsVariableDefaultsOutsideTheDeclaredEnum(string defaultValue)
	{
		var cancellation = TestContext.Current.CancellationToken;
		var builder = WebApplication.CreateBuilder();
		using var options = new MemoryStream(Encoding.UTF8.GetBytes($$"""
			<options><option path="/web/openapi"><servers>
				<server server.name="restricted" url="{scheme}://localhost">
					<variable variable.name="scheme" default="{{defaultValue}}"><value>http</value><value>https</value></variable>
				</server>
			</servers></option></options>
			"""));
		builder.Configuration.AddOptionStream(options);
		builder.Host.UseServiceProviderFactory(new ServiceProviderFactory());
		builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
		builder.Services.AddControllers(options => options.Conventions.Add(new CaptureControllers()))
			.AddApplicationPart(typeof(CatalogController).Assembly);
		await using var app = builder.Build();
		await using var application = new TestApplication(app.Services);
		Exception failure = null;
		app.Use(async (context, next) =>
		{
			try { await next(context); }
			catch(Exception exception)
			{
				failure = exception;
				context.Response.StatusCode = StatusCodes.Status500InternalServerError;
			}
		});
		app.MapControllers();
		app.UseOpenApi();
		await app.StartAsync(cancellation);
		using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
		using var response = await client.GetAsync("/openapi/v1.json", cancellation);
		Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
		Assert.IsType<InvalidOperationException>(failure);
	}

	private sealed class CaptureControllers : IApplicationModelConvention
	{
		public void Apply(ApplicationModel application) => ApplicationContext.Current.Properties[typeof(ControllerServiceDescriptorCollection)] = new ControllerServiceDescriptorCollection(application.Controllers);
	}

	private sealed class TestApplication(IServiceProvider services) : ApplicationContext(services)
	{
		protected override Version GetVersion() => new(3, 2, 0);
	}
}
