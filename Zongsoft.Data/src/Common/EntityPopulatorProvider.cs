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
using System.Data;
using System.Reflection;
using System.Collections.Generic;

using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.Common
{
	public partial class EntityPopulatorProvider : IDataPopulatorProvider
	{
		#region 单例模式
		public static readonly EntityPopulatorProvider Instance = new EntityPopulatorProvider();
		#endregion

		#region 构造函数
		private EntityPopulatorProvider() { }
		#endregion

		#region 公共方法
		public bool CanPopulate(Type type)
		{
			return !(type.IsPrimitive || type.IsArray || type.IsEnum ||
			         Zongsoft.Common.TypeExtension.IsScalarType(type) ||
			         Zongsoft.Common.TypeExtension.IsEnumerable(type));
		}

		public IDataPopulator GetPopulator(IDataEntity entity, Type type, IDataRecord reader)
		{
			var members = Zongsoft.Common.TypeExtension.IsNullable(type, out var underlying) ?
				EntityMemberProvider.Instance.GetMembers(underlying) :
				EntityMemberProvider.Instance.GetMembers(type);

			var tokens = new List<EntityPopulator.PopulateToken>(reader.FieldCount);

			for(int ordinal = 0; ordinal < reader.FieldCount; ordinal++)
			{
				//获取当前列对应的属性名（注意：由查询引擎确保返回的列名就是属性名）
				var name = reader.GetName(ordinal);

				//如果属性名的首字符不是字母或下划线则忽略当前列
				if(!IsLetterOrUnderscore(name[0]))
					continue;

				//构建当前属性的层级结构
				FillTokens(entity, members, tokens, name, ordinal);
			}

			return new EntityPopulator(underlying ?? type, tokens);
		}
		#endregion

		#region 私有方法
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static bool IsLetterOrUnderscore(char chr)
		{
			return (chr >= 'A' && chr <= 'Z') ||
			       (chr >= 'a' && chr <= 'z') || chr == '_';
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static void FillTokens(IDataEntity entity, Collections.INamedCollection<EntityMember> members, ICollection<EntityPopulator.PopulateToken> tokens, string name, int ordinal)
		{
			int index, last = 0;
			EntityPopulator.PopulateToken? token = null;

			while((index = name.IndexOf('.', last + 1)) > 0)
			{
				token = FillToken(entity, members, tokens, name.Substring(GetLast(last), index - GetLast(last)));
				last = index;

				if(token == null)
					return;

				entity = token.Value.Entity;
				members = EntityMemberProvider.Instance.GetMembers(token.Value.Member.Type);
				tokens = token.Value.Tokens;
			}

			if(members.TryGet(name.Substring(GetLast(last)), out var member))
			{
				if(token.HasValue && entity.Properties.Get(member.Name).IsPrimaryKey)
				{
					for(int i = 0; i < entity.Key.Length; i++)
					{
						if(string.Equals(entity.Key[i].Name, member.Name))
						{
							token.Value.Keys[i] = ordinal;
							break;
						}
					}
				}

				if(entity.Properties.TryGet(member.Name, out var property) && property is IDataEntitySimplexProperty simplex)
					member.EnsureConvertFrom(simplex.Type);

				tokens.Add(new EntityPopulator.PopulateToken(entity, member, ordinal));
			}
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static EntityPopulator.PopulateToken? FillToken(IDataEntity entity, Collections.INamedCollection<EntityMember> members, ICollection<EntityPopulator.PopulateToken> tokens, string name)
		{
			foreach(var token in tokens)
			{
				if(string.Equals(token.Member.Name, name))
					return token;
			}

			if(members.TryGet(name, out var member))
			{
				if(entity.Properties[name].IsSimplex)
					throw new InvalidOperationException($"The '{name}' property of '{entity}' entity is not a complex(navigation) property.");

				var token = new EntityPopulator.PopulateToken(((IDataEntityComplexProperty)entity.Properties[name]).Foreign, (EntityMember)member);
				tokens.Add(token);
				return token;
			}

			return null;
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static int GetLast(int last) => last > 0 ? last + 1 : last;
		#endregion
	}

	public partial class EntityPopulatorProvider
	{
		#region 公共方法
		public IDataPopulator<T> GetPopulator<T>(IDataEntity entity, IDataRecord reader)
		{
			var populator = new EntityPopulator<T>(entity);

			for(int ordinal = 0; ordinal < reader.FieldCount; ordinal++)
			{
				//获取当前列对应的属性名（注意：由查询引擎确保返回的列名就是属性名）
				var name = reader.GetName(ordinal);

				//如果属性名的首字符不是字母或下划线则忽略当前列
				if(!IsLetterOrUnderscore(name[0]))
					continue;

				//初始化模型组装器
				Initialize(populator, name, ordinal);
			}

			return populator;
		}
		#endregion

		#region 私有方法
		private static void Initialize<T>(EntityPopulator<T> populator, string name, int ordinal)
		{
			var index = name.IndexOf('.');
			var members = EntityMemberProvider.Instance.GetMembers<T>();

			if(index > 0 && index < name.Length - 1)
			{
				if(members.TryGet(name.AsSpan().Slice(0, index).ToString(), out var member))
				{
					var subsidiary = GetAssociativePopulator(populator, member, name.AsSpan().Slice(index + 1), ordinal);

					if(subsidiary != null && !populator.Members.Contains(member.Name))
						populator.Members.Add(new EntityPopulator<T>.PopulateMember(member, subsidiary));
				}
			}
			else
			{
				if(members.TryGet(name, out var member))
				{
					populator.Members.Add(new EntityPopulator<T>.PopulateMember(member, ordinal));
				}
			}
		}

		private static IDataPopulator GetAssociativePopulator<T>(EntityPopulator<T> populator, EntityMember<T> member, ReadOnlySpan<char> children, int ordinal)
		{
			if(!populator.Entity.Properties.TryGet(member.Name, out var property))
				throw new DataException($"The property named '{member.Name}' is undefined in the '{populator.Entity}' data entity mapping.");

			if(property.IsSimplex)
				throw new DataException($"The '{member.Name}' property of '{populator.Entity}' entity is not a complex(navigation) property.");

			var result = populator.Members.TryGetValue(member.Name, out var m) ?
				m.Populator :
				Activator.CreateInstance(typeof(EntityPopulator<>).MakeGenericType(member.Type), new object[] { ((IDataEntityComplexProperty)property).Foreign });

			if(!children.IsEmpty)
				DoInitialize(result, children.ToString(), ordinal);

			return (IDataPopulator)result;
		}

		private static void DoInitialize(object populator, string name, int ordinal)
		{
			var type = populator.GetType().GetGenericArguments()[0];
			var method = typeof(EntityPopulatorProvider).GetMethod(nameof(Initialize), BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(type);
			method.Invoke(null, new object[] { populator, name, ordinal });
		}
		#endregion
	}
}
