using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

using Zongsoft.Services;

namespace Zongsoft.Security.Captcha.Tests;

public class AuthencodeCaptchaFormatterTest
{
	[Theory]
	[InlineData(null)]
	[InlineData("unrelated")]
	[InlineData(123)]
	public async Task FormatAsync_UnrelatedValue_PreservesResponse(object value)
	{
		var context = new DefaultHttpContext();
		context.Response.StatusCode = StatusCodes.Status202Accepted;
		context.Response.Headers[Zongsoft.Web.Http.Headers.Captcha] = "existing";

		Assert.Null(await new AuthencodeCaptchaFormatter().FormatAsync(context, value, TestContext.Current.CancellationToken));
		Assert.Equal("existing", context.Response.Headers[Zongsoft.Web.Http.Headers.Captcha].ToString());
		Assert.Equal(StatusCodes.Status202Accepted, context.Response.StatusCode);
	}

	[Fact]
	public async Task FormatAsync_IssuedChallenge_PreservesPayloadAndOverwritesTokenHeader()
	{
		var captcha = CacheDouble.Create(out var cache, out _);
		cache.Write = (_, _, _, _, _) => ValueTask.FromResult(true);
		var value = await captcha.IssueAsync(null, null, TestContext.Current.CancellationToken);
		var firstContext = new DefaultHttpContext();
		var secondContext = new DefaultHttpContext();
		secondContext.Response.Headers[Zongsoft.Web.Http.Headers.Captcha] = "old-token";
		var formatter = new AuthencodeCaptchaFormatter();

		var first = Assert.IsType<FileContentResult>(await formatter.FormatAsync(firstContext, value, TestContext.Current.CancellationToken));
		var second = Assert.IsType<FileContentResult>(await formatter.FormatAsync(secondContext, value, TestContext.Current.CancellationToken));

		Assert.Same(first.FileContents, second.FileContents);
		Assert.Equal("image/png", second.ContentType);
		Assert.Equal(firstContext.Response.Headers[Zongsoft.Web.Http.Headers.Captcha].ToString(), secondContext.Response.Headers[Zongsoft.Web.Http.Headers.Captcha].ToString());
		Assert.NotEqual("old-token", secondContext.Response.Headers[Zongsoft.Web.Http.Headers.Captcha].ToString());
		Assert.Equal(12, secondContext.Response.Headers[Zongsoft.Web.Http.Headers.Captcha].ToString().Length);
	}

	[Theory]
	[InlineData("Authencode", true)]
	[InlineData("AUTHENCODE", true)]
	[InlineData("Other", false)]
	[InlineData(null, false)]
	public void Match_Scheme_IsCaseInsensitive(string scheme, bool expected)
	{
		var formatter = new AuthencodeCaptchaFormatter();
		Assert.Equal(expected, ((IMatchable<string>)formatter).Match(scheme));
		Assert.Equal(expected, ((IMatchable)formatter).Match(scheme));
		Assert.False(((IMatchable)formatter).Match(new object()));
	}
}
