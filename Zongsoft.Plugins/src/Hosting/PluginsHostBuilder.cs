/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Plugins library.
 *
 * The Zongsoft.Plugins is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Plugins is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Plugins library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Plugins.Hosting
{
	public class PluginsHostBuilder : IPluginsHostBuilder
	{
		#region 成员字段
		private readonly IHostBuilder _builder;
		private Func<HostBuilderContext, PluginOptions> _initialize;
		#endregion

		#region 构造函数
		public PluginsHostBuilder(IHostBuilder builder)
		{
			_builder = builder ?? throw new ArgumentNullException(nameof(builder));

			builder.ConfigureHostConfiguration(configurator =>
			{
				configurator.AddCommandLine(new string[]
				{
					"/plugins:directory", "plugins",
				});
			});

			builder.ConfigureAppConfiguration((ctx, configurator) =>
			{
				var options = _initialize?.Invoke(ctx);
				if(options != null)
					ctx.Properties[typeof(PluginOptions)] = options;

				var pluginsHostBuilderContext = GetPluginsBuilderContext(ctx);
				configurator.Add(new Zongsoft.Configuration.PluginConfigurationSource(pluginsHostBuilderContext.Options));
			});

			builder.ConfigureServices((ctx, services) =>
			{
				if(ctx.Properties.TryGetValue(typeof(PluginOptions), out var options))
					services.AddSingleton((PluginOptions)options);
			});

			builder.UseServiceProviderFactory(new Zongsoft.Services.ServiceProviderFactory());
		}
		#endregion

		#region 配置方法
		public IPluginsHostBuilder UseOptions(Func<HostBuilderContext, PluginOptions> initialize)
		{
			_initialize = initialize;
			return this;
		}

		public IPluginsHostBuilder ConfigureConfiguration(Action<PluginsHostBuilderContext, IConfigurationBuilder> configure)
		{
			_builder.ConfigureAppConfiguration((context, configurator) =>
			{
				var pluginsHostBuilderContext = GetPluginsBuilderContext(context);
				configure(pluginsHostBuilderContext, configurator);
			});

			return this;
		}

		public IPluginsHostBuilder ConfigureServices(Action<IServiceCollection> configure)
		{
			return this.ConfigureServices((context, services) => configure(services));
		}

		public IPluginsHostBuilder ConfigureServices(Action<PluginsHostBuilderContext, IServiceCollection> configure)
		{
			_builder.ConfigureServices((context, services) =>
			{
				var pluginsHostBuilderContext = GetPluginsBuilderContext(context);
				configure(pluginsHostBuilderContext, services);
			});

			return this;
		}
		#endregion

		#region 私有方法
		private PluginsHostBuilderContext GetPluginsBuilderContext(HostBuilderContext context)
		{
			if(!context.Properties.TryGetValue(typeof(PluginsHostBuilderContext), out var contextValue))
			{
				PluginOptions options;

				if(context.Properties.TryGetValue(typeof(PluginOptions), out var optionsValue))
					options = (PluginOptions)optionsValue;
				else
					context.Properties[typeof(PluginOptions)] = options = new PluginOptions(context.HostingEnvironment.ContentRootPath);

				var pluginsHostBuilderContext = new PluginsHostBuilderContext(options, context.Properties)
				{
					Configuration = context.Configuration,
					HostingEnvironment = context.HostingEnvironment,
				};

				context.Properties[typeof(PluginsHostBuilderContext)] = pluginsHostBuilderContext;
				return pluginsHostBuilderContext;
			}

			var pluginsHostContext = (PluginsHostBuilderContext)contextValue;
			pluginsHostContext.Configuration = context.Configuration;
			return pluginsHostContext;
		}
		#endregion
	}
}
