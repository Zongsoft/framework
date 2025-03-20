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
 * Copyright (C) 2020-2025 Zongsoft Studio <http://www.zongsoft.com>
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
