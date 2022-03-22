/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
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
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Zongsoft.Data.Common
{
	public class EntityMemberProvider
	{
		#region 单例模式
		public static readonly EntityMemberProvider Instance = new EntityMemberProvider();
		#endregion

		#region 成员字段
		private readonly ConcurrentDictionary<Type, Collections.INamedCollection<EntityMember>> _cache;
		#endregion

		#region 构造函数
		private EntityMemberProvider()
		{
			_cache = new ConcurrentDictionary<Type, Collections.INamedCollection<EntityMember>>();
		}
		#endregion

		#region 公共方法
		public Collections.INamedCollection<EntityMember> GetMembers(Type type)
		{
			if(type == null)
				throw new ArgumentNullException(nameof(type));

			//如果指定的类型是单值类型则返回空
			if(Zongsoft.Common.TypeExtension.IsScalarType(type))
				return null;

			return _cache.GetOrAdd(type, key => CreateMembers(key));
		}
		#endregion

		#region 私有方法
		private static Collections.INamedCollection<EntityMember> CreateMembers(Type type)
		{
			//如果是字典则返回空
			if(Zongsoft.Common.TypeExtension.IsDictionary(type))
				return null;

			if(Zongsoft.Common.TypeExtension.IsEnumerable(type))
				type = Zongsoft.Common.TypeExtension.GetElementType(type);

			var members = FindMembers(type);
			var tokens = new Collections.NamedCollection<EntityMember>(item => item.Name);

			foreach(var member in members)
			{
				var token = CreateMemberToken(member);

				if(token != null)
					tokens.Add(token.Value);
			}

			return tokens;
		}

		private static EntityMember? CreateMemberToken(MemberInfo member)
		{
			switch(member.MemberType)
			{
				case MemberTypes.Field:
					var field = (FieldInfo)member;

					if(!field.IsInitOnly)
					{
						var converter = Utility.GetConverter(member);
						return new EntityMember(field, converter, EntityEmitter.GenerateFieldSetter(field, converter));
					}

					break;
				case MemberTypes.Property:
					var property = (PropertyInfo)member;

					if(property.CanRead && property.CanWrite)
					{
						var converter = Utility.GetConverter(member);
						return new EntityMember(property, converter, EntityEmitter.GeneratePropertySetter(property, converter));
					}

					break;
			}

			return null;
		}

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
		#endregion
	}
}
