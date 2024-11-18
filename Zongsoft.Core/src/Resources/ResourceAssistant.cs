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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Zongsoft.Resources;

public static class ResourceAssistant
{
	#region 成员字段
	private static readonly ConcurrentDictionary<string, IResource> _resources = new();
	#endregion

	#region 公共属性
	public static ICollection<IResource> Resources => _resources.Values;
	#endregion

	#region 公共方法
	public static IResource GetResource(MemberInfo member)
	{
		if(member == null)
			throw new ArgumentNullException(nameof(member));

		if(member is Type type)
			return GetResource(type);

		type = member.ReflectedType ?? member.DeclaringType;
		return type != null ? GetResource(type) : GetResource(member.GetType().Assembly);
	}

	public static IResource GetResource(Type type) => GetResource(type?.Assembly);
	public static IResource GetResource<T>() => GetResource(typeof(T).Assembly);
	public static IResource GetResource(Assembly assembly)
	{
		if(assembly == null)
			throw new ArgumentNullException(nameof(assembly));

		return _resources.GetOrAdd(assembly.GetName().FullName, key => new Resource(assembly));
	}
	#endregion
}
