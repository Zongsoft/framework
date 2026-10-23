using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Zongsoft.Externals.Wechat.Platforms;
using Xunit;

namespace Zongsoft.Externals.Wechat.Web.Tests;

public sealed class AccountHttpContext : IAsyncLifetime, IComponentStore, IHttpMessageHandlerFactory
{
	#region 成员字段
	private readonly IServiceProvider _services;
	private readonly IConfigurationRoot _configuration;
	private readonly Context _context;
	#endregion

	#region 构造函数
	public AccountHttpContext()
	{
		_configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
		{
			["Externals:Wechat:Components:component:Name"] = "component",
			["Externals:Wechat:Components:component:Secret"] = "synthetic-secret",
			["Externals:Wechat:Components:component:Token"] = "synthetic-token",
			["Externals:Wechat:Components:component:EncodingAESKey"] = new string('A', 43),
		}).Build();
		_services = new Zongsoft.Services.ServiceProviderFactory().CreateServiceProvider(new ServiceCollection().AddSingleton<IConfigurationRoot>(_configuration).AddSingleton<IConfiguration>(_configuration).AddSingleton<IComponentStore>(this).AddSingleton<IHttpMessageHandlerFactory>(this));
		_context = new Context(_services);
	}
	#endregion

	#region 公共属性
	public IComponentStore Store { get; set; }
	public IHttpMessageHandlerFactory Transport { get; set; }
	#endregion

	#region 公共方法
	public ValueTask InitializeAsync() => ValueTask.CompletedTask;
	public async ValueTask DisposeAsync()
	{
		await _context.DisposeAsync();
		await ((IAsyncDisposable)_services).DisposeAsync();
		((IDisposable)_configuration).Dispose();
	}

	public HttpMessageHandler CreateHandler(string name) => (this.Transport ?? throw new InvalidOperationException()).CreateHandler(name);
	public ValueTask<Component.VerificationTicket> GetTicketAsync(string componentId, CancellationToken cancellation = default) => this.Store.GetTicketAsync(componentId, cancellation);
	public ValueTask<Component.Receipt> ReceiveAsync(Component.Notification notification, CancellationToken cancellation = default) => this.Store.ReceiveAsync(notification, cancellation);
	public ValueTask<bool> CreateAuthorizationAsync(Component.AuthorizationRequest request, CancellationToken cancellation = default) => this.Store.CreateAuthorizationAsync(request, cancellation);
	#endregion

	#region 嵌套类型
	private sealed class Context(IServiceProvider services) : Zongsoft.Services.ApplicationContext(services);
	#endregion
}
