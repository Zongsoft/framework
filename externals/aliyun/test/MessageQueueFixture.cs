using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace Zongsoft.Externals.Aliyun.Tests;

public sealed class MessageQueueFixture : IAsyncLifetime
{
	private readonly TestApplicationContext _context;
	private readonly IServiceProvider _services;

	public MessageQueueFixture()
	{
		var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
		{
			["Externals:Aliyun:General:Name"] = "Shenzhen",
			["Externals:Aliyun:General:Certificates:Default"] = "test",
			["Externals:Aliyun:General:Certificates:test:Name"] = "test",
			["Externals:Aliyun:General:Certificates:test:Code"] = "offline-test-key",
			["Externals:Aliyun:General:Certificates:test:Secret"] = "offline-test-secret",
			["Externals:Aliyun:Messaging:Name"] = "offline-test-account",
			["Externals:Aliyun:Messaging:Queues:Region"] = "Shenzhen",
		}).Build();
		_services = new Zongsoft.Services.ServiceProviderFactory().CreateServiceProvider(new ServiceCollection().AddSingleton<IConfigurationRoot>(configuration));
		_context = new TestApplicationContext(_services);
	}

	public Messaging.MessageQueue CreateQueue(HttpMessageHandler transport)
	{
		var queue = new Messaging.MessageQueue("alarms");
		//只替换测试传输，不扩大生产 API；构造和配置解析仍走实际代码。
		var field = typeof(Messaging.MessageQueue).GetField("_http", BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.NotNull(field);
		((HttpClient)field.GetValue(queue)).Dispose();
		field.SetValue(queue, new HttpClient(transport));
		return queue;
	}

	public ValueTask InitializeAsync() => ValueTask.CompletedTask;
	public async ValueTask DisposeAsync()
	{
		try { await _context.DisposeAsync(); }
		finally { await ((IAsyncDisposable)_services).DisposeAsync(); }
	}

	private sealed class TestApplicationContext(IServiceProvider services) : Zongsoft.Services.ApplicationContext(services);
}
