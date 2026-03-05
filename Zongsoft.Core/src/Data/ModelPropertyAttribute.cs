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

namespace Zongsoft.Data;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class ModelPropertyAttribute : Attribute
{
	#region 构造函数
	public ModelPropertyAttribute(DbType type, bool nullable = false, object defaultValue = null) : this(null, type, nullable, defaultValue) { }
	public ModelPropertyAttribute(DbType type, int length, bool nullable = false, object defaultValue = null) : this(null, type, length, nullable, defaultValue) { }
	public ModelPropertyAttribute(DbType type, byte precision, byte scale, bool nullable = false, object defaultValue = null) : this(null, type, precision, scale, nullable, defaultValue) { }

	public ModelPropertyAttribute(string alias, DbType type = DbType.Object, bool nullable = false, object defaultValue = null) : this(alias, type, 0, nullable, defaultValue) { }
	public ModelPropertyAttribute(string alias, DbType type, int length, bool nullable = false, object defaultValue = null)
	{
		this.Alias = alias;
		this.Type = type;
		this.Length = length;
		this.Nullable = nullable;
		this.DefaultValue = defaultValue;
	}
	public ModelPropertyAttribute(string alias, DbType type, byte precision, byte scale, bool nullable = false, object defaultValue = null)
	{
		this.Alias = alias;
		this.Type = type;
		this.Precision = precision;
		this.Scale = scale;
		this.Nullable = nullable;
		this.DefaultValue = defaultValue;
	}

	public ModelPropertyAttribute(string port, Metadata.DataAssociationMultiplicity multiplicity, Metadata.DataEntityComplexPropertyBehaviors behaviors = Metadata.DataEntityComplexPropertyBehaviors.None)
	{
		this.Port = port;
		this.Type = DataType.Object;
		this.Multiplicity = multiplicity;
		this.Behaviors = behaviors;
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置属性的别名。</summary>
	public string Alias { get; set; }

	/// <summary>获取或设置属性的数据类型。</summary>
	public DataType Type { get; set; }

	/// <summary>获取或设置一个值，指示当前属性是否为主键。</summary>
	public bool IsPrimaryKey { get; set; }

	/// <summary>获取或设置文本或数组属性的最大长度。</summary>
	public int Length { get; set; }

	/// <summary>获取或设置数值属性的精度。</summary>
	public byte Precision { get; set; }

	/// <summary>获取或设置数值属性的小数点位数。</summary>
	public byte Scale { get; set; }

	/// <summary>获取或设置属性是否允许为空。</summary>
	public bool Nullable { get; set; }

	/// <summary>获取或设置属性是否可以参与排序。</summary>
	public bool Sortable { get; set; }

	/// <summary>获取或设置数据序号器元数据。</summary>
	public string Sequence { get; set; }

	/// <summary>获取或设置默认值。</summary>
	public object DefaultValue { get; set; }

	/// <summary>获取或设置属性的语义角色。</summary>
	public string Role { get; set; }

	/// <summary>获取数据实体属性的提示。</summary>
	public string Hint { get; set; }

	/// <summary>获取一个值，指示数据实体属性是否为不可变属性，默认为假(<c>False</c>)。</summary>
	/// <remarks>
	/// 	<para>对于不可变简单属性：不能被修改(Update, Upsert)，但是新增(Insert)时可以设置其内容。</para>
	/// 	<para>对于不可变复合属性：不支持任何写操作(Delete, Insert, Update, Upsert)。</para>
	/// </remarks>
	public bool Immutable { get; }

	/// <summary>获取或设置一个值，指示是否忽略该属性。</summary>
	public bool Ignored { get; set; }

	/// <summary>获取或设置导航属性的关联目标，通常它是目标实体名，也支持跳跃关联(即关联到一个复合属性)。</summary>
	/// <remarks>跳跃关联是指关联目标为实体的导航属性，实体与导航属性之间以冒号(<c>:</c>)区隔。</remarks>
	public string Port { get; set; }

	/// <summary>获取或设置属性的特性。</summary>
	public Metadata.DataEntityComplexPropertyBehaviors Behaviors { get; set; }

	/// <summary>获取或设置一个值，指示导航属性的重复性关系。</summary>
	public Metadata.DataAssociationMultiplicity Multiplicity { get; set; }
	#endregion
}