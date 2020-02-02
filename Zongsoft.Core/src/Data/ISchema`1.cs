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
	/// 表示数据模式的接口。
	/// </summary>
	/// <typeparam name="TMember">泛型参数，表示数据模式的成员类型。</typeparam>
	public interface ISchema<TMember> : ISchema where TMember : SchemaMemberBase
	{
		/// <summary>
		/// 获取数据模式元素集合。
		/// </summary>
		Collections.INamedCollection<TMember> Members
		{
			get;
		}

		/// <summary>
		/// 查找指定路径的模式元素。
		/// </summary>
		/// <param name="path">指定要查找的元素路径，路径是以句点或斜杠为分隔符而连接的成员名字符串。</param>
		/// <returns>返回找到的模式元素，如果查找失败则返回空(null)。</returns>
		new TMember Find(string path);

		/// <summary>
		/// 添加一个元素到位于指定路径处的元素集中。
		/// </summary>
		/// <param name="path">指定要添加的元素路径，路径是以句点或斜杠为分隔符而连接的成员名字符串。</param>
		/// <returns>返回当前数据模式。</returns>
		new ISchema<TMember> Include(string path);

		/// <summary>
		/// 从元素集中移除指定位置的元素。
		/// </summary>
		/// <param name="path">指定要移除的元素路径，路径是以句点或斜杠为分隔符而连接的成员名字符串。</param>
		/// <returns>返回当前数据模式。</returns>
		new ISchema<TMember> Exclude(string path);

		/// <summary>
		/// 从元素集中移除指定位置的元素。
		/// </summary>
		/// <param name="path">指定要移除的元素路径，路径是以句点或斜杠为分隔符而连接的成员名字符串。</param>
		/// <param name="member">输出参数，如果排除成功则返回被排除的模式成员。</param>
		/// <returns>返回一个值指示是否排除成功，如果为真(True)则表示成功，否则为失败。</returns>
		bool Exclude(string path, out TMember member);
	}
}
