using System;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Collections;
using Zongsoft.Components;

namespace Zongsoft.Externals.Hangfire.Tests;

public class ServerTests
{
	[Fact]
	public void Constructor_DefaultName_InitializesWorkerAndHandlers()
	{
		var server = new Server();

		Assert.Equal(nameof(Server), server.Name);
		Assert.True(server.Enabled);
		Assert.Equal(WorkerState.Stopped, server.State);
		Assert.False(server.CanPauseAndContinue);
		Assert.Empty(server.Handlers);
	}

	[Fact]
	public void Constructor_CustomName_TrimsNameAndUsesCaseInsensitiveHandlers()
	{
		var server = new Server("  CriticalJobs  ");
		var handler = new Handler();

		server.Handlers.Add("Nightly", handler);

		Assert.Equal("CriticalJobs", server.Name);
		Assert.Same(handler, server.Handlers["nightly"]);
		Assert.Single(server.Handlers);
	}

	[Fact]
	public void Storage_SetNull_ThrowsArgumentNullException()
	{
		var server = new Server();

		var exception = Assert.Throws<ArgumentNullException>(() =>
		{
			server.Storage = null;
		});

		Assert.Equal("value", exception.ParamName);
	}

	private sealed class Handler : IHandler
	{
		public ValueTask HandleAsync(object argument, CancellationToken cancellation = default) => ValueTask.CompletedTask;
		public ValueTask HandleAsync(object argument, Parameters parameters, CancellationToken cancellation = default) => ValueTask.CompletedTask;
	}
}
