using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

using Zongsoft.Services;

namespace Zongsoft.Externals.Wechat.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class AccountManagerAsyncCollection
{
	public const string Name = nameof(AccountManagerAsyncCollection);
}

[Collection(AccountManagerAsyncCollection.Name)]
public sealed partial class AccountManagerAsyncTest(AccountManagerTestContext context) : IClassFixture<AccountManagerTestContext>
{
	private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(5);

	[Theory]
	[InlineData(AccountType.Applet)]
	[InlineData(AccountType.Channel)]
	public async Task FindAsync_AwaitsProviderAndCachesCompletedInstance(AccountType type)
	{
		using var cancellation = new CancellationTokenSource();
		var provider = new ControlledProvider();
		using var scope = new ProviderScope(type, provider);
		var code = NewCode();
		var operation = LookupAsync(type, code, false, cancellation.Token);
		var call = await provider.NextAsync();

		try
		{
			Assert.False(operation.IsCompleted);
			AssertCall(call, type, code, cancellation.Token);
			call.Completion.SetResult(new Account(type, code, description: "dynamic-account"));
			var result = await operation.WaitAsync(_timeout, TestContext.Current.CancellationToken);

			AssertAccount(result, type, code, "dynamic-account");
			cancellation.Cancel();
			Assert.Same(result, await LookupAsync(type, code, false, cancellation.Token));
			Assert.Same(result, await LookupAsync(type, code, true, cancellation.Token));
			Assert.Same(result, await LookupAsync(type, new Account(type, code), false, cancellation.Token));
			Assert.Same(result, await LookupAsync(type, new Account(type, code), true, cancellation.Token));
			Assert.Equal(1, provider.Count);
		}
		finally
		{
			call.Completion.TrySetResult(new Account(type, code));
			await ObserveAsync(operation);
		}
	}

	[Theory]
	[InlineData(AccountType.Applet)]
	[InlineData(AccountType.Channel)]
	public async Task FindAndGetAsync_MissingProviderDifferWithoutCaching(AccountType type)
	{
		using var scope = new ProviderScope(type, null);
		var code = NewCode();
		Assert.Null(await LookupAsync(type, code, false, TestContext.Current.CancellationToken));
		await Assert.ThrowsAsync<WechatException>(() => LookupAsync(type, code, true, TestContext.Current.CancellationToken));
		var provider = new ControlledProvider();
		SetProvider(type, provider);
		var operation = LookupAsync(type, code, true, TestContext.Current.CancellationToken);
		var call = await provider.NextAsync();
		call.Completion.SetResult(new Account(type, code));

		AssertAccount(await operation.WaitAsync(_timeout, TestContext.Current.CancellationToken), type, code);
		Assert.Equal(1, provider.Count);
	}

	[Theory]
	[InlineData(AccountType.Applet, false)]
	[InlineData(AccountType.Applet, true)]
	[InlineData(AccountType.Channel, false)]
	[InlineData(AccountType.Channel, true)]
	public async Task FindAndGetAsync_EmptyProviderAccountDifferAndRemainRetryable(AccountType type, bool required)
	{
		var provider = new ControlledProvider();
		using var scope = new ProviderScope(type, provider);
		var code = NewCode();
		var operation = LookupAsync(type, code, required, TestContext.Current.CancellationToken);
		var call = await provider.NextAsync();
		Assert.False(operation.IsCompleted);
		AssertCall(call, type, code, TestContext.Current.CancellationToken);
		call.Completion.SetResult(default);

		if(required)
			await Assert.ThrowsAsync<WechatException>(() => operation.WaitAsync(_timeout, TestContext.Current.CancellationToken));
		else
			Assert.Null(await operation.WaitAsync(_timeout, TestContext.Current.CancellationToken));

		var retry = LookupAsync(type, code, required, TestContext.Current.CancellationToken);
		var retryCall = await provider.NextAsync();
		retryCall.Completion.SetResult(new Account(type, code));
		AssertAccount(await retry.WaitAsync(_timeout, TestContext.Current.CancellationToken), type, code);
		Assert.Equal(2, provider.Count);
	}

