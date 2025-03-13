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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Resources;

public static class ResourceUtility
{
	public static object GetObject<T>(this IResource resource, string name) => resource?.GetObject(name, GetLocation(typeof(T)));
	public static object GetObject(this IResource resource, string name, Type location) => resource?.GetObject(name, GetLocation(location));
	public static object GetObject(this IResource resource, string name, MemberInfo location) => resource?.GetObject(name, GetLocation(location));
	public static string GetString<T>(this IResource resource, string name) => resource?.GetString(name, GetLocation(typeof(T)));
	public static string GetString(this IResource resource, string name, Type location) => resource?.GetString(name, GetLocation(location));
	public static string GetString(this IResource resource, string name, MemberInfo location) => resource?.GetString(name, GetLocation(location));

	public static object GetObject(this IResource resource, params ReadOnlySpan<string> names) => GetObject(resource, null, names);
	public static object GetObject(this IResource resource, Type location, params ReadOnlySpan<string> names)
	{
		if(resource == null)
			return null;

		for(int i = 0; i < names.Length; i++)
		{
			var result = resource.GetObject(names[i], location);

			if(result != null)
				return result;
		}

		return null;
	}
	public static object GetObject(this IResource resource, MemberInfo location, params ReadOnlySpan<string> names)
	{
		if(resource == null)
			return null;

		for(int i = 0; i < names.Length; i++)
		{
			var result = resource.GetObject(names[i], location);

			if(result != null)
				return result;
		}

		return null;
	}

	public static string GetString(this IResource resource, params ReadOnlySpan<string> names) => GetString(resource, null, names);
	public static string GetString(this IResource resource, Type location, params ReadOnlySpan<string> names)
	{
		if(resource == null)
			return null;

		for(int i = 0; i < names.Length; i++)
		{
			var result = resource.GetString(names[i], location);

			if(result != null)
				return result;
		}

		return null;
	}
	public static string GetString(this IResource resource, MemberInfo location, params ReadOnlySpan<string> names)
	{
		if(resource == null)
			return null;

		for(int i = 0; i < names.Length; i++)
		{
			var result = resource.GetString(names[i], location);

			if(result != null)
				return result;
		}

		return null;
	}

	public static bool TryGetObject<T>(this IResource resource, string name, out object value) => resource.TryGetObject(name, GetLocation(typeof(T)), out value);
	public static bool TryGetObject(this IResource resource, string name, Type location, out object value) => resource.TryGetObject(name, GetLocation(location), out value);
	public static bool TryGetObject(this IResource resource, string name, MemberInfo location, out object value) => resource.TryGetObject(name, GetLocation(location), out value);
	public static bool TryGetString<T>(this IResource resource, string name, out string value) => resource.TryGetString(name, GetLocation(typeof(T)), out value);
	public static bool TryGetString(this IResource resource, string name, Type location, out string value) => resource.TryGetString(name, GetLocation(location), out value);
	public static bool TryGetString(this IResource resource, string name, MemberInfo location, out string value) => resource.TryGetString(name, GetLocation(location), out value);

	internal static string GetLocation(MemberInfo member) => member == null ? null : GetLocation(member.ReflectedType);
	internal static string GetLocation(Type type) => type == null ? null : (string.IsNullOrEmpty(type.Namespace) ? type.Name : $"{type.Namespace}{Type.Delimiter}{type.Name}");

	public static object GetResourceObject(this Type location, string name) => Resource.GetResource(location).GetObject(name, location);
	public static object GetResourceObject(this MemberInfo location, string name) => Resource.GetResource(location).GetObject(name, location);
	public static object GetResourceObject(this Assembly assembly, string name, Type location) => Resource.GetResource(assembly).GetObject(name, location);
	public static object GetResourceObject(this Assembly assembly, string name, MemberInfo location) => Resource.GetResource(assembly).GetObject(name, location);
	public static object GetResourceObject(this Assembly assembly, string name, string location = null) => Resource.GetResource(assembly).GetObject(name, location);

