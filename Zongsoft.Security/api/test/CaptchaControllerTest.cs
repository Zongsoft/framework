using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Collections;
using Zongsoft.Services;
using Zongsoft.Security.Controllers;

namespace Zongsoft.Security.Web.Tests;

public class CaptchaControllerTest
{
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Issue_StringAndFormRequests_ForwardPayloadParametersAndCancellation(bool form)
	{
		using var cancellation = new CancellationTokenSource();
		var payload = new object();
		var captcha = new Captcha
		{
			Issue = (argument, parameters, token) =>
			{
				AssertRequest(form, argument, parameters, token, cancellation.Token);
				return ValueTask.FromResult(payload);
			},
		};
		using var services = new ServiceCollection().AddSingleton<ICaptcha>(captcha).BuildServiceProvider();
		var controller = Create(services, form);

		var result = Assert.IsType<OkObjectResult>(await controller.IssueAsync("test", cancellation.Token));

		Assert.Same(payload, result.Value);
		Assert.Equal(1, captcha.Calls);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	public async Task Issue_Formatter_MapsFormattedResult(int mode)
	{
		using var cancellation = new CancellationTokenSource();
		var issued = new object();
		object formatted = mode switch { 0 => null, 1 => new FileContentResult([1, 2, 3], "image/png"), _ => "formatted" };
		var formatCalls = 0;
		var captcha = new Captcha { Issue = (_, _, _) => ValueTask.FromResult(issued) };
		var formatter = new Formatter
		{
			Format = (context, value, token) =>
			{
				Assert.Same(issued, value);
				Assert.Equal(cancellation.Token, token);
				context.Response.Headers["X-Captcha"] = "challenge-id";
				formatCalls++;
				return ValueTask.FromResult(formatted);
			},
		};
		using var services = new ServiceCollection().AddSingleton<ICaptcha>(captcha).AddSingleton<ICaptchaFormatter<HttpContext>>(formatter).BuildServiceProvider();
		var controller = Create(services, false);

		var result = await controller.IssueAsync("test", cancellation.Token);

		if(mode == 0)
			Assert.IsType<NoContentResult>(result);
		else if(mode == 1)
			Assert.Same(formatted, result);
		else
			Assert.Same(formatted, Assert.IsType<OkObjectResult>(result).Value);
		Assert.Equal("challenge-id", controller.Response.Headers["X-Captcha"].ToString());
		Assert.Equal(1, formatCalls);
		Assert.Equal(1, captcha.Calls);
	}

	[Fact]
	public async Task Issue_UnformattedActionOrNull_MapsDirectly()
	{
		var direct = new ContentResult { Content = "issued-result" };
		var captcha = new Captcha { Issue = (_, _, _) => ValueTask.FromResult<object>(direct) };
		using var services = new ServiceCollection().AddSingleton<ICaptcha>(captcha).BuildServiceProvider();
		Assert.Same(direct, await Create(services, false).IssueAsync("test"));
		captcha.Issue = (_, _, _) => ValueTask.FromResult<object>(null);
		Assert.IsType<NoContentResult>(await Create(services, false).IssueAsync("test"));
		Assert.Equal(2, captcha.Calls);
	}

	[Theory]
	[InlineData(false, false)]
	[InlineData(false, true)]
	[InlineData(true, false)]
	[InlineData(true, true)]
	public async Task Verify_MapsTokenAndEmptyResult(bool form, bool valid)
	{
		using var cancellation = new CancellationTokenSource();
		var captcha = new Captcha
		{
			Verify = (argument, parameters, token) =>
			{
				AssertRequest(form, argument, parameters, token, cancellation.Token);
				return ValueTask.FromResult(valid ? "confirmation" : string.Empty);
			},
		};
		using var services = new ServiceCollection().AddSingleton<ICaptcha>(captcha).BuildServiceProvider();

		var result = await Create(services, form).VerifyAsync("test", cancellation.Token);

		if(valid)
			Assert.Equal("confirmation", Assert.IsType<ContentResult>(result).Content);
		else
			Assert.IsType<NoContentResult>(result);
		Assert.Equal(1, captcha.Calls);
	}

	[Theory]
	[InlineData(null)]
	[InlineData("")]
	[InlineData("missing")]
	public async Task MissingScheme_ReturnsBadRequestWithoutInvokingServices(string scheme)
	{
		var controller = TestContext.Attach(new CaptchaController(), "payload");
		Assert.Equal(StatusCodes.Status400BadRequest, ((Microsoft.AspNetCore.Mvc.Infrastructure.IStatusCodeActionResult)await controller.IssueAsync(scheme)).StatusCode);
		Assert.Equal(StatusCodes.Status400BadRequest, ((Microsoft.AspNetCore.Mvc.Infrastructure.IStatusCodeActionResult)await controller.VerifyAsync(scheme)).StatusCode);
	}

	private static CaptchaController Create(IServiceProvider services, bool form)
	{
		var controller = TestContext.Attach(new CaptchaController(), form ? "answer=sample" : "sample", form ? "application/x-www-form-urlencoded" : "text/plain", services);
		controller.Request.RouteValues["locale"] = "zh";
		controller.Request.QueryString = new QueryString("?tenant=tenant-a");
		controller.Request.Headers["X-Trace"] = "trace-id";
		return controller;
	}

	private static void AssertRequest(bool form, object argument, Parameters parameters, CancellationToken actual, CancellationToken expected)
	{
		if(form)
		{
			Assert.Equal("sample", Assert.IsAssignableFrom<IFormCollection>(argument)["answer"].ToString());
			Assert.Equal("sample", parameters["answer"]);
		}
		else
			Assert.Equal("sample", argument);

		Assert.Equal("tenant-a", parameters["tenant"]);
		Assert.Equal("trace-id", parameters["Trace"]);
		Assert.Equal("zh", parameters["locale"]);
		Assert.Equal(expected, actual);
	}

	private sealed class Captcha : ICaptcha, IMatchable
	{
		public string Scheme => "test";
		public int Calls { get; private set; }
		public Func<object, Parameters, CancellationToken, ValueTask<object>> Issue { get; set; }
		public Func<object, Parameters, CancellationToken, ValueTask<string>> Verify { get; set; }
		public bool Match(object argument) => Equals(argument, this.Scheme);
		public ValueTask<object> IssueAsync(object argument, Parameters parameters, CancellationToken cancellation = default)
		{
			this.Calls++;
			return this.Issue(argument, parameters, cancellation);
		}
		public ValueTask<string> VerifyAsync(object argument, Parameters parameters, CancellationToken cancellation = default)
		{
			this.Calls++;
			return this.Verify(argument, parameters, cancellation);
		}
		public ValueTask<bool> VerifyAsync(string token, CancellationToken cancellation = default) => throw new InvalidOperationException();
	}

	private sealed class Formatter : ICaptchaFormatter<HttpContext>, IMatchable
	{
		public Func<HttpContext, object, CancellationToken, ValueTask<object>> Format { get; set; }
		public bool Match(object argument) => Equals(argument, "test");
		public ValueTask<object> FormatAsync(HttpContext context, object value, CancellationToken cancellation = default) => this.Format(context, value, cancellation);
	}
}
