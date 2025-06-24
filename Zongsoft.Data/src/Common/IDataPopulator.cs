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
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Data;

namespace Zongsoft.Data.Common;

/// <summary>
/// 提供数据实体装配的接口。
/// </summary>
public interface IDataPopulator
{
	/// <summary>数据实体装配方法。</summary>
	/// <param name="record">指定要装配的数据记录。</param>
	/// <returns>返回装配成功的数据对象。</returns>
	object Populate(IDataRecord record);

	T Populate<T>(IDataRecord record);
}

/// <summary>
/// 提供数据实体装配的泛型接口。
/// </summary>
/// <typeparam name="T">装配的实体类型。</typeparam>
public interface IDataPopulator<out T>
{
	/// <summary>数据实体装配方法。</summary>
	/// <param name="record">指定要装配的数据记录。</param>
	/// <returns>返回装配成功的数据对象。</returns>
	T Populate(IDataRecord record);
}
