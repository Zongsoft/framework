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
using System.ComponentModel;

namespace Zongsoft.Plugins
{
	/// <summary>
	/// 表示工作台的接口，包含对工作台的基本行为特性的定义。
	/// </summary>
	public interface IWorkbenchBase
	{
		/// <summary>当工作台被打开后。</summary>
		event EventHandler Opened;
		/// <summary>当工作台被打开前。</summary>
		event EventHandler Opening;
		/// <summary>当工作台被关闭后。</summary>
		event EventHandler Closed;
		/// <summary>当工作台被关闭前。</summary>
		event CancelEventHandler Closing;

		/// <summary>
		/// 获取工作台的当前状态。
		/// </summary>
		WorkbenchStatus Status
		{
			get;
		}

		/// <summary>
		/// 获取或设置工作台标题。
		/// </summary>
		string Title
		{
			get;
			set;
		}

		/// <summary>
		/// 关闭工作台。
		/// </summary>
		void Close();

		/// <summary>
		/// 启动工作台。
		/// </summary>
		void Open();
	}
}
