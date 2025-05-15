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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Components;

public sealed class CommandTreeNodeAliaser(CommandTreeNode root) : IEnumerable<string>
{
	#region 成员字段
	private readonly CommandTreeNode _root = root;
	#endregion

	#region 公共方法
	public bool Set(string path, string alias)
	{
		if(string.IsNullOrEmpty(path))
			return false;

		if(string.IsNullOrWhiteSpace(alias))
			throw new ArgumentNullException(nameof(alias));

		if(alias.Length > 100)
			throw new ArgumentOutOfRangeException(nameof(alias));

		if(alias.Contains('/') || alias.Contains('.') || alias.Contains('\\'))
			throw new ArgumentException($"The specified alias contains illegal characters.", nameof(alias));

		var node = _root.Find(path);
		return node != null && node.Aliases.Add(alias);
	}
	#endregion

	#region 私有方法
	private static IEnumerable<string> GetAliases(CommandTreeNode node)
	{
		if(node == null)
			yield break;

		foreach(string alias in node.Aliases)
			yield return alias;

		foreach(var child in node.Children)
		{
			var aliases = GetAliases(child);

			foreach(var alias in aliases)
				yield return alias;
		}
	}
	#endregion

	#region 枚举遍历
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	public IEnumerator<string> GetEnumerator() => GetAliases(_root).GetEnumerator();
	#endregion
}