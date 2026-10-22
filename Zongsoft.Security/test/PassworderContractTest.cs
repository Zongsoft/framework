using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Xunit;

using Zongsoft.Data;
using Zongsoft.Components;
using Zongsoft.Security.Privileges;

namespace Zongsoft.Security.Tests;

public abstract class PassworderContractTest
{
	private static readonly Identifier _user = new(typeof(IUser), 42u);
	protected abstract Passworder Create(IDataAccess accessor);

	[Fact]
	public async Task SetAndVerify_RoundTripsCompletePasswordAndPreservesIdentity()
	{
		var passworder = this.Create(out var accessor);
		using var cancellation = new CancellationTokenSource();
		Assert.True(await passworder.SetAsync(_user, "contract-password", cancellation.Token));
		Assert.True(Password.TryParse(accessor.Password, out var stored));
		Assert.Equal("SHA256", stored.Algorithm);
		Assert.Equal(10, stored.Factor);
		Assert.True(await passworder.VerifyAsync(_user, "contract-password", cancellation.Token));
		var identifier = await passworder.VerifyAsync("contract-user", "tenant", "contract-password", cancellation.Token);
		Assert.Equal(_user, identifier);
		Assert.False(await passworder.VerifyAsync(_user, "wrong", cancellation.Token));
		Assert.True((await passworder.VerifyAsync("contract-user", "tenant", "wrong", cancellation.Token)).IsEmpty);
		Assert.False(await passworder.VerifyAsync(_user, null, cancellation.Token));
		Assert.True((await passworder.VerifyAsync("contract-user", "tenant", string.Empty, cancellation.Token)).IsEmpty);
		Assert.Equal(cancellation.Token, accessor.Cancellation);
		Assert.Equal(accessor.Queries, accessor.Disposals);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Verify_EmptyStoredPassword_AcceptsOnlyEmptyInput(bool emptyArray)
	{
		var passworder = this.Create(out var accessor);
		accessor.Password = emptyArray ? [] : null;

		foreach(var password in new[] { null, string.Empty })
		{
			Assert.True(await passworder.VerifyAsync(_user, password, TestContext.Current.CancellationToken));
			Assert.Equal(_user, await passworder.VerifyAsync("contract-user", null, password, TestContext.Current.CancellationToken));
		}

		foreach(var password in new[] { "contract-password", " " })
		{
			Assert.False(await passworder.VerifyAsync(_user, password, TestContext.Current.CancellationToken));
			Assert.True((await passworder.VerifyAsync("contract-user", null, password, TestContext.Current.CancellationToken)).IsEmpty);
		}
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Verify_MalformedPassword_RejectsEmptyAndNonemptyInput(bool truncated)
	{
		var passworder = this.Create(out var accessor);
		accessor.Password = truncated ? [(byte)'Z', (byte)'S', 2, 10] : new byte[20];

		foreach(var password in new[] { null, string.Empty, "contract-password" })
		{
			Assert.False(await passworder.VerifyAsync(_user, password, TestContext.Current.CancellationToken));
			Assert.True((await passworder.VerifyAsync("contract-user", null, password, TestContext.Current.CancellationToken)).IsEmpty);
		}
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	public async Task Set_EmptyPassword_ClearsStoredPassword(string password)
	{
		var passworder = this.Create(out var accessor);
		accessor.Password = Password.Generate("before", 0);
		Assert.True(await passworder.SetAsync(_user, password, TestContext.Current.CancellationToken));
		Assert.Null(accessor.Password);
		Assert.True(await passworder.VerifyAsync(_user, null, TestContext.Current.CancellationToken));
		Assert.True(await passworder.VerifyAsync(_user, string.Empty, TestContext.Current.CancellationToken));
		Assert.False(await passworder.VerifyAsync(_user, "before", TestContext.Current.CancellationToken));
	}

	[Fact]
	public async Task Verify_MissingAccount_PreservesInvalidIdentityReason()
	{
		var passworder = this.Create(out var accessor);
		accessor.Exists = false;
		Assert.False(await passworder.VerifyAsync(_user, "contract-password", TestContext.Current.CancellationToken));
		var error = await Assert.ThrowsAsync<AuthenticationException>(() => passworder.VerifyAsync("missing", null, "contract-password", TestContext.Current.CancellationToken).AsTask());
		Assert.Equal(SecurityReasons.InvalidIdentity, error.Reason);
	}

	[Fact]
	public async Task Verify_DisabledAccount_RejectsBeforePasswordValidation()
	{
		var passworder = this.Create(out var accessor);
		accessor.Enabled = false;
		var error = await Assert.ThrowsAsync<AuthenticationException>(() => passworder.VerifyAsync(_user, null, TestContext.Current.CancellationToken).AsTask());
		Assert.Equal(SecurityReasons.AccountDisabled, error.Reason);
		error = await Assert.ThrowsAsync<AuthenticationException>(() => passworder.VerifyAsync("disabled", null, null, TestContext.Current.CancellationToken).AsTask());
		Assert.Equal(SecurityReasons.AccountDisabled, error.Reason);
	}

	[Fact]
	public async Task EmptyIdentifierAndCancellation_DoNotAccessStorage()
	{
		var passworder = this.Create(out var accessor);
		Assert.False(await passworder.SetAsync(default, "contract-password", TestContext.Current.CancellationToken));
		Assert.False(await passworder.VerifyAsync(default(Identifier), "contract-password", TestContext.Current.CancellationToken));
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => passworder.SetAsync(_user, "contract-password", cancellation.Token).AsTask());
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => passworder.VerifyAsync(_user, "contract-password", cancellation.Token).AsTask());
		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => passworder.VerifyAsync("user", null, "contract-password", cancellation.Token).AsTask());
		Assert.Equal(0, accessor.Queries);
		Assert.Equal(0, accessor.Updates);
	}

