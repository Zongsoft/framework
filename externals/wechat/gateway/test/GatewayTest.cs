using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Xunit;

using Zongsoft.Externals.Wechat.Paying;
using Zongsoft.Externals.Wechat.Gateway.Controllers;

namespace Zongsoft.Externals.Wechat.Gateway.Tests;

[Collection("Gateway")]
public class GatewayTest
{
	[Fact]
	public async Task Http_UsesExplicitRoutesAndAcknowledgesOnlyReliableReceipts()
	{
		var builder = WebApplication.CreateBuilder();
		builder.Logging.ClearProviders();
		builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0));
		builder.Services.AddControllers().AddApplicationPart(typeof(FallbackController).Assembly);
		await using var application = builder.Build();
		application.MapControllers();
		var handler = new Handler();
		FallbackExecutor.Instance.Handlers.Add("Payment", handler);

		try
		{
			await application.StartAsync(TestContext.Current.CancellationToken);
			using var client = new HttpClient { BaseAddress = new Uri(application.Urls.Single()) };

			foreach(var receipt in new[] { FallbackReceipt.Completed, FallbackReceipt.Accepted, FallbackReceipt.Duplicate })
			{
				handler.Receipt = receipt;
				using var result = await SendAsync("payment/1900000100");
				Assert.Equal(HttpStatusCode.NoContent, result.StatusCode);
				Assert.Empty(await result.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
			}

			Assert.Equal("1900000100", handler.Merchant);
			Assert.Equal(3, handler.Calls);

			using(var result = await SendAsync("payment/1900000100?name=refundment&key=other"))
				Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

			using(var result = await SendAsync("payment/1900000100", "text/plain"))
				Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

			using(var result = await SendAsync("unknown/1900000100"))
				Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);

			using(var result = await SendAsync("refundment/1900000100"))
				Assert.Equal(HttpStatusCode.ServiceUnavailable, result.StatusCode);

			Assert.Equal(3, handler.Calls);
			handler.Receipt = FallbackReceipt.None;

			using(var result = await SendAsync("payment/1900000100"))
			{
				Assert.Equal(HttpStatusCode.ServiceUnavailable, result.StatusCode);
				using var json = JsonDocument.Parse(await result.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken));
				Assert.Equal("FAIL", json.RootElement.GetProperty("code").GetString());
			}

			handler.Failure = true;

			using(var result = await SendAsync("payment/1900000100"))
				Assert.Equal(HttpStatusCode.ServiceUnavailable, result.StatusCode);

			async Task<HttpResponseMessage> SendAsync(string path, string contentType = "application/json")
			{
				using var content = new StringContent("{}", Encoding.UTF8, contentType);
				return await client.PostAsync($"/Externals/Wechat/Fallback/{path}", content, TestContext.Current.CancellationToken);
			}
		}
		finally
		{
			FallbackExecutor.Instance.Handlers.Clear();
			await application.StopAsync(TestContext.Current.CancellationToken);
		}
	}

	private sealed class Handler : IFallbackHandler
	{
		public FallbackKind Kind => FallbackKind.Payment;
		public FallbackReceipt Receipt { get; set; }
		public bool Failure { get; set; }
		public int Calls { get; private set; }
		public string Merchant { get; private set; }

		public ValueTask<FallbackReceipt> ReceiveAsync(FallbackRequest request, CancellationToken cancellation = default)
		{
			this.Calls++;
			this.Merchant = request.Merchant;
			return this.Failure ? ValueTask.FromException<FallbackReceipt>(new InvalidOperationException()) : ValueTask.FromResult(this.Receipt);
		}
	}
}
