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
using System.Reflection;
using System.Collections.Concurrent;

namespace Zongsoft.Resources;

partial class Resource
{
	#region 成员字段
	private static readonly ConcurrentDictionary<string, IResource> _cache = new();
	#endregion

	#region 公共方法
	public static IResource GetResource(MemberInfo member, IResourceLocator locator = null)
	{
		if(member == null)
			return null;

		if(member is Type type)
			return GetResource(type);

		type = member.ReflectedType ?? member.DeclaringType;
		return type != null ? GetResource(type, locator) : GetResource(member.GetType().Assembly, locator);
	}

	public static IResource GetResource(Type type, IResourceLocator locator = null) => GetResource(type?.Assembly, locator);
	public static IResource GetResource<T>(IResourceLocator locator = null) => GetResource(typeof(T).Assembly, locator);
	public static IResource GetResource(Assembly assembly, IResourceLocator locator = null)
	{
		return assembly == null ? null : _cache.GetOrAdd(assembly.GetName().FullName, (key, argument) => new Resource(assembly, argument), locator);
	}
	#endregion
}
