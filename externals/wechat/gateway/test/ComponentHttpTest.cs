using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Zongsoft.Externals.Wechat.Platforms;
using Xunit;

namespace Zongsoft.Externals.Wechat.Gateway.Tests;

[CollectionDefinition("Gateway", DisableParallelization = true)]
public class GatewayCollection;

[Collection("Gateway")]
public class ComponentHttpTest
{
	[Fact]
	public async Task Http_OnlyAcknowledgesVerifiedPersistedComponentEvents()
	{
		var store = new Store();
		var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
		{
			["Externals:Wechat:Components:component:Name"] = "component",
			["Externals:Wechat:Components:component:Secret"] = "synthetic-secret",
			["Externals:Wechat:Components:component:Token"] = "synthetic-token",
			["Externals:Wechat:Components:component:EncodingAESKey"] = new string('A', 43),
		}).Build();
		var services = new Zongsoft.Services.ServiceProviderFactory().CreateServiceProvider(new ServiceCollection().AddSingleton<IConfigurationRoot>(configuration).AddSingleton<IConfiguration>(configuration).AddSingleton<IComponentStore>(store));
		await using var lifetime = (IAsyncDisposable)services;
		await using var context = new Context(services);
		var builder = WebApplication.CreateBuilder();
		builder.Logging.ClearProviders();
		builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
		builder.Services.AddControllers().AddApplicationPart(typeof(Controllers.ComponentController).Assembly);
		await using var app = builder.Build();
		app.MapControllers();
		await app.StartAsync(TestContext.Current.CancellationToken);

		try
		{
			using var client = new HttpClient { BaseAddress = new Uri(app.Urls.Single()) };
			var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
			var plaintext = $"<xml><AppId>component</AppId><CreateTime>{timestamp}</CreateTime><InfoType>component_verify_ticket</InfoType><ComponentVerifyTicket>synthetic-ticket</ComponentVerifyTicket></xml>";
			var ciphertext = CryptographyUtility.Encrypt(plaintext, new string('A', 43), "component");
			var signature = CryptographyUtility.Sign("synthetic-token", timestamp, "nonce", ciphertext);
			var query = $"?timestamp={timestamp}&nonce=nonce&msg_signature={signature}&encrypt_type=aes";

			foreach(var receipt in new[] { Component.Receipt.Accepted, Component.Receipt.Duplicate })
			{
				store.Receipt = receipt;
				using var response = await Send("component", query);
				Assert.Equal(HttpStatusCode.OK, response.StatusCode);
				Assert.Equal("success", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
			}

			Assert.Equal(2, store.Calls);
			Assert.Equal("synthetic-ticket", store.Notification.Value);

			using(var response = await Send("component", query + "&nonce=duplicate"))
				Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

			using(var response = await Send("component", query.Replace(signature, new string('0', 40), StringComparison.Ordinal)))
				Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

			using(var response = await Send("component", query, "application/json"))
				Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

			using(var response = await Send("unknown", query))
				Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

			Assert.Equal(2, store.Calls);
			store.Receipt = Component.Receipt.Rejected;

			using(var response = await Send("component", query))
				Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

			store.Failure = true;

			using(var response = await Send("component", query))
				Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

			async Task<HttpResponseMessage> Send(string componentId, string queryString, string contentType = "application/xml")
			{
				using var body = new StringContent($"<xml><AppId>component</AppId><Encrypt>{ciphertext}</Encrypt></xml>", Encoding.UTF8, contentType);
				return await client.PostAsync($"/Externals/Wechat/Components/{componentId}/Events{queryString}", body, TestContext.Current.CancellationToken);
			}
		}
		finally
		{
			await app.StopAsync(TestContext.Current.CancellationToken);
			((IDisposable)configuration).Dispose();
		}
	}

	private sealed class Context(IServiceProvider services) : Zongsoft.Services.ApplicationContext(services);

	private sealed class Store : IComponentStore
	{
		public Component.Receipt Receipt { get; set; }
		public bool Failure { get; set; }
		public int Calls { get; private set; }
		public Component.Notification Notification { get; private set; }
		public ValueTask<bool> CreateAuthorizationAsync(Component.AuthorizationRequest request, CancellationToken cancellation = default) => throw new NotSupportedException();
		public ValueTask<Component.VerificationTicket> GetTicketAsync(string componentId, CancellationToken cancellation = default) => throw new NotSupportedException();
		public ValueTask<Component.Receipt> ReceiveAsync(Component.Notification notification, CancellationToken cancellation = default)
		{
			this.Calls++;
			this.Notification = notification;
			return this.Failure ? ValueTask.FromException<Component.Receipt>(new InvalidOperationException()) : ValueTask.FromResult(this.Receipt);
		}
	}
}
