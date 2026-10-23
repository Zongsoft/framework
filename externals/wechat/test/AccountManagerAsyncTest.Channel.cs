using System;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography;

using Xunit;

using Zongsoft.Caching;

namespace Zongsoft.Externals.Wechat.Tests;

public sealed partial class AccountManagerAsyncTest
{
	[Fact]
	public async Task ChannelPostmarkAsync_ConcurrentAccountsPreserveSha1Protocol()
	{
		var previous = CredentialManager.Cache;
		var cache = DispatchProxy.Create<IDistributedCache, TicketCache>();
		var tickets = ((TicketCache)cache).Tickets;
		var channels = Enumerable.Range(0, 32).Select(_ => new Channel(new Account(AccountType.Channel, NewCode()))).ToArray();

		foreach(var channel in channels)
			tickets.Add($"Zongsoft.Wechat.Self:Channel:{channel.Account.Code}:ticket:jsapi", System.Text.Json.JsonSerializer.Serialize(new { value = $"offline-ticket-{channel.Account.Code}", revision = "synthetic" }));

		CredentialManager.Cache = cache;

		try
		{
			await Parallel.ForEachAsync(Enumerable.Range(0, 1024), new ParallelOptions { MaxDegreeOfParallelism = 8, CancellationToken = TestContext.Current.CancellationToken }, async (index, cancellation) =>
			{
				var channel = channels[index % channels.Length];
				var url = $"https://example.com/offline/{index}";
				var result = await channel.PostmarkAsync(url, cancellation);
				var text = $"jsapi_ticket=offline-ticket-{channel.Account.Code}&noncestr={result.nonce}&timestamp={result.timestamp}&url={url}";
				using var algorithm = SHA1.Create();
				Assert.Equal(algorithm.ComputeHash(Encoding.UTF8.GetBytes(text)), result.data);
				Assert.Equal(16, result.nonce.Length);
				Assert.True(result.timestamp > 0);
				Assert.True(result.period > TimeSpan.Zero);
			});
		}
		finally
		{
			CredentialManager.Cache = previous;
		}
	}

	//只提供已有票据；任何其它缓存操作均失败，测试不会退回远端获取票据。
	private class TicketCache : DispatchProxy
	{
		public TicketCache() { }
		public Dictionary<string, string> Tickets { get; } = new(StringComparer.Ordinal);

		protected override object Invoke(MethodInfo method, object[] arguments)
		{
			if(method.Name == nameof(IDistributedCache.GetValueExpiryAsync) && method.IsGenericMethod && method.GetGenericArguments()[0] == typeof(string))
			{
				((CancellationToken)arguments[1]).ThrowIfCancellationRequested();
				return new ValueTask<(string, TimeSpan?)>((this.Tickets[(string)arguments[0]], TimeSpan.FromMinutes(5)));
			}

			throw new NotSupportedException();
		}
	}
}
