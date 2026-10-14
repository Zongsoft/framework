
using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Plugins.Tests;

public class ApplicationLifecycleTests
{
	[Theory]
	[Trait("Category", "Integration")]
	[InlineData("workbench", new[] { "opening:Opening", "opened:Running", "closing:Closing", "closed:None" })]
	[InlineData("daemon", new[] { "opened", "started", "closed" })]
	[InlineData("terminal", new[] { "created" })]
	[InlineData("web", new[] { "created" })]
	[InlineData("registration", new[] { "registered" })]
	public async Task HostLifecycleRunsInAnIsolatedProcessAsync(string scenario, string[] expected)
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin($"<extension path=\"/Workspace/Environment/Services\"><object name=\"FixtureService\" type=\"{typeof(ProbeService).AssemblyQualifiedName}\" /></extension>");

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

		try
		{
			await process.WaitForExitAsync(timeout.Token);
		}
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
		Assert.True(process.ExitCode == 0, $"ExitCode={process.ExitCode}\n{diagnostics}\n{string.Join('\n', trace)}");
		Assert.Equal(expected, trace);
	}

	[Fact]
	public void InitializationIsIdempotentAndWorkbenchDefersStartupNodes()
	{
		var initializer = new TestInitializer();
		using var scope = new PluginTestScope(configure: services => services.AddSingleton<Services.IApplicationInitializer>(initializer));
		scope.Options.Properties["Mountion:ApplicationContext"] = "/Environment/Context";
		scope.Options.Properties["Mountion:Workbench"] = "/Desktop";
		scope.WritePlugin("<extension path=\"/Desktop\"><object name=\"Ready\" type=\"System.Object\" /></extension><extension path=\"/Desktop/Startup\"><object name=\"Deferred\" type=\"System.Object\" /></extension>");
		var created = 0;
		scope.Application.WorkbenchCreated += (_, _) => created++;

		Assert.True(scope.Application.Initialize());
		Assert.False(scope.Application.Initialize());

		Assert.Equal(1, initializer.Calls);
		Assert.Same(scope.Application, initializer.Context);
		Assert.Same(scope.Application, scope.Tree.Find("/Environment/Context").Value);
		Assert.False(scope.Builtin("/Desktop/Ready").IsBuilded);

		var workbench = scope.Application.Workbench;

		Assert.Same(workbench, scope.Application.Workbench);
		Assert.Same(workbench, scope.Tree.Find("/Desktop").Value);
		Assert.Equal(1, created);
		Assert.True(scope.Builtin("/Desktop/Ready").IsBuilded);
		Assert.False(scope.Builtin("/Desktop/Startup/Deferred").IsBuilded);
	}

	[Fact]
	public void WorkbenchRestoresStateWhenOpeningFailsAndCanRetry()
	{
		using var scope = new PluginTestScope();
		scope.Application.Initialize();
		scope.Tree.EnsurePath("/Workbench");
		using var workbench = new ControlledWorkbench(scope.Application) { FailOpening = true };
		//成功关闭会退出进程；此用例的结束使用取消关闭，成功路径由子进程验证。
		workbench.Closing += (_, args) => args.Cancel = true;

		Assert.Throws<InvalidOperationException>(() => workbench.Open());

		Assert.Equal(WorkbenchStatus.None, workbench.Status);
		Assert.Null(scope.Tree.Find("/Workbench"));
		scope.Tree.EnsurePath("/Workbench");
		workbench.FailOpening = false;
		workbench.Open();

		Assert.Equal(WorkbenchStatus.Running, workbench.Status);
		Assert.Equal(2, workbench.OpenCalls);
	}

	[Fact]
	public void ClosingCancellationAndFailurePreserveRunningState()
	{
		using var scope = new PluginTestScope();
		scope.Application.Initialize();
		scope.Tree.EnsurePath("/Workbench");
		using var workbench = new ControlledWorkbench(scope.Application);
		var cancel = true;
		workbench.Closing += (_, args) => args.Cancel = cancel;
		workbench.Open();

		workbench.Close();

		Assert.Equal(WorkbenchStatus.Running, workbench.Status);
		Assert.Equal(0, workbench.CloseCalls);
		cancel = false;
		workbench.FailClosing = true;

		try
		{
			Assert.Throws<InvalidOperationException>(() => workbench.Close());
			Assert.Equal(WorkbenchStatus.Running, workbench.Status);
			Assert.Equal(1, workbench.CloseCalls);
		}
		finally
		{
			cancel = true;
		}
	}

	[Fact]
	public void WorkbenchCollectsOnlyEnabledStartupWorkers()
	{
		using var scope = new PluginTestScope();
		scope.WritePlugin($"<extension path=\"/Workbench/Startup/Group\"><object name=\"Enabled\" type=\"{typeof(ProbeWorker).AssemblyQualifiedName}\" /><object name=\"Disabled\" type=\"{typeof(ProbeWorker).AssemblyQualifiedName}\" Enabled=\"false\" /></extension>");
		scope.Application.Initialize();
		using var workbench = new ControlledWorkbench(scope.Application);
		workbench.Closing += (_, args) => args.Cancel = true;

		workbench.Open();
		workbench.Open();

		var enabled = Assert.IsType<ProbeWorker>(Assert.Single(scope.Application.Workers));
		Assert.Same(scope.Builtin("/Workbench/Startup/Group/Enabled").Value, enabled);
		Assert.Equal(0, enabled.Starts);
		Assert.True(scope.Builtin("/Workbench/Startup/Group/Disabled").IsBuilded);
	}

	[Fact]
	public void TitleChangedOnlyFiresForChangedValues()
	{
		using var scope = new PluginTestScope();
		using var workbench = new ControlledWorkbench(scope.Application);
		var titles = new List<string>();
		workbench.TitleChanged += (_, _) => titles.Add(workbench.Title);

		workbench.Title = "Fixture";
		workbench.Title = "Fixture";
		workbench.Title = null;
		workbench.Title = "";

		Assert.Equal(new[] { "Fixture", "" }, titles);
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

	public sealed class ProbeWorker(string name) : Components.WorkerBase(name)
	{
		public int Starts { get; private set; }
		protected override Task OnStartAsync(string[] args, CancellationToken cancellation)
		{
			this.Starts++;
			return Task.CompletedTask;
		}
		protected override Task OnStopAsync(string[] args, CancellationToken cancellation) => Task.CompletedTask;
	}

	private sealed class ControlledWorkbench(PluginApplicationContext application) : WorkbenchBase(application)
	{
		public bool FailOpening { get; set; }
		public bool FailClosing { get; set; }
		public int OpenCalls { get; private set; }
		public int CloseCalls { get; private set; }

		protected override void OnOpen()
		{
			this.OpenCalls++;
			if(this.FailOpening)
				throw new InvalidOperationException();

			base.OnOpen();
		}

		protected override void OnClose()
		{
			this.CloseCalls++;
			if(this.FailClosing)
				throw new InvalidOperationException();
		}
	}

	private sealed class TestInitializer : Services.IApplicationInitializer
	{
		public int Calls { get; private set; }
		public Services.IApplicationContext Context { get; private set; }
		public void Initialize(Services.IApplicationContext context)
		{
			this.Calls++;
			this.Context = context;
		}
	}
}
