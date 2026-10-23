using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Zongsoft.Externals.Wechat.Platforms;
using Zongsoft.Externals.Wechat.Web.Controllers;
using Xunit;

namespace Zongsoft.Externals.Wechat.Web.Tests;

public partial class AccountHttpTest
{
	#region 测试方法
	[Theory]
	[InlineData(false)]
	[InlineData(true)]
	public async Task Components_RequireTenantAndRedirectPolicyBeforeCreatingAuthorization(bool policyConfigured)
	{
		var store = new ComponentStore();
		var transport = new ComponentTransport();
		fixture.Store = store;
		fixture.Transport = transport;
		var cache = CredentialManager.Cache;
		CredentialManager.Cache = DispatchProxy.Create<Zongsoft.Caching.IDistributedCache, ComponentCache>();
		var builder = WebApplication.CreateBuilder();
		builder.Logging.ClearProviders();
		builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
		builder.Services.AddControllers().AddApplicationPart(typeof(ComponentController).Assembly);
		builder.Services.AddAuthorization(options =>
		{
			if(policyConfigured)
				options.AddPolicy("Wechat.Components", policy => policy.RequireAssertion(context => context.Resource is ComponentController.AuthorizationContext resource && resource.ComponentId == "component" && resource.Context == "tenant" && resource.RedirectUri.Host == "allowed.example.invalid"));
		});
		await using var app = builder.Build();
		app.Use(async (http, next) =>
		{
			if(http.Request.Headers.ContainsKey("Synthetic-Login"))
				http.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "user")], "synthetic"));

			await next(http);
		});
		app.MapControllers();
		await app.StartAsync(TestContext.Current.CancellationToken);

		try
		{
			using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };

			using(var response = await Send(false, "tenant", "allowed.example.invalid"))
				Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

			using(var response = await Send(true, "other-tenant", "allowed.example.invalid"))
				Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

			using(var response = await Send(true, "tenant", "foreign.example.invalid"))
				Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

			Assert.Equal(0, transport.Calls);
			Assert.Null(store.Request);

			using(var response = await Send(true, "tenant", "allowed.example.invalid"))
			{
				Assert.Equal(policyConfigured ? HttpStatusCode.OK : HttpStatusCode.Forbidden, response.StatusCode);
				var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
				Assert.DoesNotContain("server-token", body, StringComparison.Ordinal);
				Assert.DoesNotContain("synthetic-secret", body, StringComparison.Ordinal);
			}

			if(policyConfigured)
			{
				Assert.Equal("user", store.Request.Owner);
				Assert.Equal("tenant", store.Request.Context);
				Assert.Equal("pre-code", store.Request.PreauthorizationCode);
				store.Accept = false;
				using var response = await Send(true, "tenant", "allowed.example.invalid");
				Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
			}

			async Task<HttpResponseMessage> Send(bool login, string scope, string host)
			{
				using var message = new HttpRequestMessage(HttpMethod.Post, "/Externals/Wechat/Components/component/Authorizations")
				{
					Content = System.Net.Http.Json.JsonContent.Create(new { Context = scope, RedirectUri = $"https://{host}/return", AccountType = (int)AccountType.Channel }),
				};

				if(login)
					message.Headers.Add("Synthetic-Login", "yes");

				return await client.SendAsync(message, TestContext.Current.CancellationToken);
			}
		}
		finally
		{
			CredentialManager.Cache = cache;
			await app.StopAsync(TestContext.Current.CancellationToken);
			fixture.Store = null;
			fixture.Transport = null;
			transport.Dispose();
		}
	}
	#endregion

	#region 嵌套类型
	private sealed class ComponentStore : IComponentStore
	{
		public bool Accept { get; set; } = true;
		public Component.AuthorizationRequest Request { get; private set; }
		public ValueTask<Component.VerificationTicket> GetTicketAsync(string componentId, CancellationToken cancellation = default) => throw new NotSupportedException();
		public ValueTask<Component.Receipt> ReceiveAsync(Component.Notification notification, CancellationToken cancellation = default) => throw new NotSupportedException();
		public ValueTask<bool> CreateAuthorizationAsync(Component.AuthorizationRequest request, CancellationToken cancellation = default)
		{
			this.Request = request;
			return ValueTask.FromResult(this.Accept);
		}
	}

	private sealed class ComponentTransport : HttpMessageHandler, IHttpMessageHandlerFactory
	{
		public int Calls { get; private set; }
		public HttpMessageHandler CreateHandler(string name)
		{
			Assert.Equal("Wechat.Components", name);
			return this;
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation)
		{
			Assert.EndsWith("api_create_preauthcode?access_token=server-token", request.RequestUri.AbsoluteUri, StringComparison.Ordinal);
			this.Calls++;
			return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = System.Net.Http.Json.JsonContent.Create(new { pre_auth_code = "pre-code", expires_in = 600 }) });
		}
	}

	private class ComponentCache : DispatchProxy
	{
		protected override object Invoke(MethodInfo method, object[] arguments)
		{
			Assert.Equal("GetValueExpiryAsync", method.Name);
			return new ValueTask<(string, TimeSpan?)>(("""{"Value":"server-token","Revision":"revision"}""", TimeSpan.FromMinutes(5)));
		}
	}
	#endregion
}
