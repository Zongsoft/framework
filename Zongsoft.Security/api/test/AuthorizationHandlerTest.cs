using System;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

using Zongsoft.Collections;
using Zongsoft.Security.Privileges;
using Zongsoft.Security.Privileges.Web;

namespace Zongsoft.Security.Web.Tests;

public class AuthorizationHandlerTest
{
	[Theory]
	[InlineData(null, null)]
	[InlineData("api-test", null)]
	[InlineData("unregistered-api-test", "Alice")]
	public async Task AnonymousOrUnregisteredIdentity_DoesNotEvaluatePrivileges(string authenticationType, string name)
	{
		using var state = new SecurityState();
		var identity = new ClaimsIdentity(name == null ? [] : [new Claim(ClaimTypes.Name, name)], authenticationType);
		var requirement = new OperationAuthorizationRequirement { Name = "orders:read" };
		var context = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(identity), null);

		await new AuthorizationHandler().HandleAsync(context);

		Assert.False(context.HasSucceeded);
		Assert.False(context.HasFailed);
		Assert.Contains(requirement, context.PendingRequirements);
	}

	[Fact]
	public async Task Administrator_SucceedsWithoutPrivilegeEvaluation()
	{
		using var state = new SecurityState();
		var authorizer = ServiceProxy.Create<IAuthorizer>((method, args) => method.Name == "get_Name" ? "api-test" : throw new InvalidOperationException("Administrators must bypass privilege evaluation."));
		Authorization.Authorizers.Add(authorizer);
		var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "Alice"), new Claim(ClaimTypes.Role, IRole.Administrators)], "api-test");
		var requirement = new OperationAuthorizationRequirement { Name = "orders:read" };
		var context = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(identity), null);

		await new AuthorizationHandler().HandleAsync(context);

		Assert.True(context.HasSucceeded);
		Assert.False(context.HasFailed);
		Assert.Empty(context.PendingRequirements);
	}

	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task MatchingPrivileges_UsesAnySemanticsAndRequestParameters(bool allowed)
	{
		using var state = new SecurityState();
		var privileger = new Privileger();
		privileger.Privileges.Add(new Privilege("denied", new Privilege.Permission("orders", "read")));
		var category = new PrivilegeCategory("nested");
		category.Privileges.Add(new Privilege("allowed", new Privilege.Permission("orders", "read")));
		category.Privileges.Add(new Privilege("unvisited", new Privilege.Permission("orders", "read")));
		privileger.Categories.Add(category);
		var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "Alice")], "api-test");
		var calls = new List<string>();
		var authorizer = ServiceProxy.Create<IAuthorizer>((method, args) =>
		{
			if(method.Name == "get_Name")
				return "api-test";
			if(method.Name == "get_Privileger")
				return privileger;
			Assert.Equal(nameof(IAuthorizer.AuthorizeAsync), method.Name);
			Assert.Same(identity, args[0]);
			var privilege = Assert.IsType<string>(args[1]);
			var parameters = Assert.IsType<Parameters>(args[2]);
			Assert.Equal("tenant-a", parameters["tenant"]);
			Assert.Equal("trace-id", parameters["Trace"]);
			calls.Add(privilege);
			return new ValueTask<bool>(allowed && privilege == "allowed");
		});
		Authorization.Authorizers.Add(authorizer);
		var http = new DefaultHttpContext();
		http.Request.QueryString = new QueryString("?tenant=tenant-a");
		http.Request.Headers["X-Trace"] = "trace-id";
		var requirement = new OperationAuthorizationRequirement { Name = "orders:read" };
		var context = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(identity), http);

		await new AuthorizationHandler().HandleAsync(context);

		Assert.Equal(allowed, context.HasSucceeded);
		Assert.Equal(!allowed, context.HasFailed);
		Assert.Equal(allowed ? new[] { "denied", "allowed" } : ["denied", "allowed", "unvisited"], calls);

		if(!allowed)
			Assert.Same(requirement, Assert.Single(context.PendingRequirements));
	}

	[Fact]
	public async Task SupplementaryIdentity_UsesItsSchemeAndNullParametersForNonHttpResource()
	{
		using var state = new SecurityState();
		var privileger = new Privileger();
		privileger.Privileges.Add(new Privilege("read", new Privilege.Permission("orders", "read")));
		var supplementary = new ClaimsIdentity([new Claim(ClaimTypes.Name, "Second")], "api-test");
		var calls = 0;
		var authorizer = ServiceProxy.Create<IAuthorizer>((method, args) =>
		{
			if(method.Name == "get_Name")
				return "api-test";
			if(method.Name == "get_Privileger")
				return privileger;
			Assert.Same(supplementary, args[0]);
			Assert.Equal("read", args[1]);
			Assert.Null(args[2]);
			calls++;
			return new ValueTask<bool>(true);
		});
		Authorization.Authorizers.Add(authorizer);
		var principal = new ClaimsPrincipal([new ClaimsIdentity([new Claim(ClaimTypes.Name, "First")], "unregistered-api-test"), supplementary]);
		var context = new AuthorizationHandlerContext([new OperationAuthorizationRequirement { Name = "orders:read" }], principal, new object());

		await new AuthorizationHandler().HandleAsync(context);

		Assert.True(context.HasSucceeded);
		Assert.False(context.HasFailed);
		Assert.Equal(1, calls);
	}

	[Fact]
	public async Task DeniedPrimaryIdentity_CannotBeRescuedByAllowedSupplementaryIdentity()
	{
		using var state = new SecurityState();
		var privileger = new Privileger();
		privileger.Privileges.Add(new Privilege("read", new Privilege.Permission("orders", "read")));
		var evaluated = new List<string>();
		var authorizer = ServiceProxy.Create<IAuthorizer>((method, args) =>
		{
			if(method.Name == "get_Name")
				return "api-test";
			if(method.Name == "get_Privileger")
				return privileger;

			var identity = Assert.IsType<ClaimsIdentity>(args[0]);
			evaluated.Add(identity.Name);
			return new ValueTask<bool>(identity.Name == "Allowed");
		});
		Authorization.Authorizers.Add(authorizer);
		var principal = new ClaimsPrincipal
		([
			new ClaimsIdentity([new Claim(ClaimTypes.Name, "Denied")], "api-test"),
			new ClaimsIdentity([new Claim(ClaimTypes.Name, "Allowed")], "api-test"),
		]);
		var context = new AuthorizationHandlerContext([new OperationAuthorizationRequirement { Name = "orders:read" }], principal, null);

		await new AuthorizationHandler().HandleAsync(context);

		Assert.Equal(["Denied", "Allowed"], evaluated);
		Assert.True(context.HasFailed);
		Assert.False(context.HasSucceeded);
		Assert.Single(context.FailureReasons);
	}

	[Fact]
	public async Task NoMatchingPrivilege_FailsRequirement()
	{
		using var state = new SecurityState();
		var authorizer = ServiceProxy.Create<IAuthorizer>((method, args) => method.Name switch
		{
			"get_Name" => "api-test",
			"get_Privileger" => new Privileger(),
			_ => throw new InvalidOperationException(),
		});
		Authorization.Authorizers.Add(authorizer);
		var requirement = new OperationAuthorizationRequirement { Name = "orders:read" };
		var context = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "Alice")], "api-test")), null);

		await new AuthorizationHandler().HandleAsync(context);

		Assert.True(context.HasFailed);
		Assert.False(context.HasSucceeded);
		Assert.Single(context.FailureReasons);
	}
}
