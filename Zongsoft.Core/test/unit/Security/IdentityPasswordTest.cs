using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Xunit;

using Zongsoft.Data;
using Zongsoft.Components;
using Zongsoft.Security.Privileges;

namespace Zongsoft.Security.Tests;

[Collection(AuthenticationAsyncCollection.Name)]
public class IdentityPasswordTest
{
	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task VerifyAsync_AwaitsPasswordVerificationAndRecordsOutcome(bool success)
	{
		using var services = new ServiceCollection().BuildServiceProvider();
		using var cancellation = new CancellationTokenSource();
		var passworder = new TestPassworder();
		var users = DispatchProxy.Create<IUserService, UserProxy>();
		((UserProxy)users).Passworder = passworder;
		var previous = Authentication.Servicer.Users;
		Authentication.Servicer.Users = users;
		var attempter = new TestAttempter();
		var authenticator = new Authenticator(services) { Attempter = attempter };
		var requirement = new Authentication.IdentityAuthenticatorBase.Requirement("tenant", "account", "contract-password");

		try
		{
			var operation = authenticator.VerifyAsync(null, requirement, null, null, cancellation.Token).AsTask();
			Assert.False(operation.IsCompleted);
			Assert.Equal("account", passworder.Identity);
			Assert.Equal("tenant", passworder.Namespace);
			Assert.Equal("contract-password", passworder.Password);
			Assert.Equal(cancellation.Token, passworder.Cancellation);
			var identifier = new Identifier(typeof(IUser), 42u);
			passworder.Completion.SetResult(success ? identifier : default);

			if(success)
			{
				var ticket = await operation;
				Assert.Equal(identifier, ticket.Identifier);
				Assert.Equal("account", ticket.Identity);
				Assert.Equal("tenant", ticket.Namespace);
			}
			else
			{
				var error = await Assert.ThrowsAsync<AuthenticationException>(() => operation);
				Assert.Equal(SecurityReasons.InvalidPassword, error.Reason);
			}

			Assert.Equal(success ? 1 : 0, attempter.Successes);
			Assert.Equal(success ? 0 : 1, attempter.Failures);
		}
		finally
		{
			passworder.Completion.TrySetResult(default);
			Authentication.Servicer.Users = previous;
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task VerifyAsync_AccountFailure_PreservesReason(bool disabled)
	{
		var reason = disabled ? SecurityReasons.AccountDisabled : SecurityReasons.InvalidIdentity;
		using var services = new ServiceCollection().BuildServiceProvider();
		var passworder = new TestPassworder();
		passworder.Completion.SetException(new AuthenticationException(reason));
		var users = DispatchProxy.Create<IUserService, UserProxy>();
		((UserProxy)users).Passworder = passworder;
		var previous = Authentication.Servicer.Users;
		Authentication.Servicer.Users = users;
		var attempter = new TestAttempter();

		try
		{
			var authenticator = new Authenticator(services) { Attempter = attempter };
			var error = await Assert.ThrowsAsync<AuthenticationException>(() => authenticator.VerifyAsync(null, new(null, "account", "contract-password"), null, null).AsTask());
			Assert.Equal(reason, error.Reason);
			Assert.Equal(0, attempter.Successes);
			Assert.Equal(0, attempter.Failures);
		}
		finally
		{
			Authentication.Servicer.Users = previous;
		}
	}

	private sealed class Authenticator(IServiceProvider services) : Authentication.IdentityAuthenticatorBase(services)
	{
		protected override IDataAccess Accessor => throw new NotSupportedException();
	}

	private sealed class TestPassworder : Passworder
	{
		public TaskCompletionSource<Identifier> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public string Identity { get; private set; }
		public string Namespace { get; private set; }
		public string Password { get; private set; }
		public CancellationToken Cancellation { get; private set; }

		public override ValueTask<Identifier> VerifyAsync(string identity, string @namespace, string password, CancellationToken cancellation = default)
		{
			this.Identity = identity;
			this.Namespace = @namespace;
			this.Password = password;
			this.Cancellation = cancellation;
			return new(this.Completion.Task);
		}

		public override ValueTask<bool> VerifyAsync(Identifier identifier, string password, CancellationToken cancellation = default) => throw new NotSupportedException();
		public override ValueTask<bool> SetAsync(Identifier identifier, string password, CancellationToken cancellation = default) => throw new NotSupportedException();
	}

	public class UserProxy : DispatchProxy
	{
		public Passworder Passworder { get; set; }
		protected override object Invoke(MethodInfo method, object[] arguments) => method.Name == "get_Passworder" ? this.Passworder : throw new NotSupportedException();
	}

	private sealed class TestAttempter : IAttempter
	{
		public IAttempterOptions Options { get; set; }
		public int Successes { get; private set; }
		public int Failures { get; private set; }
		public ValueTask<bool> CheckAsync(string key, CancellationToken cancellation = default) => ValueTask.FromResult(true);
		public ValueTask<bool> DoneAsync(string key, CancellationToken cancellation = default)
		{
			this.Successes++;
			return ValueTask.FromResult(true);
		}

		public ValueTask<bool> FailAsync(string key, CancellationToken cancellation = default)
		{
			this.Failures++;
			return ValueTask.FromResult(false);
		}
	}
}
