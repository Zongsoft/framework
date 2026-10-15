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
using System.Threading;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Web.Hosting;

/// <summary>提供插件 Web 端点的接入及配置监视。</summary>
public static class WebEndpointExtension
{
	#region 公共方法
	/// <summary>从最终插件配置创建端点快照、适配 Kestrel 并安装请求范围管线。</summary>
	/// <param name="services">服务集合。</param>
	/// <param name="configuration">包含宿主及插件配置的根配置；由宿主管理其生命周期。</param>
	/// <param name="section">包含 Endpoints 的服务器配置节路径。</param>
	/// <returns>返回原服务集合；注册阶段不读取配置或构建服务提供程序。</returns>
	public static IServiceCollection AddWebEndpoints(this IServiceCollection services, IConfigurationRoot configuration, string section = "Kestrel")
	{
		ArgumentNullException.ThrowIfNull(services);
		ArgumentNullException.ThrowIfNull(configuration);
		ArgumentException.ThrowIfNullOrEmpty(section);

		services.AddWebEndpoints(provider => provider.GetRequiredService<EndpointConfiguration>().Endpoints);
		services.AddSingleton(provider => new EndpointConfiguration(configuration, section, provider.GetServices<WebEndpointProtocol>()));
		services.AddOptions<KestrelServerOptions>().PostConfigure<EndpointConfiguration, WebEndpointCollection>((options, snapshot, _) => options.Configure(snapshot.Projection, reloadOnChange: false));
		services.AddOptions<SocketTransportOptions>().PostConfigure<WebEndpointCollection>((options, endpoints) => KestrelUtility.Configure(options, endpoints));

		return services.AddWebEndpointPipeline();
	}

