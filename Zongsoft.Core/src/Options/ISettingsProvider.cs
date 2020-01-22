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
using System.Collections.Generic;

namespace Zongsoft.Options
{
	/// <summary>
	/// 关于自定义设置的供应者程序。
	/// </summary>
	public interface ISettingsProvider
	{
		/// <summary>
		/// 获取指定名称的自定义设置项的值。
		/// </summary>
		/// <param name="name">要查找的自定义设置项的名称。</param>
		/// <returns>返回指定名称的自定义设置项的值。如果指定名称的设置项不存在则返回空(null)。</returns>
		/// <remarks>
		///		<para>实现要求：如果指定名称的设置项不存在或查找失败，不要抛出异常，应返回空(null)。</para>
		/// </remarks>
		object GetValue(string name);

		void SetValue(string name, object value);
	}
}
