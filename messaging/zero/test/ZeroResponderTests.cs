using System;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Collections;
using Zongsoft.Components;
using Zongsoft.Communication;

namespace Zongsoft.Messaging.ZeroMQ.Tests;

public class ZeroResponderTests
{
	[Fact]
	public async Task ResponderStartFailureUnsubscribesPartialSubscriptions()
	{
		using var server = await ZeroServerScope.StartAsync();
		using var queue = ZeroTestUtility.CreateQueue(server.Port, "responder");
		var responder = new ZeroResponder { Queue = queue };

		responder.Handlers.Add(new RollbackHandler());
		responder.Handlers.Add(null);

		try
		{
			await responder.StartAsync([]);

			Assert.Equal(WorkerState.Stopped, responder.State);
			Assert.True(await ZeroTestUtility.WaitUntilAsync(() => queue.Subscribers.Count == 0, TimeSpan.FromSeconds(5)));
		}
		finally
		{
			((IDisposable)responder).Dispose();
		}
	}

	[Handler("rpc/rollback")]
	private sealed class RollbackHandler : HandlerBase<IRequest>
	{
		protected override ValueTask OnHandleAsync(IRequest request, Parameters parameters, CancellationToken cancellation) => ValueTask.CompletedTask;
	}
}