	[Fact]
	public async Task Set_NoAffectedRow_ReturnsFalse()
	{
		var passworder = this.Create(out var accessor);
		accessor.Exists = false;
		Assert.False(await passworder.SetAsync(_user, "contract-password", TestContext.Current.CancellationToken));
	}

	private Passworder Create(out PasswordAccessor accessor)
	{
		var proxy = DispatchProxy.Create<IDataAccess, PasswordAccessor>();
		accessor = (PasswordAccessor)proxy;
		return this.Create(proxy);
	}
}

public class PasswordAccessor : DispatchProxy
{
	public bool Exists { get; set; } = true;
	public bool Enabled { get; set; } = true;
	public byte[] Password { get; set; }
	public int Queries { get; private set; }
	public int Disposals { get; private set; }
	public int Updates { get; private set; }
	public CancellationToken Cancellation { get; private set; }

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2012", Justification = "DispatchProxy forwards the boxed ValueTask to its caller, which awaits it.")]
	protected override object Invoke(MethodInfo method, object[] arguments)
	{
		this.Cancellation = Assert.Single(arguments.OfType<CancellationToken>());
		if(method.Name == nameof(IDataAccess.SelectAsync))
		{
			this.Queries++;
			return typeof(PasswordAccessor).GetMethod(nameof(this.Select), BindingFlags.NonPublic | BindingFlags.Instance)
				.MakeGenericMethod(method.GetGenericArguments()[0]).Invoke(this, [this.Cancellation]);
		}

		Assert.Equal(nameof(IDataAccess.UpdateAsync), method.Name);
		this.Updates++;
		var index = Array.FindIndex(method.GetParameters(), parameter => parameter.Name == "data");
		var data = DataDictionary.GetDictionary(arguments[index]);
		Assert.Equal("Password", Assert.Single(((IDictionary<string, object>)data).Keys));
		this.Password = data.GetValue<byte[]>("Password", null);
		return ValueTask.FromResult(this.Exists ? 1 : 0);
	}

	private async IAsyncEnumerable<T> Select<T>([EnumeratorCancellation] CancellationToken cancellation)
	{
		try
		{
			await Task.Yield();
			cancellation.ThrowIfCancellationRequested();
			if(!this.Exists)
				yield break;

			var type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
			var record = Activator.CreateInstance(type);
			type.GetProperty("UserId").SetValue(record, 42u);
			type.GetProperty("Enabled").SetValue(record, this.Enabled);
			type.GetProperty("Password").SetValue(record, this.Password);
			yield return (T)record;
		}
		finally
		{
			this.Disposals++;
		}
	}
}
