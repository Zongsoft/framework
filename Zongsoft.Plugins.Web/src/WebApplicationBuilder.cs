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
using System.Linq;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ApplicationParts;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Web;
using Zongsoft.Web.Security;
using Zongsoft.Plugins;
using Zongsoft.Plugins.Hosting;
using Zongsoft.Security;
using Zongsoft.Services;

namespace Zongsoft.Web
{
#if NET7_0_OR_GREATER
	public partial class WebApplicationBuilder : ApplicationBuilderBase<WebApplication>
	{
		private readonly Microsoft.AspNetCore.Builder.WebApplicationBuilder _builder;
		private readonly Action<Microsoft.AspNetCore.Builder.WebApplicationBuilder> _configure;

		public WebApplicationBuilder(string name, string[] args, Action<Microsoft.AspNetCore.Builder.WebApplicationBuilder> configure = null)
		{
			//注意：在.NET7.0 中必须通过 WebApplicationOptions 来设置环境变量
			var environment = System.Environment.GetEnvironmentVariable(HostDefaults.EnvironmentKey);

			var options = string.IsNullOrWhiteSpace(environment) ?
				new WebApplicationOptions() { ApplicationName = name, Args = args } :
				new WebApplicationOptions() { ApplicationName = name, Args = args, EnvironmentName = environment };

			_builder = WebApplication.CreateBuilder(options);
			_configure = configure;

			//处理应用名为空的情况
			if(string.IsNullOrEmpty(name))
			{
				name = GetApplicationName(_builder.Configuration);

				if(!string.IsNullOrEmpty(name))
				{
					_builder.Environment.ApplicationName = name;
					_builder.Configuration[HostDefaults.ApplicationKey] = name;
					_builder.Host.ConfigureHostOptions((ctx, opt) => ctx.HostingEnvironment.ApplicationName = name);
				}
			}

			//设置服务提供程序工厂
			_builder.Host.UseServiceProviderFactory(new Services.ServiceProviderFactory());

			//挂载插件宿主生命期
			_builder.Services.AddSingleton<IHostLifetime, PluginsHostLifetime>();
		}

		public override IServiceCollection Services => _builder.Services;
		public override ConfigurationManager Configuration => _builder.Configuration;
		public override IHostEnvironment Environment => _builder.Environment;
		public override WebApplication Build()
		{
			_configure?.Invoke(_builder);

			//创建默认的插件环境配置
			var options = this.CreateOptions();

			//添加插件配置文件源到配置管理器中
			((IConfigurationBuilder)_builder.Configuration).Add(new Zongsoft.Configuration.PluginConfigurationSource(options));

			//注册插件服务
			this.RegisterServices(_builder.Services, options);

			return _builder.Build();
		}

		protected override void RegisterServices(IServiceCollection services, PluginOptions options)
		{
			services.AddSingleton<WebApplicationContext>();
			services.AddSingleton<PluginApplicationContext>(provider => provider.GetRequiredService<WebApplicationContext>());
			services.AddSingleton<IApplicationContext>(provider => provider.GetRequiredService<WebApplicationContext>());

			base.RegisterServices(services, options);
			WebApplicationUtility.RegisterServices(services);
		}
	}
#else
	public partial class WebApplicationBuilder : Zongsoft.Plugins.Hosting.ApplicationBuilder
	{
		public WebApplicationBuilder(string name, string[] args, Action<IHostBuilder> configure = null) : base(name, args, configure) { }
		public WebApplicationBuilder(string name, IHostBuilder builder, Action<IHostBuilder> configure = null) : base(name, builder, configure) { }
		protected override void RegisterServices(IServiceCollection services, PluginOptions options)
		{
			services.AddSingleton<WebApplicationContext>();
			services.AddSingleton<PluginApplicationContext>(provider => provider.GetRequiredService<WebApplicationContext>());
			services.AddSingleton<IApplicationContext>(provider => provider.GetRequiredService<WebApplicationContext>());

			base.RegisterServices(services, options);
			WebApplicationUtility.RegisterServices(services);
		}
	}
#endif

	internal static class WebApplicationUtility
	{
		public static void RegisterServices(IServiceCollection services)
		{
			services.AddHttpContextAccessor();
			services.AddSingleton<IControllerActivator, ControllerActivator>();
			services.AddSingleton<IApplicationFeatureProvider, ControllerFeatureProvider>();
			services.AddSingleton<IApplicationFeatureProvider<ControllerFeature>, ControllerFeatureProvider>();
			services.AddSingleton<IApplicationFeatureProvider, SignalR.HubFeatureProvider>();
			services.AddSingleton<IApplicationFeatureProvider<SignalR.HubFeature>, SignalR.HubFeatureProvider>();
			services.AddAuthentication(CredentialPrincipal.Scheme).AddCredentials();

			services.AddCors(options => options.AddDefaultPolicy(builder =>
				builder
				.AllowAnyHeader()
				.AllowAnyMethod()
				.AllowCredentials()
				.WithExposedHeaders(Http.Headers.Pagination)
				.SetIsOriginAllowed(origin => true)));

			services.AddEndpointsApiExplorer();

			services
				.AddSignalR(options => { });

			services
				.AddControllers(options =>
				{
					options.InputFormatters.RemoveType<SystemTextJsonInputFormatter>();
					options.OutputFormatters.RemoveType<SystemTextJsonOutputFormatter>();

					options.InputFormatters.Add(new Zongsoft.Web.Formatters.JsonInputFormatter());
					options.OutputFormatters.Add(new Zongsoft.Web.Formatters.JsonOutputFormatter());

					options.Filters.Add(new Zongsoft.Web.Filters.ExceptionFilter());
					options.Conventions.Add(new ApplicationModelConvention());
					options.Conventions.Add(new Zongsoft.Web.Filters.GlobalFilterConvention());
					options.ModelBinderProviders.Insert(0, new Zongsoft.Web.Binders.RangeModelBinderProvider());
					options.ModelBinderProviders.Insert(0, new Zongsoft.Web.Binders.MixtureModelBinderProvider());
				});
		}
	}
}