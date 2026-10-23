using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Zongsoft.Collections;
using Zongsoft.Security;
using Zongsoft.Security.Privileges;
using Zongsoft.Externals.Wechat.Web.Controllers;
using Xunit;

namespace Zongsoft.Externals.Wechat.Web.Tests;

public partial class AccountHttpTest(AccountHttpContext fixture) : IClassFixture<AccountHttpContext>
{
	[Theory]
	[InlineData(false, "Applets")]
	[InlineData(true, "Applets")]
	[InlineData(false, "Channels")]
	[InlineData(true, "Channels")]
	public async Task Login_IssuesApplicationSessionOnlyAfterAccountAuthorization(bool policyConfigured, string group)
	{
		var builder = WebApplication.CreateBuilder();
		builder.Logging.ClearProviders();
		builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
		builder.Services.AddControllers().AddApplicationPart(typeof(AppletController).Assembly).ConfigureApplicationPartManager(manager => manager.FeatureProviders.Add(new Zongsoft.Web.ControllerFeatureProvider()));
		builder.Services.AddAuthorization(options =>
		{
			if(policyConfigured)
				options.AddPolicy("Wechat.Accounts", policy => policy.RequireAssertion(context => context.Resource is Account account && account.Code == "allowed"));
		});
		await using var application = builder.Build();
		application.MapControllers();
		var authority = DispatchProxy.Create<Zongsoft.Security.ICredentialProvider, CredentialStore>();
		var authenticator = new Authenticator();
		Authentication.Authority ??= DispatchProxy.Create<Zongsoft.Security.ICredentialProvider, CredentialStore>();
		var previous = Authentication.Authority;
		Authentication.Authority = authority;
		Authentication.Authenticators.Add(authenticator);

		try
		{
			await application.StartAsync(TestContext.Current.CancellationToken);
			using var client = new HttpClient { BaseAddress = new Uri(application.Urls.Single()) };

			using(var result = await PostAsync("other/Login?scenario=customer"))
				Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);

			using(var result = await PostAsync("allowed/Login?scenario=customer"))
			{
				Assert.Equal(policyConfigured ? HttpStatusCode.OK : HttpStatusCode.Forbidden, result.StatusCode);
				var body = await result.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
				Assert.DoesNotContain("session_key", body, StringComparison.OrdinalIgnoreCase);
				Assert.DoesNotContain("access_token", body, StringComparison.OrdinalIgnoreCase);
				Assert.DoesNotContain("synthetic-code", body);

				if(policyConfigured)
				{
					var principal = ((CredentialStore)authority).Principal;
					Assert.NotNull(principal);
					Assert.Contains(principal.CredentialId, body);
					Assert.Equal("customer", principal.Scenario);
					Assert.Equal(group == "Applets" ? "Applet:allowed" : "Channel:allowed", authenticator.Key);
				}
			}

			using(var content = new StringContent("{\"code\":\"phone-code\"}", System.Text.Encoding.UTF8, "application/json"))
			using(var result = await client.PostAsync("/Externals/Wechat/Applets/allowed/PhoneNumber", content, TestContext.Current.CancellationToken))
				Assert.Equal(HttpStatusCode.Forbidden, result.StatusCode);

			Assert.Equal(policyConfigured ? 1 : 0, authenticator.Calls);

			using(var result = await PostAsync("allowed%3Dinjected/Login?scenario=customer"))
				Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

			foreach(var path in new[] { "Applets/Credential/allowed", "Applets/Users/allowed", "Channels/Credential/allowed" })
			{
				using var result = await client.GetAsync("/Externals/Wechat/" + path, TestContext.Current.CancellationToken);
				Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
			}

			async Task<HttpResponseMessage> PostAsync(string path)
			{
				using var content = new StringContent("synthetic-code");
				return await client.PostAsync("/Externals/Wechat/" + group + "/" + path, content, TestContext.Current.CancellationToken);
			}
		}
		finally
		{
			Authentication.Authenticators.Remove(authenticator);
			Authentication.Authority = previous;
			await application.StopAsync(TestContext.Current.CancellationToken);
		}
	}

	[Fact]
	public async Task ChannelRoutes_RequireAccountOwnershipAndExplicitPostmarkSites()
	{
		var builder = WebApplication.CreateBuilder();
		builder.Logging.ClearProviders();
		builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
		builder.Services.AddControllers().AddApplicationPart(typeof(ChannelController).Assembly).ConfigureApplicationPartManager(manager => manager.FeatureProviders.Add(new Zongsoft.Web.ControllerFeatureProvider()));
		builder.Services.AddAuthorization(options =>
		{
			options.AddPolicy("Wechat.Accounts", policy => policy.RequireAssertion(context => context.Resource is Account account && account.Code == "allowed"));
			options.AddPolicy("Wechat.JSSDK", policy => policy.RequireAssertion(context => context.Resource is ChannelController.PostmarkContext value && value.Account.Code == "allowed" && value.Url.Host == "allowed.example.test"));
		});
		await using var application = builder.Build();
		application.Use((context, next) =>
		{
			context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "synthetic")], "Test"));
			return next(context);
		});
		application.MapControllers();
		var previous = ChannelManager.Provider;
		var previousCache = CredentialManager.Cache;
		var provider = new MissingAccountProvider();
		ChannelManager.Provider = provider;

		try
		{
			await application.StartAsync(TestContext.Current.CancellationToken);
			using var client = new HttpClient { BaseAddress = new Uri(application.Urls.Single()) };

			using(var content = new StringContent("https://forbidden.example.test/page"))
			using(var response = await client.PostAsync("/Externals/Wechat/Channels/allowed/Postmark", content, TestContext.Current.CancellationToken))
				Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

			Assert.Equal(0, provider.Calls);

			using(var content = new StringContent("https://allowed.example.test/page#fragment"))
			using(var response = await client.PostAsync("/Externals/Wechat/Channels/allowed/Postmark", content, TestContext.Current.CancellationToken))
				Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

			Assert.Equal(1, provider.Calls);

			foreach(var path in new[] { "Users", "Users/open", "Messager/Templates" })
			{
				using var denied = await client.GetAsync("/Externals/Wechat/Channels/other/" + path, TestContext.Current.CancellationToken);
				Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
				using var allowed = await client.GetAsync("/Externals/Wechat/Channels/allowed/" + path, TestContext.Current.CancellationToken);
				Assert.Equal(HttpStatusCode.NotFound, allowed.StatusCode);
			}

			Assert.Equal(4, provider.Calls);
			provider.Present = true;
			CredentialManager.Cache = DispatchProxy.Create<Zongsoft.Caching.IDistributedCache, TicketCache>();

			using(var content = new StringContent("https://allowed.example.test/page#fragment"))
			using(var response = await client.PostAsync("/Externals/Wechat/Channels/allowed/Postmark", content, TestContext.Current.CancellationToken))
			{
				Assert.Equal(HttpStatusCode.OK, response.StatusCode);
				var body = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
				Assert.Equal("allowed", body.GetProperty("appId").GetString());
				var text = $"jsapi_ticket=synthetic-ticket&noncestr={body.GetProperty("nonceStr").GetString()}&timestamp={body.GetProperty("timestamp").GetInt64()}&url=https://allowed.example.test/page";
				Assert.Equal(Convert.ToHexString(System.Security.Cryptography.SHA1.HashData(System.Text.Encoding.UTF8.GetBytes(text))).ToLowerInvariant(), body.GetProperty("signature").GetString());
			}
		}
		finally
		{
			ChannelManager.Provider = previous;
			CredentialManager.Cache = previousCache;
			await application.StopAsync(TestContext.Current.CancellationToken);
		}
	}

	private sealed class MissingAccountProvider : IAccountProvider
	{
		public bool Present { get; set; }
		public int Calls { get; private set; }
		public ValueTask<Account> GetAccountAsync(string code, AccountType type, CancellationToken cancellation = default)
		{
			Assert.Equal("allowed", code);
			Assert.Equal(AccountType.Channel, type);
			this.Calls++;
			return ValueTask.FromResult(this.Present ? Account.Channel(code) : default);
		}
	}

	private class TicketCache : DispatchProxy
	{
		protected override object Invoke(MethodInfo method, object[] arguments)
		{
			Assert.Equal(nameof(Zongsoft.Caching.IDistributedCache.GetValueExpiryAsync), method.Name);
			Assert.Equal("Zongsoft.Wechat.Self:Channel:allowed:ticket:jsapi", arguments[0]);
			return new ValueTask<(string, TimeSpan?)>(("{\"value\":\"synthetic-ticket\",\"revision\":\"test\"}", TimeSpan.FromMinutes(5)));
		}
	}

	private sealed class Authenticator : IAuthenticator
	{
		public string Name => "Wechat";
		public int Calls { get; private set; }
		public string Key { get; private set; }
		public ValueTask<object> VerifyAsync(string key, object data, string scenario, Parameters parameters, CancellationToken cancellation = default)
		{
			this.Calls++;
			this.Key = key;
			Assert.Equal("synthetic-code", data);
			return new ValueTask<object>(new object());
		}
		public ValueTask<ClaimsIdentity> IssueAsync(object identity, string scenario, Parameters parameters, CancellationToken cancellation = default) =>
			ValueTask.FromResult(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "900001"), new Claim(ClaimTypes.Name, "synthetic-user")], "Wechat"));
	}

	private class CredentialStore : DispatchProxy
	{
		public CredentialPrincipal Principal { get; private set; }
		protected override object Invoke(MethodInfo method, object[] arguments)
		{
			Assert.Equal(nameof(Zongsoft.Security.ICredentialProvider.RegisterAsync), method.Name);
			this.Principal = Assert.IsType<CredentialPrincipal>(arguments[0]);
			return ValueTask.CompletedTask;
		}
	}
}
