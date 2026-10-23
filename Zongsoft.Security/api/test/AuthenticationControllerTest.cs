using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Zongsoft.Collections;
using Zongsoft.Security.Privileges;
using Zongsoft.Security.Web.Controllers;

namespace Zongsoft.Security.Web.Tests;

public class AuthenticationControllerTest
{
	[Theory]
	[InlineData(0)]
	[InlineData(1)]
	[InlineData(2)]
	public async Task Signin_ForwardsPayloadParametersAndCancellation_RegistersAndTransformsIdentity(int payloadKind)
	{
		using var state = new SecurityState();
		using var cancellation = new CancellationTokenSource();
		var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "42")], "api-test");
		var ticket = new object();
		var transformed = new object();
		CredentialPrincipal registered = null;
		var calls = new List<string>();
		Authentication.Authority = ServiceProxy.Create<ICredentialProvider>((method, args) =>
		{
			Assert.Equal(nameof(ICredentialProvider.RegisterAsync), method.Name);
			registered = Assert.IsType<CredentialPrincipal>(args[0]);
			Assert.Equal(cancellation.Token, args[1]);
			calls.Add("register");
			return ValueTask.CompletedTask;
		});
		Authentication.Transformer = ServiceProxy.Create<IClaimsPrincipalTransformer>((method, args) =>
		{
			Assert.Same(registered, args[0]);
			calls.Add("transform");
			return transformed;
		});
		var authenticator = ServiceProxy.Create<IAuthenticator>((method, args) =>
		{
			if(method.Name == "get_Name")
				return "api-test";

			if(method.Name == nameof(IAuthenticator.VerifyAsync))
			{
				Assert.Equal("account", args[0]);
				if(payloadKind == 2)
					Assert.Equal("sample", Assert.IsAssignableFrom<IDictionary<string, object>>(args[1])["answer"].ToString());
				else if(payloadKind == 0)
					Assert.Null(args[1]);
				else
					Assert.Equal("sample", args[1]);
				Assert.Equal("web", args[2]);
				var parameters = Assert.IsType<Parameters>(args[3]);
				Assert.Equal("tenant-a", parameters["tenant"]);
				Assert.Equal("trace-id", parameters["Trace"]);
				Assert.Equal(cancellation.Token, args[4]);
				calls.Add("verify");
				return new ValueTask<object>(ticket);
			}

			Assert.Equal(nameof(IAuthenticator.IssueAsync), method.Name);
			Assert.Same(ticket, args[0]);
			Assert.Equal("web", args[1]);
			Assert.Equal(cancellation.Token, args[3]);
			calls.Add("issue");
			return new ValueTask<ClaimsIdentity>(identity);
		});
		Authentication.Authenticators.Add(authenticator);
		Authentication.Challengers.Clear();
		var controller = TestContext.Attach(new AuthenticationController(), payloadKind switch { 0 => null, 2 => "answer=sample", _ => "sample" }, payloadKind == 2 ? "application/x-www-form-urlencoded" : "text/plain");
		controller.Request.QueryString = new QueryString("?tenant=tenant-a");
		controller.Request.Headers["X-Trace"] = "trace-id";

		var result = Assert.IsType<OkObjectResult>(await controller.Signin("api-test", "account", "web", cancellation.Token));

		Assert.Same(transformed, result.Value);
		Assert.Same(identity, registered.Identity);
		Assert.Equal("web", registered.Scenario);
		Assert.Equal(["verify", "issue", "register", "transform"], calls);
		registered.Dispose();
	}

	[Fact]
	public async Task Signin_AuthenticationFailure_ReturnsForbiddenReason()
	{
		using var state = new SecurityState();
		Authentication.Authority = ServiceProxy.Create<ICredentialProvider>((method, args) => throw new InvalidOperationException());
		var controller = TestContext.Attach(new AuthenticationController());
		Assert.IsType<BadRequestResult>(await controller.Signin("missing", "account", " "));

		var result = Assert.IsType<ObjectResult>(await controller.Signin("missing-api-test", "account", "web"));

		Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
		Assert.Equal(SecurityReasons.InvalidArgument, result.Value.GetType().GetProperty("Reason").GetValue(result.Value));
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Renew_CredentialPrincipal_MapsAuthorityResult(bool found)
	{
		using var state = new SecurityState();
		using var cancellation = new CancellationTokenSource();
		using var current = new CredentialPrincipal(new ClaimsIdentity([], "api-test"), "old-id", "old-token", "web");
		using var renewed = new CredentialPrincipal(new ClaimsIdentity([], "api-test"), "new-id", "new-token", "web");
		var calls = 0;
		Authentication.Authority = ServiceProxy.Create<ICredentialProvider>((method, args) =>
		{
			Assert.Equal(nameof(ICredentialProvider.RenewAsync), method.Name);
			Assert.Equal("old-id", args[0]);
			Assert.Equal("renewal-token", args[1]);
			Assert.Equal(cancellation.Token, args[2]);
			calls++;
			return new ValueTask<CredentialPrincipal>(found ? renewed : null);
		});
		Authentication.Transformer = ServiceProxy.Create<IClaimsPrincipalTransformer>((method, args) =>
		{
			Assert.Same(renewed, args[0]);
			return "transformed";
		});
		var controller = TestContext.Attach(new AuthenticationController());
		Assert.IsType<BadRequestResult>(await controller.Renew(" ", cancellation.Token));
		Assert.IsType<UnauthorizedResult>(await controller.Renew("renewal-token", cancellation.Token));
		Assert.Equal(0, calls);
		controller.HttpContext.User = current;

		var result = await controller.Renew("renewal-token", cancellation.Token);

		if(found)
			Assert.Equal("transformed", Assert.IsType<OkObjectResult>(result).Value);
		else
			Assert.IsType<NotFoundResult>(result);
		Assert.Equal(1, calls);
	}

	[Fact]
	public async Task Signout_CredentialPrincipal_UnregistersCredential()
	{
		using var state = new SecurityState();
		using var cancellation = new CancellationTokenSource();
		using var principal = new CredentialPrincipal(new ClaimsIdentity([], "api-test"), "credential-id", "renewal-token", "web");
		var count = 0;
		Authentication.Authority = ServiceProxy.Create<ICredentialProvider>((method, args) =>
		{
			Assert.Equal(nameof(ICredentialProvider.UnregisterAsync), method.Name);
			Assert.Equal("credential-id", args[0]);
			Assert.Equal(cancellation.Token, args[1]);
			count++;
			return ValueTask.CompletedTask;
		});
		var controller = TestContext.Attach(new AuthenticationController());
		await controller.Signout(cancellation.Token);
		Assert.Equal(0, count);
		controller.HttpContext.User = principal;
		await controller.Signout(cancellation.Token);
		Assert.Equal(1, count);
	}

	[Fact]
	public async Task Secret_ForwardsChallengeAndDestination()
	{
		using var cancellation = new CancellationTokenSource();
		var transmitter = new RecordingTransmitter();
		var controller = TestContext.Attach(new AuthenticationController
		{
			Secretor = ServiceProxy.Create<ISecretor>((method, args) => method.Name == "get_Transmitter" ? transmitter : throw new InvalidOperationException()),
		});
		controller.Request.Headers["X-Captcha"] = "captcha-confirmation";

		var result = Assert.IsType<ContentResult>(await controller.SecretAsync("email", "test@example.invalid", "web", "primary", cancellation.Token));

		Assert.Equal("secret-token", result.Content);
		Assert.Equal(new[] { "email", "test@example.invalid", "Authentication", "Singin:web", "captcha-confirmation", "primary", "test@example.invalid" }, transmitter.Arguments);
		Assert.Equal(cancellation.Token, transmitter.Cancellation);
	}

	[Theory]
	[InlineData(null, "destination")]
	[InlineData("email", null)]
	[InlineData("email", "a,b")]
	[InlineData("email", "a;b")]
	[InlineData("email", "a|b")]
	[InlineData("email", "a/b")]
	[InlineData("email", "a\\b")]
	public async Task Secret_InvalidDestination_DoesNotTransmit(string scheme, string destination)
	{
		var controller = TestContext.Attach(new AuthenticationController());
		Assert.Equal(StatusCodes.Status400BadRequest, ((Microsoft.AspNetCore.Mvc.Infrastructure.IStatusCodeActionResult)await controller.SecretAsync(scheme, destination, "web")).StatusCode);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Verify_ForwardsSecretAndMapsSuccess(bool success)
	{
		using var cancellation = new CancellationTokenSource();
		var calls = 0;
		var controller = TestContext.Attach(new AuthenticationController
		{
			Secretor = ServiceProxy.Create<ISecretor>((method, args) =>
			{
				Assert.Equal(nameof(ISecretor.VerifyAsync), method.Name);
				Assert.Equal("verification-token", args[0]);
				Assert.Equal("secret-value", args[1]);
				Assert.Equal(cancellation.Token, args[2]);
				calls++;
				return new ValueTask<(bool, string)>((success, "ignored-extra"));
			}),
		}, "secret-value");

		var result = await controller.VerifyAsync("verification-token", cancellation.Token);

		Assert.Equal(success ? typeof(NoContentResult) : typeof(BadRequestResult), result.GetType());
		Assert.Equal(1, calls);
		Assert.IsType<BadRequestResult>(await TestContext.Attach(new AuthenticationController()).VerifyAsync("verification-token"));
		Assert.IsType<BadRequestResult>(await TestContext.Attach(new AuthenticationController(), "secret").VerifyAsync(null));
	}

	private sealed class RecordingTransmitter : ISecretor.SecretTransmitter
	{
		public string[] Arguments { get; private set; }
		public CancellationToken Cancellation { get; private set; }
		public override ValueTask<string> TransmitAsync(string scheme, string destination, string template, string scenario, string captcha, string channel, string extra, CancellationToken cancellation = default)
		{
			this.Arguments = [scheme, destination, template, scenario, captcha, channel, extra];
			this.Cancellation = cancellation;
			return ValueTask.FromResult("secret-token");
		}
	}
}
