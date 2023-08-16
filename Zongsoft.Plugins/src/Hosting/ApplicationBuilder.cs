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
using System.IO;
using System.Reflection;
using System.Collections.Generic;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Plugins.Hosting
{
	public abstract class ApplicationBuilderBase<TApplication> : Services.IApplicationBuilder<TApplication> where TApplication : IHost
	{
		#region 公共属性
		public abstract IServiceCollection Services { get; }
		public abstract IHostEnvironment Environment { get; }
		public abstract ConfigurationManager Configuration { get; }
		#endregion

		#region 抽象方法
		public abstract TApplication Build();
		#endregion

		#region 虚拟方法
		protected virtual PluginOptions CreateOptions() => new PluginOptions(this.Environment);
		protected virtual void RegisterServices(IServiceCollection services, PluginOptions options)
		{
			//获取插件树并加载它
			var tree = PluginTree.Get(options).Load();

			//定义已注册完成的程序集
			var registry = new HashSet<Assembly>();

			//注册宿主程序集中的服务
			Zongsoft.Services.ServiceCollectionExtension.Register(services, Assembly.GetEntryAssembly(), this.Configuration);

			foreach(var plugin in tree.Plugins)
			{
				RegisterPlugin(plugin, services, this.Configuration, registry);
			}
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
		#endregion

		#region 静态方法
		protected static string GetApplicationName(IConfigurationRoot configuration)
		{
			if(configuration == null)
				return null;

			foreach(var provider in configuration.Providers)
			{
				if(provider is FileConfigurationProvider fileProvider && fileProvider.Source != null)
				{
					var fileName = Path.GetFileName(fileProvider.Source.Path);

					if(string.Equals(fileName, "appsettings.json", StringComparison.OrdinalIgnoreCase) && provider.TryGet(HostDefaults.ApplicationKey, out var value))
						return value;
				}
			}

			return null;
		}

		protected static string GetApplicationName()
		{
			var filePath = Path.Combine(System.Environment.CurrentDirectory, "appsettings.json");
			if(!File.Exists(filePath))
				return null;

			using var stream = File.OpenRead(filePath);
			var settings = System.Text.Json.JsonSerializer.Deserialize<IDictionary<string, object>>(stream);
			if(settings == null || settings.Count == 0)
				return null;

			if(settings.TryGetValue("ApplicationName", out var value) && value != null)
				return value.ToString();
			if(settings.TryGetValue(HostDefaults.ApplicationKey, out value) && value != null)
				return value.ToString();

			return null;
		}
		#endregion
	}

	public class ApplicationBuilder : ApplicationBuilderBase<IHost>
	{
#if NET7_0
		private readonly HostApplicationBuilder _builder;
		private readonly Action<HostApplicationBuilder> _configure;

		public ApplicationBuilder(string name, string[] args, Action<HostApplicationBuilder> configure = null)
		{
			//注意：在.NET7.0 中必须通过 WebApplicationOptions 来设置环境变量
			var environment = System.Environment.GetEnvironmentVariable(HostDefaults.EnvironmentKey);

			var options = string.IsNullOrWhiteSpace(environment) ?
				new HostApplicationBuilderSettings() { ApplicationName = name, Args = args } :
				new HostApplicationBuilderSettings() { ApplicationName = name, Args = args, EnvironmentName = environment };

			_builder = new(options);
			_configure = configure;

			//处理应用名为空的情况
			if(string.IsNullOrEmpty(name))
			{
				name = GetApplicationName(_builder.Configuration);

				if(!string.IsNullOrEmpty(name))
				{
					_builder.Environment.ApplicationName = name;
					_builder.Configuration[HostDefaults.ApplicationKey] = name;
				}
			}

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

		public ApplicationBuilder(string name, string[] args, Action<IHostBuilder> configure = null) : this(name, Host.CreateDefaultBuilder(args), configure) { }
		public ApplicationBuilder(string name, IHostBuilder builder, Action<IHostBuilder> configure = null)
		{
			_builder = builder ?? throw new ArgumentNullException(nameof(builder));
			_configure = configure;

			//处理应用名为空的情况
			if(string.IsNullOrEmpty(name))
				name = GetApplicationName() ?? Assembly.GetEntryAssembly().GetName().Name;

			this.Services = new ServiceCollection();
			this.Configuration = new ConfigurationManager();
			this.Environment = new ApplicationEnvironment(name, builder.Properties);

			//初始化环境变量
			_builder.ConfigureHostConfiguration(configurator =>
			{
				configurator.Properties[HostDefaults.ApplicationKey] = this.Environment.ApplicationName;
				configurator.Properties[HostDefaults.EnvironmentKey] = this.Environment.EnvironmentName;
			});

			//设置服务提供程序工厂
			_builder.UseServiceProviderFactory(new Zongsoft.Services.ServiceProviderFactory());

			//挂载插件宿主生命期
			this.Services.AddSingleton<IHostLifetime, Hosting.PluginsHostLifetime>();
		}

		public override IServiceCollection Services { get; }
		public override ConfigurationManager Configuration { get; }
		public override IHostEnvironment Environment { get; }

		public override IHost Build()
		{
			_configure?.Invoke(_builder);

			_builder.ConfigureAppConfiguration((ctx, configurator) =>
			{
				ctx.HostingEnvironment.ApplicationName = this.Environment.ApplicationName;
				ctx.HostingEnvironment.EnvironmentName = this.Environment.EnvironmentName;

				//将当前配置管理添加到应用配置器中
				configurator.AddConfiguration(this.Configuration);

				//将插件配置文件源添加到应用配置器中
				configurator.Add(new Zongsoft.Configuration.PluginConfigurationSource(this.CreateOptions()));

				//再将应用配置器中的配置源全部加入到干净的当前配置管理中
				foreach(var source in configurator.Sources)
				{
					//确保不会将配置管理加入到自己以免递归栈溢出
					if(source is ChainedConfigurationSource chained && chained.Configuration == this.Configuration)
						continue;

					//注意：以下Add(...)方法会触发source参数的IConfigurationSource.Build()方法
					//以及其返回的IConfigurationProvider的Load()方法
					((IConfigurationBuilder)this.Configuration).Add(source);
				}
			});

			//注册插件服务
			_builder.ConfigureServices(services => this.RegisterServices(services, this.CreateOptions()));

			var services = this.Services;
			if(services != null)
			{
				foreach(var service in services)
					_builder.ConfigureServices(services => services.Add(service));
			}

			return _builder.Build();
		}

		private sealed class ApplicationEnvironment : Services.IApplicationEnvironment, IHostEnvironment
		{
			public ApplicationEnvironment(string applicationName, IDictionary<object, object> properties)
			{
				this.ApplicationName = applicationName;
				this.EnvironmentName = System.Environment.GetEnvironmentVariable(HostDefaults.EnvironmentKey) ?? Environments.Development;
				this.ContentRootPath = System.Environment.CurrentDirectory;
				this.Properties = properties ?? new Dictionary<object, object>();

				this.Properties.Add(HostDefaults.ApplicationKey, this.ApplicationName);
				this.Properties.Add(HostDefaults.EnvironmentKey, this.EnvironmentName);
				this.Properties.Add(HostDefaults.ContentRootKey, this.ContentRootPath);
			}

			public string EnvironmentName { get; set; }
			public string ApplicationName { get; set; }
			public string ContentRootPath { get; set; }
			public IFileProvider ContentRootFileProvider { get; set; }

			string Services.IApplicationEnvironment.Name => this.EnvironmentName;
			public IDictionary<object, object> Properties { get; }
		}
#endif
	}
}