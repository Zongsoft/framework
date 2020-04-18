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
using System.IO;

namespace Zongsoft.Plugins
{
	/// <summary>
	/// 有关插件运行环境的设置信息。
	/// </summary>
	public class PluginOptions
	{
		#region 构造函数
		/// <summary>
		/// 构造插件设置对象。
		/// </summary>
		public PluginOptions() : this(null, null, null)
		{
		}

		/// <summary>
		/// 构造插件设置对象。
		/// </summary>
		/// <param name="applicationDirectory">应用程序目录完整限定路径。</param>
		public PluginOptions(string applicationDirectory) : this(applicationDirectory, null, null)
		{
		}

		/// <summary>
		/// 构造插件设置对象。
		/// </summary>
		/// <param name="applicationDirectory">应用程序目录完整限定路径。</param>
		/// <param name="pluginsDirectoryName">插件目录名，非完整路径。默认为“plugins”。</param>
		/// <param name="settings">插件挂载点的设置。</param>
		/// <exception cref="System.ArgumentException">当<paramref name="applicationDirectory"/>参数值不为路径完全限定格式。</exception>
		public PluginOptions(string applicationDirectory, string pluginsDirectoryName, MountionSettings settings = null)
		{
			if(string.IsNullOrWhiteSpace(applicationDirectory))
			{
				this.ApplicationDirectory = AppContext.BaseDirectory;
			}
			else
			{
				this.ApplicationDirectory = applicationDirectory.Trim();

				if(!Path.IsPathRooted(ApplicationDirectory))
					throw new ArgumentException("This value of 'applicationDirectory' parameter is invalid.");
			}

			this.PluginsPath = Path.Combine(this.ApplicationDirectory, string.IsNullOrWhiteSpace(pluginsDirectoryName) ? "plugins" : pluginsDirectoryName);
			this.Mountion = settings ?? new MountionSettings();
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取应用程序目录的完全限定路径，该属性值由构造函数注入。
		/// </summary>
		public string ApplicationDirectory
		{
			get;
		}

		/// <summary>
		/// 获取插件目录的完全限定路径。
		/// </summary>
		public string PluginsPath
		{
			get;
		}

		/// <summary>
		/// 获取系统内置对象的插件挂载点设置。
		/// </summary>
		public MountionSettings Mountion
		{
			get;
		}
		#endregion

		#region 重写方法
		public override string ToString()
		{
			return this.PluginsPath;
		}
		#endregion

		#region 嵌套结构
		public class MountionSettings
		{
			#region 常量定义
			private const string APPLICATIONCONTEXT_DEFAULT_PATH = "/Workspace/Environment/ApplicationContext";
			private const string WORKBENCH_DEFAULT_PATH = "/Workbench";
			#endregion

			#region 构造函数
			public MountionSettings()
			{
				this.ApplicationContextPath = APPLICATIONCONTEXT_DEFAULT_PATH;
				this.WorkbenchPath = WORKBENCH_DEFAULT_PATH;
			}

			public MountionSettings(string applicationContextPath, string workbenchPath)
			{
				this.ApplicationContextPath = string.IsNullOrEmpty(applicationContextPath) ? APPLICATIONCONTEXT_DEFAULT_PATH : applicationContextPath;
				this.WorkbenchPath = string.IsNullOrEmpty(workbenchPath) ? WORKBENCH_DEFAULT_PATH : workbenchPath;
			}
			#endregion

			#region 公告属性
			/// <summary>获取应用程序上下文对象(<see cref="Zongsoft.Services.IApplicationContext"/>)的插件挂载点位置。默认值为：/Workspace/Environment/ApplicationContext</summary>
			public string ApplicationContextPath
			{
				get;
			}

			/// <summary>获取工作台对象(<see cref="IWorkbench"/>)的插件挂载点位置。默认值为：/Workspace/Environment/ApplicationContext</summary>
			public string WorkbenchPath
			{
				get;
			}
			#endregion
		}
		#endregion
	}
}
