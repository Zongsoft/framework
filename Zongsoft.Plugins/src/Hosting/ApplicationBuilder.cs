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
 * Copyright (C) 2010-2023 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Reflection;
using System.Collections.Generic;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Plugins.Hosting
{
	public abstract class ApplicationBuilderBase<TApplication> : Services.IApplicationBuilder<TApplication> where TApplication : IHost
	{
		public abstract IServiceCollection Services { get; }
		public abstract ConfigurationManager Configuration { get; }
		public abstract IHostEnvironment Environment { get; }

		public abstract TApplication Build();

		protected virtual PluginOptions CreateOptions() => new PluginOptions(this.Environment);
		protected virtual void RegisterServices(IServiceCollection services, PluginOptions options)
		{
			//获取插件树并加载它
			var tree = PluginTree.Get(options).Load();

			//定义已注册完成的程序集
			var registry = new HashSet<Assembly>();

			foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				if(!assembly.IsDynamic && assembly.FullName.StartsWith("Zongsoft.") && registry.Add(assembly))
					Zongsoft.Services.ServiceCollectionExtension.Register(services, assembly, this.Configuration);
			}

			foreach(var plugin in tree.Plugins)
			{
				RegisterPlugin(plugin, services, this.Configuration, registry);
			}
		}

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
	}

	public class ApplicationBuilder : ApplicationBuilderBase<IHost>
	{
#if NET7_0
		private readonly HostApplicationBuilder _builder;
		private readonly Action<HostApplicationBuilder> _configure;

		public ApplicationBuilder(string name, string[] args, Action<HostApplicationBuilder> configure = null)
		{
			_builder = new(new HostApplicationBuilderSettings() { ApplicationName = name, Args = args, });
			_configure = configure;

			//设置环境变量
			var environment = System.Environment.GetEnvironmentVariable(HostDefaults.EnvironmentKey);
			if(!string.IsNullOrEmpty(environment))
				_builder.Environment.EnvironmentName = environment;

			//设置服务提供程序工厂
			_builder.ConfigureContainer(new Services.ServiceProviderFactory());

			//挂载插件宿主生命期
			_builder.Services.AddSingleton<IHostLifetime, Hosting.PluginsHostLifetime>();
		}

		public override IServiceCollection Services => _builder.Services;
		public override ConfigurationManager Configuration => _builder.Configuration;
		public override IHostEnvironment Environment => _builder.Environment;
		public override IHost Build()
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
#else
		private readonly IHostBuilder _builder;
		private readonly Action<IHostBuilder> _configure;
		private IHostEnvironment _environment;

		public ApplicationBuilder(string name, string[] args, Action<IHostBuilder> configure = null) : this(name, Host.CreateDefaultBuilder(args), configure) { }
		public ApplicationBuilder(string name, IHostBuilder builder, Action<IHostBuilder> configure = null)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			_builder = builder ?? throw new ArgumentNullException(nameof(builder));
			_configure = configure;

			this.Services = new ServiceCollection();
			this.Configuration = new ConfigurationManager();

			//初始化环境变量
			_builder.ConfigureHostConfiguration(configurator =>
			{
				configurator.Properties[HostDefaults.ApplicationKey] = name;
				configurator.Properties[HostDefaults.EnvironmentKey] = System.Environment.GetEnvironmentVariable(HostDefaults.EnvironmentKey);
			});

			//设置服务提供程序工厂
			_builder.UseServiceProviderFactory(new Zongsoft.Services.ServiceProviderFactory());

			//挂载插件宿主生命期
			this.Services.AddSingleton<IHostLifetime, Hosting.PluginsHostLifetime>();

			//添加插件配置文件源到配置管理器中
			_builder.ConfigureAppConfiguration((ctx, configurator) =>
			{
				_environment = ctx.HostingEnvironment;
				ctx.HostingEnvironment.ApplicationName = name;
				ctx.HostingEnvironment.EnvironmentName = System.Environment.GetEnvironmentVariable(HostDefaults.EnvironmentKey);

				configurator.Add(new Zongsoft.Configuration.PluginConfigurationSource(this.CreateOptions()));
			});

			//注册插件服务
			_builder.ConfigureServices(services => this.RegisterServices(services, this.CreateOptions()));
		}

		public override IServiceCollection Services { get; }
		public override ConfigurationManager Configuration { get; }
		public override IHostEnvironment Environment => _environment;

		public override IHost Build()
		{
			_configure?.Invoke(_builder);

			var services = this.Services;
			if(services != null)
			{
				foreach(var service in services)
					_builder.ConfigureServices(services => services.Add(service));
			}

			var configuration = this.Configuration;
			if(configuration != null)
			{
				foreach(var source in ((IConfigurationBuilder)configuration).Sources)
					_builder.ConfigureHostConfiguration(configurator => configurator.Add(source));
			}

			return _builder.Build();
		}
#endif
	}
}