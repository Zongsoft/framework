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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Threading;
using System.Reflection;
using System.Security.Claims;
using System.Collections.Generic;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Services;

[DefaultMember(nameof(Modules))]
public partial class ApplicationContext : IApplicationContext, IApplicationModule, IHostedLifecycleService, IAsyncDisposable
{
	#region 单例字段
	private volatile static IApplicationContext _current;
	#endregion

	#region 事件声明
	public event EventHandler Started;
	public event EventHandler Stopped;
	public event EventHandler Stopping;
	#endregion

	#region 成员字段
	private volatile int _disposed;
	private volatile int _initialized;

	private readonly ServiceProvider _services;
	private readonly List<Components.IWorker> _workers;
	private List<IApplicationInitializer> _initializers;
	private readonly HashSet<object> _serviceInitializers = new(ReferenceEqualityComparer.Instance);

	private readonly IHostApplicationLifetime _lifetime;
	#endregion

	#region 构造函数
	protected ApplicationContext(IServiceProvider services)
	{
		if(services == null)
			throw new ArgumentNullException(nameof(services));

		_services = services as ServiceProvider ?? services.GetRequiredService<ServiceProvider>();
		_initializers = new List<IApplicationInitializer>();
		_workers = new List<Components.IWorker>();

		this.Modules = new ApplicationModuleCollection();
		this.Properties = new Collections.Parameters();

		_lifetime = _services.GetService<IHostApplicationLifetime>();
		_current = this;
	}
	#endregion

	#region 单例属性
	/// <summary>获取当前应用程序的<see cref="IApplicationContext"/>上下文。</summary>
	public static IApplicationContext Current => _current;
	#endregion

	#region 公共属性
	public string Name => field ??= this.GetName();
	public string Edition => field ??= this.GetEdition();
	public Version Version => field ??= this.GetVersion();
	public string Title => this.GetTitle();
	public string Description => this.GetDescription();

	[System.Text.Json.Serialization.JsonIgnore]
	[Serialization.SerializationMember(Ignored = true)]
	public Assembly Assembly => Assembly.GetEntryAssembly();

	public virtual string ApplicationType { get; }
	public virtual string ApplicationPath => field ??= this.Services?.GetService<IHostEnvironment>()?.ContentRootPath ?? AppContext.BaseDirectory;
	public virtual IConfigurationRoot Configuration => field ??= this.Services?.GetService<IConfigurationRoot>() ?? this.Services?.GetService<IConfiguration>() as IConfigurationRoot;

	public virtual IApplicationEnvironment Environment
	{
		get
		{
			if(this.Properties.TryGetValue(nameof(this.Environment), out var value) && value is IApplicationEnvironment environment)
				return environment;

			environment = this.Services?.GetService<IApplicationEnvironment>();

			if(environment == null)
			{
				var hosting = this.Services?.GetService<IHostEnvironment>();

				if(hosting == null)
					return null;

				var context = this.Services?.GetService<HostBuilderContext>();
				this.Properties[nameof(this.Environment)] = environment = this.CreateEnvironment(hosting, context?.Properties);
			}

			return environment;
		}
	}

	public virtual ClaimsPrincipal Principal => Thread.CurrentPrincipal is ClaimsPrincipal principal ? principal : Security.Anonymous.Principal;
	public virtual IServiceProvider Services => _services;
	public virtual IDictionary<string, object> Session { get; init; }
	public Components.EventManager Events => Components.EventManager.Global;
	public ApplicationModuleCollection Modules { get; }
	public ICollection<IApplicationInitializer> Initializers => _initializers;
	public ICollection<Components.IWorker> Workers => _workers;
	public Collections.Parameters Properties { get; }
	#endregion

	#region 公共方法
	/// <summary>请求宿主停止；本方法不等待收尾，也不终止进程。</summary>
	public void Exit() => this.Exit(System.Environment.ExitCode);
	/// <summary>设置退出代码并请求宿主停止；进程由宿主入口在收尾完成后退出。</summary>
	/// <param name="exitCode">宿主入口使用的进程退出代码。</param>
	public void Exit(int exitCode)
	{
		if(_disposed != 0)
			return;

		var lifetime = _lifetime ?? _services.GetRequiredService<IHostApplicationLifetime>();
		System.Environment.ExitCode = exitCode;
		lifetime.StopApplication();
	}
	#endregion

	#region 虚拟方法
	protected virtual IApplicationEnvironment CreateEnvironment(IHostEnvironment environment, IDictionary<object, object> properties) => new ApplicationEnvironment(environment, properties);
	protected virtual string GetName() => this.Services.GetService<IHostEnvironment>()?.ApplicationName ?? Assembly.GetEntryAssembly()?.GetName().Name;
	protected virtual string GetEdition() => ApplicationIdentifier.Load(this).Edition;
	protected virtual Version GetVersion() => ApplicationIdentifier.Load(this).Version;
	protected virtual string GetTitle() => ApplicationModuleUtility.GetTitle(this) ?? this.Configuration?.GetSection("ApplicationTitle")?.Value;
	protected virtual string GetDescription() => ApplicationModuleUtility.GetDescription(this) ?? this.Configuration?.GetSection("ApplicationDescription")?.Value;
	#endregion

	#region 初始方法
	public virtual bool Initialize()
	{
		ObjectDisposedException.ThrowIf(_disposed != 0 || _initializers == null, this);

		var initialized = Interlocked.Exchange(ref _initialized, 1);
		if(initialized != 0)
			return false;

		var services = this.Services;

		if(services != null)
		{
			var initializers = services.ResolveAll<IApplicationInitializer>().ToArray();
			_serviceInitializers.UnionWith(initializers);
			_initializers.AddRange(initializers);
		}

		foreach(var initializer in _initializers)
			initializer?.Initialize(this);

		return true;
	}
	#endregion

	#region 激发事件
	protected virtual void OnStarted() => this.Started?.Invoke(this, EventArgs.Empty);
	protected virtual void OnStopped() => this.Stopped?.Invoke(this, EventArgs.Empty);
	protected virtual void OnStopping() => this.Stopping?.Invoke(this, EventArgs.Empty);
	#endregion

	#region 重写方法
	public override string ToString() => $"{this.Name}@{this.Version}";
	#endregion

	#region 嵌套子类
	private class ApplicationEnvironment : IApplicationEnvironment
	{
		private readonly IHostEnvironment _environment;

		public ApplicationEnvironment(IHostEnvironment environment, IEnumerable<KeyValuePair<object, object>> properties = null)
		{
			_environment = environment ?? throw new ArgumentNullException(nameof(environment));
			this.Properties = properties == null ? new Dictionary<object, object>() : new Dictionary<object, object>(properties);
		}

		public string Name => _environment.EnvironmentName;
		public IDictionary<object, object> Properties { get; }
	}
	#endregion
}
