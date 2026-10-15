using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Configuration;
using Zongsoft.Web.Hosting;

using Xunit;

namespace Zongsoft.Plugins.Web.Tests;

internal static partial class EndpointScenarios
{
	private static async Task NetworkChangesWarnOnceWithoutChangingSnapshot(EndpointTestScope scope)
	{
		var port = EndpointTestScope.Port();
		var changes = new ChangeProvider();
		changes.Set("Kestrel:Endpoints:api:Url", $"http://127.0.0.1:{port}/api");
		changes.Set("Kestrel:Endpoints:api:Protocols", "http1");
		var logs = new LogProvider();
		await using var application = Application(scope, builder =>
		{
			((IConfigurationBuilder)builder.Configuration).Add(changes);
			builder.Configuration["Fixture:rescan:enabled"] = "true";
			builder.Logging.AddProvider(logs);
		}, endpoints: false);
		CancelWorkbenchExit(application);
		var endpoints = application.Services.GetRequiredService<WebEndpointCollection>();
		application.Run(context => context.Response.WriteAsync(context.Features.Get<WebEndpointFeature>().Endpoint.Address.Path));
		await application.StartAsync();
		changes.Change("Business:Value", "updated");
		Assert.Empty(logs.Warnings);
		changes.Change("Kestrel:Endpoints:api:Url", $"http://127.0.0.1:{port}/changed");
		Assert.Single(logs.Warnings);
		changes.Change("Kestrel:Endpoints:api:Protocols", "unknown+tcp");
		changes.Change("Kestrel:Endpoints:api:Url", $"http://127.0.0.1:{port}/again");
		var warning = Assert.Single(logs.Warnings);
		Assert.Equal(1, warning.Event.Id);
		Assert.Equal("WebEndpointsChanged", warning.Event.Name);
		Assert.DoesNotContain("/changed", warning.Message, StringComparison.Ordinal);
		Assert.Equal("/api", Assert.Single(endpoints).Address.Path);
		Assert.Equal("http1", Assert.Single(Assert.Single(endpoints).Protocols).ToString());
		using var client = Client(port);
		Assert.Equal("/api", await client.GetStringAsync("/api/item"));
		await Status(client, "/changed/item", HttpStatusCode.NotFound);
		await application.StopAsync();
	}

	private static async Task DisposingHostReleasesUnlatchedMonitor(EndpointTestScope scope, string lifecycle)
	{
		var changes = new ChangeProvider();
		changes.Set("Kestrel:Endpoints:api:Url", $"http://127.0.0.1:{EndpointTestScope.Port()}/api");
		changes.Set("Kestrel:Endpoints:api:Protocols", "http1");
		var logs = new LogProvider();
		var application = Builder(scope, builder =>
		{
			((IConfigurationBuilder)builder.Configuration).Add(changes);
			builder.Logging.AddProvider(logs);
		}, endpoints: false).Build();
		CancelWorkbenchExit(application);
		await application.StartAsync();
		var token = Assert.IsType<ConfigurationReloadToken>(((IConfiguration)application.Configuration).GetReloadToken());
		Assert.Empty(logs.Warnings);
		//不触发通知，保留尚未锁存的监听器；释放后触发当时的公开根令牌。
		changes.Set("Kestrel:Endpoints:api:Url", "http://127.0.0.1:18082/changed");

		if(lifecycle == "stop")
		{
			await application.StopAsync();
			changes.Change("Kestrel:Endpoints:api:Url", "http://127.0.0.1:18083/stopped");
			Assert.Empty(logs.Warnings);
		}

		await application.DisposeAsync();
		token.OnReload();
		Assert.Empty(logs.Warnings);
	}

	private static async Task NewPluginConflictRequiresRestart(EndpointTestScope scope)
	{
		var port = EndpointTestScope.Port();
		var option = EndpointTestScope.Endpoint("api", $"http://127.0.0.1:{port}/api");
		scope.Plugin("Alpha", option);
		scope.Plugin("Beta", option);
		var logs = new LogProvider();
		await using var application = Builder(scope, builder => builder.Logging.AddProvider(logs), monitoring: true).Build();
		CancelWorkbenchExit(application);
		await application.StartAsync();
		var provider = Assert.Single(((IConfigurationRoot)application.Configuration).Providers.OfType<PluginConfigurationProvider>());
		((IConfigurationBuilder)application.Configuration).AddInMemoryCollection(new Dictionary<string, string>
		{
			["Kestrel:Endpoints:api:Url"] = $"http://127.0.0.1:{port}/api",
		});
		Assert.Empty(logs.Warnings);
		//仅改变一个插件的最终值，即使跨插件最终枚举值没变化也必须报告冲突。
		var file = provider.Providers.Cast<CompositeConfigurationProvider>().SelectMany(composite => composite.Providers).OfType<FileConfigurationProvider>()
			.Single(candidate => candidate.Source.Path.EndsWith("Beta.option", StringComparison.Ordinal));
		scope.Write("plugins/Beta.option", EndpointTestScope.Endpoint("api", "http://127.0.0.1:18082/conflicting"));
		file.Load();
		var warning = Assert.Single(logs.Warnings);
		Assert.Equal("WebEndpointsChanged", warning.Event.Name);
		Assert.Equal($"http://127.0.0.1:{port}/api", application.Configuration["Kestrel:Endpoints:api:Url"]);
		Assert.Equal("/api", Assert.Single(application.Services.GetRequiredService<WebEndpointCollection>()).Address.Path);
		await application.StopAsync();
	}

