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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Zongsoft.Configuration;

namespace Zongsoft.Plugins.Hosting;

public abstract class ApplicationBuilderBase<TApplication> : Services.IApplicationBuilder<TApplication> where TApplication : IHost
{
	#region 公共属性
	public abstract IServiceCollection Services { get; }
	public abstract IHostEnvironment Environment { get; }
	public abstract ConfigurationManager Configuration { get; }
	#endregion

	#region 公共方法
	/// <summary>按应用名称及宿主程序集名称加载配置入口。</summary>
	public void LoadConfiguration()
	{
		this.LoadConfiguration(this.Environment.ApplicationName);

		var assemblyName = Assembly.GetEntryAssembly().GetName().Name;
		if(!string.Equals(this.Environment.ApplicationName, assemblyName, StringComparison.OrdinalIgnoreCase))
			this.LoadConfiguration(assemblyName);
	}

	/// <summary>加载指定名称的宿主配置入口，其余配置由入口显式导入。</summary>
	/// <param name="name">不含扩展名的配置名称，可以包含相对于配置文件提供程序根目录的路径或绝对路径。</param>
	public void LoadConfiguration(string name)
	{
		if(string.IsNullOrEmpty(name))
			return;

		this.Configuration.AddOptionFile($"{name}.option", true);
	}
	#endregion

	#region 抽象方法
	/// <summary>构建已加载配置并完成服务注册的应用宿主。</summary>
	/// <returns>可启动的应用宿主。</returns>
	public abstract ValueTask<TApplication> BuildAsync();
	#endregion

	#region 虚拟方法
	/// <summary>根据当前宿主环境创建插件加载选项。</summary>
	/// <param name="configuration">插件选项使用的配置。</param>
	/// <returns>包含宿主环境信息的插件加载选项。</returns>
	protected virtual PluginOptions CreateOptions(IConfiguration configuration) => new(configuration, this.Environment);
	/// <summary>加载完整插件配置，并按程序集去重注册宿主和插件服务。</summary>
	/// <param name="services">承载宿主和插件服务的集合。</param>
	/// <param name="options">插件加载选项。</param>
	protected virtual void RegisterServices(IServiceCollection services, PluginOptions options)
	{
		//获取插件树并加载它
		var tree = PluginTree.Get(options).Load();

		//定义已注册完成的程序集
		var registry = new HashSet<Assembly>();

		//注册宿主程序依赖的程序集中的服务
		foreach(var assemblyName in Assembly.GetEntryAssembly().GetReferencedAssemblies())
		{
			var assembly = GetAssembly(assemblyName, AppDomain.CurrentDomain.GetAssemblies());

			if(assembly != null && registry.Add(assembly))
				Zongsoft.Services.ServiceCollectionExtension.Register(services, assembly, this.Configuration);
		}

		//注册宿主程序集中的服务
		var entry = Assembly.GetEntryAssembly();
		if(entry != null && registry.Add(entry))
			Zongsoft.Services.ServiceCollectionExtension.Register(services, entry, this.Configuration);

		//由宿主等待应用工作器的启动和停止。
		services.AddSingleton<IHostedService>(provider => provider.GetRequiredService<PluginApplicationContext>());

		//添加默认的HTTP客户端服务
		services.AddHttpClient();

		foreach(var plugin in tree.Plugins)
		{
			RegisterPlugin(plugin, services, this.Configuration, registry);
		}

		//定位到待注册的服务节点
		var node = tree.Find("/Workspace/Environment/Services");

		if(node != null)
		{
			//依次注册服务节点下的所有服务
			foreach(var child in node.Children)
				services.AddSingleton(child.ValueType, provider =>
				{
					var result = child.UnwrapValue(ObtainMode.Auto);
					if(child.Value is Builtin builtin)
						builtin.OwnsValue = false;

					return result;
				});
		}
	}
	#endregion

