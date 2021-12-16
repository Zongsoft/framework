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

using Zongsoft.Collections;

namespace Zongsoft.Data.Metadata
{
	/// <summary>
	/// 实体元数据的扩展类。
	/// </summary>
	public static class DataEntityExtension
	{
		#region 私有变量
		private static readonly ConcurrentDictionary<IDataEntity, EntityTokenCache> _cache = new ConcurrentDictionary<IDataEntity, EntityTokenCache>();
		#endregion

		#region 公共方法
		public static IDataEntityProperty Find(this IDataEntity entity, string path)
		{
			if(string.IsNullOrEmpty(path))
				return null;

			int index, last = 0;
			IDataEntityProperty property;
			var properties = entity.Properties;

			int GetLast(int position)
			{
				return position > 0 ? position + 1 : position;
			}

			while((index = path.IndexOf('.', last + 1)) > 0)
			{
				if(properties.TryGet(path.Substring(GetLast(last), index - GetLast(last)), out property) && property.IsComplex)
				{
					var complex = (IDataEntityComplexProperty)property;

					if(complex.ForeignProperty == null)
						properties = complex.Foreign.Properties;
					else
						properties = complex.ForeignProperty.Entity.Properties;
				}
				else
				{
					if(property == null)
						throw new InvalidOperationException($"The specified '{path}' member does not exist in the '{entity}' entity.");
					else
						throw new InvalidOperationException($"The specified '{path}' member does not exist in the '{entity}' entity.");
				}

				last = index;
			}

			if(properties.TryGet(path.Substring(GetLast(last)), out property))
				return property;

			throw new InvalidOperationException($"The specified '{path}' member does not exist in the '{entity}' entity.");
		}

		public static IDataEntity GetEntity(this IDataEntity entity, string name)
		{
			if(entity == null || string.IsNullOrEmpty(name))
				return null;

			if(name.Contains('.'))
				return entity.Metadata.Manager.Entities.Get(name);

			if(entity.Metadata.Entities.TryGet(entity.Namespace + '.' + name, out var found))
				return found;

			return entity.Metadata.Manager.Entities.Get(name);
		}

		/// <summary>
		/// 查找指定实体元素继承的父实体元素。
		/// </summary>
		/// <param name="entity">指定的实体元素。</param>
		/// <returns>如果 <paramref name="entity"/> 参数指定的实体元素设置了继承关系，则返回它继承的父实体元素（如果指定父实体元素不存在，则抛出异常）；否则返回空(null)。</returns>
		public static IDataEntity GetBaseEntity(this IDataEntity entity)
		{
			if(entity == null || string.IsNullOrEmpty(entity.BaseName))
				return null;

			return GetEntity(entity, entity.BaseName) ?? throw new DataException($"The '{entity.BaseName}' base of '{entity.Name}' entity does not exist.");
		}

		/// <summary>
		/// 获取指定实体元素的继承链（所有的继承元素），从最顶级的根元素开始一直到当前元素本身。
		/// </summary>
		/// <param name="entity">指定的实体元素。</param>
		/// <returns>返回的继承链（即继承关系的实体元素数组）。</returns>
		public static IEnumerable<IDataEntity> GetInherits(this IDataEntity entity)
		{
			if(entity == null)
				throw new ArgumentNullException(nameof(entity));

			if(string.IsNullOrEmpty(entity.BaseName))
			{
				yield return entity;
				yield break;
			}

			var super = entity;
			var stack = new Stack<IDataEntity>();

			while(super != null)
			{
				stack.Push(super);
				super = GetBaseEntity(super);
			}

			while(stack.Count > 0)
			{
				yield return stack.Pop();
			}
		}

		/// <summary>
		/// 获取指定实体元素的表名。
		/// </summary>
		/// <param name="entity">指定的实体元素。</param>
		/// <returns>如果实体元素未声明表名则返回该实体元素名。</returns>
		public static string GetTableName(this IDataEntity entity)
		{
			if(entity == null)
				throw new ArgumentNullException(nameof(entity));

			return string.IsNullOrEmpty(entity.Alias) ? entity.Name : entity.Alias;
		}

		/// <summary>
		/// 获取指定实体元素对应于指定数据类型的成员标记集。
		/// </summary>
		/// <param name="entity">指定的实体元素。</param>
		/// <param name="type">指定的数据类型，即实体元素对应到的输入或输出的数据类型。</param>
		/// <returns>返回实体元素对应指定数据类型的成员标记集。</returns>
		public static IReadOnlyNamedCollection<DataEntityPropertyToken> GetTokens(this IDataEntity entity, Type type)
		{
			if(entity == null)
				throw new ArgumentNullException(nameof(entity));

			if(type == null)
				throw new ArgumentNullException(nameof(type));

			return _cache.GetOrAdd(entity, e => new EntityTokenCache(e)).GetTokens(type);
		}
		#endregion

		#region 嵌套子类
		private class EntityTokenCache
		{
			#region 成员字段
			private readonly IDataEntity _entity;
			private readonly ConcurrentDictionary<Type, IReadOnlyNamedCollection<DataEntityPropertyToken>> _cache = new ConcurrentDictionary<Type, IReadOnlyNamedCollection<DataEntityPropertyToken>>();
			#endregion

			#region 构造函数
			internal EntityTokenCache(IDataEntity entity)
			{
				_entity = entity;
			}
			#endregion

			#region 公共方法
			public IReadOnlyNamedCollection<DataEntityPropertyToken> GetTokens(Type type)
			{
				if(type == null)
					throw new ArgumentNullException(nameof(type));

				return _cache.GetOrAdd(type, t => CreateTokens(t));
			}
			#endregion

			#region 私有方法
			private IReadOnlyNamedCollection<DataEntityPropertyToken> CreateTokens(Type type)
			{
				var collection = new NamedCollection<DataEntityPropertyToken>(m => m.Property.Name);

				if(type == null || type == typeof(object) || Zongsoft.Common.TypeExtension.IsDictionary(type))
				{
					foreach(var property in _entity.Properties)
					{
						collection.Add(new DataEntityPropertyToken(property, null));
					}
				}
				else
				{
					foreach(var property in _entity.Properties)
					{
						var member = this.FindMember(type, property.Name);

						if(member != null)
							collection.Add(new DataEntityPropertyToken(property, member));
					}
				}

				return collection;
			}

			private MemberInfo FindMember(Type type, string name)
			{
				if(Zongsoft.Common.TypeExtension.IsNullable(type, out var underlyingType))
					type = underlyingType;

				var members = type.GetMember(name, MemberTypes.Field | MemberTypes.Property, BindingFlags.Public | BindingFlags.Instance);

				if(members != null && members.Length > 0)
					return members[0];

				if(type.IsInterface)
				{
					var contracts = type.GetInterfaces();

					foreach(var contract in contracts)
					{
						members = contract.GetMember(name, MemberTypes.Field | MemberTypes.Property, BindingFlags.Public | BindingFlags.Instance);

						if(members != null && members.Length > 0)
							return members[0];
					}
				}

				return null;
			}
			#endregion
		}
		#endregion
	}
}
