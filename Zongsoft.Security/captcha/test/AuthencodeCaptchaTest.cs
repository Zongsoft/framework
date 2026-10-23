using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

using SixLabors.ImageSharp;

using Zongsoft.Caching;
using Zongsoft.Services;

namespace Zongsoft.Security.Captcha.Tests;

public class AuthencodeCaptchaTest
{
	private const string PREFIX = "Zongsoft.Security.Captcha:Authencode:";

	[Fact]
	public void Constructor_MissingProvider_ThrowsWithParameterName()
	{
		Assert.Equal("serviceProvider", Assert.Throws<ArgumentNullException>(() => new AuthencodeCaptcha(null)).ParamName);
	}

	[Theory]
	[InlineData("Authencode", true)]
	[InlineData("aUtHeNcOdE", true)]
	[InlineData("Other", false)]
	[InlineData("", false)]
	[InlineData(null, false)]
	public void Match_Scheme_IsCaseInsensitive(string scheme, bool expected)
	{
		var captcha = CacheDouble.Create(out var cache, out var provider);
		Assert.Equal("Authencode", captcha.Scheme);
		Assert.Equal(expected, ((IMatchable<string>)captcha).Match(scheme));
		Assert.Equal(expected, ((IMatchable)captcha).Match(scheme));
		Assert.False(((IMatchable)captcha).Match(42));
		Assert.Empty(cache.Calls);
		Assert.Equal(0, provider.Resolutions);
	}