	[Theory]
	[InlineData(AccountType.Applet, false, false)]
	[InlineData(AccountType.Applet, false, true)]
	[InlineData(AccountType.Applet, true, false)]
	[InlineData(AccountType.Applet, true, true)]
	[InlineData(AccountType.Channel, false, false)]
	[InlineData(AccountType.Channel, false, true)]
	[InlineData(AccountType.Channel, true, false)]
	[InlineData(AccountType.Channel, true, true)]
	public async Task LookupAsync_FailureOrCancellationIsNotCached(AccountType type, bool required, bool cancel)
	{
		using var cancellation = new CancellationTokenSource();
		var provider = new ControlledProvider();
		using var scope = new ProviderScope(type, provider);
		var code = NewCode();
		var failure = new InvalidOperationException();
		var operation = LookupAsync(type, code, required, cancellation.Token);
		var call = await provider.NextAsync();

		try
		{
			Assert.False(operation.IsCompleted);
			AssertCall(call, type, code, cancellation.Token);

			if(cancel)
				cancellation.Cancel();
			else
				call.Completion.SetException(failure);

			if(cancel)
			{
				var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation.WaitAsync(_timeout, TestContext.Current.CancellationToken));
				Assert.Equal(cancellation.Token, error.CancellationToken);
			}
			else
			{
				Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => operation.WaitAsync(_timeout, TestContext.Current.CancellationToken)));
			}

			var retry = LookupAsync(type, code, required, TestContext.Current.CancellationToken);
			var retryCall = await provider.NextAsync();
			AssertCall(retryCall, type, code, TestContext.Current.CancellationToken);
			retryCall.Completion.SetResult(new Account(type, code));
			var result = await retry.WaitAsync(_timeout, TestContext.Current.CancellationToken);
			AssertAccount(result, type, code);
			Assert.Same(result, await LookupAsync(type, code, false, TestContext.Current.CancellationToken));
			Assert.Equal(2, provider.Count);
		}
		finally
		{
			call.Completion.TrySetResult(new Account(type, code));
			await ObserveAsync(operation);
		}
	}

	[Theory]
	[InlineData(AccountType.Applet)]
	[InlineData(AccountType.Channel)]
	public async Task ConcurrentLookupsAsync_ReturnOneCachedInstanceWithoutSharingPendingQueries(AccountType type)
	{
		using var firstCancellation = new CancellationTokenSource();
		using var secondCancellation = new CancellationTokenSource();
		var provider = new ControlledProvider();
		using var scope = new ProviderScope(type, provider);
		var code = NewCode();
		var first = LookupAsync(type, code, false, firstCancellation.Token);
		var firstCall = await provider.NextAsync();
		var second = LookupAsync(type, code, true, secondCancellation.Token);
		var secondCall = await provider.NextAsync();

		try
		{
			Assert.False(first.IsCompleted);
			Assert.False(second.IsCompleted);
			AssertCall(firstCall, type, code, firstCancellation.Token);
			AssertCall(secondCall, type, code, secondCancellation.Token);
			Assert.Equal(2, provider.Count);
			secondCall.Completion.SetResult(new Account(type, code, description: "second-completed-first"));
			var winner = await second.WaitAsync(_timeout, TestContext.Current.CancellationToken);
			Assert.False(first.IsCompleted);
			firstCall.Completion.SetResult(new Account(type, code, description: "first-completed-second"));
			var follower = await first.WaitAsync(_timeout, TestContext.Current.CancellationToken);

			Assert.Same(winner, follower);
			AssertAccount(winner, type, code, "second-completed-first");
			Assert.Same(winner, await LookupAsync(type, code, false, TestContext.Current.CancellationToken));
			Assert.Equal(2, provider.Count);
		}
		finally
		{
			firstCall.Completion.TrySetResult(new Account(type, code));
			secondCall.Completion.TrySetResult(new Account(type, code));
			await ObserveAsync(first);
			await ObserveAsync(second);
		}
	}

	[Theory]
	[InlineData(AccountType.Applet)]
	[InlineData(AccountType.Channel)]
	public async Task ConcurrentLookupsAsync_CancelingOneDoesNotCancelOther(AccountType type)
	{
		using var cancellation = new CancellationTokenSource();
		var provider = new ControlledProvider();
		using var scope = new ProviderScope(type, provider);
		var code = NewCode();
		var first = LookupAsync(type, code, false, cancellation.Token);
		var firstCall = await provider.NextAsync();
		var second = LookupAsync(type, code, true, TestContext.Current.CancellationToken);
		var secondCall = await provider.NextAsync();

		try
		{
			cancellation.Cancel();
			var error = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first.WaitAsync(_timeout, TestContext.Current.CancellationToken));
			Assert.Equal(cancellation.Token, error.CancellationToken);
			Assert.False(second.IsCompleted);
			AssertCall(firstCall, type, code, cancellation.Token);
			AssertCall(secondCall, type, code, TestContext.Current.CancellationToken);
			secondCall.Completion.SetResult(new Account(type, code));
			var result = await second.WaitAsync(_timeout, TestContext.Current.CancellationToken);

			AssertAccount(result, type, code);
			Assert.Same(result, await LookupAsync(type, code, true, TestContext.Current.CancellationToken));
			Assert.Equal(2, provider.Count);
		}
		finally
		{
			firstCall.Completion.TrySetResult(new Account(type, code));
			secondCall.Completion.TrySetResult(new Account(type, code));
			await ObserveAsync(first);
			await ObserveAsync(second);
		}
	}

	[Theory]
	[InlineData(AccountType.Applet, false)]
	[InlineData(AccountType.Applet, true)]
	[InlineData(AccountType.Channel, false)]
	[InlineData(AccountType.Channel, true)]
	public async Task AccountOverloads_RejectEmptyAndWrongTypeWithoutProvider(AccountType type, bool empty)
	{
		var provider = new ControlledProvider();
		using var scope = new ProviderScope(type, provider);
		var account = empty ? default : new Account(type == AccountType.Applet ? AccountType.Channel : AccountType.Applet, NewCode());
		Assert.Null(await LookupAsync(type, account, false, TestContext.Current.CancellationToken));

		if(empty)
		{
			var error = await Assert.ThrowsAsync<ArgumentNullException>(() => LookupAsync(type, account, true, TestContext.Current.CancellationToken));
			Assert.Equal("account", error.ParamName);
		}
		else
		{
			await Assert.ThrowsAsync<ArgumentException>(() => LookupAsync(type, account, true, TestContext.Current.CancellationToken));
		}

		Assert.Equal(0, provider.Count);
	}

	[Theory]
	[InlineData(AccountType.Applet)]
	[InlineData(AccountType.Channel)]
	public async Task ConfiguredAccounts_AreCachedByNameAndDefault(AccountType type)
	{
		var provider = new ControlledProvider();
		using var scope = new ProviderScope(type, provider);
		var code = type == AccountType.Applet ? context.AppletCode : context.ChannelCode;
		var named = await LookupAsync(type, code, false, TestContext.Current.CancellationToken);

		AssertAccount(named, type, code);
		Assert.Same(named, await LookupAsync(type, code, true, TestContext.Current.CancellationToken));
		Assert.Same(named, await LookupAsync(type, (string)null, true, TestContext.Current.CancellationToken));
		Assert.Same(named, await LookupAsync(type, string.Empty, false, TestContext.Current.CancellationToken));
		Assert.Same(named, await LookupAsync(type, new Account(type, code), true, TestContext.Current.CancellationToken));
		Assert.Equal(0, provider.Count);
	}

	private static string NewCode() => $"offline{Guid.NewGuid():N}";
	private static async Task<object> LookupAsync(AccountType type, string code, bool required, CancellationToken cancellation)
	{
		if(type == AccountType.Applet)
			return required ? await WaitAsync(AppletManager.GetAppletAsync(code, cancellation)) : await WaitAsync(AppletManager.FindAppletAsync(code, cancellation));

		return required ? await WaitAsync(ChannelManager.GetChannelAsync(code, cancellation)) : await WaitAsync(ChannelManager.FindChannelAsync(code, cancellation));
	}

	private static async Task<object> LookupAsync(AccountType type, Account account, bool required, CancellationToken cancellation)
	{
		if(type == AccountType.Applet)
			return required ? await WaitAsync(account.GetAppletAsync(cancellation)) : await WaitAsync(account.FindAppletAsync(cancellation));

		return required ? await WaitAsync(account.GetChannelAsync(cancellation)) : await WaitAsync(account.FindChannelAsync(cancellation));
	}

	private static Task<T> WaitAsync<T>(ValueTask<T> operation) => operation.AsTask().WaitAsync(_timeout, TestContext.Current.CancellationToken);
	private static void AssertAccount(object result, AccountType type, string code, string description = null)
	{
		var account = type == AccountType.Applet ? Assert.IsType<Applet>(result).Account : Assert.IsType<Channel>(result).Account;
		Assert.Equal(type, account.Type);
		Assert.Equal(code, account.Code);
		Assert.Equal(description, account.Description);
	}

	private static void AssertCall(ProviderCall call, AccountType type, string code, CancellationToken cancellation)
	{
		Assert.Equal(type, call.Type);
		Assert.Equal(code, call.Code);
		Assert.Equal(cancellation, call.Cancellation);
	}
	private static void SetProvider(AccountType type, IAccountProvider provider)
	{
		if(type == AccountType.Applet)
			AppletManager.Provider = provider;
		else
			ChannelManager.Provider = provider;
	}
	private static async Task ObserveAsync(Task operation) => await Record.ExceptionAsync(() => operation.WaitAsync(_timeout, TestContext.Current.CancellationToken));

	private sealed class ProviderScope : IDisposable
	{
		private readonly AccountType _type;
		private readonly IAccountProvider _previous;

		public ProviderScope(AccountType type, IAccountProvider provider)
		{
			_type = type;
			_previous = type == AccountType.Applet ? AppletManager.Provider : ChannelManager.Provider;
			SetProvider(type, provider);
		}

		public void Dispose() => SetProvider(_type, _previous);
	}

	private sealed class ProviderCall(string code, AccountType type, CancellationToken cancellation)
	{
		public string Code { get; } = code;
		public AccountType Type { get; } = type;
		public CancellationToken Cancellation { get; } = cancellation;
		public TaskCompletionSource<Account> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
	}

	private sealed class ControlledProvider : IAccountProvider
	{
		private readonly System.Threading.Channels.Channel<ProviderCall> _calls = System.Threading.Channels.Channel.CreateUnbounded<ProviderCall>();
		private int _count;
		public int Count => Volatile.Read(ref _count);

		public async ValueTask<Account> GetAccountAsync(string code, AccountType type, CancellationToken cancellation = default)
		{
			var call = new ProviderCall(code, type, cancellation);
			Interlocked.Increment(ref _count);
			Assert.True(_calls.Writer.TryWrite(call));
			return await call.Completion.Task.WaitAsync(cancellation);
		}

		public async Task<ProviderCall> NextAsync() => await _calls.Reader.ReadAsync(TestContext.Current.CancellationToken).AsTask().WaitAsync(_timeout, TestContext.Current.CancellationToken);
	}
}

