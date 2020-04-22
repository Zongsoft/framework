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
using System.Collections.Generic;

namespace Zongsoft.Plugins
{
	/// <summary>
	/// 封装了有关插件特定的信息。
	/// </summary>
	public sealed class PluginContext
	{
		#region 构造函数
		internal PluginContext(PluginApplicationContext applicationContext, PluginOptions options)
		{
			this.ApplicationContext = applicationContext ?? throw new ArgumentNullException(nameof(applicationContext));
			this.Options = options ?? throw new ArgumentNullException(nameof(options));
			this.PluginTree = new PluginTree(this);
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取当前插件运行时的唯一插件树对象。
		/// </summary>
		public PluginTree PluginTree { get; }

		/// <summary>
		/// 获取加载的根插件集。
		/// </summary>
		public IEnumerable<Plugin> Plugins
		{
			get => PluginTree.Plugins;
		}

		/// <summary>
		/// 获取当前插件运行时所属的应用程序上下文对象。
		/// </summary>
		public PluginApplicationContext ApplicationContext { get; }

		/// <summary>
		/// 获取当前插件上下文对应的设置。
		/// </summary>
		public PluginOptions Options { get; }

		/// <summary>
		/// 获取当前工作台(主界面)对象。
		/// </summary>
		public IWorkbenchBase Workbench
		{
			get => this.PluginTree.RootNode.Resolve(this.Options.Mountion.WorkbenchPath) as IWorkbenchBase;
		}
		#endregion
	}
}
