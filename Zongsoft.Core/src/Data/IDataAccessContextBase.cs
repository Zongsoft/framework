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
	/// 表示数据访问的上下文的基本接口。
	/// </summary>
	public interface IDataAccessContextBase
	{
		/// <summary>获取数据访问的名称。</summary>
		string Name { get; }

		/// <summary>获取数据驱动的名称。</summary>
		string Driver { get; }

		/// <summary>获取数据访问的方法。</summary>
		DataAccessMethod Method { get; }

		/// <summary>获取当前上下文关联的数据访问器。</summary>
		IDataAccess DataAccess { get; }
	}

	/// <summary>
	/// 表示数据访问的上下文的基本接口。
	/// </summary>
	/// <typeparam name="TOptions">当前数据操作的选项类型。</typeparam>
	public interface IDataAccessContextBase<TOptions> : IDataAccessContextBase where TOptions : IDataOptions
	{
		/// <summary>获取当前数据访问操作的选项对象。</summary>
		TOptions Options { get; }
	}
}
