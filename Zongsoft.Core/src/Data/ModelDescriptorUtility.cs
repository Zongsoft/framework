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

using Zongsoft.Data.Metadata;

namespace Zongsoft.Data;

public static class ModelDescriptorUtility
{
	public static IDataEntity ToEntity(this ModelDescriptor model, string driver = null)
	{
		if(model == null)
			return null;

		var entity = new DataEntity(model.Namespace, model.Name, model.Immutable)
		{
			Alias = model.Alias,
			Driver = driver,
		};

		foreach(var descriptor in model.Properties)
		{
			if(descriptor.IsSimplex(out var simplex))
			{
				var property = entity.Properties.Simplex(simplex.Name, simplex.DataType, simplex.Nullable, simplex.Immutable);

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
			else if(descriptor.IsComplex(out var complex))
			{
				if(complex.Links == null || complex.Links.Length == 0)
					throw new InvalidOperationException($"The '{complex}' property of the '{model}' data model cannot be mapped as a navigation property because it lacks the required association(links) definition.");

				var property = entity.Properties.Complex(complex.Name, complex.Port, complex.Immutable, complex.Behaviors, complex.Multiplicity);
				property.Hint = complex.Hint;
			}
		}

		return entity;
	}
}
