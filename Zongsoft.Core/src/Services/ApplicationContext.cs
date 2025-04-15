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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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
using System.IO;
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
public class ApplicationContext : IApplicationContext, IApplicationModule, IDisposable
{
	#region 单例字段
	private volatile static IApplicationContext _current;
	#endregion

	#region 事件声明
	public event EventHandler Started;
	public event EventHandler Stopped;
	#endregion

	#region 成员字段
	private volatile int _started;
	private volatile int _stopped;
	private volatile int _disposed;
	private volatile int _initialized;

	private string _title;
	private string _description;

	private readonly ServiceProvider _services;
	private readonly List<IWorker> _workers;
	private List<IApplicationInitializer> _initializers;

	private readonly CancellationTokenRegistration _applicationStarted;
	private readonly CancellationTokenRegistration _applicationStopped;
	#endregion

	#region 构造函数
	protected ApplicationContext(IServiceProvider services)
	{
		if(services == null)
			throw new ArgumentNullException(nameof(services));

		_services = services as ServiceProvider ?? services.GetRequiredService<ServiceProvider>();
		_initializers = new List<IApplicationInitializer>();
		_workers = new List<IWorker>();

		this.Modules = new ApplicationModuleCollection();
		this.Properties = new Collections.Parameters();

		var lifetime = _services.GetService<IHostApplicationLifetime>();

		if(lifetime != null)
		{
			_applicationStarted = lifetime.ApplicationStarted.Register(() => this.OnStarted(EventArgs.Empty));
			_applicationStopped = lifetime.ApplicationStopped.Register(() => this.OnStopped(EventArgs.Empty));
		}

		_current = this;
	}
	#endregion

	#region 单例属性
	/// <summary>获取当前应用程序的<see cref="IApplicationContext"/>上下文。</summary>
	public static IApplicationContext Current => _current;
	#endregion

	#region 公共属性
	public virtual string Name => this.Services.GetService<IHostEnvironment>()?.ApplicationName;

	public string Title
	{
		get => string.IsNullOrWhiteSpace(_title) ? this.Configuration?.GetSection("ApplicationTitle").Value : _title;
		protected set => _title = value;
	}

	public string Description
	{
		get => string.IsNullOrWhiteSpace(_description) ? this.Configuration?.GetSection("ApplicationDescription").Value : _description;
		protected set => _description = value;
	}

	public virtual string ApplicationType { get; }
	public virtual string ApplicationPath => this.Services?.GetService<IHostEnvironment>()?.ContentRootPath ?? AppContext.BaseDirectory;
	public virtual IConfigurationRoot Configuration => this.Services?.GetService<IConfigurationRoot>() ?? this.Services?.GetService<IConfiguration>() as IConfigurationRoot;

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
	public ICollection<IWorker> Workers => _workers;
	public Collections.Parameters Properties { get; }
	#endregion

	#region 公共方法
	public string EnsureDirectory(string relativePath)
	{
		string fullPath = this.ApplicationPath;

		if(string.IsNullOrEmpty(relativePath))
			return fullPath;

		var parts = Common.StringExtension.Slice(relativePath, '/', '\\');
		var illegals = Path.GetInvalidPathChars();

		foreach(var part in parts)
		{
			if(part == ".." || part.IndexOfAny(illegals) >= 0)
				throw new ArgumentException($"The specified '{relativePath}' relative path contains illegal path character(s).");

			fullPath = Path.Combine(fullPath, part);

			if(!Directory.Exists(fullPath))
				Directory.CreateDirectory(fullPath);
		}

		return fullPath;
	}
	#endregion

	#region 虚拟方法
	protected virtual IApplicationEnvironment CreateEnvironment(IHostEnvironment environment, IDictionary<object, object> properties)
	{
		return new ApplicationEnvironment(environment, properties);
	}
	#endregion

	#region 初始方法
	public virtual bool Initialize()
	{
		if(_initialized != 0)
			return false;

		if(_disposed != 0 || _initializers == null)
			throw new ObjectDisposedException(this.GetType().FullName);

		if(_started != 0 || _stopped != 0)
			throw new InvalidOperationException();

		var initialized = Interlocked.Exchange(ref _initialized, 1);

		if(initialized != 0)
			return false;

		var services = this.Services;

		if(services != null)
			_initializers.AddRange(services.ResolveAll<IApplicationInitializer>());

		foreach(var initializer in _initializers)
		{
			initializer?.Initialize(this);
		}

		return true;
	}
	#endregion

	#region 处置方法
	public void Dispose()
	{
		this.Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if(Interlocked.Exchange(ref _disposed, 1) != 0)
			return;

		if(disposing)
		{
			_applicationStarted.Dispose();
			_applicationStopped.Dispose();

			var initializers = Interlocked.Exchange(ref _initializers, null);

			if(initializers != null)
			{
				foreach(var initializer in initializers)
				{
					if(initializer is IDisposable disposable)
						disposable.Dispose();
				}
			}

			foreach(var module in this.Modules)
			{
				if(module is IDisposable disposable)
					disposable.Dispose();
			}

			//清空模块集
			this.Modules.Clear();

			if(_services is IDisposable services)
				services.Dispose();
		}
	}
	#endregion

	#region 激发事件
	protected virtual void OnStarted(EventArgs args)
	{
		//确保“Started”事件只会被触发一次
		if(Interlocked.CompareExchange(ref _started, 1, 0) == 0)
			this.Started?.Invoke(this, args);
	}

	protected virtual void OnStopped(EventArgs args)
	{
		//确保“Stopped”事件只会被触发一次
		if(Interlocked.CompareExchange(ref _stopped, 1, 0) == 0)
			this.Stopped?.Invoke(this, args);
	}
	#endregion

	#region 重写方法
	public override string ToString()
	{
		if(string.IsNullOrEmpty(this.Title) || string.Equals(this.Name, this.Title))
			return this.Name;
		else
			return $"[{this.Name}] {this.Title}";
	}
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
