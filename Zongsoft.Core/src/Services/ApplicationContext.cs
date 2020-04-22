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
using System.Security.Claims;
using System.Collections.Generic;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Zongsoft.Services
{
	[System.Reflection.DefaultMember(nameof(Modules))]
	public class ApplicationContext : IApplicationContext, IApplicationModule
	{
		#region 单例字段
		private volatile static IApplicationContext _current;
		#endregion

		#region 事件声明
		public event EventHandler Exiting;
		public event EventHandler Started;
		#endregion

		#region 成员字段
		private System.IServiceProvider _services;
		#endregion

		#region 构造函数
		protected ApplicationContext()
		{
			_current = this;

			this.Modules = new Collections.NamedCollection<IApplicationModule>(p => p.Name);
			this.Initializers = new List<IApplicationInitializer>();
			this.Schemas = new ComponentModel.SchemaCollection();
			this.Properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
		}

		protected ApplicationContext(System.IServiceProvider services) : this()
		{
			_services = services ?? throw new ArgumentNullException(nameof(services));

			var lifetime = _services.GetService<IHostApplicationLifetime>();

			if(lifetime != null)
			{
				lifetime.ApplicationStarted.Register(() => this.OnStarted(EventArgs.Empty));
				lifetime.ApplicationStopping.Register(() => this.OnExiting(EventArgs.Empty));
			}
		}
		#endregion

		#region 单例属性
		/// <summary>
		/// 获取当前应用程序的<see cref="IApplicationContext"/>上下文。
		/// </summary>
		public static IApplicationContext Current
		{
			get => _current;
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

		public virtual string ApplicationDirectory
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

					environment = new ApplicationEnvironment(hosting);
				}

				if(this.Properties.TryAdd(nameof(this.Environment), environment))
					return environment;

				return this.Properties[nameof(this.Environment)] as IApplicationEnvironment;
			}
		}

		public virtual System.IServiceProvider Services
		{
			get => _services;
		}

		public virtual ClaimsPrincipal Principal
		{
			get => Thread.CurrentPrincipal is ClaimsPrincipal principal ? principal : ClaimsPrincipal.Current;
		}

		public Collections.INamedCollection<IApplicationModule> Modules { get; }

		public ICollection<IApplicationInitializer> Initializers { get; }

		public Collections.INamedCollection<ComponentModel.Schema> Schemas { get; }

		public IDictionary<string, object> Properties { get; }
		#endregion

		#region 公共方法
		public string EnsureDirectory(string relativePath)
		{
			string fullPath = this.ApplicationDirectory;

			if(string.IsNullOrWhiteSpace(relativePath))
				return fullPath;

			var parts = relativePath.Split('/', '\\', Path.DirectorySeparatorChar);

			foreach(var part in parts)
			{
				if(string.IsNullOrWhiteSpace(part))
					continue;

				fullPath = Path.Combine(fullPath, part);

				if(!Directory.Exists(fullPath))
					Directory.CreateDirectory(fullPath);
			}

			return fullPath;
		}
		#endregion

		#region 激发事件
		protected virtual void OnExiting(EventArgs args)
		{
			this.Exiting?.Invoke(this, args);
		}

		protected virtual void OnStarted(EventArgs args)
		{
			this.Started?.Invoke(this, args);
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
