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

using Zongsoft.IO;
using Zongsoft.Reflection;
using Zongsoft.Reflection.Expressions;

namespace Zongsoft.Collections
{
	/// <summary>
	/// 表示层次结构的表达式类。
	/// </summary>
	/// <remarks>
	///		<para>层次结构表达式由“路径”和“成员集”两部分组成，其文本格式如下：</para>
	///		<list type="number">
	///			<item>
	///				<term>绝对路径：/root/node1/node2/node3@property1.property2 或 /Data/ConnectionStrings['Automao.SaaS'] </term>
	///				<term>相对路径：../siblingNode/node1/node2@property1.property2 或 childNode/node1/node2@property1.property2</term>
	///				<term>属性路径：../@property1.property2 或 ./@collectionProperty[index]（对于本节点的属性也可以简写成：@property1.property2）</term>
	///			</item>
	///		</list>
	/// </remarks>
	public class HierarchicalExpression
	{
		#region 成员字段
		private string _path;
		private string[] _segments;
		private PathAnchor _anchor;
		private IMemberExpression _members;
		#endregion

		#region 构造函数
		public HierarchicalExpression(PathAnchor anchor, string[] segments, IMemberExpression members)
		{
			_anchor = anchor;
			_segments = segments ?? Array.Empty<string>();
			_members = members;

			switch(_anchor)
			{
				case IO.PathAnchor.Root:
					_path = HierarchicalNode.PathSeparatorChar + string.Join(HierarchicalNode.PathSeparatorChar, _segments);
					break;
				case IO.PathAnchor.Current:
					_path = "." + HierarchicalNode.PathSeparatorChar + string.Join(HierarchicalNode.PathSeparatorChar, _segments);
					break;
				case IO.PathAnchor.Parent:
					_path = "." + HierarchicalNode.PathSeparatorChar + string.Join(HierarchicalNode.PathSeparatorChar, _segments);
					break;
				default:
					_path = string.Join(HierarchicalNode.PathSeparatorChar, _segments);
					break;
			}
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取层次路径的锚定点。
		/// </summary>
		public IO.PathAnchor Anchor
		{
			get
			{
				return _anchor;
			}
		}

		/// <summary>
		/// 获取层次表达式的路径。
		/// </summary>
		public string Path
		{
			get
			{
				return _path;
			}
		}

		/// <summary>
		/// 获取包含构成<see cref="Path"/>路径段的数组。
		/// </summary>
		public string[] Segments
		{
			get
			{
				return _segments;
			}
		}

		/// <summary>
		/// 获取层次表达式中的成员访问表达式。
		/// </summary>
		public IMemberExpression Members
		{
			get
			{
				return _members;
			}
		}
		#endregion

		#region 静态方法
		public static HierarchicalExpression Parse(string text)
		{
			return HierarchicalExpressionParser.Parse(text);
		}

		public static bool TryParse(string text, out HierarchicalExpression result)
		{
			return HierarchicalExpressionParser.TryParse(text, out result);
		}
		#endregion
	}
}
