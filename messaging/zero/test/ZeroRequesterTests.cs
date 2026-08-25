using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Communication;

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
			await using var requester = new ZeroRequester { Queue = requesterQueue };

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
			await using var requester = new ZeroRequester { Queue = requesterQueue };
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
		await using var requester = new ZeroRequester { Queue = queue };

		using var cancellation = new CancellationTokenSource();
		await cancellation.CancelAsync();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
			await requester.RequestAsync("rpc/canceled", Encoding.UTF8.GetBytes("canceled"), cancellation.Token));
	}

	[Fact]
	public async Task RequesterDisposalReleasesSubscriptionAndRejectsQueueReplacement()
	{
		if(!Global.IsTestingEnabled)
			return;

		using var server = await ZeroServerScope.StartAsync();
		using var first = ZeroTestUtility.CreateQueue(server.Port, "requester-first");
		using var second = ZeroTestUtility.CreateQueue(server.Port, "requester-second");
		using var responderQueue = ZeroTestUtility.CreateQueue(server.Port, "requester-responder");
		var responder = new ZeroResponder { Queue = responderQueue };
		responder.Handlers.Add(new EchoHandler());
		await responder.StartAsync([]);
		var requester = new ZeroRequester { Queue = first };

		try
		{
			using(await requester.RequestAsync("rpc/echo", Encoding.UTF8.GetBytes("dispose"))) { }
			Assert.Single(first.Subscribers);
			Assert.Throws<InvalidOperationException>(() => requester.Queue = second);

			await requester.DisposeAsync();

			Assert.Empty(first.Subscribers);
			await Assert.ThrowsAsync<ObjectDisposedException>(() => requester.RequestAsync("rpc/disposed", ReadOnlyMemory<byte>.Empty).AsTask());
		}
		finally
		{
			await responder.StopAsync([]);
			((IDisposable)responder).Dispose();
		}
	}

	[Fact]
	public async Task GroupedRequesterAndResponderUseLogicalTopics()
	{
		if(!Global.IsTestingEnabled)
			return;

		using var server = await ZeroServerScope.StartAsync();
		using var requesterQueue = ZeroTestUtility.CreateQueue(server.Port, "group-requester", settings => settings.Group = "tenant");
		using var responderQueue = ZeroTestUtility.CreateQueue(server.Port, "group-responder", settings => settings.Group = "tenant");
		await using var requester = new ZeroRequester { Queue = requesterQueue };
		var responder = new ZeroResponder { Queue = responderQueue };
		responder.Handlers.Add(new EchoHandler());
		await responder.StartAsync([]);

		try
		{
			IResponse response = null;
			for(var attempt = 0; attempt < 10 && response == null; attempt++)
			{
				using var token = await requester.RequestAsync("rpc/echo", Encoding.UTF8.GetBytes("grouped"));
				response = token.GetResponses(TimeSpan.FromMilliseconds(500)).FirstOrDefault();
			}

			Assert.NotNull(response);
			Assert.Equal("rpc/echo/reply", response.Url);
			Assert.Equal("grouped", Encoding.UTF8.GetString(response.Data.Span));
		}
		finally
		{
			await responder.StopAsync([]);
			((IDisposable)responder).Dispose();
		}
	}
}
