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
		if(!Global.IsTestingEnabled)
			return;

		await using var server = await ZeroServerScope.StartAsync();
		await using var queue = ZeroTestUtility.CreateQueue(server.Port, "responder");
		var responder = new ZeroResponder { Queue = queue };

		responder.Handlers.Add(new RollbackHandler());
		responder.Handlers.Add(null);

		try
		{
			await Assert.ThrowsAsync<NullReferenceException>(() => responder.StartAsync([]));

			Assert.Equal(WorkerState.Stopped, responder.State);
			Assert.True(await ZeroTestUtility.WaitUntilAsync(() => queue.Subscribers.Count == 0, TimeSpan.FromSeconds(5)));
		}
		finally
		{
			await responder.DisposeAsync();
		}
	}

	[Handler("rpc/rollback")]
	private sealed class RollbackHandler : HandlerBase<IRequest>
	{
		protected override ValueTask OnHandleAsync(IRequest request, Parameters parameters, CancellationToken cancellation) => ValueTask.CompletedTask;
	}
}
