using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Zongsoft.Caching;
using Zongsoft.Communication;

namespace Zongsoft.Messaging.ZeroMQ.Tests;

public class ZeroRequesterMultipleReplyTests
{
	[Theory]
	[InlineData(3)]
	[InlineData(8)]
	public async Task CacheReplacementKeepsTokenForAllReplies(int count)
	{
		await using var requester = new ZeroRequester();
		var (request, token, tokens, pending) = Register(requester);
		using var lifetime = token;
		using var replaced = new SemaphoreSlim(0);

		pending.Evicted += (_, args) =>
		{
			if(args.Reason == CacheEvictedReason.Replaced)
				replaced.Release();
		};

		for(var index = 0; index < count; index++)
		{
			await ReplyAsync(requester, request, (byte)index);

			//等待真实替换回调，保证旧实现必然在下一份响应之前暴露令牌丢失。
			if(index > 0)
				Assert.True(await replaced.WaitAsync(TimeSpan.FromSeconds(5)));

			Assert.True(tokens.Contains(request.Identifier));
			Assert.True(pending.Contains(request.Identifier));
		}

		AssertReplies(await ZeroTestUtility.ReadResponsesAsync(token), request.Identifier, count);
	}

	[Fact]
	public async Task ExpiredTokenIsRemovedAndLateRepliesAreIgnored()
	{
		await using var requester = new ZeroRequester();
		var (request, token, tokens, pending) = Register(requester);
		using var lifetime = token;
		await ReplyAsync(requester, request, 0);
		AssertReplies(await ZeroTestUtility.ReadResponsesAsync(token), request.Identifier, 1);

		var expired = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		pending.Evicted += (_, args) =>
		{
			if(args.Reason == CacheEvictedReason.Expired)
				expired.TrySetResult();
		};

		//只缩短该测试缓存项的有效期，不改变生产默认的十分钟清理间隔。
		pending.SetValue(request.Identifier, (object)null, TimeSpan.FromMilliseconds(20));
		var deadline = DateTime.UtcNow.AddSeconds(5);

		//TimeSpan 表示滑动过期，探测间隔必须超过有效期，避免 Contains 不断续期。
		do
		{
			await Task.Delay(50);
		}
		while(pending.Contains(request.Identifier) && DateTime.UtcNow < deadline);

		await expired.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.False(tokens.Contains(request.Identifier));
		Assert.False(pending.Contains(request.Identifier));

		await ReplyAsync(requester, request, 1);
		Assert.Empty(await ZeroTestUtility.ReadResponsesAsync(token));
		Assert.False(pending.Contains(request.Identifier));
	}

	[Fact]
	public async Task TokenDisposalRemovesPendingEntryAndIgnoresLateReplies()
	{
		await using var requester = new ZeroRequester();
		var (request, token, tokens, pending) = Register(requester);
		using var lifetime = token;
		await ReplyAsync(requester, request, 0);
		Assert.True(tokens.Contains(request.Identifier));
		Assert.True(pending.Contains(request.Identifier));

		token.Dispose();
		token.Dispose();
		Assert.False(tokens.Contains(request.Identifier));
		Assert.False(pending.Contains(request.Identifier));
		Assert.Empty(await ZeroTestUtility.ReadResponsesAsync(token));

		await ReplyAsync(requester, request, 1);
		Assert.Empty(await ZeroTestUtility.ReadResponsesAsync(token));
		Assert.False(tokens.Contains(request.Identifier));
		Assert.False(pending.Contains(request.Identifier));
	}

	[Fact]
	public async Task RequesterDisposalRemovesAllTokens()
	{
		await using var requester = new ZeroRequester();
		var first = Register(requester);
		var second = Register(requester);
		using var firstToken = first.Token;
		using var secondToken = second.Token;
		await ReplyAsync(requester, first.Request, 0);
		await ReplyAsync(requester, second.Request, 0);
		Assert.Equal(2, first.Tokens.Count);

		await requester.DisposeAsync();
		await requester.DisposeAsync();
		Assert.Empty(first.Tokens);
		Assert.Empty(await ZeroTestUtility.ReadResponsesAsync(first.Token));
		Assert.Empty(await ZeroTestUtility.ReadResponsesAsync(second.Token));
		await Assert.ThrowsAsync<ObjectDisposedException>(() => requester.RequestAsync("rpc/multiple-replies", ReadOnlyMemory<byte>.Empty).AsTask());
	}

