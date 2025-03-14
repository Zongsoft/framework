﻿/*
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
using System.Collections.ObjectModel;

namespace Zongsoft.Security.Privileges;

public class PrivilegeCollection(PrivilegeCategory category) : KeyedCollection<string, Privilege>(StringComparer.OrdinalIgnoreCase)
{
	private PrivilegeCategory _root;
	private readonly PrivilegeCategory _category = category;

	private PrivilegeCategory Root => _root ??= _category.Find("/");

	protected override string GetKeyForItem(Privilege privilege) => privilege.Name;

	public IEnumerable<Privilege> FindAll(string target, string action)
	{
		foreach(var privilege in FindAll(this.Root, target, action))
			yield return privilege;
	}

	private static IEnumerable<Privilege> FindAll(PrivilegeCategory category, string target, string action)
	{
		if(category == null || target == null)
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