public sealed class AccountManagerTestContext : IAsyncLifetime
{
	private readonly IServiceProvider _services;
	private readonly IConfigurationRoot _configuration;
	private readonly TestApplicationContext _application;

	public AccountManagerTestContext()
	{
		Assert.Null(ApplicationContext.Current);
		this.AppletCode = $"configuredapplet{Guid.NewGuid():N}";
		this.ChannelCode = $"configuredchannel{Guid.NewGuid():N}";

		_configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
		{
			["Externals:Wechat:Applets:Default"] = this.AppletCode,
			[$"Externals:Wechat:Applets:{this.AppletCode}:Name"] = this.AppletCode,
			["Externals:Wechat:Channels:Default"] = this.ChannelCode,
			[$"Externals:Wechat:Channels:{this.ChannelCode}:Name"] = this.ChannelCode,
		}).Build();

		_services = new ServiceProviderFactory().CreateServiceProvider(new ServiceCollection()
			.AddSingleton<IConfigurationRoot>(_configuration)
			.AddSingleton<IConfiguration>(_configuration)
			.AddSingleton<System.Net.Http.IHttpClientFactory>(this.HttpClients)
			.AddSingleton<System.Net.Http.IHttpMessageHandlerFactory>(this.HttpClients));

		_application = new(_services);
	}

	public string AppletCode { get; }
	public string ChannelCode { get; }
	public CertificateHttpClientFactory HttpClients { get; } = new();

	public ValueTask InitializeAsync() => ValueTask.CompletedTask;
	public async ValueTask DisposeAsync()
	{
		try
		{
			await _application.DisposeAsync();
		}
		finally
		{
			await ((IAsyncDisposable)_services).DisposeAsync();
			((IDisposable)_configuration).Dispose();
			this.HttpClients.Dispose();
		}
	}

	private sealed class TestApplicationContext(IServiceProvider services) : ApplicationContext(services)
	{
	}

	public sealed class CertificateHttpClientFactory : System.Net.Http.IHttpClientFactory, System.Net.Http.IHttpMessageHandlerFactory, IDisposable
	{
		private readonly List<System.Net.Http.HttpMessageHandler> _handlers = new();
		public Func<System.Net.Http.HttpRequestMessage, CancellationToken, Task<System.Net.Http.HttpResponseMessage>> Handler { get; set; }
		public Func<System.Net.Http.HttpRequestMessage, CancellationToken, Task<System.Net.Http.HttpResponseMessage>> AccountHandler { get; set; }
		public Func<System.Net.Http.HttpRequestMessage, CancellationToken, Task<System.Net.Http.HttpResponseMessage>> CredentialHandler { get; set; }
		public Func<System.Net.Http.HttpRequestMessage, CancellationToken, Task<System.Net.Http.HttpResponseMessage>> ComponentHandler { get; set; }
		public Func<System.Net.Http.HttpRequestMessage, CancellationToken, Task<System.Net.Http.HttpResponseMessage>> PaymentHandler { get; set; }
		public Func<System.Net.Http.HttpMessageHandler> PaymentTransport { get; set; }
		public Func<System.Net.Http.HttpRequestMessage, CancellationToken, Task<System.Net.Http.HttpResponseMessage>> XmlHandler { get; set; }
		public string XmlTransportName { get; private set; }
		public System.Net.Http.HttpMessageHandler CreateHandler(string name)
		{
			System.Net.Http.HttpMessageHandler handler;
			if(name == "Wechat.Accounts")
				handler = new HandlerAdapter(this.AccountHandler ?? throw new InvalidOperationException());
			else if(name == "Wechat.Components")
				handler = new HandlerAdapter(this.ComponentHandler ?? throw new InvalidOperationException());
			else if(name == "Wechat.Credentials")
				handler = new HandlerAdapter(this.CredentialHandler ?? throw new InvalidOperationException());
			else if(name.StartsWith("Wechat.Payments.Xml", StringComparison.Ordinal))
			{
				this.XmlTransportName = name;
				handler = new HandlerAdapter(this.XmlHandler ?? throw new InvalidOperationException());
			}
			else
			{
				Assert.Equal("Wechat.Payments", name);
				handler = this.PaymentTransport?.Invoke() ?? new HandlerAdapter(this.PaymentHandler ?? throw new InvalidOperationException());
			}

			lock(_handlers)
				_handlers.Add(handler);

			return handler;
		}

		public void Dispose()
		{
			foreach(var handler in _handlers)
				handler.Dispose();

			_handlers.Clear();
		}

		public System.Net.Http.HttpClient CreateClient(string name)
		{
			Assert.Equal("Wechat.Certificates", name);
			return new(new HandlerAdapter(this.Handler ?? throw new InvalidOperationException()));
		}

		private sealed class HandlerAdapter(Func<System.Net.Http.HttpRequestMessage, CancellationToken, Task<System.Net.Http.HttpResponseMessage>> handler) : System.Net.Http.HttpMessageHandler
		{
			protected override Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request, CancellationToken cancellation) => handler(request, cancellation);
		}
	}
}
