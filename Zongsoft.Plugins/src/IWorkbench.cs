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
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Plugins
{
	/// <summary>
	/// 表示工作台的接口，包含对工作台的基本行为特性的定义。
	/// </summary>
	public interface IWorkbench : IWorkbenchBase
	{
		/// <summary>当视图被激活。</summary>
		event EventHandler<ViewEventArgs> ViewActivate;
		/// <summary>当视图失去焦点，当视图被关闭时也会触发该事件。</summary>
		event EventHandler<ViewEventArgs> ViewDeactivate;

		/// <summary>
		/// 获取当前活动的视图对象。
		/// </summary>
		object ActiveView
		{
			get;
		}

		/// <summary>
		/// 获取当前工作台的所有打开的视图对象。
		/// </summary>
		object[] Views
		{
			get;
		}

		/// <summary>
		/// 获取当前工作台的窗口对象。
		/// </summary>
		object Window
		{
			get;
		}

		/// <summary>
		/// 激活指定名称的视图对象。
		/// </summary>
		/// <param name="name">视图名称。</param>
		/// <returns>被激活的视图对象。</returns>
		void ActivateView(string name);
	}
}
