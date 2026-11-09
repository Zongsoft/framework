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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;

using Zongsoft.IO;
using Zongsoft.Reflection.Expressions;

namespace Zongsoft.Collections;

/// <summary>表示层次结构的表达式类。</summary>
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
					this.Path = string.Join(HierarchicalNode.PathSeparator, this.Segments);
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
