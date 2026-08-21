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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using Zongsoft.Data.Metadata;

namespace Zongsoft.Data;

public class SchemaParser : SchemaParserBase<SchemaMember>
{
	#region 单例字段
	public static readonly SchemaParser Instance = new();
	#endregion

	#region 解析方法
	public override ISchema<SchemaMember> Parse(string name, string expression, Type entityType)
	{
		var entity = Mapping.Entities[name];

		if(string.IsNullOrWhiteSpace(expression))
			expression = "*";

		var data = new SchemaData(entity, entityType ?? typeof(object));
		return new Schema(this, expression, entity, entityType, base.Parse(expression, data));
	}

	protected override IEnumerable<SchemaMember> Resolve(SchemaEntryToken token)
	{
		var data = (SchemaData)token.Data;
		var (entity, modelType) = GetScope(data.Entity, data.ModelType, token.Parent);

		if(token.Name == "*")
		{
			var members = new Dictionary<string, SchemaMember>(StringComparer.OrdinalIgnoreCase);
			var current = entity;

			while(current != null)
			{
				foreach(var mapped in current.GetTokens(modelType).Where(token => token.Property.IsSimplex))
					members.TryAdd(mapped.Property.Name, new SchemaMember(mapped));

				current = current.GetBaseEntity();
			}

			return members.Values;
		}

		var currentEntity = entity;
		List<IDataEntity> ancestors = null;

		while(currentEntity != null)
		{
			if(Zongsoft.Common.TypeExtension.IsScalarType(modelType) && currentEntity.Properties.TryGetValue(token.Name, out var property))
				return [new SchemaMember(property, ancestors)];

			if(currentEntity.GetTokens(modelType).TryGetValue(token.Name, out var mapped))
				return [new SchemaMember(mapped, ancestors)];

			ancestors ??= [];
			currentEntity = currentEntity.GetBaseEntity();

			if(currentEntity != null)
				ancestors.Add(currentEntity);
		}

		return null;
	}
	#endregion

	#region 虚拟方法
	protected override bool ShouldExpand(SchemaMember member) => member?.Property?.IsComplex == true;
	protected sealed override IEnumerable<SchemaMember> OnUnrecognized(SchemaEntryToken token)
	{
		var data = (SchemaData)token.Data;
		var (entity, modelType) = GetScope(data.Entity, data.ModelType, token.Parent);
		var member = this.OnUnrecognized(entity, modelType, token.Parent, token.Name) ?? FindMember(modelType, token.Name);

		if(member == null)
			throw new DataArgumentException("$schema", string.Format(Properties.Resources.Schema_PropertyNotFound_Message, token.Name, entity?.Name ?? modelType.Name));

		return [new SchemaMember(token.Name, member)];
	}

	/// <summary>处理未映射的显式模式成员。</summary>
	/// <param name="entity">当前数据实体。</param>
	/// <param name="modelType">当前模型类型。</param>
	/// <param name="parent">当前父成员，根成员则为空。</param>
	/// <param name="name">未识别的成员名称。</param>
	/// <returns>返回对应的公共实例字段或属性；不处理则返回空，由基类按名称从模型类型中查找。</returns>
	protected virtual MemberInfo OnUnrecognized(IDataEntity entity, Type modelType, ISchemaMember parent, string name) => null;
	#endregion

	#region 内部方法
	internal IEnumerable<SchemaMember> Append(Schema schema, string expression)
	{
		var data = new SchemaData(schema.Entity, schema.ModelType ?? typeof(object));
		return base.Parse(expression, data, schema.Members);
	}
	#endregion

	#region 私有方法
	private static (IDataEntity Entity, Type ModelType) GetScope(IDataEntity entity, Type modelType, SchemaMember parent)
	{
		if(parent == null)
			return (entity, modelType ?? typeof(object));

		if(parent.Ignored)
		{
			var type = GetMemberType(parent.Member);

			if(type == null || Zongsoft.Common.TypeExtension.IsScalarType(type))
				throw new DataArgumentException("$schema", string.Format(Properties.Resources.Schema_ComplexPropertyRequired_Message, parent));

			return (null, type);
		}

		if(parent.Token.Property.IsSimplex)
			throw new DataArgumentException("$schema", string.Format(Properties.Resources.Schema_ComplexPropertyRequired_Message, parent));

		var complex = (IDataEntityComplexProperty)parent.Token.Property;
		entity = complex.Foreign;

		while(complex.ForeignProperty != null && complex.ForeignProperty.IsComplex)
		{
			complex = (IDataEntityComplexProperty)complex.ForeignProperty;
			entity = complex.Foreign;
		}

		return (entity, GetMemberType(parent.Token.Member) ?? typeof(object));
	}

	private static MemberInfo FindMember(Type type, string name)
	{
		if(type == null || string.IsNullOrEmpty(name))
			return null;

		if(Zongsoft.Common.TypeExtension.IsNullable(type, out var underlyingType))
			type = underlyingType;

		var members = type.GetMember(name, MemberTypes.Field | MemberTypes.Property, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
		var member = SelectMember(members, name);

		if(member != null || !type.IsInterface)
			return member;

		foreach(var contract in type.GetInterfaces())
		{
			members = contract.GetMember(name, MemberTypes.Field | MemberTypes.Property, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
			member = SelectMember(members, name);

			if(member != null)
				return member;
		}

		return null;
	}

	private static MemberInfo SelectMember(MemberInfo[] members, string name)
	{
		if(members == null || members.Length == 0)
			return null;

		foreach(var member in members)
		{
			if(string.Equals(member.Name, name, StringComparison.Ordinal) && IsValid(member))
				return member;
		}

		foreach(var member in members)
		{
			if(IsValid(member))
				return member;
		}

		return null;
	}

	private static bool IsValid(MemberInfo member) => member switch
	{
		FieldInfo field => !field.IsStatic,
		PropertyInfo property => property.GetIndexParameters().Length == 0 &&
			(property.GetMethod == null || !property.GetMethod.IsStatic) &&
			(property.SetMethod == null || !property.SetMethod.IsStatic),
		_ => false,
	};

	private static Type GetMemberType(MemberInfo member)
	{
		var type = member switch
		{
			FieldInfo field => field.FieldType,
			PropertyInfo property => property.PropertyType,
			MethodInfo method => method.ReturnType,
			null => null,
			_ => throw new DataArgumentException("$schema", string.Format(Properties.Resources.Schema_InvalidMemberKind_Message, member)),
		};

		return type == null ? null : Zongsoft.Common.TypeExtension.GetElementType(type) ?? type;
	}

	#endregion

	#region 嵌套子类
	private sealed class SchemaData(IDataEntity entity, Type modelType)
	{
		public IDataEntity Entity { get; } = entity;
		public Type ModelType { get; } = modelType;
	}
	#endregion
}