	#region 私有方法
	/// <summary>在已加载的程序集集合中查找指定程序集。</summary>
	/// <param name="name">包含版本等标识信息的程序集名称。</param>
	/// <param name="assemblies">已加载的程序集集合。</param>
	/// <returns>完整名称匹配的程序集；未找到时返回 <see langword="null"/>。</returns>
	private static Assembly GetAssembly(AssemblyName name, Assembly[] assemblies)
	{
		for(int i = 0; i < assemblies.Length; i++)
		{
			if(assemblies[i].FullName == name.FullName)
				return assemblies[i];
		}

		return null;
	}

	/// <summary>递归注册已加载插件的程序集服务，并避免重复扫描同一程序集。</summary>
	/// <param name="plugin">当前插件。</param>
	/// <param name="services">用于添加插件服务的集合。</param>
	/// <param name="configuration">提供给服务注册过程的完整配置。</param>
	/// <param name="registry">已注册程序集的集合。</param>
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
	/// <summary>从应用设置文件对应的配置提供程序读取宿主名称。</summary>
	/// <param name="configuration">包含文件配置提供程序的配置根。</param>
	/// <returns>配置中的应用名称；未配置时返回 <see langword="null"/>。</returns>
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

	/// <summary>从当前目录的应用设置文件读取宿主名称。</summary>
	/// <returns>应用名称；文件不存在或未配置时返回 <see langword="null"/>。</returns>
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
	#if NET7_0_OR_GREATER
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
	}

	#region 重写属性
	public override IServiceCollection Services => _builder.Services;
	public override ConfigurationManager Configuration => _builder.Configuration;
	public override IHostEnvironment Environment => _builder.Environment;
	#endregion

	#region 重写方法
	public override ValueTask<IHost> BuildAsync()
	{
		_configure?.Invoke(_builder);

		//创建默认的插件环境配置
		var options = this.CreateOptions(_builder.Configuration);

		//添加插件配置文件源到配置管理器中
		((IConfigurationBuilder)_builder.Configuration).Add(new PluginConfigurationSource(options));

		//注册插件服务
		this.RegisterServices(_builder.Services, options);

		//创建并初始化宿主
		return _builder.Build().InitializeAsync();
	}
	#endregion
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
	}

	#region 重写属性
	public override IServiceCollection Services { get; }
	public override ConfigurationManager Configuration { get; }
	public override IHostEnvironment Environment { get; }
	#endregion

	#region 重写方法
	public override ValueTask<IHost> BuildAsync()
	{
		_configure?.Invoke(_builder);

		_builder.ConfigureAppConfiguration((ctx, configurator) =>
		{
			ctx.HostingEnvironment.ApplicationName = this.Environment.ApplicationName;
			ctx.HostingEnvironment.EnvironmentName = this.Environment.EnvironmentName;

			//将当前配置管理添加到应用配置器中
			configurator.AddConfiguration(this.Configuration);

			//将插件配置文件源添加到应用配置器中
			configurator.Add(new Zongsoft.Configuration.PluginConfigurationSource(this.CreateOptions(ctx.Configuration)));

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
		_builder.ConfigureServices(services => this.RegisterServices(services, this.CreateOptions(this.Configuration)));

		var services = this.Services;
		if(services != null)
		{
			foreach(var service in services)
				_builder.ConfigureServices(services => services.Add(service));
		}

		//创建并初始化宿主
		return _builder.Build().InitializeAsync();
	}
	#endregion

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

		#region 公共属性
		public string EnvironmentName { get; set; }
		public string ApplicationName { get; set; }
		public string ContentRootPath { get; set; }
		public IFileProvider ContentRootFileProvider { get; set; }
		#endregion

		#region 显式实现
		string Services.IApplicationEnvironment.Name => this.EnvironmentName;
		#endregion
		#region 公共属性
		public IDictionary<object, object> Properties { get; }
		#endregion
	}
	#endif
}
