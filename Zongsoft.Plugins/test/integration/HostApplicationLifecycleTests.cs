
using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Plugins.IntegrationTests;

public class HostApplicationLifecycleTests
{
	[Theory]
	[Trait("Category", "Integration")]
	[InlineData("workbench", new[] { "opening:Opening", "opened:Running", "closing:Closing", "closed:None", "returned" })]
	[InlineData("daemon", new[] { "opened", "started", "closed", "stopped" })]
	[InlineData("terminal", new[] { "created" })]
	[InlineData("registration", new[] { "registered" })]
	[InlineData("environment", new[] { "environment" })]
	public Task HostLifecycleRunsInAnIsolatedProcessAsync(string scenario, string[] expected) => RunScenarioAsync(scenario, expected);

	[Fact]
	[Trait("Category", "Integration")]
	public Task HostInitializationFailureWaitsForAsyncResources() => RunScenarioAsync("host-initializer-failure", ["host-initializer-failure"]);

	internal static async Task RunInitializationFailureAsync(string directory)
	{
		var disposable = new AsyncResource();
		var initializer = new FailingInitializer();
		var initialization = Hosting.Application.DaemonAsync(typeof(TestProgram).Assembly.GetName().Name, [$"--contentRoot={directory}"], builder =>
		{
			builder.Environment.EnvironmentName = "Development";
			builder.Services.AddSingleton(_ => disposable);
			builder.Services.AddSingleton<Services.IApplicationInitializer>(initializer);
		}).AsTask();

		try
		{
			await disposable.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10));

			Assert.False(initialization.IsCompleted);
			Assert.Equal(1, disposable.Disposals);
			disposable.Release.TrySetResult();
			var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => initialization.WaitAsync(TimeSpan.FromSeconds(10)));

			Assert.Same(initializer.Failure, failure);
			Assert.Equal(1, disposable.Disposals);
		}
		finally { disposable.Release.TrySetResult(); }
	}

	internal static async Task RunScenarioAsync(string scenario, string[] expected)
	{
		using var scope = new HostTestDirectory();
		scope.WritePlugin($"<extension path=\"/Workspace/Environment/Services\"><object name=\"FixtureService\" type=\"{typeof(ProbeService).AssemblyQualifiedName}\" /></extension>");

		if(scenario.StartsWith("terminal-loop-", StringComparison.Ordinal))
			scope.WritePlugin($"<extension path=\"/Workbench\"><object name=\"Executor\" type=\"{typeof(TerminalLifecycleTests.ControlledExecutor).AssemblyQualifiedName}\"><object.property name=\"Executor\" target=\"{{path:..}}\" value=\"{{path:.}}\" /></object></extension>");

		if(scenario == "terminal-shipped-commands")
		{
			scope.WriteFile("Main.plugin", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Main.plugin")));
			scope.WriteFile("Terminal.plugin", File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Terminal.plugin")));
		}

		if(scenario == "registration")
		{
			var assembly = typeof(RegistrationProbe).Assembly.GetName().Name;
			scope.WriteFile("Feature.plugin", $"<plugin name=\"Feature\"><manifest><assemblies><assembly name=\"{assembly}\" /></assemblies></manifest></plugin>");
			scope.WriteFile("nested/Child.plugin", $"<plugin name=\"Child\"><manifest><assemblies><assembly name=\"{assembly}\" /></assemblies></manifest></plugin>");
			scope.WriteFile("nested/Child.option", "<options><option path=\"Kestrel/Endpoints\"><telemetry url=\"http://*:4317/opentelemetry\" protocols=\"grpc+http2\" /></option></options>");
		}

		var start = new ProcessStartInfo("dotnet")
		{
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			WorkingDirectory = scope.DirectoryPath,
		};
		start.ArgumentList.Add(typeof(TestProgram).Assembly.Location);
		start.ArgumentList.Add("--plugin-probe");
		start.ArgumentList.Add(scenario);
		start.ArgumentList.Add(scope.DirectoryPath);

		using var process = Process.Start(start);
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
		var output = process.StandardOutput.ReadToEndAsync();
		var error = process.StandardError.ReadToEndAsync();
		var timedOut = false;

		try
		{
			await process.WaitForExitAsync(timeout.Token);
		}
		catch(OperationCanceledException) when(timeout.IsCancellationRequested) { timedOut = true; }
		finally
		{
			if(!process.HasExited)
			{
				process.Kill(true);
				await process.WaitForExitAsync();
			}
		}

		var diagnostics = await output + await error;
		var tracePath = Path.Combine(scope.DirectoryPath, "lifecycle.txt");
		var trace = File.Exists(tracePath) ? File.ReadAllLines(tracePath) : [];
		Assert.False(timedOut, $"{scenario}: child process timed out.\n{diagnostics}\n{string.Join('\n', trace)}");
		Assert.True(process.ExitCode == 0, $"ExitCode={process.ExitCode}\n{diagnostics}\n{string.Join('\n', trace)}");
		Assert.Equal(expected, trace);
	}

	public sealed class ProbeService;

	public sealed class RegistrationProbe : Services.IServiceRegistration
	{
		public void Register(IServiceCollection services, IConfiguration configuration)
		{
			if(configuration?["Fixture:registration:enabled"] == "true")
				services.AddSingleton(new RegistrationSnapshot(configuration["Kestrel:Endpoints:telemetry:Url"], configuration["Kestrel:Endpoints:telemetry:Protocols"]));
		}
	}

	public sealed class RegistrationSnapshot(string url, string protocols)
	{
		public string Url { get; } = url;
		public string Protocols { get; } = protocols;
	}

	private sealed class FailingInitializer : Services.IApplicationInitializer
	{
		public InvalidOperationException Failure { get; } = new("Fixture initialization failed.");
		public void Initialize(Services.IApplicationContext context)
		{
			context.Services.GetRequiredService<AsyncResource>();
			throw this.Failure;
		}
	}

	private sealed class AsyncResource : IAsyncDisposable
	{
		public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public int Disposals { get; private set; }
		public async ValueTask DisposeAsync()
		{
			this.Disposals++;
			this.Entered.TrySetResult();
			await this.Release.Task;
		}
	}
}
