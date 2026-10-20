using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

namespace Zongsoft.Plugins.Tests;

public class AsyncPluginDisposalTests
{
	private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(10);

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task DestroyAsyncWaitsForValueAndClearsCacheOnce(bool synchronousAlternative)
	{
		await using var scope = new PluginTestScope();
		var type = synchronousAlternative ? typeof(DualDisposable) : typeof(AsyncDisposable);
		scope.WritePlugin($"<builders><builder name=\"object\" type=\"{typeof(BuilderLifecycleTests.RecordingBuilder).AssemblyQualifiedName}\" /></builders><extension path=\"/Items\"><object name=\"Item\" type=\"{type.AssemblyQualifiedName}\" /></extension>");
		scope.Tree.Load();
		var builder = Assert.IsType<BuilderLifecycleTests.RecordingBuilder>(scope.Tree.Plugins["Feature"].GetBuilder("object"));
		var builtin = scope.Builtin("/Items/Item");
		var value = Assert.IsAssignableFrom<AsyncDisposable>(builtin.Build());
		var context = Assert.Single(builder.Completed);

		try
		{
			var first = builder.DestroyAsync(context).AsTask();
			await value.Entered.Task.WaitAsync(_timeout);
			var second = builder.DestroyAsync(context).AsTask();

			Assert.False(first.IsCompleted);
			Assert.False(second.IsCompleted);
			Assert.Equal(1, value.Disposals);
			Assert.Throws<InvalidOperationException>(() => builtin.GetValue(ObtainMode.Auto));

			value.Release.TrySetResult();
			await Task.WhenAll(first, second).WaitAsync(_timeout);
			await builder.DestroyAsync(context);

			Assert.Equal(1, value.Disposals);
			Assert.False(builtin.IsBuilded);
			Assert.Null(builtin.GetValue(ObtainMode.Never));

			if(value is DualDisposable dual)
				Assert.Equal(0, dual.SynchronousDisposals);
		}
		finally { value.Release.TrySetResult(); }
	}

	[Fact]
	public async Task DestroyAsyncRejectsReentryFromItsOwnDisposalCallback()
	{
		await using var scope = new PluginTestScope();
		scope.WritePlugin($"<builders><builder name=\"object\" type=\"{typeof(BuilderLifecycleTests.RecordingBuilder).AssemblyQualifiedName}\" /></builders><extension path=\"/Items\"><object name=\"Item\" type=\"{typeof(ReentrantDisposable).AssemblyQualifiedName}\" /></extension>");
		scope.Tree.Load();
		var builder = Assert.IsType<BuilderLifecycleTests.RecordingBuilder>(scope.Tree.Plugins["Feature"].GetBuilder("object"));
		var builtin = scope.Builtin("/Items/Item");
		var value = Assert.IsType<ReentrantDisposable>(builtin.Build());
		var context = Assert.Single(builder.Completed);
		var rejected = false;
		value.Disposing = async () =>
		{
			await Assert.ThrowsAsync<InvalidOperationException>(() => builder.DestroyAsync(context).AsTask());
			rejected = true;
		};

		await builder.DestroyAsync(context).AsTask().WaitAsync(_timeout);

		Assert.True(rejected);
		Assert.Equal(1, value.Disposals);
		Assert.False(builtin.IsBuilded);
		Assert.Null(builtin.GetValue(ObtainMode.Never));
	}

	[Fact]
	public async Task UnloadAsyncWaitsForNestedAsyncDisposalAndDoesNotRepeatIt()
	{
		await using var scope = new PluginTestScope();
		scope.WritePlugin($"<extension path=\"/Items\"><object name=\"Parent\" type=\"System.Collections.ArrayList\"><object name=\"First\" type=\"{typeof(AsyncDisposable).AssemblyQualifiedName}\" /><object name=\"Second\" type=\"{typeof(AsyncDisposable).AssemblyQualifiedName}\" /></object></extension>");
		scope.Tree.Load();
		scope.Builtin("/Items/Parent").Build();
		var plugin = scope.Tree.Plugins["Feature"];
		var first = Assert.IsType<AsyncDisposable>(scope.Builtin("/Items/Parent/First").Value);
		var second = Assert.IsType<AsyncDisposable>(scope.Builtin("/Items/Parent/Second").Value);
		var unloaded = 0;
		scope.Tree.Loader.PluginUnloaded += (_, args) =>
		{
			if(args.Plugin == plugin)
				unloaded++;
		};

		try
		{
			var unloading = scope.Tree.UnloadAsync(plugin).AsTask();
			await Task.WhenAny(first.Entered.Task, second.Entered.Task).WaitAsync(_timeout);

			Assert.False(unloading.IsCompleted);
			Assert.Equal(0, unloaded);

			first.Release.TrySetResult();
			second.Release.TrySetResult();
			await unloading.WaitAsync(_timeout);
			await scope.Tree.UnloadAsync(plugin);

			Assert.Equal(1, first.Disposals);
			Assert.Equal(1, second.Disposals);
			Assert.Equal(1, unloaded);
			Assert.Equal(PluginStatus.Unloaded, plugin.Status);
			Assert.Empty(plugin.Builtins);
			Assert.Null(scope.Tree.Find("/Items/Parent"));
		}
		finally
		{
			first.Release.TrySetResult();
			second.Release.TrySetResult();
		}
	}

