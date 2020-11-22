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
using System.Linq;
using System.Reflection;
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
		public PluginsHostBuilder(string applicationName, IHostBuilder builder)
		{
			_builder = builder ?? throw new ArgumentNullException(nameof(builder));

			builder.ConfigureAppConfiguration((ctx, configurator) =>
			{
				var options = _initialize?.Invoke(ctx);
				if(options != null)
					ctx.Properties[typeof(PluginOptions)] = options;

				var environment = Environment.GetEnvironmentVariable(HostDefaults.EnvironmentKey);

				if(!string.IsNullOrWhiteSpace(environment))
					ctx.HostingEnvironment.EnvironmentName = environment.Trim();

				if(!string.IsNullOrWhiteSpace(applicationName))
					ctx.HostingEnvironment.ApplicationName = applicationName;

				var pluginsHostBuilderContext = GetPluginsBuilderContext(ctx);
				configurator.Add(new Zongsoft.Configuration.PluginConfigurationSource(pluginsHostBuilderContext.Options));
			});

			builder.ConfigureServices((ctx, services) =>
			{
				if(ctx.Properties.TryGetValue(typeof(PluginOptions), out var options))
				{
					//将插件启动设置选项加载到服务中
					services.AddSingleton((PluginOptions)options);

					//获取插件树并加载它
					var tree = PluginTree.Get((PluginOptions)options).Load();

					var registry = new HashSet<Assembly>();

					foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
					{
						if(!assembly.IsDynamic && assembly.FullName.StartsWith("Zongsoft.") && registry.Add(assembly))
							Zongsoft.Services.ServiceCollectionExtension.Register(services, assembly, ctx.Configuration);
					}

					foreach(var plugin in tree.Plugins)
					{
						RegisterPlugin(plugin, services, ctx.Configuration, registry);
					}
				}
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
		private static void RegisterPlugin(Plugin plugin, IServiceCollection services, IConfiguration configuration, ISet<Assembly> registry)
		{
			if(plugin == null || plugin.Status != PluginStatus.Loaded)
				return;

			foreach(var assembly in plugin.Manifest.Assemblies)
			{
				if(registry.Add(assembly))
					Zongsoft.Services.ServiceCollectionExtension.Register(services, assembly, configuration);
			}

			if(plugin.HasChildren)
			{
				foreach(var child in plugin.Children)
					RegisterPlugin(child, services, configuration, registry);
			}
		}

		private PluginsHostBuilderContext GetPluginsBuilderContext(HostBuilderContext context)
		{
			if(!context.Properties.TryGetValue(typeof(PluginsHostBuilderContext), out var contextValue))
			{
				PluginOptions options;

				if(context.Properties.TryGetValue(typeof(PluginOptions), out var optionsValue))
					options = (PluginOptions)optionsValue;
				else
					context.Properties[typeof(PluginOptions)] = options = new PluginOptions(context.HostingEnvironment);

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
