/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@qq.com>
 *
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;

using Zongsoft.Services;

namespace Zongsoft.Web;

public static class Application
{
	#if NET7_0_OR_GREATER
	public static ValueTask<WebApplication> WebAsync(Action<Microsoft.AspNetCore.Builder.WebApplicationBuilder> configure = null) => WebAsync(null, null, configure);
	public static ValueTask<WebApplication> WebAsync(string[] args, Action<Microsoft.AspNetCore.Builder.WebApplicationBuilder> configure = null) => WebAsync(null, args, configure);
	public static ValueTask<WebApplication> WebAsync(string name, Action<Microsoft.AspNetCore.Builder.WebApplicationBuilder> configure = null) => WebAsync(name, null, configure);
	public static async ValueTask<WebApplication> WebAsync(string name, string[] args, Action<Microsoft.AspNetCore.Builder.WebApplicationBuilder> configure = null)
	{
		var builder = new WebApplicationBuilder(name, args, configure);

		//加载宿主配置文件
		builder.LoadConfiguration();

		//加载 Web 配置文件
		builder.LoadConfiguration("web");

		var app = await builder.BuildAsync().ConfigureAwait(false);

		try
		{
			//初始化应用上下文（该初始化器中会加载相应的Web部件）
			var context = (WebApplicationContext)app.Services.GetService(typeof(WebApplicationContext));
			context.Initialize();

			//获取所有应用初始化器，并依次调用初始化方法
			var initializers = app.Services.ResolveAll<IApplicationInitializer<IApplicationBuilder>>();
			foreach(var initializer in initializers)
				initializer.Initialize(app);

			if(app.Environment.IsDevelopment())
				app.UseDeveloperExceptionPage();

			app.UseCors();
			app.UseRequestLocalization();
			app.UseHttpMethodOverride();
			app.UseRouting();
			app.UseAuthentication();
			app.UseAuthorization();
			app.UseResponseCompression();
			app.UseStaticFiles(app.Services.Resolve<Microsoft.Extensions.Options.IOptions<StaticFileOptions>>()?.Value);

			app.MapControllers();

			//映射 SignalR 的 Hub 实现者
			SignalR.HubEndpointRouteBuilderExtensions.MapHubs(app);

			return app;
		}
		catch(Exception exception)
		{
			try { await app.DisposeAsync().ConfigureAwait(false); }
			catch(Exception cleanup) { throw new AggregateException(exception, cleanup); }

			throw;
		}
	}
	#else
	public static ValueTask<IHost> WebAsync(Action<WebHostBuilderContext, IApplicationBuilder> applicate = null) => WebAsync(null, null, null, applicate);
	public static ValueTask<IHost> WebAsync(string[] args, Action<WebHostBuilderContext, IApplicationBuilder> applicate = null) => WebAsync(null, args, null, applicate);
	public static ValueTask<IHost> WebAsync(Action<IHostBuilder> configure, Action<WebHostBuilderContext, IApplicationBuilder> applicate = null) => WebAsync(null, null, configure, applicate);
	public static ValueTask<IHost> WebAsync(string[] args, Action<IHostBuilder> configure, Action<WebHostBuilderContext, IApplicationBuilder> applicate = null) => WebAsync(null, args, configure, applicate);

	public static ValueTask<IHost> WebAsync(string name, Action<WebHostBuilderContext, IApplicationBuilder> applicate = null) => WebAsync(name, null, null, applicate);
	public static ValueTask<IHost> WebAsync(string name, string[] args, Action<WebHostBuilderContext, IApplicationBuilder> applicate = null) => WebAsync(name, args, null, applicate);
	public static ValueTask<IHost> WebAsync(string name, Action<IHostBuilder> configure, Action<WebHostBuilderContext, IApplicationBuilder> applicate = null) => WebAsync(name, null, configure, applicate);
	public static ValueTask<IHost> WebAsync(string name, string[] args, Action<IHostBuilder> configure, Action<WebHostBuilderContext, IApplicationBuilder> applicate = null)
	{
		var builder = new WebApplicationBuilder(name, args, host =>
		{
			Configure(host, applicate);
			configure?.Invoke(host);
		});

		//加载宿主配置文件
		builder.LoadConfiguration();

		//加载 Web 配置文件
		builder.LoadConfiguration("web");

		return builder.BuildAsync();

		static void Configure(IHostBuilder builder, Action<WebHostBuilderContext, IApplicationBuilder> applicate)
		{
			builder.ConfigureWebHostDefaults(web => web.Configure((ctx, app) =>
			{
				//初始化应用上下文（该初始化器中会加载相应的Web部件）
				var context = (WebApplicationContext)app.ApplicationServices.GetService(typeof(WebApplicationContext));
				context.Initialize();

				//获取所有应用初始化器，并依次调用初始化方法
				var initializers = app.ApplicationServices.ResolveAll<IApplicationInitializer<IApplicationBuilder>>();
				foreach(var initializer in initializers)
					initializer.Initialize(app);

				if(ctx.HostingEnvironment.IsDevelopment())
					app.UseDeveloperExceptionPage();

				app.UseCors();
				app.UseRequestLocalization();
				app.UseHttpMethodOverride();
				app.UseRouting();
				app.UseAuthentication();
				app.UseAuthorization();
				app.UseResponseCompression();
				app.UseStaticFiles(app.ApplicationServices.Resolve<Microsoft.Extensions.Options.IOptions<StaticFileOptions>>()?.Value);

				app.UseEndpoints(endpoints =>
				{
					endpoints.MapControllers();
					SignalR.HubEndpointRouteBuilderExtensions.MapHubs(endpoints);
				});

				applicate?.Invoke(ctx, app);
			}));
		}
	}
	#endif
}