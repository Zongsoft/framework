using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Messaging.ZeroMQ.Tests;

public class ZeroRequesterTests
{
	[Fact]
	public async Task RequesterReceivesImmediateResponses()
	{
		if(!Global.IsTestingEnabled)
			return;

		using var server = await ZeroServerScope.StartAsync();
		using var requesterQueue = ZeroTestUtility.CreateQueue(server.Port, "requester");
		using var responderQueue = ZeroTestUtility.CreateQueue(server.Port, "responder");

		var responder = new ZeroResponder { Queue = responderQueue };
		responder.Handlers.Add(new EchoHandler());
		await responder.StartAsync([]);

		try
		{
			var requester = new ZeroRequester { Queue = requesterQueue };

			for(int i = 0; i < 20; i++)
			{
				using var token = await requester.RequestAsync("rpc/echo", Encoding.UTF8.GetBytes($"message-{i}"));
				Assert.NotNull(token);

				var response = token.GetResponses(TimeSpan.FromSeconds(5)).FirstOrDefault();
				Assert.NotNull(response);
				Assert.Equal($"message-{i}", Encoding.UTF8.GetString(response.Data.Span));
				Assert.Equal(token.Request.Identifier, response.Request.Identifier);
			}
		}
		finally
		{
			await responder.StopAsync([]);
			((IDisposable)responder).Dispose();
		}
	}

	[Fact]
	public async Task RequesterSharesInitialReplySubscriptionAcrossConcurrentRequests()
	{
		if(!Global.IsTestingEnabled)
			return;

		using var server = await ZeroServerScope.StartAsync();
		using var requesterQueue = ZeroTestUtility.CreateQueue(server.Port, "requester");
		using var responderQueue = ZeroTestUtility.CreateQueue(server.Port, "responder");

		var responder = new ZeroResponder { Queue = responderQueue };
		responder.Handlers.Add(new EchoHandler());
		await responder.StartAsync([]);

		try
		{
			var requester = new ZeroRequester { Queue = requesterQueue };
			var tasks = Enumerable.Range(0, 20).Select(async index =>
			{
				using var token = await requester.RequestAsync("rpc/echo", Encoding.UTF8.GetBytes($"concurrent-{index}"));
				Assert.NotNull(token);

				var response = token.GetResponses(TimeSpan.FromSeconds(5)).FirstOrDefault();
				Assert.NotNull(response);
				Assert.Equal($"concurrent-{index}", Encoding.UTF8.GetString(response.Data.Span));
			});

			await Task.WhenAll(tasks);
		}
		finally
		{
			await responder.StopAsync([]);
			((IDisposable)responder).Dispose();
		}
	}

	[Fact]
	public async Task CanceledInitialSubscriptionThrowsOperationCanceledException()
	{
		if(!Global.IsTestingEnabled)
			return;

		using var server = await ZeroServerScope.StartAsync();
		using var queue = ZeroTestUtility.CreateQueue(server.Port, "requester");
		var requester = new ZeroRequester { Queue = queue };

		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
			await requester.RequestAsync("rpc/canceled", Encoding.UTF8.GetBytes("canceled"), cancellation.Token));
	}
}
