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

using Zongsoft.Data.Metadata;

namespace Zongsoft.Data.Common.Expressions;

/// <summary>
/// 表示表标识的表达式。
/// </summary>
public class TableIdentifier : Expression, IIdentifier, ISource, IEquatable<TableIdentifier>
{
	#region 构造函数
	public TableIdentifier(IDataEntity entity, string alias = null)
	{
		this.Entity = entity ?? throw new ArgumentNullException(nameof(entity));
		this.Alias = alias;
		this.Name = entity.GetTableName();
	}

	public TableIdentifier(TableDefinition table, string alias = null)
	{
		this.Table = table ?? throw new ArgumentNullException(nameof(table));
		this.Alias = alias;
		this.Name = table.Name;
	}
	#endregion

	#region 公共属性
	/// <summary>获取对应的表元数据元素，与<see cref="Table"/>属性互斥。</summary>
	public IDataEntity Entity { get; }

	/// <summary>获取对应的表定义，与<see cref="Entity"/>属性互斥。</summary>
	public TableDefinition Table { get; }

	/// <summary>获取表的物理名称（即数据库中表的名称）。</summary>
	public string Name { get; }

	/// <summary>获取或设置表标识的别名。</summary>
	public string Alias { get; set; }

	/// <summary>获取一个值，指示当前表标识是否指向一个临时表。</summary>
	public bool IsTemporary => this.Table != null && this.Table.IsTemporary;
	#endregion

	#region 公共方法
	/// <summary>创建一个关联当前表的字段标识。</summary>
	/// <param name="name">指定的字段名称。</param>
	/// <param name="alias">指定的字段别名。</param>
	/// <returns>返回创建成功的字段标识。</returns>
	public FieldIdentifier CreateField(string name, string alias = null)
	{
		return new FieldIdentifier(this, name, alias);
	}

	public FieldIdentifier CreateField(IDataEntityProperty property)
	{
		return new FieldIdentifier(this, property.GetFieldName(out var alias), alias)
		{
			Token = new DataEntityPropertyToken(property)
		};
	}

	public FieldIdentifier CreateField(IDataEntitySimplexProperty property, string alias = null)
	{
		if(string.IsNullOrEmpty(property.Alias))
			return new FieldIdentifier(this, property.Name, alias)
			{
				Token = new DataEntityPropertyToken(property)
			};
		else
			return new FieldIdentifier(this, property.Alias, alias)
			{
				Token = new DataEntityPropertyToken(property)
			};
	}

	public FieldIdentifier CreateField(DataEntityPropertyToken token, string alias = null)
	{
		var simplex = token.Property.IsSimplex ?
			(IDataEntitySimplexProperty)token.Property :
			throw new ArgumentException($"The specified '{token.Property.Name}' property is not a simplex property, so you cannot create a field identifier for it.");

		if(string.IsNullOrEmpty(simplex.Alias))
			return new FieldIdentifier(this, simplex.Name, alias) { Token = token };
		else
			return new FieldIdentifier(this, simplex.Alias, alias) { Token = token };
	}
	#endregion

	#region 重写方法
	public override bool Equals(object obj) => this.Equals(obj as TableIdentifier);
	public bool Equals(TableIdentifier other) => other is not null && this.IsTemporary == other.IsTemporary &&
		string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase) &&
		string.Equals(this.Alias, other.Alias, StringComparison.OrdinalIgnoreCase);

	public override int GetHashCode()
	{
		var alias = this.Alias;

		if(string.IsNullOrEmpty(alias))
			return this.Name.ToUpperInvariant().GetHashCode();
		else
			return this.Name.ToUpperInvariant().GetHashCode() ^ alias.ToUpperInvariant().GetHashCode();
	}

	public override string ToString()
	{
		if(string.IsNullOrEmpty(this.Alias))
			return (this.IsTemporary ? "#" : string.Empty) + this.Name;
		else
			return (this.IsTemporary ? "#" : string.Empty) + this.Name + " AS " + this.Alias;
	}
	#endregion
}