	public static object GetResourceObject(this Type location, params ReadOnlySpan<string> names)
	{
		var resource = Resource.GetResource(location);
		if(resource == null)
			return null;

		for(int i = 0; i < names.Length; i++)
		{
			var result = resource.GetObject(names[i], location);

			if(result != null)
				return result;
		}

		return null;
	}
	public static object GetResourceObject(this MemberInfo location, params ReadOnlySpan<string> names)
	{
		var resource = Resource.GetResource(location);
		if(resource == null)
			return null;

		for(int i = 0; i < names.Length; i++)
		{
			var result = resource.GetObject(names[i], location);

			if(result != null)
				return result;
		}

		return null;
	}
	public static object GetResourceObject(this Assembly assembly, params ReadOnlySpan<string> names)
	{
		var resource = Resource.GetResource(assembly);
		if(resource == null)
			return null;

		for(int i = 0; i < names.Length; i++)
		{
			var result = resource.GetObject(names[i]);

			if(result != null)
				return result;
		}

		return null;
	}
	public static object GetResourceObject(this Assembly assembly, Type location, params ReadOnlySpan<string> names)
	{
		var resource = Resource.GetResource(assembly);
		if(resource == null)
			return null;

		for(int i = 0; i < names.Length; i++)
		{
			var result = resource.GetObject(names[i], location);

			if(result != null)
				return result;
		}

		return null;
	}
	public static object GetResourceObject(this Assembly assembly, MemberInfo location, params ReadOnlySpan<string> names)
	{
		var resource = Resource.GetResource(assembly);
		if(resource == null)
			return null;

		for(int i = 0; i < names.Length; i++)
		{
			var result = resource.GetObject(names[i], location);

			if(result != null)
				return result;
		}

		return null;
	}

	public static string GetResourceString(this Type location, string name) => Resource.GetResource(location).GetString(name, location);
	public static string GetResourceString(this MemberInfo location, string name) => Resource.GetResource(location).GetString(name, location);
	public static string GetResourceString(this Assembly assembly, string name, Type location) => Resource.GetResource(assembly).GetString(name, location);
	public static string GetResourceString(this Assembly assembly, string name, MemberInfo location) => Resource.GetResource(assembly).GetString(name, location);
	public static string GetResourceString(this Assembly assembly, string name, string location = null) => Resource.GetResource(assembly).GetString(name, location);

	public static string GetResourceString(this Type location, params ReadOnlySpan<string> names)
	{
		var resource = Resource.GetResource(location);
		if(resource == null)
			return null;

		for(int i = 0; i < names.Length; i++)
		{
			var result = resource.GetString(names[i], location);

			if(result != null)
				return result;
		}

		return null;
	}
	public static string GetResourceString(this MemberInfo location, params ReadOnlySpan<string> names)
	{
		var resource = Resource.GetResource(location);
		if(resource == null)
			return null;

		for(int i = 0; i < names.Length; i++)
		{
			var result = resource.GetString(names[i], location);

			if(result != null)
				return result;
		}

		return null;
	}
	public static string GetResourceString(this Assembly assembly, params ReadOnlySpan<string> names)
	{
		var resource = Resource.GetResource(assembly);
		if(resource == null)
			return null;

		for(int i = 0; i < names.Length; i++)
		{
			var result = resource.GetString(names[i]);

			if(result != null)
				return result;
		}

		return null;
	}
	public static string GetResourceString(this Assembly assembly, Type location, params ReadOnlySpan<string> names)
	{
		var resource = Resource.GetResource(assembly);
		if(resource == null)
			return null;

		for(int i = 0; i < names.Length; i++)
		{
			var result = resource.GetString(names[i], location);

			if(result != null)
				return result;
		}

		return null;
	}
	public static string GetResourceString(this Assembly assembly, MemberInfo location, params ReadOnlySpan<string> names)
	{
		var resource = Resource.GetResource(assembly);
		if(resource == null)
			return null;

		for(int i = 0; i < names.Length; i++)
		{
			var result = resource.GetString(names[i], location);

			if(result != null)
				return result;
		}

		return null;
	}
}
