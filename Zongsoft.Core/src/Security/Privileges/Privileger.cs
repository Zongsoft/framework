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
using System.Collections.Generic;

namespace Zongsoft.Security.Privileges;

public class Privileger : PrivilegeCategory
{
	public IEnumerable<Privilege> FindAll(ReadOnlySpan<char> qualifiedName)
	{
		if(qualifiedName.IsEmpty)
			return [];

		var index = qualifiedName.LastIndexOfAny([':', '.']);
		if(index <= 0 || index >= qualifiedName.Length - 1)
			return [];

		return this.FindAll(qualifiedName[..index].ToString(), qualifiedName[(index + 1)..].ToString());
	}

	public IEnumerable<Privilege> FindAll(string target, string action)
	{
		foreach(var privilege in FindAll(this, target, action))
			yield return privilege;
	}

	private static IEnumerable<Privilege> FindAll(PrivilegeCategory category, string target, string action)
	{
		if(category == null || string.IsNullOrEmpty(target))
			yield break;

		foreach(var privilege in category.Privileges)
		{
			if(privilege.Permissions.Contains(target, action))
				yield return privilege;
		}

		foreach(var child in category.Categories)
		{
			foreach(var privilege in FindAll(child, target, action))
				yield return privilege;
		}
	}
}
