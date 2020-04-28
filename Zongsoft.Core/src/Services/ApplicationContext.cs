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
using System.Threading;
using System.Reflection;
using System.Security.Claims;
using System.Collections.Generic;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Services
{
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

		private readonly ServiceProvider _services;
		private ICollection<IApplicationInitializer> _initializers;

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

			this.Modules = new Collections.NamedCollection<IApplicationModule>(p => p.Name);
			this.Schemas = new ComponentModel.SchemaCollection();
			this.Properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

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
		/// <summary>
		/// 获取当前应用程序的<see cref="IApplicationContext"/>上下文。
		/// </summary>
		public static IApplicationContext Current
		{
			get => _current ?? throw new InvalidOperationException("The current application is not initialized.");
		}
		#endregion

		#region 公共属性
		public virtual string Name
		{
			get => this.Services.GetService<IHostEnvironment>()?.ApplicationName;
		}

		public string Title
		{
			get => this.Configuration?.GetSection("ApplicationTitle").Value;
		}

		public string Description
		{
			get => this.Configuration?.GetSection("ApplicationDescription").Value;
		}

		public virtual string ApplicationPath
		{
			get => this.Services?.GetService<IHostEnvironment>()?.ContentRootPath ?? AppContext.BaseDirectory;
		}

		public virtual IConfiguration Configuration
		{
			get => this.Services?.GetService<HostBuilderContext>()?.Configuration;
		}

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

					this.Properties[nameof(this.Environment)] = environment = new ApplicationEnvironment(hosting);
				}

				return environment;
			}
		}

		public virtual ServiceProvider Services
		{
			get => _services;
		}

		public ICollection<IApplicationInitializer> Initializers
		{
			get => _initializers;
		}

		public virtual ClaimsPrincipal Principal
		{
			get => Thread.CurrentPrincipal is ClaimsPrincipal principal ? principal : Security.Anonymous.Principal;
		}

		public Security.Membership.IUserIdentity User
		{
			get => Security.ClaimsIdentityExtension.AsUser(this.Principal?.Identity);
		}

		public Collections.INamedCollection<IApplicationModule> Modules { get; }

		public Collections.INamedCollection<ComponentModel.Schema> Schemas { get; }

		public IDictionary<string, object> Properties { get; }
		#endregion

		#region 公共方法
		public void Register(Assembly assembly)
		{
			if(assembly == null)
				throw new ArgumentNullException(nameof(assembly));

			foreach(var type in assembly.ExportedTypes)
			{
				if(type.IsNotPublic || type.IsAbstract || !type.IsClass)
					continue;

				var attribute = type.GetCustomAttribute<ServiceAttribute>(true);

				if(attribute == null)
					continue;

				if(string.IsNullOrEmpty(attribute.Provider))
					this.Services.Register(type, attribute.Contracts);
				else
				{
					if(!this.Modules.TryGet(attribute.Provider, out var module))
						throw new InvalidOperationException($"The '{attribute.Provider}' service provider annotated by the '{type.FullName}' type does not exist.");

					module.Services.Register(type, attribute.Contracts);
				}
			}
		}

		public string EnsureDirectory(string relativePath)
		{
			string fullPath = this.ApplicationPath;

			if(string.IsNullOrWhiteSpace(relativePath))
				return fullPath;

			var parts = Common.StringExtension.Slice(relativePath, '/', '\\');

			foreach(var part in parts)
			{
				if(part == "..")
					continue;

				fullPath = Path.Combine(fullPath, part);

				if(!Directory.Exists(fullPath))
					Directory.CreateDirectory(fullPath);
			}

			return fullPath;
		}
		#endregion

		#region 初始方法
		public virtual void Initialize()
		{
			if(_disposed != 0)
				throw new ObjectDisposedException(this.GetType().FullName);

			if(_started != 0 || _stopped != 0)
				throw new InvalidOperationException();

			foreach(var initializer in _initializers)
			{
				initializer?.Initialize(this);
			}
		}
		#endregion

		#region 处置方法
		public void Dispose()
		{
			Dispose(true);
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

				var initializers = Interlocked.Exchange(ref _initializers, Array.Empty<IApplicationInitializer>());

				foreach(var initializer in initializers)
				{
					if(initializer is IDisposable disposable)
						disposable.Dispose();
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
				return string.Format("[{0}] {1}", this.Name, this.Title);
		}
		#endregion

		#region 嵌套子类
		private class ApplicationEnvironment : IApplicationEnvironment
		{
			private IDictionary<string, object> _properties;
			private readonly IHostEnvironment _environment;

			public ApplicationEnvironment(IHostEnvironment environment)
			{
				_environment = environment ?? throw new ArgumentNullException(nameof(environment));
			}

			public string Name
			{
				get => _environment.EnvironmentName;
			}

			public bool HasProperties
			{
				get => _properties != null && _properties.Count > 0;
			}

			public IDictionary<string, object> Properties
			{
				get
				{
					if(_properties == null)
						Interlocked.CompareExchange(ref _properties, new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase), null);

					return _properties;
				}
			}
		}
		#endregion
	}
}
