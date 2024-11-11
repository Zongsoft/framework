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
 * Copyright (C) 2010-2023 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Plugins.Web library.
 *
 * The Zongsoft.Plugins.Web is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Plugins.Web is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Plugins.Web library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

using Zongsoft.Configuration;

namespace Zongsoft.Web
{
	public static class Application
	{
#if NET7_0_OR_GREATER
		public static WebApplication Web(Action<Microsoft.AspNetCore.Builder.WebApplicationBuilder> configure = null) => Web(null, null, configure);
		public static WebApplication Web(string[] args, Action<Microsoft.AspNetCore.Builder.WebApplicationBuilder> configure = null) => Web(null, args, configure);
		public static WebApplication Web(string name, Action<Microsoft.AspNetCore.Builder.WebApplicationBuilder> configure = null) => Web(name, null, configure);
		public static WebApplication Web(string name, string[] args, Action<Microsoft.AspNetCore.Builder.WebApplicationBuilder> configure = null)
		{
			var builder = new WebApplicationBuilder(name, args, configure);

			//添加 web.option 宿主配置文件
			builder.Configuration.AddOptionFile("web.option", true);

			var app = builder.Build();

			//初始化应用上下文（该初始化器中会加载相应的Web部件）
			var context = (WebApplicationContext)app.Services.GetService(typeof(WebApplicationContext));
			context.Initialize();

			if(app.Environment.IsDevelopment())
				app.UseDeveloperExceptionPage();

			app.UseCors();
			app.UseRequestLocalization();
			app.UseHttpMethodOverride();
			app.UseRouting();
			app.UseAuthentication();
			app.UseAuthorization();
			app.UseStaticFiles();
			app.MapControllers();

			//映射 SignalR 的 Hub 实现者
			SignalR.HubEndpointRouteBuilderExtensions.MapHubs(app);

			return app;
		}
#else
		public static IHost Web(Action<WebHostBuilderContext, IApplicationBuilder> applicate = null) => Web(null, null, null, applicate);
		public static IHost Web(string[] args, Action<WebHostBuilderContext, IApplicationBuilder> applicate = null) => Web(null, args, null, applicate);
		public static IHost Web(Action<IHostBuilder> configure, Action<WebHostBuilderContext, IApplicationBuilder> applicate = null) => Web(null, null, configure, applicate);
		public static IHost Web(string[] args, Action<IHostBuilder> configure, Action<WebHostBuilderContext, IApplicationBuilder> applicate = null) => Web(null, args, configure, applicate);

		public static IHost Web(string name, Action<WebHostBuilderContext, IApplicationBuilder> applicate = null) => Web(name, null, null, applicate);
		public static IHost Web(string name, string[] args, Action<WebHostBuilderContext, IApplicationBuilder> applicate = null) => Web(name, args, null, applicate);
		public static IHost Web(string name, Action<IHostBuilder> configure, Action<WebHostBuilderContext, IApplicationBuilder> applicate = null) => Web(name, null, configure, applicate);
		public static IHost Web(string name, string[] args, Action<IHostBuilder> configure, Action<WebHostBuilderContext, IApplicationBuilder> applicate = null)
		{
			var builder = new WebApplicationBuilder(name, args, host =>
			{
				Configure(host, applicate);
				configure?.Invoke(host);
			});

			//添加 web.option 宿主配置文件
			builder.Configuration.AddOptionFile(System.IO.Path.Combine(builder.Environment.ContentRootPath, "web.option"), true);

			return builder.Build();

			static void Configure(IHostBuilder builder, Action<WebHostBuilderContext, IApplicationBuilder> applicate)
			{
				builder.ConfigureWebHostDefaults(web => web.Configure((ctx, app) =>
				{
					//初始化应用上下文（该初始化器中会加载相应的Web部件）
					var context = (WebApplicationContext)app.ApplicationServices.GetService(typeof(WebApplicationContext));
					context.Initialize();

					if(ctx.HostingEnvironment.IsDevelopment())
						app.UseDeveloperExceptionPage();

					app.UseCors();
					app.UseRequestLocalization();
					app.UseHttpMethodOverride();
					app.UseRouting();
					app.UseAuthentication();
					app.UseAuthorization();
					app.UseStaticFiles();
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
}