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
using System.IO;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;

namespace Zongsoft.Plugins
{
	/// <summary>
	/// 有关插件运行环境的设置信息。
	/// </summary>
	public class PluginOptions : IEquatable<PluginOptions>
	{
		#region 构造函数
		internal PluginOptions(IConfiguration configuration, Microsoft.Extensions.Hosting.IHostEnvironment environment, string pluginsDirectoryName = null)
		{
			if(environment == null)
				throw new ArgumentNullException(nameof(environment));

			this.Configuration = configuration;
			this.EnvironmentName = environment.EnvironmentName;
			this.ApplicationDirectory = environment.ContentRootPath;
			this.PluginsPath = Path.Combine(this.ApplicationDirectory, string.IsNullOrWhiteSpace(pluginsDirectoryName) ? "plugins" : pluginsDirectoryName);
			this.Properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
		}

		/// <summary>构造插件设置对象。</summary>
		/// <param name="configuration">指定的应用配置。</param>
		/// <param name="applicationDirectory">应用程序目录完整限定路径。</param>
		/// <param name="environmentName">指定的环境名。</param>
		public PluginOptions(IConfiguration configuration, string applicationDirectory, string environmentName) : this(configuration, applicationDirectory, environmentName, null) { }

		/// <summary>构造插件设置对象。</summary>
		/// <param name="configuration">指定的应用配置。</param>
		/// <param name="applicationDirectory">应用程序目录完整限定路径。</param>
		/// <param name="pluginsDirectoryName">插件目录名，非完整路径。默认为“plugins”。</param>
		/// <param name="environmentName">指定的环境名。</param>
		/// <exception cref="System.ArgumentException">当<paramref name="applicationDirectory"/>参数值不为路径完全限定格式。</exception>
		public PluginOptions(IConfiguration configuration, string applicationDirectory, string environmentName, string pluginsDirectoryName)
		{
			if(string.IsNullOrWhiteSpace(applicationDirectory))
			{
				this.ApplicationDirectory = AppContext.BaseDirectory;
			}
			else
			{
				this.ApplicationDirectory = applicationDirectory.Trim();

				if(!Path.IsPathRooted(ApplicationDirectory))
					throw new ArgumentException($"The specified '{applicationDirectory}' application directory is not an absolute path.");
			}

			if(!Directory.Exists(this.ApplicationDirectory))
				throw new DirectoryNotFoundException(this.ApplicationDirectory);

			this.Configuration = configuration;
			this.EnvironmentName = environmentName;
			this.PluginsPath = Path.Combine(this.ApplicationDirectory, string.IsNullOrWhiteSpace(pluginsDirectoryName) ? "plugins" : pluginsDirectoryName);
			this.Properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
		}
		#endregion

		#region 公共属性
		/// <summary>获取应用环境名称。</summary>
		public string EnvironmentName { get; }

		/// <summary>获取配置管理器。</summary>
		public IConfiguration Configuration { get; }

		/// <summary>获取应用程序目录的完全限定路径，该属性值由构造函数注入。</summary>
		public string ApplicationDirectory { get; }

		/// <summary>获取插件目录的完全限定路径。</summary>
		public string PluginsPath { get; }

		/// <summary>获取扩展属性集。</summary>
		public IDictionary<string, object> Properties { get; }
		#endregion

		#region 内部方法
		internal string GetApplicationContextMountion()
		{
			if(this.Properties.TryGetValue("Mountion:ApplicationContext", out var value) && value is string text && !string.IsNullOrWhiteSpace(text))
				return text;

			return "/Workspace/Environment/ApplicationContext";
		}

		internal string GetWorkbenchMountion()
		{
			if(this.Properties.TryGetValue("Mountion:Workbench", out var value) && value is string text && !string.IsNullOrWhiteSpace(text))
				return text;

			return "/Workbench";
		}

		internal string GetStartupMountion() => PluginPath.Combine(this.GetWorkbenchMountion(), "Startup");
		#endregion

		#region 重写方法
		public bool Equals(PluginOptions other) => other is not null && string.Equals(this.PluginsPath, other.PluginsPath);
		public override bool Equals(object obj) => obj is PluginOptions other && this.Equals(other);
		public override int GetHashCode() => HashCode.Combine(this.PluginsPath);
		public override string ToString() => this.PluginsPath;
		#endregion
	}
}
