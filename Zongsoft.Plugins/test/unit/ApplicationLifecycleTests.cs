
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Plugins.Tests;

public class ApplicationLifecycleTests
{
	[Fact]
	public async Task InitializationIsIdempotentAndWorkbenchDefersStartupNodes()
	{
		var initializer = new TestInitializer();
		await using var scope = new PluginTestScope(configure: services => services.AddSingleton<Services.IApplicationInitializer>(initializer));
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
	public async Task InitializerLifetimeBelongsToItsRegistrationOwner()
	{
		var containerOwned = new AsyncInitializer();
		var applicationOwned = new AsyncInitializer();

		await using(var scope = new PluginTestScope(configure: services => services.AddSingleton<Services.IApplicationInitializer>(_ => containerOwned)))
		{
			scope.Application.Initializers.Add(applicationOwned);
			scope.Application.Initialize();

			Assert.Equal(1, containerOwned.Initializations);
			Assert.Equal(1, applicationOwned.Initializations);

			await scope.Application.DisposeAsync();

			Assert.Equal(0, containerOwned.Disposals);
			Assert.Equal(1, applicationOwned.Disposals);
		}

		Assert.Equal(1, containerOwned.Disposals);
		Assert.Equal(1, applicationOwned.Disposals);
	}

	[Fact]
	public async Task WorkbenchRestoresStateWhenOpeningFailsAndCanRetry()
	{
		await using var scope = new PluginTestScope();
		scope.Application.Initialize();
		scope.Tree.EnsurePath("/Workbench");
		using var workbench = new ControlledWorkbench(scope.Application) { FailOpening = true };

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
	public async Task ClosingCancellationAndFailurePreserveRunningState()
	{
		await using var scope = new PluginTestScope();
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
	public async Task WorkbenchRegistersEveryStartupWorkerWithoutStartingIt()
	{
		await using var scope = new PluginTestScope();
		scope.WritePlugin($"<extension path=\"/Workbench/Startup/Group\"><object name=\"Enabled\" type=\"{typeof(ProbeWorker).AssemblyQualifiedName}\" /><object name=\"Disabled\" type=\"{typeof(ProbeWorker).AssemblyQualifiedName}\" Enabled=\"false\" /></extension>");
		scope.Application.Initialize();
		using var workbench = new ControlledWorkbench(scope.Application);
		workbench.Closing += (_, args) => args.Cancel = true;

		workbench.Open();
		workbench.Open();

		Assert.Equal(2, scope.Application.Workers.Count);
		var enabled = Assert.IsType<ProbeWorker>(scope.Builtin("/Workbench/Startup/Group/Enabled").Value);
		var disabled = Assert.IsType<ProbeWorker>(scope.Builtin("/Workbench/Startup/Group/Disabled").Value);
		Assert.Contains(enabled, scope.Application.Workers);
		Assert.Contains(disabled, scope.Application.Workers);
		Assert.False(disabled.Enabled);
		Assert.Equal(0, disabled.Starts);
		Assert.Same(scope.Builtin("/Workbench/Startup/Group/Enabled").Value, enabled);
		Assert.Equal(0, enabled.Starts);
		Assert.True(scope.Builtin("/Workbench/Startup/Group/Disabled").IsBuilded);
	}

	[Fact]
	public async Task TitleChangedOnlyFiresForChangedValues()
	{
		await using var scope = new PluginTestScope();
		using var workbench = new ControlledWorkbench(scope.Application);
		var titles = new List<string>();
		workbench.TitleChanged += (_, _) => titles.Add(workbench.Title);

		workbench.Title = "Fixture";
		workbench.Title = "Fixture";
		workbench.Title = null;
		workbench.Title = "";

		Assert.Equal(new[] { "Fixture", "" }, titles);
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

	private sealed class AsyncInitializer : Services.IApplicationInitializer, IAsyncDisposable
	{
		public int Initializations { get; private set; }
		public int Disposals { get; private set; }
		public void Initialize(Services.IApplicationContext context) => this.Initializations++;
		public ValueTask DisposeAsync()
		{
			this.Disposals++;
			return ValueTask.CompletedTask;
		}
	}


}