	[Fact]
	public async Task IssueAsync_CachesChallengeWithExpiryAndReturnsPng()
	{
		var captcha = CacheDouble.Create(out var cache, out _);
		cache.Write = (_, _, _, _, _) => ValueTask.FromResult(true);
		var cancellation = TestContext.Current.CancellationToken;
		var value = await captcha.IssueAsync(null, null, cancellation);
		var context = new DefaultHttpContext();
		var result = Assert.IsType<FileContentResult>(await new AuthencodeCaptchaFormatter().FormatAsync(context, value, cancellation));
		var token = context.Response.Headers[Zongsoft.Web.Http.Headers.Captcha].ToString();
		var call = Assert.Single(cache.Calls);

		Assert.Equal(12, token.Length);
		Assert.Equal(nameof(IDistributedCache.SetValueAsync), call.Method);
		Assert.Equal(PREFIX + token, call.Arguments[0]);
		Assert.Equal(4, Assert.IsType<string>(call.Arguments[1]).Length);
		Assert.Equal(TimeSpan.FromMinutes(10), call.Arguments[2]);
		Assert.Equal(CacheRequisite.Always, call.Arguments[3]);
		Assert.Equal(cancellation, call.Arguments[4]);
		Assert.Equal("image/png", result.ContentType);
		Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, result.FileContents.Take(8));
		using var image = Image.Load(result.FileContents);
		Assert.Equal(120, image.Width);
		Assert.Equal(50, image.Height);
	}

	[Fact]
	public async Task IssueAsync_CacheFailure_PropagatesOriginalException()
	{
		var captcha = CacheDouble.Create(out var cache, out _);
		var failure = new InvalidOperationException();
		cache.Write = (_, _, _, _, _) => ValueTask.FromException<bool>(failure);

		Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => captcha.IssueAsync(null, null, TestContext.Current.CancellationToken).AsTask()));
		Assert.Equal(nameof(IDistributedCache.SetValueAsync), Assert.Single(cache.Calls).Method);
	}

	[Fact]
	public async Task IssueAsync_Canceled_DoesNotWriteChallenge()
	{
		var captcha = CacheDouble.Create(out var cache, out _);
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();

		await Assert.ThrowsAnyAsync<OperationCanceledException>(() => captcha.IssueAsync(null, null, cancellation.Token).AsTask());
		Assert.Empty(cache.Calls);
	}

	[Fact]
	public async Task IssueAnswerAndConfirm_UsesSeparateTokensAndConsumesReceiptOnce()
	{
		var captcha = CacheDouble.Create(out var cache, out _);
		var cancellation = TestContext.Current.CancellationToken;
		string challengeKey = null;
		string challengeCode = null;
		string confirmationKey = null;
		object confirmationValue = null;
		cache.Write = (key, value, expiry, requisite, token) =>
		{
			Assert.Equal(cancellation, token);
			Assert.Equal(CacheRequisite.Always, requisite);

			if(expiry == TimeSpan.FromMinutes(10))
			{
				challengeKey = key;
				challengeCode = Assert.IsType<string>(value);
			}
			else
			{
				Assert.Equal(TimeSpan.FromMinutes(5), expiry);
				confirmationKey = key;
				confirmationValue = value;
				Assert.Equal(challengeKey, PREFIX + Assert.IsType<string>(value));
			}

			return ValueTask.FromResult(true);
		};
		cache.Read = (key, token) =>
		{
			Assert.Equal(challengeKey, key);
			Assert.Equal(cancellation, token);
			return ValueTask.FromResult<object>(challengeCode);
		};
		cache.Take = key =>
		{
			if(key != confirmationKey)
				return (false, null);

			var value = Interlocked.Exchange(ref confirmationValue, null);
			return (value != null, value);
		};
		cache.Delete = (key, token) =>
		{
			Assert.Equal(challengeKey, key);
			Assert.Equal(cancellation, token);
			return ValueTask.FromResult(Interlocked.Exchange(ref challengeCode, null) != null);
		};

		var issued = await captcha.IssueAsync(null, null, cancellation);
		var context = new DefaultHttpContext();
		Assert.IsType<FileContentResult>(await new AuthencodeCaptchaFormatter().FormatAsync(context, issued, cancellation));
		var challengeToken = context.Response.Headers[Zongsoft.Web.Http.Headers.Captcha].ToString();
		Assert.Equal(PREFIX + challengeToken, challengeKey);
		Assert.Equal(4, challengeCode.Length);
		var answer = $"{challengeToken}={challengeCode.ToLowerInvariant()}";
		var confirmation = await captcha.VerifyAsync(answer, null, cancellation);
		Assert.Equal(10, confirmation.Length);
		Assert.Equal(PREFIX + confirmation + "!OK", confirmationKey);

		Assert.False(await captcha.VerifyAsync(challengeToken, cancellation));
		Assert.NotNull(challengeCode);
		Assert.True(await captcha.VerifyAsync(confirmation, cancellation));
		Assert.Null(challengeCode);
		Assert.Null(confirmationValue);
		Assert.False(await captcha.VerifyAsync(confirmation, cancellation));
		Assert.Null(await captcha.VerifyAsync(answer, null, cancellation));
		Assert.Equal(1, cache.Calls.Count(call => call.Method == nameof(IDistributedCache.RemoveAsync)));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("token")]
	[InlineData(":code")]
	[InlineData("=code")]
	[InlineData("token:")]
	[InlineData("token=")]
	[InlineData(123)]
	public async Task VerifyAsync_MalformedAnswer_DoesNotAccessCache(object answer)
	{
		var captcha = CacheDouble.Create(out var cache, out var provider);

		Assert.Null(await captcha.VerifyAsync(answer, null, TestContext.Current.CancellationToken));
		Assert.Empty(cache.Calls);
		Assert.Equal(0, provider.Resolutions);
	}

	[Theory]
	[InlineData(':')]
	[InlineData('=')]
	public async Task VerifyAsync_CorrectAnswer_CreatesBoundConfirmationWithExpiry(char separator)
	{
		var captcha = CacheDouble.Create(out var cache, out _);
		cache.Read = (_, _) => ValueTask.FromResult<object>("aB3d");
		cache.Write = (_, _, _, _, _) => ValueTask.FromResult(true);
		var cancellation = TestContext.Current.CancellationToken;

		var confirmation = await captcha.VerifyAsync($"challenge{separator}Ab3D", null, cancellation);

		Assert.Equal(10, confirmation.Length);
		Assert.Collection(cache.Calls,
			read =>
			{
				Assert.Equal(nameof(IDistributedCache.GetValueAsync), read.Method);
				Assert.Equal(PREFIX + "challenge", read.Arguments[0]);
				Assert.Equal(cancellation, read.Arguments[1]);
			},
			write =>
			{
				Assert.Equal(nameof(IDistributedCache.SetValueAsync), write.Method);
				Assert.Equal(PREFIX + confirmation + "!OK", write.Arguments[0]);
				Assert.Equal("challenge", write.Arguments[1]);
				Assert.Equal(TimeSpan.FromMinutes(5), write.Arguments[2]);
				Assert.Equal(CacheRequisite.Always, write.Arguments[3]);
				Assert.Equal(cancellation, write.Arguments[4]);
			});
	}

	[Theory]
	[InlineData(null)]
	[InlineData("wrong")]
	public async Task VerifyAsync_MissingOrWrongAnswer_DoesNotCreateConfirmation(string cached)
	{
		var captcha = CacheDouble.Create(out var cache, out _);
		cache.Read = (_, _) => ValueTask.FromResult<object>(cached);

		Assert.Null(await captcha.VerifyAsync("challenge:aB3d", null, TestContext.Current.CancellationToken));
		Assert.Equal(nameof(IDistributedCache.GetValueAsync), Assert.Single(cache.Calls).Method);
	}

	[Fact]
	public async Task VerifyAsync_RejectedConfirmationWrite_ReturnsNull()
	{
		var captcha = CacheDouble.Create(out var cache, out _);
		cache.Read = (_, _) => ValueTask.FromResult<object>("aB3d");
		cache.Write = (_, _, _, _, _) => ValueTask.FromResult(false);

		Assert.Null(await captcha.VerifyAsync("challenge:aB3d", null, TestContext.Current.CancellationToken));
		Assert.Equal(new[] { nameof(IDistributedCache.GetValueAsync), nameof(IDistributedCache.SetValueAsync) }, cache.Calls.Select(call => call.Method));
	}

	[Fact]
	public async Task VerifyAsync_CanceledCacheRead_PropagatesCancellationWithoutWriting()
	{
		var captcha = CacheDouble.Create(out var cache, out _);
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		cache.Read = (_, token) => ValueTask.FromCanceled<object>(token);

		var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => captcha.VerifyAsync("challenge:aB3d", null, cancellation.Token).AsTask());
		Assert.Equal(cancellation.Token, failure.CancellationToken);
		Assert.Equal(nameof(IDistributedCache.GetValueAsync), Assert.Single(cache.Calls).Method);
	}

	[Fact]
	public async Task VerifyAsync_CacheReadFailure_PropagatesWithoutWritingConfirmation()
	{
		var captcha = CacheDouble.Create(out var cache, out _);
		var failure = new InvalidOperationException();
		cache.Read = (_, _) => ValueTask.FromException<object>(failure);

		Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => captcha.VerifyAsync("challenge:aB3d", null, TestContext.Current.CancellationToken).AsTask()));
		Assert.Equal(nameof(IDistributedCache.GetValueAsync), Assert.Single(cache.Calls).Method);
	}

	[Fact]
	public async Task VerifyAsync_ConfirmationWriteFailure_PropagatesWithoutConsumingChallenge()
	{
		var captcha = CacheDouble.Create(out var cache, out _);
		cache.Read = (_, _) => ValueTask.FromResult<object>("aB3d");
		var failure = new InvalidOperationException();
		cache.Write = (_, _, _, _, _) => ValueTask.FromException<bool>(failure);

		Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => captcha.VerifyAsync("challenge:aB3d", null, TestContext.Current.CancellationToken).AsTask()));
		Assert.Equal(new[] { nameof(IDistributedCache.GetValueAsync), nameof(IDistributedCache.SetValueAsync) }, cache.Calls.Select(call => call.Method));
	}

	[Fact]
	public async Task VerifyAsync_RepeatedAnswers_KeepChallengeUntilFinalConfirmation()
	{
		var captcha = CacheDouble.Create(out var cache, out _);
		cache.Read = (_, _) => ValueTask.FromResult<object>("aB3d");
		cache.Write = (_, _, _, _, _) => ValueTask.FromResult(true);

		var first = await captcha.VerifyAsync("challenge:aB3d", null, TestContext.Current.CancellationToken);
		var second = await captcha.VerifyAsync("challenge=AB3D", null, TestContext.Current.CancellationToken);

		Assert.Equal(10, first.Length);
		Assert.Equal(10, second.Length);
		var calls = cache.Calls.ToArray();
		Assert.Equal(4, calls.Length);
		Assert.Equal(PREFIX + first + "!OK", calls[1].Arguments[0]);
		Assert.Equal(PREFIX + second + "!OK", calls[3].Arguments[0]);
		Assert.All(calls.Where(call => call.Method == nameof(IDistributedCache.SetValueAsync)), call => Assert.Equal("challenge", call.Arguments[1]));
		Assert.DoesNotContain(calls, call => call.Method == nameof(IDistributedCache.Remove) || call.Method == nameof(IDistributedCache.RemoveAsync));
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	public async Task ConfirmAsync_EmptyToken_DoesNotResolveCache(string token)
	{
		var captcha = CacheDouble.Create(out var cache, out var provider);
		Assert.False(await captcha.VerifyAsync(token, TestContext.Current.CancellationToken));
		Assert.Empty(cache.Calls);
		Assert.Equal(0, provider.Resolutions);
	}

	[Theory]
	[InlineData(false, null)]
	[InlineData(false, "challenge")]
	[InlineData(true, null)]
	public async Task ConfirmAsync_MissingOrEmptyConfirmation_DoesNotDeleteChallenge(bool removed, string value)
	{
		var captcha = CacheDouble.Create(out var cache, out _);
		cache.Take = _ => (removed, value);

		Assert.False(await captcha.VerifyAsync("confirmation", TestContext.Current.CancellationToken));
		var call = Assert.Single(cache.Calls);
		Assert.Equal(nameof(IDistributedCache.Remove), call.Method);
		Assert.Equal(PREFIX + "confirmation!OK", call.Arguments[0]);
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public async Task ConfirmAsync_ConsumesConfirmationBeforeDeletingChallenge(bool challengeRemoved)
	{
		var captcha = CacheDouble.Create(out var cache, out _);
		cache.Take = _ => (true, "challenge");
		cache.Delete = (_, _) => ValueTask.FromResult(challengeRemoved);
		var cancellation = TestContext.Current.CancellationToken;

		Assert.True(await captcha.VerifyAsync("confirmation", cancellation));
		Assert.Collection(cache.Calls,
			confirmation =>
			{
				Assert.Equal(nameof(IDistributedCache.Remove), confirmation.Method);
				Assert.Equal(PREFIX + "confirmation!OK", confirmation.Arguments[0]);
			},
			challenge =>
			{
				Assert.Equal(nameof(IDistributedCache.RemoveAsync), challenge.Method);
				Assert.Equal(PREFIX + "challenge", challenge.Arguments[0]);
				Assert.Equal(cancellation, challenge.Arguments[1]);
			});
	}

	[Fact]
	public async Task ConfirmAsync_Replay_OnlyFirstAttemptSucceeds()
	{
		var captcha = CacheDouble.Create(out var cache, out _);
		object confirmation = "challenge";
		cache.Take = _ =>
		{
			var value = Interlocked.Exchange(ref confirmation, null);
			return (value != null, value);
		};
		cache.Delete = (_, _) => ValueTask.FromResult(true);

		Assert.True(await captcha.VerifyAsync("confirmation", TestContext.Current.CancellationToken));
		Assert.False(await captcha.VerifyAsync("confirmation", TestContext.Current.CancellationToken));
		Assert.Equal(new[] { nameof(IDistributedCache.Remove), nameof(IDistributedCache.RemoveAsync), nameof(IDistributedCache.Remove) }, cache.Calls.Select(call => call.Method));
	}

	[Fact]
	public async Task ConfirmAsync_ConcurrentAttempts_HasOneSuccessfulConsumer()
	{
		var captcha = CacheDouble.Create(out var cache, out _);
		object confirmation = "challenge";
		//用原子交换模拟缓存的取出并删除契约，不用先读取再删除模拟。
		cache.Take = _ =>
		{
			var value = Interlocked.Exchange(ref confirmation, null);
			return (value != null, value);
		};
		cache.Delete = (_, _) => ValueTask.FromResult(true);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var cancellation = TestContext.Current.CancellationToken;
		var attempts = Enumerable.Range(0, 8).Select(async _ =>
		{
			await release.Task;
			return await captcha.VerifyAsync("confirmation", cancellation);
		}).ToArray();
		release.SetResult();

		var results = await Task.WhenAll(attempts);

		Assert.Equal(1, results.Count(result => result));
		Assert.Equal(8, cache.Calls.Count(call => call.Method == nameof(IDistributedCache.Remove)));
		Assert.Equal(PREFIX + "challenge", Assert.Single(cache.Calls, call => call.Method == nameof(IDistributedCache.RemoveAsync)).Arguments[0]);
	}

	[Fact]
	public async Task ConfirmAsync_ChallengeDeletionFails_PropagatesFailureAndPreventsReplay()
	{
		var captcha = CacheDouble.Create(out var cache, out _);
		object confirmation = "challenge";
		cache.Take = _ =>
		{
			var value = Interlocked.Exchange(ref confirmation, null);
			return (value != null, value);
		};
		var failure = new InvalidOperationException();
		cache.Delete = (_, _) => ValueTask.FromException<bool>(failure);

		Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => captcha.VerifyAsync("confirmation", TestContext.Current.CancellationToken).AsTask()));
		Assert.False(await captcha.VerifyAsync("confirmation", TestContext.Current.CancellationToken));
		Assert.Equal(1, cache.Calls.Count(call => call.Method == nameof(IDistributedCache.RemoveAsync)));
	}

	[Fact]
	public async Task ConfirmAsync_AtomicRemovalFails_DoesNotDeleteChallenge()
	{
		var captcha = CacheDouble.Create(out var cache, out _);
		var failure = new InvalidOperationException();
		cache.Take = _ => throw failure;

		Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => captcha.VerifyAsync("confirmation", TestContext.Current.CancellationToken).AsTask()));
		Assert.Equal(nameof(IDistributedCache.Remove), Assert.Single(cache.Calls).Method);
	}

	[Fact]
	public async Task ConfirmAsync_ChallengeDeletionCanceled_PropagatesTokenAndPreventsReplay()
	{
		var captcha = CacheDouble.Create(out var cache, out _);
		object confirmation = "challenge";
		cache.Take = _ =>
		{
			var value = Interlocked.Exchange(ref confirmation, null);
			return (value != null, value);
		};
		cache.Delete = (_, token) => ValueTask.FromCanceled<bool>(token);
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();

		var failure = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => captcha.VerifyAsync("confirmation", cancellation.Token).AsTask());
		Assert.Equal(cancellation.Token, failure.CancellationToken);
		Assert.False(await captcha.VerifyAsync("confirmation", TestContext.Current.CancellationToken));
		Assert.Equal(1, cache.Calls.Count(call => call.Method == nameof(IDistributedCache.RemoveAsync)));
	}
}
