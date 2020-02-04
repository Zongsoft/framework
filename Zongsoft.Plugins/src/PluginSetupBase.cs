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
using System.ComponentModel;
using System.Collections.Generic;

namespace Zongsoft.Plugins
{
	public class PluginSetupBase : MarshalByRefObject, INotifyPropertyChanged
	{
		#region 事件定义
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion

		#region 成员变量
		private string _applicationDirectory;
		private string _pluginsDirectoryName;
		private string _pluginsDirectory;
		#endregion

		#region 构造函数
		/// <summary>
		/// 构造插件设置对象。
		/// </summary>
		protected PluginSetupBase() : this(null, null)
		{
		}

		/// <summary>
		/// 构造插件设置对象。
		/// </summary>
		/// <param name="applicationDirectory">应用程序目录完整限定路径。</param>
		protected PluginSetupBase(string applicationDirectory) : this(applicationDirectory, null)
		{
		}

		/// <summary>
		/// 构造插件设置对象。
		/// </summary>
		/// <param name="applicationDirectory">应用程序目录完整限定路径。</param>
		/// <param name="pluginsDirectoryName">插件目录名，非完整路径。默认为 plugins</param>
		/// <remarks>
		///		如果<paramref name="applicationDirectory"/>参数为空或空字符串("")，则试图获取默认应用域(AppDomain)中的入口程序集路径。但在类似ASP.NET这样的服务器程序中无法获取入口程序集，因此本方法会抛出<seealso cref="System.ArgumentNullException"/>异常，在此场景下请指定<paramref name="applicationDirectory"/>参数值。
		/// </remarks>
		/// <exception cref="System.ArgumentNullException">当<paramref name="applicationDirectory"/>参数为空并且无法获取默认应用域(AppDomain)中入口程序集。</exception>
		/// <exception cref="System.ArgumentException">当<paramref name="applicationDirectory"/>参数值不为路径完全限定格式。</exception>
		/// <exception cref="System.ArgumentException">当<paramref name="pluginsDirectoryName"/>参数值为路径完全限定格式，但其并不是基于<paramref name="applicationDirectory"/>参数指定的应用程序路径下的子目录。</exception>
		protected PluginSetupBase(string applicationDirectory, string pluginsDirectoryName)
		{
			_pluginsDirectory = null;

			if(string.IsNullOrWhiteSpace(applicationDirectory))
			{
				//以下被注释代码为原有设计，该设计在Web应用中无效，特改为使用当前AppDomain的基目录。by Popepe Zong(2012.3.9)
				/*
				Assembly entryAssembly = Assembly.GetEntryAssembly();

				if(entryAssembly == null)
					throw new ArgumentNullException("applicationDirectory");

				_applicationDirectory = Path.GetDirectoryName(entryAssembly.Location);
				*/
				_applicationDirectory = AppDomain.CurrentDomain.BaseDirectory;
			}
			else
			{
				_applicationDirectory = applicationDirectory.Trim();

				if(!Path.IsPathRooted(_applicationDirectory))
					throw new ArgumentException("This value of 'applicationDirectory' parameter is invalid.");
			}

			if(string.IsNullOrWhiteSpace(pluginsDirectoryName))
				_pluginsDirectoryName = "plugins";
			else
			{
				_pluginsDirectoryName = pluginsDirectoryName.Trim();

				if(Path.IsPathRooted(_pluginsDirectoryName))
				{
					if(_applicationDirectory.StartsWith(_pluginsDirectoryName))
						_pluginsDirectoryName = _pluginsDirectoryName.Substring(_applicationDirectory.Length);
					else
						throw new ArgumentException("This value of 'pluginsDirectoryName' parameter is invalid.");
				}
			}
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取应用程序的目录路径，该属性值由构造函数注入。
		/// </summary>
		/// <remarks>
		/// 应用目录是基于完全限定的路径格式，在Windows操作系统中大致如：C:\Program Files\Zongsoft.EAS 或 D:\Zongsoft\Zongsoft.EAS 等等。
		/// </remarks>
		public string ApplicationDirectory
		{
			get
			{
				return _applicationDirectory;
			}
		}

		/// <summary>
		/// 获取插件文件夹名称，该属性值由构造函数注入。
		/// </summary>
		/// <remarks>
		/// 注意：该属性值仅为目录名本身，不含其它路径部分。它不是一个完全限定的路径格式。如果要获取插件文件夹的完全限定路径，请使用<seealso cref="Zongsoft.Plugins.PluginSetupBase.PluginsDirectory"/>属性。
		/// </remarks>
		public string PluginsDirectoryName
		{
			get
			{
				return _pluginsDirectoryName;
			}
		}

		/// <summary>
		/// 获取插件文件夹的完全限定路径。
		/// </summary>
		/// <remarks>
		/// 该属性值根据<seealso cref="Zongsoft.Plugins.PluginSetupBase.ApplicationDirectory"/>和<seealso cref="Zongsoft.Plugins.PluginSetupBase.PluginsDirectoryName"/>属性值合并计算而来。
		/// </remarks>
		public string PluginsDirectory
		{
			get
			{
				if(string.IsNullOrEmpty(_pluginsDirectory))
					_pluginsDirectory = Path.Combine(_applicationDirectory, _pluginsDirectoryName);

				return _pluginsDirectory;
			}
		}
		#endregion

		#region 保护方法
		protected void OnPropertyChanged(string propertyName)
		{
			this.OnPropertyChanged(new PropertyChangedEventArgs(propertyName ?? string.Empty));
		}

		protected virtual void OnPropertyChanged(PropertyChangedEventArgs args)
		{
			if(this.PropertyChanged != null)
				this.PropertyChanged(this, args);
		}
		#endregion
	}
}