	/// <summary>为已接入的插件端点启用配置变更提示；不热更新监听或路由。</summary>
	/// <param name="services">服务集合；须先调用 AddWebEndpoints。</param>
	/// <returns>返回原服务集合；重复调用不会重复监视。</returns>
	public static IServiceCollection AddWebEndpointMonitoring(this IServiceCollection services)
	{
		ArgumentNullException.ThrowIfNull(services);

		if(!services.Any(descriptor => descriptor.ServiceType == typeof(EndpointConfiguration)))
			throw InvalidEndpoint("Endpoints", "MissingEndpointRegistration");

		services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, EndpointMonitor>());
		return services;
	}
	#endregion

	#region 私有方法
	private static void ValidatePluginEndpoints(IConfigurationRoot configuration, string section)
	{
		var entries = new List<(string Key, string Value, string Source)>();
		foreach(var provider in configuration.Providers.OfType<PluginConfigurationProvider>().SelectMany(provider => provider.Providers))
		{
			foreach(var key in GetKeys(provider, section))
			{
				if(provider.TryGet(key, out var value))
					entries.Add((key, value, GetSource(provider, key)));
			}
		}

		var values = new Dictionary<string, (string Value, string Source)>(StringComparer.OrdinalIgnoreCase);
		foreach(var entry in entries.OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase).ThenBy(entry => entry.Source, StringComparer.Ordinal))
		{
			if(values.TryGetValue(entry.Key, out var existing) && existing.Value != entry.Value)
				throw InvalidEndpoint(entry.Key, "ConflictingPluginConfiguration", [existing.Source, entry.Source]);

			values[entry.Key] = (entry.Value, entry.Source);
		}
	}

	private static IEnumerable<string> GetKeys(IConfigurationProvider provider, string parent)
	{
		yield return parent;

		foreach(var child in provider.GetChildKeys([], parent).Distinct(StringComparer.OrdinalIgnoreCase))
		{
			foreach(var key in GetKeys(provider, parent + ConfigurationPath.KeyDelimiter + child))
				yield return key;
		}
	}

	private static string GetSource(IConfigurationProvider provider, string key)
	{
		if(provider is CompositeConfigurationProvider composite)
		{
			foreach(var child in composite.Providers)
			{
				if(child.TryGet(key, out _))
					return GetSource(child, key);
			}
		}

		return provider is FileConfigurationProvider file ?
			file.Source.FileProvider?.GetFileInfo(file.Source.Path).PhysicalPath ?? file.Source.Path : provider.GetType().FullName;
	}

	private static InvalidOperationException InvalidEndpoint(string key, string reason, string[] sources = null)
	{
		var error = new InvalidOperationException(
			string.Format(CultureInfo.CurrentCulture, global::Zongsoft.Plugins.Web.Properties.Resources.WebEndpoint_Invalid_Message,
			key, reason, sources == null ? string.Empty : string.Join(", ", sources)));

		error.Data["Key"] = key;
		error.Data["Reason"] = reason;

		if(sources != null)
			error.Data["Sources"] = sources;

		return error;
	}

	private static Dictionary<string, string> Read(IConfiguration configuration) => configuration.AsEnumerable(true)
		.Where(entry => !string.IsNullOrEmpty(entry.Key) && entry.Value != null)
		.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase);
	#endregion

	#region 服务注册
	/// <summary>通过服务扫描为具有端点声明的插件 Web 宿主自动接入端点及监视。</summary>
	public sealed class Registration : IServiceRegistration
	{
		/// <summary>在已有配置声明且未显式接入端点时登记服务，快照由最终容器创建。</summary>
		/// <param name="services">宿主服务集合。</param>
		/// <param name="configuration">包含完整插件配置的根配置。</param>
		public void Register(IServiceCollection services, IConfiguration configuration)
		{
			ArgumentNullException.ThrowIfNull(services);

			if(configuration is not IConfigurationRoot root ||
				!services.Any(service => service.ServiceType == typeof(IWebHostEnvironment)) ||
				services.Any(service => service.ServiceType == typeof(WebEndpointCollection)) ||
				!configuration.GetSection("Kestrel:Endpoints").GetChildren().Any())
				return;

			services.AddWebEndpoints(root).AddWebEndpointMonitoring();
		}
	}
	#endregion

	#region 嵌套类型
	private sealed class EndpointConfiguration : IDisposable
	{
		private readonly IConfigurationRoot _configuration;
		private readonly Dictionary<string, string> _source;
		private readonly string _section;

		public EndpointConfiguration(IConfigurationRoot configuration, string section, IEnumerable<WebEndpointProtocol> protocols)
		{
			ValidatePluginEndpoints(configuration, section);

			_configuration = configuration;
			_section = section;
			_source = Read(configuration.GetSection(section));

			using var input = (ConfigurationRoot)new ConfigurationBuilder().AddInMemoryCollection(_source).Build();
			this.Projection = KestrelUtility.Convert(input, out var endpoints, protocols);
			this.Endpoints = endpoints;
		}

		public ConfigurationRoot Projection { get; }
		public IReadOnlyList<WebEndpoint> Endpoints { get; }
		public IChangeToken GetReloadToken() => _configuration.GetReloadToken();
		public void Dispose() => this.Projection.Dispose();

		public bool HasChanged()
		{
			ValidatePluginEndpoints(_configuration, _section);
			var current = Read(_configuration.GetSection(_section));
			return current.Count != _source.Count || _source.Any(entry => !current.TryGetValue(entry.Key, out var value) || entry.Value != value);
		}
	}

	private sealed class EndpointMonitor(EndpointConfiguration snapshot, ILogger<EndpointMonitor> logger) : IHostedService, IDisposable
	{
		private readonly object _sync = new();
		private IDisposable _subscription;
		private bool _disposed;
		private bool _changed;

		public Task StartAsync(CancellationToken cancellationToken)
		{
			_subscription = ChangeToken.OnChange(snapshot.GetReloadToken, this.OnChanged);
			this.OnChanged();
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			this.Dispose();
			return Task.CompletedTask;
		}

		public void Dispose()
		{
			IDisposable subscription;

			lock(_sync)
			{
				if(_disposed)
					return;

				_disposed = true;
				subscription = _subscription;
				_subscription = null;
			}

			subscription?.Dispose();
		}

		private void OnChanged()
		{
			lock(_sync)
			{
				if(_disposed || _changed)
					return;

				try
				{
					if(!snapshot.HasChanged())
						return;
				}
				catch(InvalidOperationException)
				{
					//配置冲突同样要求重启，不向配置通知传播异常。
				}

				_changed = true;
				logger.LogWarning(new EventId(1, "WebEndpointsChanged"), Zongsoft.Plugins.Web.Properties.Resources.WebEndpoint_RestartRequired_Message);
			}
		}
	}
	#endregion
}