	[Fact]
	public async Task UnloadAsyncReleasesChildPluginsWithoutSkippingSiblings()
	{
		await using var scope = new PluginTestScope();
		scope.WriteFile("children/First.plugin", $"<plugin name=\"First\"><extension path=\"/Items\"><object name=\"First\" type=\"{typeof(AsyncDisposable).AssemblyQualifiedName}\" /></extension></plugin>");
		scope.WriteFile("children/Second.plugin", $"<plugin name=\"Second\"><extension path=\"/Items\"><object name=\"Second\" type=\"{typeof(AsyncDisposable).AssemblyQualifiedName}\" /></extension></plugin>");
		scope.Tree.Load();
		var parent = scope.Tree.Plugins["Main"];
		var first = Assert.IsType<AsyncDisposable>(scope.Builtin("/Items/First").Build());
		var second = Assert.IsType<AsyncDisposable>(scope.Builtin("/Items/Second").Build());
		var children = parent.Children.ToArray();
		Assert.Equal(2, children.Length);
		first.Release.TrySetResult();
		second.Release.TrySetResult();

		await scope.Tree.UnloadAsync(parent);

		Assert.All(children, child => Assert.Equal(PluginStatus.Unloaded, child.Status));
		Assert.Equal(1, first.Disposals);
		Assert.Equal(1, second.Disposals);
		Assert.Empty(parent.Children);
		Assert.Empty(scope.Tree.Plugins);
		Assert.Null(scope.Tree.Find("/Items/First"));
		Assert.Null(scope.Tree.Find("/Items/Second"));
	}

	[Fact]
	public async Task DisposeAsyncIgnoresUnloadCancellationAndEvictsTreeCache()
	{
		await using var scope = new PluginTestScope();
		scope.WritePlugin($"<extension path=\"/Items\"><object name=\"Item\" type=\"{typeof(AsyncDisposable).AssemblyQualifiedName}\" /></extension>");
		scope.Tree.Load();
		var plugin = scope.Tree.Plugins["Feature"];
		var value = Assert.IsType<AsyncDisposable>(scope.Builtin("/Items/Item").Build());
		scope.Tree.Loader.PluginUnloading += (_, args) => args.Cancel = true;

		try
		{
			await scope.Tree.UnloadAsync(plugin);

			Assert.Equal(0, value.Disposals);
			Assert.Equal(PluginStatus.Loaded, plugin.Status);

			var disposing = scope.Tree.DisposeAsync().AsTask();
			await value.Entered.Task.WaitAsync(_timeout);
			Assert.False(disposing.IsCompleted);
			value.Release.TrySetResult();
			await disposing.WaitAsync(_timeout);
			await scope.Tree.DisposeAsync();

			Assert.Equal(1, value.Disposals);
			Assert.Equal(PluginStatus.Unloaded, plugin.Status);
			Assert.Empty(scope.Tree.Plugins);

			await using var replacement = PluginTree.Get(scope.Options);
			Assert.NotSame(scope.Tree, replacement);
			replacement.Load();
			Assert.Equal(PluginStatus.Loaded, replacement.Plugins["Feature"].Status);
		}
		finally { value.Release.TrySetResult(); }
	}

	[Fact]
	public async Task UnloadAsyncContinuesAfterFailuresAndAggregatesErrors()
	{
		await using var scope = new PluginTestScope();
		scope.WritePlugin($"<extension path=\"/Items\"><object name=\"First\" type=\"{typeof(AsyncDisposable).AssemblyQualifiedName}\" /><object name=\"Second\" type=\"{typeof(AsyncDisposable).AssemblyQualifiedName}\" /><object name=\"Last\" type=\"{typeof(AsyncDisposable).AssemblyQualifiedName}\" /></extension>");
		scope.Tree.Load();
		var plugin = scope.Tree.Plugins["Feature"];
		var first = Assert.IsType<AsyncDisposable>(scope.Builtin("/Items/First").Build());
		var second = Assert.IsType<AsyncDisposable>(scope.Builtin("/Items/Second").Build());
		var last = Assert.IsType<AsyncDisposable>(scope.Builtin("/Items/Last").Build());
		first.Failure = new ApplicationException("first");
		second.Failure = new InvalidOperationException("second");
		first.Release.TrySetResult();
		second.Release.TrySetResult();
		last.Release.TrySetResult();

		var failure = await Assert.ThrowsAsync<AggregateException>(() => scope.Tree.UnloadAsync(plugin).AsTask().WaitAsync(_timeout));

		Assert.Equal(new[] { first.Failure, second.Failure }, failure.Flatten().InnerExceptions);
		Assert.Equal(1, first.Disposals);
		Assert.Equal(1, second.Disposals);
		Assert.Equal(1, last.Disposals);
		Assert.Equal(PluginStatus.Unloaded, plugin.Status);
		Assert.Empty(plugin.Builtins);
		Assert.Null(scope.Tree.Find("/Items/First"));
		Assert.Null(scope.Tree.Find("/Items/Second"));
		Assert.Null(scope.Tree.Find("/Items/Last"));
		Assert.DoesNotContain(plugin, scope.Tree.Plugins);
	}