	private static Task InitializerFailureDisposesApplication(EndpointTestScope scope)
	{
		var changes = new ChangeProvider();
		changes.Set("Kestrel:Endpoints:api:Url", $"http://127.0.0.1:{EndpointTestScope.Port()}/api");
		changes.Set("Kestrel:Endpoints:api:Protocols", "http1");
		var logs = new LogProvider();
		var disposable = new DisposalProbe();
		var initializer = new FailingInitializer();
		var error = Assert.Throws<InvalidOperationException>(() => Application(scope, builder =>
		{
			((IConfigurationBuilder)builder.Configuration).Add(changes);
			builder.Logging.AddProvider(logs);
			builder.Services.AddSingleton(_ => disposable);
			builder.Services.AddSingleton<Zongsoft.Services.IApplicationInitializer<Microsoft.AspNetCore.Builder.IApplicationBuilder>>(initializer);
		}));
		Assert.Same(initializer.Error, error);
		Assert.Equal(1, disposable.Disposals);
		Assert.NotNull(initializer.Token);
		changes.Set("Kestrel:Endpoints:api:Url", "http://127.0.0.1:18082/changed");
		initializer.Token.OnReload();
		Assert.Empty(logs.Warnings);
		return Task.CompletedTask;
	}

	private static async Task MonitoringIsOptional(EndpointTestScope scope)
	{
		var port = EndpointTestScope.Port();
		var changes = new ChangeProvider();
		changes.Set("Kestrel:Endpoints:api:Url", $"http://127.0.0.1:{port}/api");
		changes.Set("Kestrel:Endpoints:api:Protocols", "http1");
		var logs = new LogProvider();
		await using var application = Builder(scope, builder =>
		{
			((IConfigurationBuilder)builder.Configuration).Add(changes);
			builder.Logging.AddProvider(logs);
		}).Build();
		CancelWorkbenchExit(application);
		application.Run(context => context.Response.WriteAsync("snapshot"));
		await application.StartAsync();
		changes.Change("Kestrel:Endpoints:api:Url", $"http://127.0.0.1:{port}/changed");
		Assert.Empty(logs.Warnings);
		Assert.Equal("/api", Assert.Single(application.Services.GetRequiredService<WebEndpointCollection>()).Address.Path);
		using var client = Client(port);
		Assert.Equal("snapshot", await client.GetStringAsync("/api/item"));
		await Status(client, "/changed/item", HttpStatusCode.NotFound);
		await application.StopAsync();
	}

	private sealed class ChangeProvider : ConfigurationProvider, IConfigurationSource
	{
		public IConfigurationProvider Build(IConfigurationBuilder builder) => this;
		public void Change(string key, string value)
		{
			this.Set(key, value);
			this.OnReload();
		}
	}

	private sealed class LogProvider : ILoggerProvider
	{
		public ConcurrentQueue<LogEntry> Entries { get; } = new();
		public IEnumerable<LogEntry> Warnings => this.Entries.Where(entry => entry.Level == LogLevel.Warning && entry.Event.Name == "WebEndpointsChanged");
		public ILogger CreateLogger(string categoryName) => new TestLogger(this);
		public void Dispose() { }

		private sealed class TestLogger(LogProvider provider) : ILogger
		{
			public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;
			public bool IsEnabled(LogLevel level) => true;
			public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) =>
				provider.Entries.Enqueue(new(level, eventId, formatter(state, exception)));
		}
	}

	private sealed record LogEntry(LogLevel Level, EventId Event, string Message);

	private sealed class DisposalProbe : IDisposable
	{
		public int Disposals { get; private set; }
		public void Dispose() => this.Disposals++;
	}

	private sealed class FailingInitializer : Zongsoft.Services.IApplicationInitializer<Microsoft.AspNetCore.Builder.IApplicationBuilder>
	{
		public InvalidOperationException Error { get; } = new("Fixture initialization failed.");
		public ConfigurationReloadToken Token { get; private set; }

		public void Initialize(Microsoft.AspNetCore.Builder.IApplicationBuilder application)
		{
			application.ApplicationServices.GetRequiredService<DisposalProbe>();
			this.Token = Assert.IsType<ConfigurationReloadToken>(application.ApplicationServices.GetRequiredService<IConfiguration>().GetReloadToken());
			throw this.Error;
		}
	}
}
