using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Diagnostics;

using Xunit;

namespace Zongsoft.Plugins.Tests;

public class PluginLoggerLifecycleTests
{
	[Fact]
	public async Task UnloadOwnedLoggerDoesNotLeaveDisposedSinkRegistered()
	{
		using var registrations = new LoggerRegistrations();
		var scope = new PluginTestScope();
		var owned = BuildOwned(scope);

		try
		{
			await scope.Tree.UnloadAsync(scope.Tree.Plugins["Feature"]);
			await Logging.FlushAsync();

			Assert.Equal(1, owned.Flushes);
			Assert.Equal(1, owned.Disposals);
			Assert.Equal(0, owned.FlushesAfterDisposal);
			Assert.DoesNotContain(Logging.Loggers, logger => ReferenceEquals(logger, owned));
			Assert.Equal(1, registrations.Shared.Flushes);
			Assert.Equal(0, registrations.Shared.Disposals);
		}
		finally
		{
			registrations.Remove(owned);
			await scope.DisposeAsync();
		}
	}

	[Fact]
	public async Task ApplicationDisposalFlushesOwnedLoggerBeforeDisposal()
	{
		using var registrations = new LoggerRegistrations();
		var scope = new PluginTestScope();
		var owned = BuildOwned(scope);

		try
		{
			await scope.Application.DisposeAsync();

			Assert.Equal(1, owned.Flushes);
			Assert.Equal(1, owned.Disposals);
			Assert.Equal(0, owned.FlushesAfterDisposal);
			Assert.Equal(1, registrations.Shared.Flushes);
			Assert.Equal(0, registrations.Shared.Disposals);
			Assert.DoesNotContain(Logging.Loggers, logger => ReferenceEquals(logger, owned));
		}
		finally
		{
			registrations.Remove(owned);
			// Application disposal shares its failure; observe cleanup without masking the original assertion.
			await Record.ExceptionAsync(() => scope.DisposeAsync().AsTask());
		}
	}

