using System;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;

using Xunit;

namespace Zongsoft.Messaging.ZeroMQ.Tests;

public class ZeroRequesterTests
{
	[Fact]
	public async Task RequesterReceivesImmediateResponses()
	{
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
	public void RequestTokenDisposeRemovesTokenWithoutEnumeratingResponses()
	{
		var assembly = typeof(ZeroRequester).Assembly;
		var requestType = assembly.GetType("Zongsoft.Messaging.ZeroMQ.ZeroRequest", true);
		var tokenType = typeof(ZeroRequester).GetNestedType("Token", BindingFlags.NonPublic);
		Assert.NotNull(requestType);
		Assert.NotNull(tokenType);

		var request = Activator.CreateInstance(requestType, ["rpc/token", ReadOnlyMemory<byte>.Empty, null]);
		Assert.NotNull(request);

		var actionType = typeof(Action<>).MakeGenericType(requestType);
		var method = typeof(TokenCallbacks).GetMethod(nameof(TokenCallbacks.OnDisposed), BindingFlags.Public | BindingFlags.Static)!.MakeGenericMethod(requestType);
		var action = Delegate.CreateDelegate(actionType, method);
		var token = (IDisposable)Activator.CreateInstance(tokenType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, [request, action], null)!;

		TokenCallbacks.Invoked = false;
		token.Dispose();

		Assert.True(TokenCallbacks.Invoked);
		Assert.Null(tokenType.GetField("_disposed", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(token));
		Assert.Null(tokenType.GetField("_responses", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(token));
	}

	private static class TokenCallbacks
	{
		public static bool Invoked;
		public static void OnDisposed<T>(T request) => Invoked = true;
	}
}
