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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Data.Metadata;

public static class DataEntityExtension
{
	public static DataEntity Add(this DataEntityCollection entities, string qualifiedName, bool immutable = false)
	{
		ArgumentNullException.ThrowIfNull(entities);
		(var name, var @namespace) = DataUtility.ParseQualifiedName(qualifiedName);
		var entity = new DataEntity(@namespace, name, immutable);
		entities.Add(entity);
		return entity;
	}

	public static IDataEntity Key(this IDataEntity entity, params string[] keys)
	{
		ArgumentNullException.ThrowIfNull(entity);

		if(entity is DataEntityBase dataEntity)
			dataEntity.SetKey(keys);
		else
			throw new InvalidOperationException();

		return entity;
	}

	public static IDataEntity Property(this IDataEntity entity, string name, DataType type, bool nullable = true, bool immutable = false) => Property(entity, name, type, 0, nullable, immutable);
	public static IDataEntity Property(this IDataEntity entity, string name, DataType type, int length, bool nullable = true, bool immutable = false)
	{
		ArgumentNullException.ThrowIfNull(entity);
		entity.Properties.Simplex(name, type, length, nullable, immutable);
		return entity;
	}
	public static IDataEntity Property(this IDataEntity entity, string name, DataType type, byte precision, byte scale, bool nullable, bool immutable = false)
	{
		ArgumentNullException.ThrowIfNull(entity);
		entity.Properties.Simplex(name, type, precision, scale, nullable, immutable);
		return entity;
	}

	public static IDataEntity Property(this IDataEntity entity, string name, string port, DataAssociationMultiplicity multiplicity, params DataAssociationLink[] links) => Property(entity, name, port, false, DataEntityComplexPropertyBehaviors.None, multiplicity, links);
	public static IDataEntity Property(this IDataEntity entity, string name, string port, bool immutable, DataAssociationMultiplicity multiplicity, params DataAssociationLink[] links) => Property(entity, name, port, immutable, DataEntityComplexPropertyBehaviors.None, multiplicity, links);
	public static IDataEntity Property(this IDataEntity entity, string name, string port, DataEntityComplexPropertyBehaviors behaviors, DataAssociationMultiplicity multiplicity, params DataAssociationLink[] links) => Property(entity, name, port, false, behaviors, multiplicity, links);
	public static IDataEntity Property(this IDataEntity entity, string name, string port, bool immutable, DataEntityComplexPropertyBehaviors behaviors, DataAssociationMultiplicity multiplicity, params DataAssociationLink[] links)
	{
		ArgumentNullException.ThrowIfNull(entity);
		entity.Properties.Complex(name, port, immutable, behaviors, multiplicity, links);
		return entity;
	}
}
