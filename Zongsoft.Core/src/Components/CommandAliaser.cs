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
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Components;

internal sealed class CommandAliaser(CommandNode root) : ICommandAliaser, IEnumerable<string>
{
	#region 成员字段
	private readonly CommandNode _root = root;
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

		if(alias.Contains('.') || alias.Contains('/') || alias.Contains('\\'))
			throw new ArgumentException(Properties.Resources.CommandAlias_InvalidCharacters_Message, nameof(alias));

		var node = _root.Find(path);
		return node != null && node.Aliases.Add(alias);
	}
	#endregion

	#region 私有方法
	private static IEnumerable<string> GetAliases(CommandNode node)
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