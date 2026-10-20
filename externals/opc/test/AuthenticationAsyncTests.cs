using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Opc.Ua;
using Opc.Ua.Identity;

using Xunit;

using Zongsoft.Externals.Opc.Security;

namespace Zongsoft.Externals.Opc.Tests;

public class AuthenticationAsyncTests
{
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task AuthenticateAsync_PendingDecision_IsAwaitedAndForwardsIdentity(bool accepted)
	{
		var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var returned = new TaskCompletionSource<Task<AuthenticationResult>>(TaskCreationOptions.RunContinuationsAsynchronously);
		using var cancellation = new CancellationTokenSource();
		var authenticator = new ControlledAuthenticator((_, _, _) => new(release.Task));

		await using var server = new OpcServer { Authenticator = authenticator };
		var adapter = CreateAdapter(server, UserTokenType.UserName);
		var context = CreateContext(new UserNameIdentityTokenHandler("test-user", "test-password"u8, null));
		var invocation = Task.Run(() => returned.TrySetResult(adapter.AuthenticateAsync(context, cancellation.Token).AsTask()));

		try
		{
			var pending = await returned.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.False(pending.IsCompleted);
			Assert.Equal(1, authenticator.Calls);
			Assert.Same(server, authenticator.Server);
			Assert.Equal(cancellation.Token, authenticator.Cancellation);

			var identity = Assert.IsType<AuthenticationIdentity.Account>(authenticator.Identity);
			Assert.Equal("test-user", identity.UserName);
			Assert.Equal("test-password", identity.Password);

			release.SetResult(accepted);
			var result = await pending.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.Equal(accepted ? AuthenticationOutcome.Accepted : AuthenticationOutcome.Rejected, result.Outcome);

			if(accepted)
			{
				Assert.NotNull(result.Identity);
				Assert.Equal(UserTokenType.UserName, result.Identity.TokenType);
				Assert.Null(result.Error);
			}
			else
			{
				Assert.Null(result.Identity);
				Assert.Equal(StatusCodes.BadSecurityChecksFailed, result.Error.StatusCode);
			}
		}
		finally
		{
			release.TrySetResult(false);
			await invocation.WaitAsync(TimeSpan.FromSeconds(5));
		}
	}

	[Fact]
	public async Task AuthenticateAsync_AuthenticatorFailure_RejectsWithoutIdentity()
	{
		var failure = new InvalidOperationException();
		var authenticator = new ControlledAuthenticator((_, _, _) => ValueTask.FromException<bool>(failure));

		await using var server = new OpcServer { Authenticator = authenticator };
		var adapter = CreateAdapter(server, UserTokenType.UserName);
		var result = await adapter.AuthenticateAsync(CreateContext(new UserNameIdentityTokenHandler("test-user", "test-password"u8, null)));

		Assert.Equal(1, authenticator.Calls);
		Assert.Equal(AuthenticationOutcome.Rejected, result.Outcome);
		Assert.Null(result.Identity);
		Assert.Equal(StatusCodes.BadSecurityChecksFailed, result.Error.StatusCode);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task AuthenticateAsync_Cancellation_PreservesRequestToken(bool preCanceled)
	{
		using var cancellation = new CancellationTokenSource();
		var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var authenticator = new ControlledAuthenticator((_, _, token) => new(release.Task.WaitAsync(token)));
		await using var server = new OpcServer { Authenticator = authenticator };
		var adapter = CreateAdapter(server, UserTokenType.UserName);

		if(preCanceled)
			cancellation.Cancel();

		var pending = adapter.AuthenticateAsync(CreateContext(new UserNameIdentityTokenHandler("test-user", "test-password"u8, null)), cancellation.Token).AsTask();

		if(!preCanceled)
		{
			Assert.False(pending.IsCompleted);
			Assert.Equal(cancellation.Token, authenticator.Cancellation);
			cancellation.Cancel();
		}

		var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.WaitAsync(TimeSpan.FromSeconds(5)));

		Assert.Equal(cancellation.Token, exception.CancellationToken);
		Assert.Equal(preCanceled ? 0 : 1, authenticator.Calls);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task AuthenticateAsync_AnonymousPolicy_IsEnforcedWithoutCallingAuthenticator(bool allowed)
	{
		var authenticator = new ControlledAuthenticator((_, _, _) => throw new InvalidOperationException());
		await using var server = new OpcServer(new OpcServerOptions("offline-authentication") { Anonymous = allowed })
		{
			Authenticator = authenticator,
		};

		var adapter = CreateAdapter(server, UserTokenType.Anonymous);
		var result = await adapter.AuthenticateAsync(CreateContext(new AnonymousIdentityTokenHandler(new AnonymousIdentityToken())));

		Assert.Equal(0, authenticator.Calls);
		Assert.Equal(allowed ? AuthenticationOutcome.Accepted : AuthenticationOutcome.Rejected, result.Outcome);

		if(allowed)
			Assert.NotNull(result.Identity);
		else
		{
			Assert.Null(result.Identity);
			Assert.Equal(StatusCodes.BadIdentityTokenRejected, result.Error.StatusCode);
		}
	}

	private static IUserTokenAuthenticator CreateAdapter(OpcServer server, UserTokenType tokenType)
	{
		//构造真实适配器并从 SDK 公开接口验证行为，无需启动监听或生成证书。
		var type = typeof(OpcServer).GetNestedType("AuthenticationAdapter", BindingFlags.NonPublic);
		Assert.NotNull(type);
		var constructor = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
			[typeof(OpcServer), typeof(UserTokenType)], null);
		Assert.NotNull(constructor);
		return Assert.IsAssignableFrom<IUserTokenAuthenticator>(constructor.Invoke([server, tokenType]));
	}

	private static AuthenticationContext CreateContext(IUserIdentityTokenHandler handler) =>
		new(handler, new UserTokenPolicy(handler.TokenType), new EndpointDescription(), null, default, null);

	private sealed class ControlledAuthenticator(Func<OpcServer, AuthenticationIdentity, CancellationToken, ValueTask<bool>> authenticate) : IAuthenticator
	{
		public int Calls { get; private set; }
		public OpcServer Server { get; private set; }
		public AuthenticationIdentity Identity { get; private set; }
		public CancellationToken Cancellation { get; private set; }

		public ValueTask<bool> AuthenticateAsync(OpcServer server, AuthenticationIdentity identity, CancellationToken cancellation = default)
		{
			this.Calls++;
			this.Server = server;
			this.Identity = identity;
			this.Cancellation = cancellation;
			return authenticate(server, identity, cancellation);
		}
	}
}
