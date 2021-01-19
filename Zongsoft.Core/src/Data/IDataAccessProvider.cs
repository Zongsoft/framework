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

namespace Zongsoft.Data
{
	/// <summary>
	/// 表示数据访问提供程序的接口。
	/// </summary>
	public interface IDataAccessProvider
	{
		/// <summary>
		/// 获取或创建指定应用的数据访问器。
		/// </summary>
		/// <param name="name">指定的应用名。</param>
		/// <returns>返回指定应用名的数据访问器。</returns>
		IDataAccess GetAccessor(string name);

		/// <summary>
		/// 尝试获取指定应用的数据访问器。
		/// </summary>
		/// <param name="name">指定的应用名。</param>
		/// <param name="accessor">返回指定的名称的数据访问器，如果为空则表示该应用没有数据访问器。</param>
		/// <returns>返回一个值，指示指定名称的数据访问器是否存在。</returns>
		bool TryGetAccessor(string name, out IDataAccess accessor);
	}
}
