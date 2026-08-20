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

		var data = new SchemaData(entity, entityType ?? typeof(object), new HashSet<string>(StringComparer.OrdinalIgnoreCase));
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

			var wildcardContext = new SchemaMemberResolverContext(entity, modelType, token.Parent, null);

			var descriptors = this.GetMembers(wildcardContext);
			if(descriptors != null)
			{
				foreach(var memberDescriptor in descriptors)
				{
					if(memberDescriptor == null || members.ContainsKey(memberDescriptor.Name))
						continue;

					members.Add(memberDescriptor.Name, this.CreateComputed(memberDescriptor, entity, modelType, token.Parent, data));
				}
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

		var explicitContext = new SchemaMemberResolverContext(entity, modelType, token.Parent, token.Name);

		if(this.TryResolve(explicitContext, out var descriptor) && descriptor != null)
			return [this.CreateComputed(descriptor, entity, modelType, token.Parent, data)];

		throw new DataArgumentException("$schema", string.Format(Properties.Resources.Schema_PropertyNotFound_Message, token.Name, entity?.Name ?? modelType.Name));
	}
	#endregion

	#region 虚拟方法
	protected virtual bool TryResolve(SchemaMemberResolverContext context, out SchemaMemberDescriptor descriptor)
	{
		descriptor = null;
		return false;
	}

	protected virtual IEnumerable<SchemaMemberDescriptor> GetMembers(SchemaMemberResolverContext context) => [];
	#endregion

	#region 内部方法
	internal IEnumerable<SchemaMember> Append(Schema schema, string expression)
	{
		var data = new SchemaData(schema.Entity, schema.ModelType ?? typeof(object), new HashSet<string>(StringComparer.OrdinalIgnoreCase));
		return base.Parse(expression, data, schema.Members);
	}
	#endregion

	#region 私有方法
	private SchemaMember CreateComputed(SchemaMemberDescriptor descriptor, IDataEntity entity, Type modelType, SchemaMember parent, SchemaData data)
	{
		if(descriptor == null || string.IsNullOrWhiteSpace(descriptor.Name))
			throw new DataArgumentException("$schema", "The schema parser returned an invalid member descriptor.");

		var dependencies = Array.Empty<SchemaMember>();

		if(descriptor.HasDependencies)
		{
			var scope = $"{entity?.Name ?? modelType.FullName}:{parent?.FullPath}:{descriptor.Name}";

			if(!data.Resolving.Add(scope))
				throw new DataArgumentException("$schema", $"The computed schema member dependency contains a cycle at '{scope}'.");

			try
			{
				var expressions = descriptor.Dependencies.Select(ConvertPath);
				var dependencyData = new SchemaData(entity, modelType, data.Resolving);
				dependencies = base.Parse(string.Join(',', expressions), dependencyData)?.ToArray() ?? [];
			}
			finally
			{
				data.Resolving.Remove(scope);
			}
		}

		return new SchemaMember(descriptor, dependencies);
	}

	private static (IDataEntity Entity, Type ModelType) GetScope(IDataEntity entity, Type modelType, SchemaMember parent)
	{
		if(parent == null)
			return (entity, modelType ?? typeof(object));

		if(parent.Ignored)
		{
			var type = Zongsoft.Common.TypeExtension.GetElementType(parent.Descriptor?.Type) ?? parent.Descriptor?.Type;

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

	private static string ConvertPath(string path)
	{
		if(string.IsNullOrWhiteSpace(path))
			throw new DataArgumentException("$schema", "A computed schema member contains an empty dependency.");

		var count = 0;
		var characters = path.ToCharArray();

		for(int i = 0; i < characters.Length; i++)
		{
			if(characters[i] == '.' || characters[i] == '/')
			{
				characters[i] = '{';
				count++;
			}
		}

		return count == 0 ? path : new string(characters) + new string('}', count);
	}
	#endregion

	#region 嵌套子类
	private sealed class SchemaData(IDataEntity entity, Type modelType, HashSet<string> resolving)
	{
		public IDataEntity Entity { get; } = entity;
		public Type ModelType { get; } = modelType;
		public HashSet<string> Resolving { get; } = resolving;
	}
	#endregion
}
