using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xunit;

using Zongsoft.Data;
using Zongsoft.Components;
using Zongsoft.Security.Privileges;

namespace Zongsoft.Security.Tests;

[Collection(AuthenticationAsyncCollection.Name)]
public class UserPasswordWorkflowTest
{
	[Fact]
	public async Task ChangePassword_VerifiesBeforeSettingAndForwardsCancellation()
	{
		var passwords = new TestPassworder();
		var service = new UserService(null, passwords);
		using var cancellation = new CancellationTokenSource();
		var identifier = new Identifier(typeof(IUser), 42u);
		var operation = service.ChangePasswordAsync(identifier, "old", "new", cancellation.Token).AsTask();
		Assert.False(operation.IsCompleted);
		Assert.Equal("old", passwords.Verified);
		Assert.Null(passwords.Stored);
		passwords.Verification.SetResult(true);
		Assert.True(await operation);
		Assert.Equal("new", passwords.Stored);
		Assert.Equal(identifier, passwords.Identifier);
		Assert.Equal(cancellation.Token, passwords.Cancellation);
	}

	[Fact]
	public async Task ChangePassword_InvalidOldPassword_DoesNotWrite()
	{
		var passwords = new TestPassworder();
		passwords.Verification.SetResult(false);
		var service = new UserService(null, passwords);
		var error = await Assert.ThrowsAsync<SecurityException>(() => service.ChangePasswordAsync(new(typeof(IUser), 42u), "wrong", "new").AsTask());
		Assert.Equal(SecurityReasons.InvalidPassword, error.Reason);
		Assert.Null(passwords.Stored);
	}

	[Fact]
	public async Task SecretAnswers_RoundTripFiveDistinctAnswers()
	{
		var proxy = DispatchProxy.Create<IDataAccess, AnswerAccessor>();
		var accessor = (AnswerAccessor)proxy;
		var service = new UserService(proxy, null);
		var answers = new[] { "first", "second", "third", "fourth", "fifth" };
		Assert.True(await service.WriteAnswersAsync(answers));
		Assert.Equal(226, accessor.Answers.Length);
		var stored = await service.ReadAnswersAsync();
		Assert.Equal(5, stored.Count);

		for(int i = 0; i < answers.Length; i++)
		{
			Assert.True(Password.TryParse(stored[i], out var password));
			Assert.True(password.Verify(answers[i]));
			Assert.False(password.Verify(answers[(i + 1) % answers.Length]));
			Assert.NotSame(stored[i], stored[(i + 1) % answers.Length]);
		}
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task SecretAnswers_RejectEmptyOrTruncatedCollections(bool empty)
	{
		var proxy = DispatchProxy.Create<IDataAccess, AnswerAccessor>();
		((AnswerAccessor)proxy).Answers = empty ? [3] : [3, 1, 2, 3, 4];
		var service = new UserService(proxy, null);
		Assert.Null(await service.ReadAnswersAsync());
		var error = await Assert.ThrowsAsync<ArgumentException>(() => service.WriteAnswersAsync(["first", "", "third"]).AsTask());
		Assert.Equal("answers", error.ParamName);
	}

	private sealed class UserService : UserServiceBase<IUser>
	{
		private readonly IDataAccess _accessor;
		public UserService(IDataAccess accessor, Passworder passworder) : base(passworder)
		{
			_accessor = accessor;
			this.Attempter = new NoopAttempter();
		}

		public override string Name => "Users";
		protected override IDataAccess Accessor => _accessor;
		protected override ValueTask<Identifier> EnsureIdentityAsync(Identifier identifier, CancellationToken cancellation) => ValueTask.FromResult(identifier);
		protected override void OnValidatePassword(string password) { }
		protected override ICondition GetCriteria(Identifier identifier) => Condition.Equal("UserId", identifier.Value);
		public ValueTask<IReadOnlyList<byte[]>> ReadAnswersAsync() => this.GetSecretAnswerAsync(new(typeof(IUser), 42u), TestContext.Current.CancellationToken);
		public ValueTask<bool> WriteAnswersAsync(string[] answers) => this.SetSecretAnswersAsync(new(typeof(IUser), 42u), answers.Select((_, i) => $"Question {i}").ToArray(), answers, TestContext.Current.CancellationToken);
	}

	private sealed class NoopAttempter : IAttempter
	{
		public IAttempterOptions Options { get; set; }
		public ValueTask<bool> CheckAsync(string key, CancellationToken cancellation = default) => ValueTask.FromResult(true);
		public ValueTask<bool> DoneAsync(string key, CancellationToken cancellation = default) => ValueTask.FromResult(true);
		public ValueTask<bool> FailAsync(string key, CancellationToken cancellation = default) => ValueTask.FromResult(false);
	}

	private sealed class TestPassworder : Passworder
	{
		public TaskCompletionSource<bool> Verification { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public string Verified { get; private set; }
		public string Stored { get; private set; }
		public Identifier Identifier { get; private set; }
		public CancellationToken Cancellation { get; private set; }
		public override ValueTask<bool> VerifyAsync(Identifier identifier, string password, CancellationToken cancellation = default)
		{
			this.Verified = password;
			return new(this.Verification.Task);
		}

		public override ValueTask<bool> SetAsync(Identifier identifier, string password, CancellationToken cancellation = default)
		{
			this.Identifier = identifier;
			this.Cancellation = cancellation;
			this.Stored = password;
			return ValueTask.FromResult(true);
		}

		public override ValueTask<Identifier> VerifyAsync(string identity, string @namespace, string password, CancellationToken cancellation = default) => throw new NotSupportedException();
	}

	public class AnswerAccessor : DispatchProxy
	{
		public byte[] Answers { get; set; }
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2012", Justification = "DispatchProxy forwards the boxed ValueTask to its awaiting caller.")]
		protected override object Invoke(MethodInfo method, object[] arguments)
		{
			if(method.Name == nameof(IDataAccess.UpdateAsync))
			{
				var index = Array.FindIndex(method.GetParameters(), parameter => parameter.Name == "data");
				this.Answers = DataDictionary.GetDictionary(arguments[index]).GetValue<byte[]>("SecretAnswer", null);
				return ValueTask.FromResult(1);
			}

			Assert.Equal(nameof(IDataAccess.SelectAsync), method.Name);
			return typeof(AnswerAccessor).GetMethod(nameof(this.Select), BindingFlags.NonPublic | BindingFlags.Instance)
				.MakeGenericMethod(method.GetGenericArguments()[0]).Invoke(this, null);
		}

		private async IAsyncEnumerable<T> Select<T>()
		{
			await Task.CompletedTask;
			var type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
			var record = Activator.CreateInstance(type);
			type.GetProperty("SecretAnswer").SetValue(record, this.Answers);
			yield return (T)record;
		}
	}
}
