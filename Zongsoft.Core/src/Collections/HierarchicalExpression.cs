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

using Zongsoft.IO;
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
		#region 构造函数
		public HierarchicalExpression(PathAnchor anchor, string[] segments, IMemberExpression accessor)
		{
			this.Anchor = anchor;
			this.Accessor = accessor;
			this.Segments = segments ?? Array.Empty<string>();

			switch(anchor)
			{
				case PathAnchor.Root:
					if(segments == null || segments.Length == 0)
						this.Path = HierarchicalNode.PathSeparator.ToString();
					else
						this.Path = HierarchicalNode.PathSeparator + string.Join(HierarchicalNode.PathSeparator, segments);
					break;
				case PathAnchor.Current:
					if(segments == null || segments.Length == 0)
						this.Path = ".";
					else
						this.Path = "." + HierarchicalNode.PathSeparator + string.Join(HierarchicalNode.PathSeparator, segments);
					break;
				case PathAnchor.Parent:
					if(segments == null || segments.Length == 0)
						this.Path = "..";
					else
						this.Path = ".." + HierarchicalNode.PathSeparator + string.Join(HierarchicalNode.PathSeparator, segments);
					break;
				default:
					if(segments == null || segments.Length == 0)
						this.Path = string.Empty;
					else
						this.Path = string.Join(HierarchicalNode.PathSeparator, Segments);
					break;
			}
		}
		#endregion

		#region 公共属性
		/// <summary>获取层次路径的锚定点。</summary>
		public PathAnchor Anchor { get; }

		/// <summary>获取层次表达式的路径。</summary>
		public string Path { get; }

		/// <summary>获取包含构成<see cref="Path"/>路径段的数组。</summary>
		public string[] Segments { get; }

		/// <summary>获取层次表达式中的成员访问表达式。</summary>
		public IMemberExpression Accessor { get; }
		#endregion

		#region 静态方法
		public static HierarchicalExpression Parse(ReadOnlySpan<char> text) => HierarchicalExpressionParser.Parse(text);
		public static bool TryParse(ReadOnlySpan<char> text, out HierarchicalExpression result) => HierarchicalExpressionParser.TryParse(text, out result);
		#endregion
	}
}
