using System;
using System.Threading;
using System.Collections.Generic;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Zongsoft.Collections;
using Zongsoft.Components;
using Zongsoft.Security.Privileges;
using Zongsoft.Security.Web.Controllers;

namespace Zongsoft.Security.Web.Tests;

public class AuthorizationControllerTest
{
	[Theory]
	[InlineData(null)]
	[InlineData("api-test")]
	public void Get_KnownScheme_ReturnsDefinitionCollections(string scheme)
	{
		using var state = new SecurityState();
		var privileger = new Privileger();
		privileger.Privileges.Add(new Privilege("read"));
		privileger.Categories.Add(new PrivilegeCategory("orders"));
		var authorizer = ServiceProxy.Create<IAuthorizer>((method, args) => method.Name switch
		{
			"get_Name" => "api-test",
			"get_Privileger" => privileger,
			_ => throw new InvalidOperationException(),
		});
		Authorization.Authorizers.Add(authorizer);
		Authorization.Authorizer = authorizer;
		var controller = new AuthorizationController();

		var result = Assert.IsType<OkObjectResult>(controller.Get(scheme));

		Assert.Same(privileger.Categories, result.Value.GetType().GetProperty("Categories").GetValue(result.Value));
		Assert.Same(privileger.Privileges, result.Value.GetType().GetProperty("Privileges").GetValue(result.Value));
		Assert.IsType<NotFoundResult>(controller.Get("unknown-api-test"));
	}

	[Theory]
	[InlineData(null, null)]
	[InlineData("User:42", "api-test")]
	[InlineData("77@Role", "api-test")]
	public void GetPrivileges_ForwardsMemberParametersAndCancellation(string member, string scheme)
	{
		using var state = new SecurityState();
		using var cancellation = new CancellationTokenSource();
		var expected = TestContext.Sequence<IPrivilegeEvaluatorResult>();
		var calls = 0;
		var evaluator = ServiceProxy.Create<IPrivilegeEvaluator>((method, args) =>
		{
			Assert.Equal(nameof(IPrivilegeEvaluator.EvaluateAsync), method.Name);
			var identifier = Assert.IsType<Identifier>(args[0]);

			if(member == null)
				Assert.True(identifier.IsEmpty);
			else
			{
				Assert.Equal(typeof(Member), identifier.Type);
				Assert.Equal(Member.Parse(member), Assert.IsType<Member>(identifier.Value));
			}

			var parameters = Assert.IsType<Parameters>(args[1]);
			Assert.Equal("tenant-a", parameters["tenant"]);
			Assert.Equal("trace-id", parameters["Trace"]);
			Assert.Equal(cancellation.Token, args[2]);
			calls++;
			return expected;
		});
		var authorizer = ServiceProxy.Create<IAuthorizer>((method, args) => method.Name switch
		{
			"get_Name" => "api-test",
			"get_Evaluator" => evaluator,
			_ => throw new InvalidOperationException(),
		});
		Authorization.Authorizers.Add(authorizer);
		Authorization.Authorizer = authorizer;
		var controller = TestContext.Attach(new AuthorizationController());
		controller.Request.QueryString = new QueryString("?tenant=tenant-a");
		controller.Request.Headers["X-Trace"] = "trace-id";

		Assert.Same(expected, Assert.IsType<OkObjectResult>(controller.GetPrivileges(member, scheme, cancellation.Token)).Value);
		Assert.IsType<BadRequestResult>(controller.GetPrivileges("invalid-member", scheme, cancellation.Token));
		Assert.IsType<NotFoundResult>(controller.GetPrivileges(member, "unknown-api-test", cancellation.Token));
		Assert.Equal(1, calls);
	}

	[Fact]
	public void MissingAuthorizer_ReturnsNotFound()
	{
		using var state = new SecurityState();
		Authorization.Authorizers.Clear();
		Authorization.Authorizer = null;
		var controller = TestContext.Attach(new AuthorizationController());
		Assert.IsType<NotFoundResult>(controller.Get());
		Assert.IsType<NotFoundResult>(controller.GetPrivileges(null));
	}
}
