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
	public interface ISchema
	{
		#region 属性定义
		/// <summary>获取数据模式的名称（对应数据访问操作的实体名）。</summary>
		string Name { get; }

		/// <summary>获取数据模式的原始表达式文本。</summary>
		string Text { get; }

		/// <summary>获取数据模式的映射类型（对应数据访问操作关联的数据实体元素类型）。</summary>
		Type Type { get; }

		/// <summary>获取一个值，指示没有任何元素（即空模式）。</summary>
		bool IsEmpty { get; }

		/// <summary>获取或设置一个值，指示是否只读模式。</summary>
		bool IsReadOnly { get; set; }
		#endregion

		#region 方法定义
		/// <summary>移除模式的所有元素。</summary>
		void Clear();

		/// <summary>判断是否包含指定路径的元素。</summary>
		/// <param name="path">指定的元素路径，路径是以句点或斜杠为分隔符而连接的成员名字符串。</param>
		/// <returns>如果包含指定路径的元素则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
		bool Contains(string path);

		/// <summary>查找指定路径的模式元素。</summary>
		/// <param name="path">指定要查找的元素路径，路径是以句点或斜杠为分隔符而连接的成员名字符串。</param>
		/// <returns>返回找到的模式元素，如果查找失败则返回空(<c>null</c>)。</returns>
		SchemaMemberBase Find(string path);

		/// <summary>添加一个元素到位于指定路径处的元素集中。</summary>
		/// <param name="path">指定要添加的元素路径，路径是以句点或斜杠为分隔符而连接的成员名字符串。</param>
		/// <returns>返回当前数据模式。</returns>
		ISchema Include(string path);

		/// <summary>从元素集中移除指定位置的元素。</summary>
		/// <param name="path">指定要移除的元素路径，路径是以句点或斜杠为分隔符而连接的成员名字符串。</param>
		/// <returns>返回当前数据模式。</returns>
		ISchema Exclude(string path);

		/// <summary>从元素集中移除指定位置的元素。</summary>
		/// <param name="path">指定要移除的元素路径，路径是以句点或斜杠为分隔符而连接的成员名字符串。</param>
		/// <param name="member">输出参数，如果排除成功则返回被排除的模式成员。</param>
		/// <returns>返回一个值指示是否排除成功，如果为真(<c>True</c>)则表示成功，否则为失败。</returns>
		bool Exclude(string path, out SchemaMemberBase member);
		#endregion
	}
}
