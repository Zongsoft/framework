using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Collections;
using Zongsoft.Components;
using Zongsoft.Security.Privileges;
using Zongsoft.Tests;

namespace Zongsoft.Security.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class AuthenticationAsyncCollection
{
	public const string Name = nameof(AuthenticationAsyncCollection);
}

[Collection(AuthenticationAsyncCollection.Name)]
public class AuthenticationAsyncTest
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task AuthenticateAsync_BoundEventDescriptor_AwaitsStaticEventDispatch(bool authenticated)
	{
		using var scope = new AuthenticationScope();
		using var cancellation = new CancellationTokenSource();
		var name = authenticated ? nameof(Authentication.Authenticated) : nameof(Authentication.Authenticating);
		EventDescriptor descriptor = authenticated ? new EventDescriptor<AuthenticatedEventArgs>(name) : new EventDescriptor<AuthenticatingEventArgs>(name);
		var handler = new NotificationHandler();
		descriptor.Handlers.Add(handler);
		descriptor.Bind(typeof(Authentication), name);
		var authentication = scope.AuthenticateAsync(cancellation.Token);

		try
		{
			await handler.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.False(authentication.IsCompleted);
			Assert.Equal(cancellation.Token, handler.Cancellation);

			if(authenticated)
				Assert.IsType<AuthenticatedEventArgs>(handler.Argument);
			else
				Assert.IsType<AuthenticatingEventArgs>(handler.Argument);

			handler.Release.SetResult();
			await authentication.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.Equal(1, handler.Calls);
		}
		finally
		{
			handler.Release.TrySetResult();
			await Record.ExceptionAsync(() => authentication.WaitAsync(TimeSpan.FromSeconds(5)));
			descriptor.Unbind(typeof(Authentication), name);
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task AuthenticateAsync_PendingNotification_AwaitsEveryHandlerAndForwardsContext(bool authenticated)
	{
		using var scope = new AuthenticationScope();
		using var cancellation = new CancellationTokenSource();
		var source = new AsyncOperationSource();
		var released = false;
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		object notification = null;
		var completedCalls = 0;
		using var pending = new EventRegistration(authenticated, (sender, args, token) =>
		{
			Assert.Null(sender);
			Assert.Equal(cancellation.Token, token);
			notification = args;
			entered.SetResult();
			return source.Operation;
		});
		using var completed = new EventRegistration(authenticated, (_, args, _) =>
		{
			Assert.Same(notification, args);
			completedCalls++;
			return ValueTask.CompletedTask;
		});
		var returned = new TaskCompletionSource<Task<CredentialPrincipal>>(TaskCreationOptions.RunContinuationsAsynchronously);
		var invocation = Task.Run(() => returned.SetResult(scope.AuthenticateAsync(cancellation.Token)));

		try
		{
			var authentication = await returned.Task.WaitAsync(TimeSpan.FromSeconds(5));
			await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.False(authentication.IsCompleted);
			Assert.Equal(1, completedCalls);
			Assert.Equal(0, source.ConsumptionCount);
			Assert.Equal(authenticated ? 1 : 0, scope.Authenticator.Verifications);
			Assert.Equal(authenticated ? 1 : 0, scope.Authority.Registrations);

			if(authenticated)
			{
				var args = Assert.IsType<AuthenticatedEventArgs>(notification);
				Assert.Same(scope.Authority.Principal, args.Principal);
				Assert.Same(scope.Parameters, args.Parameters);
				Assert.True(args.IsAuthenticated);
			}
			else
			{
				var args = Assert.IsType<AuthenticatingEventArgs>(notification);
				Assert.Same(scope.Ticket, args.Ticket);
				Assert.Same(scope.Parameters, args.Parameters);
				Assert.Equal(scope.Authenticator.Name, args.Scheme);
				Assert.Equal("test-scenario", args.Scenario);
			}

			released = true;
			source.Complete();
			var principal = await authentication.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.Same(scope.Authority.Principal, principal);
			Assert.Equal(1, source.ConsumptionCount);
		}
		finally
		{
			if(!released)
				source.Complete();

			await invocation.WaitAsync(TimeSpan.FromSeconds(5));
			await Record.ExceptionAsync(async () => await (await returned.Task).WaitAsync(TimeSpan.FromSeconds(5)));
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task AuthenticateAsync_SynchronousNotificationFailure_StillAwaitsOtherHandlers(bool authenticated)
	{
		using var scope = new AuthenticationScope();
		var failure = new InvalidOperationException();
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var completedCalls = 0;
		using var failed = new EventRegistration(authenticated, (_, _, _) => throw failure);
		using var pending = new EventRegistration(authenticated, (_, _, _) =>
		{
			entered.SetResult();
			return new(release.Task);
		});
		using var completed = new EventRegistration(authenticated, (_, _, _) =>
		{
			completedCalls++;
			return ValueTask.CompletedTask;
		});
		var authentication = scope.AuthenticateAsync();

		try
		{
			await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.False(authentication.IsCompleted);
			Assert.Equal(1, completedCalls);
			Assert.Equal(authenticated ? 1 : 0, scope.Authenticator.Verifications);
			release.SetResult();
			Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => authentication.WaitAsync(TimeSpan.FromSeconds(5))));
		}
		finally
		{
			release.TrySetResult();
			await Record.ExceptionAsync(() => authentication.WaitAsync(TimeSpan.FromSeconds(5)));
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task AuthenticateAsync_CanceledNotification_PreservesCallerToken(bool authenticated)
	{
		using var scope = new AuthenticationScope();
		using var cancellation = new CancellationTokenSource();
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var pending = new EventRegistration(authenticated, (_, _, token) =>
		{
			Assert.Equal(cancellation.Token, token);
			entered.SetResult();
			return new(Task.Delay(Timeout.Infinite, token));
		});
		var authentication = scope.AuthenticateAsync(cancellation.Token);

		try
		{
			await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.False(authentication.IsCompleted);
			cancellation.Cancel();
			var observed = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => authentication.WaitAsync(TimeSpan.FromSeconds(5)));
			Assert.Equal(cancellation.Token, observed.CancellationToken);
			Assert.Equal(authenticated ? 1 : 0, scope.Authority.Registrations);
		}
		finally
		{
			cancellation.Cancel();
			await Record.ExceptionAsync(() => authentication.WaitAsync(TimeSpan.FromSeconds(5)));
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task AuthenticateAsync_AuthenticationCanceled_AwaitsFailureNotificationsAndRetainsBothErrors(bool failNotification)
	{
		using var scope = new AuthenticationScope();
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		var failure = new OperationCanceledException(cancellation.Token);
		var notificationFailure = new InvalidOperationException();
		scope.Authenticator.Verify = _ => ValueTask.FromException<object>(failure);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var completedCalls = 0;
		using var failed = new EventRegistration(true, (_, _, _) => failNotification ? throw notificationFailure : ValueTask.CompletedTask);
		using var pending = new EventRegistration(true, (_, notification, token) =>
		{
			var args = Assert.IsType<AuthenticatedEventArgs>(notification);
			Assert.Same(failure, args.Exception);
			Assert.Null(args.Principal);
			Assert.False(token.CanBeCanceled);
			entered.SetResult();
			return new(release.Task);
		});
		using var completed = new EventRegistration(true, (_, _, _) =>
		{
			completedCalls++;
			return ValueTask.CompletedTask;
		});
		var authentication = scope.AuthenticateAsync(cancellation.Token);

		try
		{
			await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.False(authentication.IsCompleted);
			Assert.Equal(1, completedCalls);
			Assert.Equal(0, scope.Authority.Registrations);
			release.SetResult();

			if(failNotification)
			{
				var observed = await Assert.ThrowsAsync<AggregateException>(() => authentication.WaitAsync(TimeSpan.FromSeconds(5)));
				Assert.Collection(observed.Flatten().InnerExceptions,
					error => Assert.Same(failure, error),
					error => Assert.Same(notificationFailure, error));
			}
			else
				Assert.Same(failure, await Assert.ThrowsAnyAsync<OperationCanceledException>(() => authentication.WaitAsync(TimeSpan.FromSeconds(5))));
		}
		finally
		{
			release.TrySetResult();
			await Record.ExceptionAsync(() => authentication.WaitAsync(TimeSpan.FromSeconds(5)));
		}
	}

	private sealed class EventRegistration : IDisposable
	{
		private readonly Func<object, AuthenticatedEventArgs, CancellationToken, ValueTask> _authenticated;
		private readonly Func<object, AuthenticatingEventArgs, CancellationToken, ValueTask> _authenticating;
		public EventRegistration(bool authenticated, Func<object, object, CancellationToken, ValueTask> handler)
		{
			if(authenticated)
				Authentication.Authenticated += _authenticated = (sender, args, token) => handler(sender, args, token);
			else
				Authentication.Authenticating += _authenticating = (sender, args, token) => handler(sender, args, token);
		}
		public void Dispose()
		{
			if(_authenticated != null)
				Authentication.Authenticated -= _authenticated;

			if(_authenticating != null)
				Authentication.Authenticating -= _authenticating;
		}
	}

	private sealed class NotificationHandler : HandlerBase<object>
	{
		public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public object Argument { get; private set; }
		public CancellationToken Cancellation { get; private set; }
		public int Calls { get; private set; }
		protected override ValueTask OnHandleAsync(object argument, Parameters parameters, CancellationToken cancellation)
		{
			this.Argument = argument;
			this.Cancellation = cancellation;
			this.Calls++;
			this.Entered.SetResult();
			return new(this.Release.Task);
		}
	}

	private sealed class AuthenticationScope : IDisposable
	{
		//公共设置器不接受 null；仅使用反射恢复测试前未初始化的静态引用。
		private static readonly FieldInfo _authority = typeof(Authentication).GetField("_authority", BindingFlags.NonPublic | BindingFlags.Static);
		private readonly object _previousAuthority = _authority.GetValue(null);
		private readonly IChallenger[] _challengers = Authentication.Challengers.ToArray();
		public AuthenticationScope()
		{
			Authentication.Authority = this.Authority;
			Authentication.Authenticators.Add(this.Authenticator);
			Authentication.Challengers.Clear();
		}
		public object Ticket { get; } = new();
		public Parameters Parameters { get; } = new();
		public TestAuthenticator Authenticator { get; } = new();
		public TestAuthority Authority { get; } = new();
		public Task<CredentialPrincipal> AuthenticateAsync(CancellationToken cancellation = default) =>
			Authentication.AuthenticateAsync(this.Authenticator.Name, "test-key", this.Ticket, "test-scenario", this.Parameters, cancellation).AsTask();
		public void Dispose()
		{
			Authentication.Authenticators.Remove(this.Authenticator);
			Authentication.Challengers.Clear();
			foreach(var challenger in _challengers)
				Authentication.Challengers.Add(challenger);

			_authority.SetValue(null, _previousAuthority);
			this.Authority.Principal?.Dispose();
		}
	}

	private sealed class TestAuthenticator : IAuthenticator
	{
		public string Name { get; } = $"async-test-{Guid.NewGuid():N}";
		public int Verifications { get; private set; }
		public Func<CancellationToken, ValueTask<object>> Verify { get; set; } = _ => ValueTask.FromResult<object>(new object());
		public ValueTask<object> VerifyAsync(string key, object data, string scenario, Parameters parameters, CancellationToken cancellation = default)
		{
			this.Verifications++;
			return this.Verify(cancellation);
		}
		public ValueTask<ClaimsIdentity> IssueAsync(object token, string scenario, Parameters parameters, CancellationToken cancellation = default) =>
			ValueTask.FromResult(new ClaimsIdentity([new Claim(ClaimTypes.Name, "test-user")], this.Name));
	}

	private sealed class TestAuthority : ICredentialProvider
	{
		public int Registrations { get; private set; }
		public CredentialPrincipal Principal { get; private set; }
		public event EventHandler<CredentialRegisterEventArgs> Registered { add { } remove { } }
		public event EventHandler<CredentialRegisterEventArgs> Registering { add { } remove { } }
		public event EventHandler<CredentialUnregisterEventArgs> Unregistered { add { } remove { } }
		public event EventHandler<CredentialUnregisterEventArgs> Unregistering { add { } remove { } }
		public ValueTask RegisterAsync(CredentialPrincipal principal, CancellationToken cancellation = default)
		{
			this.Registrations++;
			this.Principal = principal;
			return ValueTask.CompletedTask;
		}
		public ValueTask UnregisterAsync(string credentialId, CancellationToken cancellation = default) => throw new NotSupportedException();
		public ValueTask<CredentialPrincipal> RenewAsync(string credentialId, string token, CancellationToken cancellation = default) => throw new NotSupportedException();
		public ValueTask<CredentialPrincipal> RefreshAsync(string credentialId, CancellationToken cancellation = default) => throw new NotSupportedException();
		public IAsyncEnumerable<CredentialPrincipal> RefreshAsync(string identifier, string scenario, CancellationToken cancellation = default) => throw new NotSupportedException();
		public ValueTask<CredentialPrincipal> GetPrincipalAsync(string credentialId, CancellationToken cancellation = default) => throw new NotSupportedException();
		public IAsyncEnumerable<CredentialPrincipal> GetPrincipalsAsync(string identifier, string scenario, CancellationToken cancellation = default) => throw new NotSupportedException();
	}
}
