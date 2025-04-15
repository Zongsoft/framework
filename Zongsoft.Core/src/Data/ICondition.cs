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

namespace Zongsoft.Data;

/// <summary>
/// 条件接口。
/// </summary>
public interface ICondition
{
	/// <summary>判断当前条件语句中是否包含指定名称的条件项。</summary>
	/// <param name="name">指定的条件项名称。</param>
	/// <returns>如果存在则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	bool Contains(string name);

	/// <summary>在条件语句中查找指定名称的条件项。</summary>
	/// <param name="name">指定要查找的条件项名称。</param>
	/// <returns>如果查找成功则返回找到的条件项，否则返回空(<c>null</c>)。</returns>
	Condition Find(string name);

	/// <summary>在条件语句中查找指定名称的所有条件项。</summary>
	/// <param name="name">指定要查找的条件项名称。</param>
	/// <returns>返回匹配成功的所有条件项数组，否则返回空(<c>null</c>)或空数组。</returns>
	Condition[] FindAll(string name);

	/// <summary>在条件语句中查找指定名称的条件项，如果匹配到则回调指定的匹配函数。</summary>
	/// <param name="name">指定要匹配的条件项名称。</param>
	/// <param name="matched">指定的匹配成功的回调函数。</param>
	/// <returns>如果匹配成功则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	bool Match(string name, Action<Condition> matched = null);

	/// <summary>在条件语句中查找指定名称的所有条件项，如果匹配到则回调指定的匹配函数。</summary>
	/// <param name="name">指定要匹配的条件项名称。</param>
	/// <param name="matched">指定的匹配成功的回调函数。</param>
	/// <returns>返回匹配成功的条件项数量，如果为零则表示没有匹配到任何条件项。</returns>
	int Matches(string name, Action<Condition> matched = null);
}
