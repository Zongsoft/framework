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

namespace Zongsoft.Collections;

public static class HierarchicalNodeUtility
{
	/// <summary>判断指定节点是否为根节点。</summary>
	/// <param name="node">要判断的层次节点；为空时返回假。</param>
	/// <returns>如果指定节点是根节点则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	public static bool IsRoot(this IHierarchicalNode node) => node != null && (string.IsNullOrEmpty(node.Name) || (node.Name.Length == 1 && node.Name[0] == HierarchicalNode.PathSeparator));
}
