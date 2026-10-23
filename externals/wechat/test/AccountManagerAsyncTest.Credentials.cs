using System;
using System.Linq;
using System.Reflection;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Zongsoft.Caching;
using Zongsoft.Common;
using Zongsoft.Services.Distributing;
using Xunit;

namespace Zongsoft.Externals.Wechat.Tests;

public sealed partial class AccountManagerAsyncTest
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Credentials_ConcurrentRefreshUsesOneStableRequestAndRemainingLifetime(bool refresh)
	{
		using var state = new CredentialScope(context);
		var account = Account.Applet(NewCode(), "synthetic-secret");
		if(refresh)
			state.Cache.Put(CredentialKey(account), "old", "old-revision", TimeSpan.FromMinutes(5));

		var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var count = 0;
		context.HttpClients.CredentialHandler = async (request, cancellation) =>
		{
			Interlocked.Increment(ref count);
			Assert.Equal("POST", request.Method.Method);
			Assert.Equal("https://api.weixin.qq.com/cgi-bin/stable_token", request.RequestUri.AbsoluteUri);
			var json = JsonSerializer.Deserialize<JsonElement>(await request.Content.ReadAsStringAsync(cancellation));
			Assert.Equal("client_credential", json.GetProperty("grant_type").GetString());
			Assert.Equal(account.Code, json.GetProperty("appid").GetString());
			Assert.Equal("synthetic-secret", json.GetProperty("secret").GetString());
			Assert.Equal(refresh, json.GetProperty("force_refresh").GetBoolean());
			started.TrySetResult();
			await release.Task.WaitAsync(cancellation);
			return CredentialResponse(new { access_token = "fresh", expires_in = 120 });
		};

		var requests = Enumerable.Range(0, 16).Select(_ => account.GetCredentialAsync(refresh, TestContext.Current.CancellationToken).AsTask()).ToArray();
		await started.Task.WaitAsync(_timeout, TestContext.Current.CancellationToken);
		release.SetResult();
		Assert.All(await Task.WhenAll(requests), value => Assert.Equal("fresh", value));
		Assert.Equal(1, count);
		Assert.Equal(1, state.Cache.Writes);
		Assert.InRange(state.Cache.LastExpiry.TotalSeconds, 100, 108);
		Assert.Equal("fresh", await account.GetCredentialAsync(false, TestContext.Current.CancellationToken));
		Assert.Equal(1, count);
	}

	[Fact]
	public async Task Credentials_RechecksDistributedCacheAfterAcquiringLock()
	{
		using var state = new CredentialScope(context);
		var account = Account.Applet(NewCode(), "synthetic");
		state.Locker.Acquired = () => state.Cache.Put(CredentialKey(account), "other-node", "new-revision", TimeSpan.FromSeconds(17));
		context.HttpClients.CredentialHandler = (_, _) => throw new InvalidOperationException();
		Assert.Equal("other-node", await account.GetCredentialAsync(false, TestContext.Current.CancellationToken));
		Assert.Equal(0, state.Cache.Writes);
	}

	[Fact]
	public async Task Credentials_TicketTypesAndAccountDomainsRemainSeparate()
	{
		using var state = new CredentialScope(context);
		var account = Account.Channel(NewCode(), "synthetic");
		var tokens = 0;
		var tickets = 0;
		context.HttpClients.CredentialHandler = (request, _) =>
		{
			if(request.Method == HttpMethod.Post)
			{
				tokens++;
				return Task.FromResult(CredentialResponse(new { access_token = "synthetic&token", expires_in = 120 }));
			}

			tickets++;
			Assert.Contains("access_token=synthetic%26token", request.RequestUri.Query);
			return Task.FromResult(CredentialResponse(new { ticket = request.RequestUri.Query.EndsWith("type=jsapi", StringComparison.Ordinal) ? "jsapi" : "wx_card", expires_in = 37 }));
		};

		var first = await account.GetTicketAsync("jsapi", false, TestContext.Current.CancellationToken);
		var second = await account.GetTicketAsync("wx_card", false, TestContext.Current.CancellationToken);
		Assert.Equal("jsapi", first.ticket);
		Assert.Equal("wx_card", second.ticket);
		Assert.InRange(first.period.TotalSeconds, 30, 33.3);
		await account.GetTicketAsync("jsapi", true, TestContext.Current.CancellationToken);
		Assert.Equal(1, tokens);
		Assert.Equal(3, tickets);
		await Account.Applet(account.Code, "synthetic").GetCredentialAsync(false, TestContext.Current.CancellationToken);
		Assert.Equal(2, tokens);
	}

	[Fact]
	public async Task Credentials_CancelledWaiterDoesNotCancelOwnerAndOwnerCancellationIsRetryable()
	{
		using var state = new CredentialScope(context);
		var account = Account.Applet(NewCode(), "synthetic");
		using var ownerCancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		using var waiterCancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		context.HttpClients.CredentialHandler = async (_, cancellation) =>
		{
			started.TrySetResult();
			await Task.Delay(Timeout.Infinite, cancellation);
			throw new InvalidOperationException();
		};
		var owner = account.GetCredentialAsync(false, ownerCancellation.Token).AsTask();
		await started.Task.WaitAsync(_timeout, TestContext.Current.CancellationToken);
		var waiter = account.GetCredentialAsync(false, waiterCancellation.Token).AsTask();
		waiterCancellation.Cancel();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiter);
		Assert.False(owner.IsCompleted);
		ownerCancellation.Cancel();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => owner);
		Assert.Equal(0, state.Cache.Writes);
		context.HttpClients.CredentialHandler = (_, _) => Task.FromResult(CredentialResponse(new { access_token = "retry", expires_in = 60 }));
		Assert.Equal("retry", await account.GetCredentialAsync(false, TestContext.Current.CancellationToken));
	}

	[Theory]
	[InlineData("error")]
	[InlineData("expired")]
	[InlineData("lost-lock")]
	[InlineData("store")]
	public async Task Credentials_FailuresNeverPublishSuccessfulCacheEntries(string fault)
	{
		using var state = new CredentialScope(context);
		var account = Account.Applet(NewCode(), "synthetic");
		context.HttpClients.CredentialHandler = (_, _) =>
		{
			if(fault == "lost-lock")
				state.Locker.Lease.Locked = false;
			if(fault == "store")
				state.Cache.AcceptWrites = false;

			return Task.FromResult(fault == "error" ? CredentialResponse(new { errcode = 40125, errmsg = "synthetic" }) :
				CredentialResponse(new { access_token = "synthetic", expires_in = fault == "expired" ? 0 : 60 }));
		};
		var error = await Assert.ThrowsAsync<OperationException>(() => account.GetCredentialAsync(false, TestContext.Current.CancellationToken).AsTask());
		Assert.Equal(fault switch { "error" => "40125", "expired" => "INVALID_CREDENTIAL_RESPONSE", "lost-lock" => "CREDENTIAL_LOCK_LOST", _ => "CREDENTIAL_STORE_FAILED" }, error.Reason);
		Assert.Empty(state.Cache.Values);
	}

	[Fact]
	public async Task Credentials_CrossNodeWaitAndBusyBackoffHonorCancellation()
	{
		using var state = new CredentialScope(context);
		var account = Account.Applet(NewCode(), "synthetic");
		using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		state.Locker.Available = false;
		state.Locker.Acquired = cancellation.Cancel;
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => account.GetCredentialAsync(false, cancellation.Token).AsTask());
		Assert.Equal(0, state.Cache.Writes);

		state.Locker.Available = true;
		state.Locker.Acquired = null;
		using var retryCancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
		context.HttpClients.CredentialHandler = (_, _) =>
		{
			retryCancellation.Cancel();
			return Task.FromResult(CredentialResponse(new { errcode = -1, errmsg = "synthetic" }));
		};
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => account.GetCredentialAsync(false, retryCancellation.Token).AsTask());
		Assert.Empty(state.Cache.Values);
	}

	[Theory]
	[InlineData(false, 3)]
	[InlineData(true, 1)]
	public async Task Credentials_OnlyOrdinaryAcquisitionRetriesExplicitBusy(bool force, int expected)
	{
		using var state = new CredentialScope(context);
		var account = Account.Applet(NewCode(), "synthetic");
		var count = 0;
		context.HttpClients.CredentialHandler = (_, _) =>
		{
			count++;
			return Task.FromResult(count < 3 ? CredentialResponse(new { errcode = -1, errmsg = "busy" }) :
				CredentialResponse(new { access_token = "synthetic", expires_in = 60 }));
		};

		if(force)
			Assert.Equal("-1", (await Assert.ThrowsAsync<OperationException>(() => account.GetCredentialAsync(true, TestContext.Current.CancellationToken).AsTask())).Reason);
		else
			Assert.Equal("synthetic", await account.GetCredentialAsync(false, TestContext.Current.CancellationToken));

		Assert.Equal(expected, count);
	}

	[Fact]
	public async Task Credentials_CompetingNodeCanPublishWhileLockIsUnavailable()
	{
		using var state = new CredentialScope(context);
		var account = Account.Applet(NewCode(), "synthetic");
		state.Locker.Available = false;
		state.Locker.Acquired = () => state.Cache.Put(CredentialKey(account), "other-node", "revision", TimeSpan.FromMinutes(1));
		context.HttpClients.CredentialHandler = (_, _) => throw new InvalidOperationException();
		Assert.Equal("other-node", await account.GetCredentialAsync(false, TestContext.Current.CancellationToken));
		Assert.Equal(0, state.Cache.Writes);
	}

	private static string CredentialKey(Account account) => $"Zongsoft.Wechat.Self:{account.Type}:{account.Code}:stable-token";
	private static HttpResponseMessage CredentialResponse(object value) => new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json") };

	private sealed class CredentialScope : IDisposable
	{
		private readonly IDistributedCache _previousCache = CredentialManager.Cache;
		private readonly IDistributedLockManager _previousLocker = CredentialManager.Locker;
		private readonly AccountManagerTestContext _context;
		public CredentialScope(AccountManagerTestContext context)
		{
			_context = context;
			CredentialManager.Cache = DispatchProxy.Create<IDistributedCache, CredentialCache>();
			CredentialManager.Locker = DispatchProxy.Create<IDistributedLockManager, CredentialLocker>();
			this.Cache = (CredentialCache)CredentialManager.Cache;
			this.Locker = (CredentialLocker)CredentialManager.Locker;
		}
		public CredentialCache Cache { get; }
		public CredentialLocker Locker { get; }
		public void Dispose()
		{
			_context.HttpClients.CredentialHandler = null;
			CredentialManager.Cache = _previousCache;
			CredentialManager.Locker = _previousLocker;
		}
	}

	private class CredentialCache : DispatchProxy
	{
		public CredentialCache() { }
		public ConcurrentDictionary<string, (string Value, DateTime Expiration)> Values { get; } = new(StringComparer.Ordinal);
		public bool AcceptWrites { get; set; } = true;
		public int Writes { get; private set; }
		public TimeSpan LastExpiry { get; private set; }
		public void Put(string key, string value, string revision, TimeSpan expiry) => this.Values[key] = (JsonSerializer.Serialize(new { value, revision }), DateTime.UtcNow.Add(expiry));
		protected override object Invoke(MethodInfo method, object[] arguments)
		{
			((CancellationToken)arguments[^1]).ThrowIfCancellationRequested();
			var key = (string)arguments[0];

			if(method.Name == nameof(IDistributedCache.GetValueExpiryAsync))
			{
				var found = this.Values.TryGetValue(key, out var value);
				return new ValueTask<(string, TimeSpan?)>(found ? (value.Value, value.Expiration - DateTime.UtcNow) : (null, null));
			}

			if(method.Name == nameof(IDistributedCache.SetValueAsync))
			{
				if(!this.AcceptWrites)
					return new ValueTask<bool>(false);

				this.LastExpiry = (TimeSpan)arguments[2];
				this.Values[key] = ((string)arguments[1], DateTime.UtcNow.Add(this.LastExpiry));
				this.Writes++;
				return new ValueTask<bool>(true);
			}

			throw new NotSupportedException();
		}
	}

	private class CredentialLocker : DispatchProxy
	{
		public CredentialLocker() { }
		public bool Available { get; set; } = true;
		public Action Acquired { get; set; }
		public CredentialLease Lease { get; private set; }
		protected override object Invoke(MethodInfo method, object[] arguments)
		{
			Assert.Equal(nameof(IDistributedLockManager.AcquireAsync), method.Name);
			((CancellationToken)arguments[^1]).ThrowIfCancellationRequested();
			var lease = DispatchProxy.Create<IDistributedLock, CredentialLease>();
			this.Lease = (CredentialLease)lease;
			this.Lease.Locked = this.Available;
			this.Acquired?.Invoke();
			return new ValueTask<IDistributedLock>(lease);
		}
	}

	private class CredentialLease : DispatchProxy
	{
		public CredentialLease() { }
		public bool Locked { get; set; }
		protected override object Invoke(MethodInfo method, object[] arguments) => method.Name switch
		{
			"get_IsLocked" => this.Locked,
			nameof(IAsyncDisposable.DisposeAsync) => ValueTask.CompletedTask,
			_ => throw new NotSupportedException(),
		};
	}
}
