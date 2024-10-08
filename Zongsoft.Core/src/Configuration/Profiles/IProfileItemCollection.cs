﻿/*
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
using System.Collections.Generic;

namespace Zongsoft.Configuration.Profiles
{
	public interface IProfileItemCollection<T> : IReadOnlyCollection<T> where T : ProfileItem
	{
		/// <summary>只读索引器，获取指定名称的元素。</summary>
		/// <param name="name">指定要获取的元素名。</param>
		/// <returns>返回指定名称的元素对象，如果没有找到则抛出<seealso cref="KeyNotFoundException"/>异常。</returns>
		/// <exception cref="KeyNotFoundException">当指定<paramref name="name"/>名称的元素不存在则激发该异常。</exception>
		T this[string name] { get; }

		/// <summary>判断当前集合是否包含指定名称的元素。</summary>
		/// <param name="name">指定要判断的元素名。</param>
		/// <returns>如果指定名称的元素是存在的则返回真(True)，否则返回假(False)。</returns>
		bool Contains(string name);

		/// <summary>尝试获取指定名称的元素。</summary>
		/// <param name="name">指定要获取的元素名。</param>
		/// <param name="value">输出参数，包含指定名称的元素对象。</param>
		/// <returns>返回一个值，指示指定名称的元素是否获取成功。</returns>
		bool TryGetValue(string name, out T value);

		/// <summary>新增指定的元素。</summary>
		/// <param name="value">指定要新增的元素。</param>
		void Add(T value);

		/// <summary>删除指定名称的元素。</summary>
		/// <param name="name">指定要删除的元素名。</param>
		/// <returns>如果删除成功则返回真（True），否则返回假（False）。</returns>
		bool Remove(string name);
	}
}