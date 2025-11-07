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
 * This file is part of Zongsoft.Data library.
 *
 * The Zongsoft.Data is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Data is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Data library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Zongsoft.Data.Common;

public static class ModelMemberTokenManager
{
	#region 私有变量
	private static readonly ConcurrentDictionary<Type, IEnumerable> _generics = new();
	#endregion

	#region 公共方法
	public static ModelMemberTokenCollection<T> GetMembers<T>(IDataDriver driver)
	{
		//如果指定的类型是单值类型则返回空
		if(Zongsoft.Common.TypeExtension.IsScalarType(typeof(T)))
			return null;

		return (ModelMemberTokenCollection<T>)_generics.GetOrAdd(typeof(T), _ => Create<T>(driver));
	}
	#endregion

	#region 私有方法
	private static IEnumerable<MemberInfo> FindMembers(Type type)
	{
		foreach(var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
			yield return field;

		foreach(var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
			yield return property;

		if(type.IsInterface)
		{
			var contracts = type.GetInterfaces();

			foreach(var contract in contracts)
			{
				foreach(var property in contract.GetProperties(BindingFlags.Public | BindingFlags.Instance))
					yield return property;
			}
		}
	}

	private static ModelMemberTokenCollection<T> Create<T>(IDataDriver driver)
	{
		var type = typeof(T);

		//如果是字典则返回空
		if(Zongsoft.Common.TypeExtension.IsDictionary(type))
			return null;

		if(Zongsoft.Common.TypeExtension.IsEnumerable(type))
			type = Zongsoft.Common.TypeExtension.GetElementType(type);

		var members = FindMembers(type);
		var tokens = new ModelMemberTokenCollection<T>();

		foreach(var member in members)
		{
			var token = CreateMemberToken<T>(driver, member);

			if(token != null)
				tokens.Add(token);
		}

		return tokens;
	}

	private static ModelMemberToken<T> CreateMemberToken<T>(IDataDriver driver, MemberInfo member)
	{
		switch(member.MemberType)
		{
			case MemberTypes.Field:
				var field = (FieldInfo)member;

				if(!field.IsInitOnly)
					return new ModelMemberToken<T>(driver, field);

				break;
			case MemberTypes.Property:
				var property = (PropertyInfo)member;

				if(property.CanRead && property.CanWrite)
					return new ModelMemberToken<T>(driver, property);

				break;
		}

		return null;
	}
	#endregion
}
