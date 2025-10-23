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
using System.Data;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Zongsoft.Data.Metadata;

public class DataEntityPropertyCollection(IDataEntity entity) : KeyedCollection<string, IDataEntityProperty>(StringComparer.OrdinalIgnoreCase)
{
	#region 公共属性
	public IDataEntity Entity { get; } = entity;
	#endregion

	#region 重写方法
	protected override string GetKeyForItem(IDataEntityProperty property) => property.Name;
	#endregion

	#region 公共方法
	public DataEntitySimplexProperty Simplex(string name, DataType type, bool nullable, bool immutable = false)
	{
		var property = new DataEntitySimplexProperty(this.Entity, name, type, nullable, immutable);
		this.Add(property);
		return property;
	}

	public DataEntitySimplexProperty Simplex(string name, DataType type, int length, bool nullable, bool immutable = false)
	{
		var property = new DataEntitySimplexProperty(this.Entity, name, type, length, nullable, immutable);
		this.Add(property);
		return property;
	}

	public DataEntitySimplexProperty Simplex(string name, DataType type, byte precision, byte scale, bool nullable, bool immutable = false)
	{
		var property = new DataEntitySimplexProperty(this.Entity, name, type, precision, scale, nullable, immutable);
		this.Add(property);
		return property;
	}

	public DataEntityComplexProperty Complex(string name, string port, DataEntityComplexPropertyBehaviors behaviors = DataEntityComplexPropertyBehaviors.None) => this.Complex(name, port, true, behaviors);
	public DataEntityComplexProperty Complex(string name, string port, bool immutable, DataEntityComplexPropertyBehaviors behaviors = DataEntityComplexPropertyBehaviors.None)
	{
		var property = new DataEntityComplexProperty(this.Entity, name, port, immutable, behaviors);
		this.Add(property);
		return property;
	}

	public DataEntityComplexProperty Complex(string name, string port, bool immutable, DataAssociationMultiplicity multiplicity, params DataAssociationLink[] links) =>
		this.Complex(name, port, immutable, DataEntityComplexPropertyBehaviors.None, multiplicity, links);
	public DataEntityComplexProperty Complex(string name, string port, DataEntityComplexPropertyBehaviors behaviors, DataAssociationMultiplicity multiplicity, params DataAssociationLink[] links) =>
		this.Complex(name, port, true, behaviors, multiplicity, links);
	public DataEntityComplexProperty Complex(string name, string port, bool immutable, DataEntityComplexPropertyBehaviors behaviors, DataAssociationMultiplicity multiplicity, params DataAssociationLink[] links)
	{
		var property = new DataEntityComplexProperty(this.Entity, name, port, immutable, behaviors, multiplicity, links);
		this.Add(property);
		return property;
	}
	#endregion
}