	[Fact]
	public async Task UnloadWaitsForOwnedFlushBeforeUnregisteringAndDisposing()
	{
		using var registrations = new LoggerRegistrations();
		await using var scope = new PluginTestScope();
		var owned = BuildOwned(scope);
		owned.FlushGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
		owned.DisposeGate = new(TaskCreationOptions.RunContinuationsAsynchronously);
		using var cancellation = new CancellationTokenSource();
		var operation = scope.Tree.UnloadAsync(scope.Tree.Plugins["Feature"], cancellation.Token).AsTask();

		try
		{
			await owned.FlushEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
			Assert.False(operation.IsCompleted);
			Assert.Equal(CancellationToken.None, owned.FlushToken);
			Assert.Contains(Logging.Loggers, logger => ReferenceEquals(logger, owned));
			Assert.Equal(0, owned.Disposals);
			Assert.Equal(0, registrations.Shared.Flushes);
			owned.FlushGate.SetResult();

			await owned.DisposeEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
			Assert.False(operation.IsCompleted);
			Assert.True(owned.RegisteredAtFlushCompletion);
			Assert.False(owned.RegisteredAtDisposal);
			Assert.DoesNotContain(Logging.Loggers, logger => ReferenceEquals(logger, owned));
			owned.DisposeGate.SetResult();
			await operation.WaitAsync(TimeSpan.FromSeconds(10));

			Assert.Equal(1, owned.Flushes);
			Assert.Equal(1, owned.Disposals);
		}
		finally
		{
			owned.FlushGate.TrySetResult();
			owned.DisposeGate.TrySetResult();
			await Record.ExceptionAsync(() => operation);
			registrations.Remove(owned);
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task FlushFailureStillUnregistersDisposesAndReleasesOtherResources(bool disposalFails)
	{
		using var registrations = new LoggerRegistrations();
		var scope = new PluginTestScope();
		var owned = BuildOwned(scope, extra: $"<extension path=\"/Resources\"><object name=\"Other\" type=\"{typeof(OtherResource).AssemblyQualifiedName}\" /></extension>");
		var other = Assert.IsType<OtherResource>(scope.Builtin("/Resources/Other").Build());
		var flushFailure = new InvalidOperationException("test flush failure");
		var disposalFailure = new ApplicationException("test disposal failure");
		owned.FlushFailure = flushFailure;
		owned.DisposeFailure = disposalFails ? disposalFailure : null;

		try
		{
			var operation = scope.Application.DisposeAsync().AsTask();
			var failure = await Record.ExceptionAsync(() => operation);
			Assert.NotNull(failure);
			var errors = failure is AggregateException aggregate ? aggregate.Flatten().InnerExceptions.ToArray() : [failure];
			Assert.Equal(disposalFails ? 2 : 1, errors.Length);
			Assert.Contains(errors, error => ReferenceEquals(error, flushFailure));

			if(disposalFails)
				Assert.Contains(errors, error => ReferenceEquals(error, disposalFailure));

			Assert.Equal(1, owned.Flushes);
			Assert.Equal(1, owned.Disposals);
			Assert.Equal(0, owned.FlushesAfterDisposal);
			Assert.Equal(1, other.Disposals);
			Assert.False(owned.RegisteredAtDisposal);
			Assert.DoesNotContain(Logging.Loggers, logger => ReferenceEquals(logger, owned));
			Assert.Equal(1, registrations.Shared.Flushes);
			Assert.Equal(0, registrations.Shared.Disposals);
			Assert.Same(failure, await Record.ExceptionAsync(() => scope.Application.DisposeAsync().AsTask()));
			Assert.Equal(1, owned.Disposals);
		}
		finally
		{
			registrations.Remove(owned);
			await Record.ExceptionAsync(() => scope.DisposeAsync().AsTask());
		}
	}

	[Fact]
	public async Task UnloadPreservesBorrowedHostLoggerUntilContainerDisposal()
	{
		using var registrations = new LoggerRegistrations();
		var scope = new PluginTestScope(configure: services => services.AddSingleton(_ => registrations.Shared));
		scope.WritePlugin($"<extension path=\"/Borrowed\"><object name=\"Logger\" type=\"{typeof(ProbeLogger).AssemblyQualifiedName}\" value=\"{{service:~@}}\" /></extension>");
		scope.Tree.Load();
		Assert.Same(registrations.Shared, scope.Builtin("/Borrowed/Logger").Build());

		try
		{
			await scope.Tree.UnloadAsync(scope.Tree.Plugins["Feature"]);

			Assert.Equal(0, registrations.Shared.Flushes);
			Assert.Equal(0, registrations.Shared.Disposals);
			Assert.Same(registrations.Shared, Assert.Single(Logging.Loggers));

			await scope.Application.DisposeAsync();
			Assert.Equal(1, registrations.Shared.Flushes);
			Assert.Equal(0, registrations.Shared.Disposals);
			Assert.Same(registrations.Shared, Assert.Single(Logging.Loggers));
		}
		finally { await scope.DisposeAsync(); }

		Assert.Equal(1, registrations.Shared.Disposals);
	}

	[Fact]
	public async Task UnloadRemovesEveryOwnedRegistrationByReferenceOnly()
	{
		var other = new EqualProbeLogger();
		using var registrations = new LoggerRegistrations(other);
		await using var scope = new PluginTestScope();
		var owned = BuildOwned(scope, typeof(EqualProbeLogger));
		Logging.Loggers.Add(owned);
		Logging.Loggers.Add(owned);
		Assert.True(other.Equals(owned));
		Assert.False(ReferenceEquals(other, owned));
		Assert.Equal(3, Logging.Loggers.Count(logger => ReferenceEquals(logger, owned)));
		var plugin = scope.Tree.Plugins["Feature"];

		try
		{
			await scope.Tree.UnloadAsync(plugin);
			await scope.Tree.UnloadAsync(plugin);

			Assert.Same(other, Assert.Single(Logging.Loggers));
			Assert.Equal(1, owned.Flushes);
			Assert.Equal(1, owned.Disposals);
			Assert.Equal(0, other.Flushes);
			Assert.Equal(0, other.Disposals);
			await Logging.FlushAsync();
			Assert.Equal(1, other.Flushes);
			Assert.Equal(0, owned.FlushesAfterDisposal);
		}
		finally { registrations.Remove(owned); }
	}

	private static ProbeLogger BuildOwned(PluginTestScope scope, Type type = null, string extra = null)
	{
		scope.WritePlugin($"<extension path=\"/Diagnostics\"><object name=\"Loggers\" value=\"{{static:Zongsoft.Diagnostics.Logging.Loggers, Zongsoft.Core}}\"><object name=\"Owned\" type=\"{(type ?? typeof(ProbeLogger)).AssemblyQualifiedName}\" /></object></extension>{extra}");
		scope.Tree.Load();
		scope.Builtin("/Diagnostics/Loggers").Build();
		var owned = Assert.IsAssignableFrom<ProbeLogger>(scope.Builtin("/Diagnostics/Loggers/Owned").Value);
		Assert.Contains(Logging.Loggers, logger => ReferenceEquals(logger, owned));
		return owned;
	}

	public class ProbeLogger : ILogger, IAsyncDisposable
	{
		public string Name => "lifecycle-probe";
		public int Flushes { get; private set; }
		public int Disposals { get; private set; }
		public int FlushesAfterDisposal { get; private set; }
		public TaskCompletionSource FlushEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource FlushGate { get; set; }
		public TaskCompletionSource DisposeEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource DisposeGate { get; set; }
		public CancellationToken FlushToken { get; private set; }
		public bool RegisteredAtFlushCompletion { get; private set; }
		public bool RegisteredAtDisposal { get; private set; }
		public Exception FlushFailure { get; set; }
		public Exception DisposeFailure { get; set; }
		public ValueTask LogAsync<TLog>(TLog log, CancellationToken cancellation = default) where TLog : ILog => ValueTask.CompletedTask;
		public async ValueTask FlushAsync(CancellationToken cancellation = default)
		{
			this.Flushes++;
			if(this.Disposals > 0)
			{
				this.FlushesAfterDisposal++;
				throw new ObjectDisposedException(nameof(ProbeLogger));
			}

			this.FlushToken = cancellation;
			this.FlushEntered.TrySetResult();
			if(this.FlushGate != null)
				await this.FlushGate.Task;

			this.RegisteredAtFlushCompletion = Logging.Loggers.Any(logger => ReferenceEquals(logger, this));
			if(this.FlushFailure != null)
				throw this.FlushFailure;
		}

		public async ValueTask DisposeAsync()
		{
			this.Disposals++;
			this.RegisteredAtDisposal = Logging.Loggers.Any(logger => ReferenceEquals(logger, this));
			this.DisposeEntered.TrySetResult();

			if(this.DisposeGate != null)
				await this.DisposeGate.Task;

			if(this.DisposeFailure != null)
				throw this.DisposeFailure;
		}
	}

	public sealed class EqualProbeLogger : ProbeLogger
	{
		public override bool Equals(object obj) => obj is EqualProbeLogger;
		public override int GetHashCode() => 0;
	}

	public sealed class OtherResource : IAsyncDisposable
	{
		public int Disposals { get; private set; }
		public ValueTask DisposeAsync()
		{
			this.Disposals++;
			return ValueTask.CompletedTask;
		}
	}

	private sealed class LoggerRegistrations : IDisposable
	{
		private readonly ILogger[] _previous = Logging.Loggers.ToArray();
		public LoggerRegistrations(ProbeLogger shared = null)
		{
			this.Shared = shared ?? new ProbeLogger();
			Logging.Loggers.Clear();
			Logging.Loggers.Add(this.Shared);
		}

		public ProbeLogger Shared { get; }
		public void Remove(ILogger value)
		{
			var survivors = Logging.Loggers.Where(logger => !ReferenceEquals(logger, value)).ToArray();
			Logging.Loggers.Clear();
			foreach(var logger in survivors)
				Logging.Loggers.Add(logger);
		}

		public void Dispose()
		{
			Logging.Loggers.Clear();
			foreach(var logger in _previous)
				Logging.Loggers.Add(logger);
		}
	}
}