	[Theory]
	[InlineData(3)]
	[InlineData(8)]
	public async Task RequesterReceivesAllRepliesOverRealSockets(int count)
	{
		if(!Global.IsTestingEnabled)
			return;

		await using var server = await ZeroServerScope.StartAsync();
		await using var requesterQueue = ZeroTestUtility.CreateQueue(server.Port, "multiple-requester");
		await using var responderQueue = ZeroTestUtility.CreateQueue(server.Port, "multiple-responder");
		await using var requester = new ZeroRequester { Queue = requesterQueue };
		await using var responder = new ZeroResponder { Queue = responderQueue };
		responder.Handlers.Add(new MultipleReplyHandler());
		await responder.StartAsync([]);

		try
		{
			//预热使用独立请求；正式测量不重发，固定延迟不充当订阅就绪证据。
			var ready = false;

			for(var attempt = 0; attempt < 10 && !ready; attempt++)
			{
				using var warmup = await requester.RequestAsync("rpc/multiple-replies", new byte[] { 1 });
				var replies = await ZeroTestUtility.ReadResponsesAsync(warmup, TimeSpan.FromMilliseconds(500), 1);

				if(replies.Length == 1)
				{
					AssertReplies(replies, warmup.Request.Identifier, 1);
					ready = true;
				}
			}

			Assert.True(ready);
			using var token = await requester.RequestAsync("rpc/multiple-replies", new byte[] { (byte)count });
			Assert.NotNull(token);
			var responses = await ZeroTestUtility.ReadResponsesAsync(token, TimeSpan.FromSeconds(3), count);
			AssertReplies(responses, token.Request.Identifier, count);
		}
		finally
		{
			await responder.StopAsync([]);
		}
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	[InlineData(3)]
	public async Task GetResponsesAsync_WaitingForReply_DoesNotBlockAndWakesForCompletion(int completion)
	{
		await using var requester = new ZeroRequester();
		var (request, token, _, _) = Register(requester);
		using var lifetime = token;
		using var cancellation = new CancellationTokenSource();
		await using var responses = token.GetResponsesAsync(TimeSpan.FromMinutes(1), cancellation.Token).GetAsyncEnumerator();
		var returned = new TaskCompletionSource<Task<bool>>(TaskCreationOptions.RunContinuationsAsynchronously);
		var invocation = Task.Run(() =>
		{
			try { returned.SetResult(responses.MoveNextAsync().AsTask()); }
			catch(Exception exception) { returned.SetResult(Task.FromException<bool>(exception)); }
		});

		try
		{
			var pending = await returned.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.False(pending.IsCompleted);

			switch(completion)
			{
				case 0:
					await ReplyAsync(requester, request, 42);
					break;
				case 1:
					token.Dispose();
					break;
				case 2:
					await requester.DisposeAsync();
					break;
				case 3:
					cancellation.Cancel();
					break;
			}

			Assert.Equal(completion == 0, await pending.WaitAsync(TimeSpan.FromSeconds(5)));

			if(completion == 0)
			{
				Assert.Equal(request.Identifier, responses.Current.Request.Identifier);
				Assert.Equal(new byte[] { 42 }, responses.Current.Data.ToArray());
			}
		}
		finally
		{
			token.Dispose();
			await invocation.WaitAsync(TimeSpan.FromSeconds(5));
			var pending = await returned.Task.WaitAsync(TimeSpan.FromSeconds(5));
			await pending.WaitAsync(TimeSpan.FromSeconds(5));
		}
	}

	[Fact]
	public async Task GetResponsesAsync_PreCanceledEnumeration_LeavesBufferedRepliesAvailable()
	{
		await using var requester = new ZeroRequester();
		var (request, token, _, _) = Register(requester);
		using var lifetime = token;
		using var cancellation = new CancellationTokenSource();
		await ReplyAsync(requester, request, 0);
		cancellation.Cancel();

		Assert.Empty(await ZeroTestUtility.ReadResponsesAsync(token, cancellation: cancellation.Token));
		AssertReplies(await ZeroTestUtility.ReadResponsesAsync(token), request.Identifier, 1);
	}

	private static ValueTask ReplyAsync(ZeroRequester requester, IRequest request, byte index) =>
		((IRequester)requester).OnRespondedAsync(request.Response("rpc/multiple-replies/reply", new byte[] { index }), CancellationToken.None);

	private static void AssertReplies(IResponse[] responses, string identifier, int count)
	{
		Assert.Equal(count, responses.Length);
		Assert.All(responses, response =>
		{
			Assert.Equal(identifier, response.Request.Identifier);
			Assert.Equal("rpc/multiple-replies/reply", response.Url);
			Assert.Equal(1, response.Data.Length);
		});
		Assert.Equal(Enumerable.Range(0, count), responses.Select(response => (int)response.Data.Span[0]).OrderBy(index => index));
	}

	private static (IRequest Request, IRequestToken Token, IDictionary Tokens, MemoryCache Pending) Register(ZeroRequester requester)
	{
		//无需网络的缓存生命周期测试；保持生产私有类型和成员的可见性不变。
		const BindingFlags FLAGS = BindingFlags.Instance | BindingFlags.NonPublic;
		var type = typeof(ZeroRequester);
		var requestType = type.Assembly.GetType("Zongsoft.Messaging.ZeroMQ.ZeroRequest", true);
		var request = (IRequest)Activator.CreateInstance(requestType, "rpc/multiple-replies", ReadOnlyMemory<byte>.Empty, Guid.NewGuid().ToString("N"));
		var parameter = Expression.Parameter(requestType, "request");
		var callback = Expression.Lambda(
			typeof(Action<>).MakeGenericType(requestType),
			Expression.Call(Expression.Constant(requester), type.GetMethod("Remove", FLAGS), Expression.Property(parameter, "Identifier")),
			parameter).Compile();

		var token = (IRequestToken)Activator.CreateInstance(type.GetNestedType("Token", BindingFlags.NonPublic), request, callback);
		var tokens = (IDictionary)type.GetField("_tokens", FLAGS).GetValue(requester);
		tokens.Add(request.Identifier, token);
		return (request, token, tokens, (MemoryCache)type.GetField("_pending", FLAGS).GetValue(requester));
	}
}

[Zongsoft.Components.Handler("rpc/multiple-replies")]
internal sealed class MultipleReplyHandler : Zongsoft.Components.HandlerBase<IRequest>
{
	protected override async ValueTask OnHandleAsync(IRequest request, Zongsoft.Collections.Parameters parameters, CancellationToken cancellation)
	{
		var responder = parameters.GetValue<IResponder>();
		var count = request.Data.Span[0];

		for(var index = 0; index < count; index++)
		{
			await responder.RespondAsync(request.Response(new byte[] { (byte)index }), cancellation);

			if(index + 1 < count)
				await Task.Delay(50, cancellation);
		}
	}
}
