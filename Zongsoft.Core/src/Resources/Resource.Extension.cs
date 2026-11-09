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
