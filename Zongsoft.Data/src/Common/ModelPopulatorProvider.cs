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
using System.Data;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Concurrent;

using Zongsoft.Common;
using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.Common
{
	public partial class ModelPopulatorProvider : IDataPopulatorProvider
	{
		#region 单例模式
		public static readonly ModelPopulatorProvider Instance = new ModelPopulatorProvider();
		#endregion

		#region 构造函数
		private ModelPopulatorProvider() { }
		#endregion

		#region 公共方法
		public bool CanPopulate(Type type)
		{
			return !(type.IsPrimitive || type.IsArray || type.IsEnum ||
			         Zongsoft.Common.TypeExtension.IsScalarType(type) ||
			         Zongsoft.Common.TypeExtension.IsEnumerable(type));
		}

		[Obsolete]
		public IDataPopulator GetPopulatorObsolete(Type type, IDataRecord reader, IDataEntity entity = null)
		{
			var members = Zongsoft.Common.TypeExtension.IsNullable(type, out var underlying) ?
				ModelMemberTokenManager.GetMembers(underlying) :
				ModelMemberTokenManager.GetMembers(type);

			var tokens = new List<ModelPopulator.MemberMapping>(reader.FieldCount);

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

			return new ModelPopulator(underlying ?? type, tokens);
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
		private static void FillTokens(IDataEntity entity, Collections.INamedCollection<ModelMemberToken> members, ICollection<ModelPopulator.MemberMapping> tokens, string name, int ordinal)
		{
			int index, last = 0;
			ModelPopulator.MemberMapping? token = null;

			while((index = name.IndexOf('.', last + 1)) > 0)
			{
				token = FillToken(entity, members, tokens, name.Substring(GetLast(last), index - GetLast(last)));
				last = index;

				if(token == null)
					return;

				entity = token.Value.Entity;
				members = ModelMemberTokenManager.GetMembers(token.Value.Member.Type);
				tokens = token.Value.Children;
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

				tokens.Add(new ModelPopulator.MemberMapping(entity, member, ordinal));
			}
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static ModelPopulator.MemberMapping? FillToken(IDataEntity entity, Collections.INamedCollection<ModelMemberToken> members, ICollection<ModelPopulator.MemberMapping> tokens, string name)
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

				var token = new ModelPopulator.MemberMapping(((IDataEntityComplexProperty)entity.Properties[name]).Foreign, (ModelMemberToken)member);
				tokens.Add(token);
				return token;
			}

			return null;
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static int GetLast(int last) => last > 0 ? last + 1 : last;
		#endregion
	}

	public partial class ModelPopulatorProvider
	{
		#region 静态变量
		private static readonly MethodInfo PopulatorTemplate = typeof(ModelPopulatorProvider).GetMethod(nameof(ModelPopulatorProvider.GetPopulator), 1, BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(IDataRecord), typeof(IDataEntity) }, null);
		private static readonly ConcurrentDictionary<PopulatorKey, IDataPopulator> _populators = new ();
		#endregion

		#region 公共方法
		public IDataPopulator GetPopulator(Type type, IDataRecord record, IDataEntity entity = null)
		{
			var key = new PopulatorKey(type, record, entity);

			return _populators.GetOrAdd(key, (key, record) =>
			{
				var method = PopulatorTemplate.MakeGenericMethod(key.ModelType);
				var invoker = method.CreateDelegate(typeof(Func<,,>).MakeGenericType(typeof(IDataRecord), typeof(IDataEntity), typeof(IDataPopulator<>).MakeGenericType(key.ModelType)), this);
				return (IDataPopulator)invoker.DynamicInvoke(record, key.Entity);
			}, record);
		}

		public IDataPopulator<T> GetPopulator<T>(IDataRecord record, IDataEntity entity = null)
		{
			var populator = new ModelPopulator<T>(entity);

			for(int ordinal = 0; ordinal < record.FieldCount; ordinal++)
			{
				//获取当前列对应的属性名（注意：由查询引擎确保返回的列名就是属性名）
				var name = record.GetName(ordinal);

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
		private static void Initialize<T>(ModelPopulator<T> populator, string name, int ordinal)
		{
			var index = name.IndexOf('.');
			var members = ModelMemberTokenManager.GetMembers<T>();

			if(index > 0 && index < name.Length - 1)
			{
				if(members.TryGetValue(name.AsSpan().Slice(0, index).ToString(), out var member))
				{
					var subsidiary = GetAssociativePopulator(populator, member, name.AsSpan().Slice(index + 1), ordinal);

					if(subsidiary != null && !populator.Members.Contains(member.Name))
						populator.Members.Add(new ModelPopulator<T>.MemberMapping(member, subsidiary));
				}
			}
			else
			{
				if(members.TryGetValue(name, out var member))
				{
					populator.Members.Add(new ModelPopulator<T>.MemberMapping(member, ordinal));
				}
			}
		}

		private static IDataPopulator GetAssociativePopulator<T>(ModelPopulator<T> populator, ModelMemberToken<T> member, ReadOnlySpan<char> children, int ordinal)
		{
			IDataPopulator result;

			if(populator.Members.TryGetValue(member.Name, out var m))
				result = m.Populator;
			else if(populator.Entity is null)
				result = (IDataPopulator)Activator.CreateInstance(typeof(ModelPopulator<>).MakeGenericType(member.Type), new object[] { null });
			else
			{
				if(!populator.Entity.Properties.TryGet(member.Name, out var property))
					throw new DataException($"The property named '{member.Name}' is undefined in the '{populator.Entity}' data entity mapping.");

				if(property.IsSimplex)
					throw new DataException($"The '{member.Name}' property of '{populator.Entity}' entity is not a complex(navigation) property.");

				result = (IDataPopulator)Activator.CreateInstance(typeof(ModelPopulator<>).MakeGenericType(member.Type), new object[] { ((IDataEntityComplexProperty)property).Foreign });
			}

			if(!children.IsEmpty)
				DoInitialize(result, children.ToString(), ordinal);

			return result;
		}

		private static void DoInitialize(object populator, string name, int ordinal)
		{
			var type = populator.GetType().GetGenericArguments()[0];
			var method = typeof(ModelPopulatorProvider).GetMethod(nameof(Initialize), BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(type);
			method.Invoke(null, new object[] { populator, name, ordinal });
		}
		#endregion

		#region 嵌套结构
		private readonly struct PopulatorKey : IEquatable<PopulatorKey>
		{
			public readonly Type ModelType;
			public readonly IDataEntity Entity;
			public readonly int Record;
			public readonly string[] Fields;

			public PopulatorKey(Type modelType, IDataRecord record, IDataEntity entity)
			{
				this.ModelType = modelType;
				this.Entity = entity;
				this.Fields = new string[record.FieldCount];

				var code = new HashCode();
				for(int i = 0; i < record.FieldCount; i++)
				{
					this.Fields[i] = record.GetName(i);
					code.Add(this.Fields[i]);
				}

				this.Record = code.ToHashCode();
			}

			public bool Equals(PopulatorKey other) =>
				this.ModelType == other.ModelType &&
				object.Equals(this.Entity, other.Entity) &&
				this.Record == other.Record &&
				System.Linq.Enumerable.SequenceEqual(this.Fields, other.Fields);

			public override bool Equals(object obj) => obj is PopulatorKey other && this.Equals(other);
			public override int GetHashCode() => HashCode.Combine(this.ModelType, this.Entity, this.Record);
		}
		#endregion
	}
}