	[Fact]
	public async Task UnloadAsyncCancellationLeavesPendingCleanupOwned()
	{
		await using var scope = new PluginTestScope();
		scope.WritePlugin($"<extension path=\"/Items\"><object name=\"Item\" type=\"{typeof(AsyncDisposable).AssemblyQualifiedName}\" /></extension>");
		scope.Tree.Load();
		var plugin = scope.Tree.Plugins["Feature"];
		var value = Assert.IsType<AsyncDisposable>(scope.Builtin("/Items/Item").Build());
		using var cancellation = new CancellationTokenSource();

		try
		{
			var cancelling = scope.Tree.UnloadAsync(plugin, cancellation.Token).AsTask();
			await value.Entered.Task.WaitAsync(_timeout);
			cancellation.Cancel();
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelling.WaitAsync(_timeout));
			var finishing = scope.Tree.UnloadAsync(plugin).AsTask();

			Assert.False(finishing.IsCompleted);
			Assert.Equal(1, value.Disposals);

			value.Release.TrySetResult();
			await finishing.WaitAsync(_timeout);

			Assert.Equal(1, value.Disposals);
			Assert.Equal(PluginStatus.Unloaded, plugin.Status);
			Assert.Null(scope.Tree.Find("/Items/Item"));
		}
		finally { value.Release.TrySetResult(); }
	}

	[Theory]
	[InlineData("object")]
	[InlineData("expose")]
	public async Task ConsumerUnloadKeepsSharedValueUntilOwnerUnloads(string ownerScheme)
	{
		await using var scope = new PluginTestScope();
		scope.WritePlugin($"<extension path=\"/Shared\"><{ownerScheme} name=\"Item\" type=\"{typeof(AsyncDisposable).AssemblyQualifiedName}\" /></extension>");
		scope.WriteFile("Consumer.plugin", "<plugin name=\"Consumer\"><manifest><dependencies><dependency name=\"Main\" /><dependency name=\"Feature\" /></dependencies></manifest><extension path=\"/Consumers\"><object name=\"ObjectReference\" value=\"{path:/Shared/Item}\" /><expose name=\"ExposedReference\" value=\"{path:/Shared/Item}\" /></extension></plugin>");
		scope.Tree.Load();
		var value = Assert.IsType<AsyncDisposable>(scope.Builtin("/Shared/Item").Build());
		Assert.Same(value, scope.Builtin("/Consumers/ObjectReference").Build());
		Assert.Same(value, scope.Builtin("/Consumers/ExposedReference").Build());

		try
		{
			await scope.Tree.UnloadAsync(scope.Tree.Plugins["Consumer"]);

			Assert.Equal(0, value.Disposals);
			Assert.Same(value, scope.Builtin("/Shared/Item").Value);
			Assert.Null(scope.Tree.Find("/Consumers/ObjectReference"));
			Assert.Null(scope.Tree.Find("/Consumers/ExposedReference"));

			var unloading = scope.Tree.UnloadAsync(scope.Tree.Plugins["Feature"]).AsTask();
			await value.Entered.Task.WaitAsync(_timeout);
			Assert.False(unloading.IsCompleted);
			value.Release.TrySetResult();
			await unloading.WaitAsync(_timeout);

			Assert.Equal(1, value.Disposals);
			Assert.Null(scope.Tree.Find("/Shared/Item"));
		}
		finally { value.Release.TrySetResult(); }
	}

	public class AsyncDisposable : IAsyncDisposable
	{
		public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public Exception Failure { get; set; }
		public int Disposals;

		public async ValueTask DisposeAsync()
		{
			Interlocked.Increment(ref this.Disposals);
			this.Entered.TrySetResult();
			await this.Release.Task;

			if(this.Failure != null)
				throw this.Failure;
		}
	}

	public sealed class ReentrantDisposable : IAsyncDisposable
	{
		public Func<ValueTask> Disposing { get; set; }
		public int Disposals { get; private set; }
		public async ValueTask DisposeAsync()
		{
			this.Disposals++;
			await this.Disposing();
		}
	}

	public sealed class DualDisposable : AsyncDisposable, IDisposable
	{
		public int SynchronousDisposals { get; private set; }
		public void Dispose() => this.SynchronousDisposals++;
	}
}
