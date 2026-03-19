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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections.Generic;

using Zongsoft.Data.Metadata;

namespace Zongsoft.Data;

public static class ModelDescriptorUtility
{
	public static IDataEntity ToEntity(this ModelDescriptor model, string driver = null)
	{
		if(model == null)
			return null;

		var complexes = new List<ModelPropertyDescriptor.ComplexPropertyDescriptor>();
		var entity = new DataEntity(model.Namespace, model.Name, model.Immutable)
		{
			Alias = model.Alias,
			Driver = driver,
		};

		foreach(var descriptor in model.Properties)
		{
			if(descriptor.IsSimplex(out var simplex))
			{
				var property = entity.Properties.Simplex(
					simplex.Name,
					simplex.DataType,
					simplex.Nullable,
					simplex.Immutable);

				property.Hint = simplex.Hint;
				property.Alias = simplex.Alias;
				property.Length = simplex.Length;
				property.Precision = simplex.Precision;
				property.Scale = simplex.Scale;
				property.Sortable = simplex.Sortable;
				property.DefaultValue = simplex.DefaultValue;
				property.IsPrimaryKey = simplex.IsPrimaryKey;

				if(!string.IsNullOrWhiteSpace(simplex.Sequence.Name))
					property.Sequence = DataEntityPropertySequence.Create(property, simplex.Sequence);
			}
			else if(descriptor.IsComplex(out var complex) && complex.Links != null && complex.Links.Length > 0)
			{
				complexes.Add(complex);
			}
		}

		//确保在所有单值属性构建完成之后再添加导航属性
		foreach(var complex in complexes)
		{
			var property = entity.Properties.Complex(
				complex.Name,
				complex.Port,
				complex.Immutable,
				complex.Behaviors,
				complex.Multiplicity);

			property.Hint = complex.Hint;
			property.Links = ToLinks(property, complex.Links);
			property.Constraints = ToConstraints(complex.Constraints);
		}

		return entity;
	}

	static DataAssociationLink[] ToLinks(IDataEntityComplexProperty property, ModelPropertyDescriptor.ComplexPropertyDescriptor.Link[] links)
	{
		if(links == null || links.Length == 0)
			return null;

		var result = new DataAssociationLink[links.Length];
		for(int i = 0; i < links.Length; i++)
			result[i] = new(property, links[i].Port, links[i].Anchor);

		return result;
	}

	static DataAssociationConstraint[] ToConstraints(ModelPropertyDescriptor.ComplexPropertyDescriptor.Constraint[] constraints)
	{
		if(constraints == null || constraints.Length == 0)
			return null;

		var result = new DataAssociationConstraint[constraints.Length];
		for(int i = 0; i < constraints.Length; i++)
		{
			var actor = string.IsNullOrEmpty(constraints[i].Actor) ?
				DataAssociationConstraintActor.Foreign :
				Enum.Parse<DataAssociationConstraintActor>(constraints[i].Actor);

			result[i] = new(constraints[i].Name, actor, constraints[i].Value);
		}

		return result;
	}
}
