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
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using Zongsoft.Data.Common;
using Zongsoft.Data.Metadata;

namespace Zongsoft.Data
{
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

			return new Schema(this, expression, entity, entityType, base.Parse(expression, token => this.Resolve(token), new SchemaData(entity, entityType)));
		}

		private IEnumerable<SchemaMember> Resolve(SchemaEntryToken token)
		{
			var data = (SchemaData)token.Data;
			var current = data.Entity;

			if(token.Parent != null)
			{
				var parent = token.Parent;

				if(parent.Token.Property.IsSimplex)
					throw new DataArgumentException("schema", $"The specified {parent} schema does not correspond to a complex property, so its child elements cannot be defined.");

				var complex = (IDataEntityComplexProperty)parent.Token.Property;
				data.Entity = complex.Foreign;

				while(complex.ForeignProperty != null && complex.ForeignProperty.IsComplex)
				{
					complex = (IDataEntityComplexProperty)complex.ForeignProperty;
					data.Entity = complex.Foreign;
				}

				if(parent.Token.Member != null)
				{
					switch(parent.Token.Member.MemberType)
					{
						case MemberTypes.Field:
							data.EntityType = Zongsoft.Common.TypeExtension.GetElementType(((FieldInfo)parent.Token.Member).FieldType) ??
							                  ((FieldInfo)parent.Token.Member).FieldType;
							break;
						case MemberTypes.Property:
							data.EntityType = Zongsoft.Common.TypeExtension.GetElementType(((PropertyInfo)parent.Token.Member).PropertyType) ??
							                  ((PropertyInfo)parent.Token.Member).PropertyType;
							break;
						case MemberTypes.Method:
							data.EntityType = Zongsoft.Common.TypeExtension.GetElementType(((MethodInfo)parent.Token.Member).ReturnType) ??
							                  ((MethodInfo)parent.Token.Member).ReturnType;
							break;
						default:
							throw new DataArgumentException("schema", $"Invalid kind of '{parent.Token.Member}' member.");
					}
				}
			}

			if(token.Name == "*")
			{
				//return data.Entity.GetTokens(data.EntityType)
				//				  .Where(p => p.Property.IsSimplex)
				//				  .Select(p => new SchemaMember(p));

				current = data.Entity;
				var members = new List<SchemaMember>();

				while(current != null)
				{
					members.AddRange(
						current.GetTokens(data.EntityType)
						       .Where(p => p.Property.IsSimplex)
						       .Select(p => new SchemaMember(p)));

					current = current.GetBaseEntity();
				}

				return members;
			}

			current = data.Entity;
			ICollection<IDataEntity> ancestors = null;

			while(current != null)
			{
				if(Zongsoft.Common.TypeExtension.IsScalarType(data.EntityType) && current.Properties.TryGetValue(token.Name, out var property))
					return new[] { new SchemaMember(property, ancestors) };

				if(current.GetTokens(data.EntityType).TryGetValue(token.Name, out var stub))
					return new []{ new SchemaMember(stub, ancestors) };

				if(ancestors == null)
					ancestors = new List<IDataEntity>();

				current = current.GetBaseEntity();

				if(current != null)
					ancestors.Add(current);
			}

			throw new DataArgumentException("schema", $"The specified '{token.Name}' property does not exist in the '{data.Entity.Name}' entity.");
		}
		#endregion

		#region 内部方法
		internal void Append(Schema schema, string expression)
		{
			var entries = base.Parse(expression, token => this.Resolve(token), new SchemaData(schema.Entity, schema.ModelType), schema.Members);
		}
		#endregion

		#region 嵌套结构
		private struct SchemaData
		{
			public IDataEntity Entity;
			public Type EntityType;

			public SchemaData(IDataEntity entity, Type entityType)
			{
				this.Entity = entity;
				this.EntityType = entityType ?? typeof(object);
			}
		}
		#endregion
	}
}
