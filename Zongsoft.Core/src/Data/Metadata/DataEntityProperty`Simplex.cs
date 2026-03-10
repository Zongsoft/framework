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

namespace Zongsoft.Data.Metadata;

public class DataEntitySimplexProperty : DataEntityPropertyBase, IDataEntitySimplexProperty
{
	#region 成员字段
	private object _defaultValue;
	#endregion

	#region 构造函数
	public DataEntitySimplexProperty(string name, DataType type, bool nullable, bool immutable = false) : this(null, name, type, nullable, immutable) { }
	public DataEntitySimplexProperty(DataEntityBase entity, string name, DataType type, bool nullable, bool immutable = false) : base(entity, name, immutable)
	{
		this.Type = type;
		this.Nullable = nullable;
	}

	public DataEntitySimplexProperty(string name, DataType type, int length, bool nullable, bool immutable = false) : this(null, name, type, length, nullable, immutable) { }
	public DataEntitySimplexProperty(DataEntityBase entity, string name, DataType type, int length, bool nullable, bool immutable = false) : base(entity, name, immutable)
	{
		this.Type = type;
		this.Length = length;
		this.Nullable = nullable;
	}

	public DataEntitySimplexProperty(string name, DataType type, byte precision, byte scale, bool nullable, bool immutable = false) : this(null, name, type, precision, scale, nullable, immutable) { }
	public DataEntitySimplexProperty(DataEntityBase entity, string name, DataType type, byte precision, byte scale, bool nullable, bool immutable = false) : base(entity, name, immutable)
	{
		this.Type = type;
		this.Precision = precision;
		this.Scale = scale;
		this.Nullable = nullable;
	}
	#endregion

	#region 公共属性
	/// <summary>获取数据实体属性的字段类型。</summary>
	public DataType Type { get; }

	/// <summary>获取或设置文本或数组属性的最大长度，单位：字节。</summary>
	public int Length
	{
		get
		{
			if(field == 0)
			{
				switch(this.Type.DbType)
				{
					case DbType.Binary:
						return 100;
					case DbType.AnsiString:
					case DbType.AnsiStringFixedLength:
					case DbType.String:
					case DbType.StringFixedLength:
						return 100;
				}
			}

			return field;
		}
		set => field = Math.Max(value, -1);
	}

	/// <summary>获取或设置数值属性的精度。</summary>
	public byte Precision { get; set; }

	/// <summary>获取或设置数值属性的小数点位数。</summary>
	public byte Scale { get; set; }

	/// <summary>获取或设置属性的默认值。</summary>
	public object DefaultValue
	{
		get => _defaultValue is DataEntityPropertyFunction function ? function.Execute(this) : _defaultValue;
		set
		{
			if(value is DataPropertyFunction token)
			{
				_defaultValue = DataEntityPropertyFunction.Get(token);
				return;
			}

			if(value is DataEntityPropertyFunction function1)
			{
				_defaultValue = function1;
				return;
			}

			if(value is string text && DataEntityPropertyFunction.TryGet(text, out var function2))
			{
				_defaultValue = function2;
				return;
			}

			var type = this.Type.DbType.AsType();

			if(type.IsValueType && this.Nullable)
				type = typeof(Nullable<>).MakeGenericType(type);

			_defaultValue = Zongsoft.Common.Convert.ConvertValue(value, type);
		}
	}

	/// <summary>获取或设置属性是否允许为空。</summary>
	public bool Nullable { get; set; }

	/// <summary>获取或设置属性是否可以参与排序。</summary>
	public bool Sortable { get; set; }

	/// <summary>获取或设置序号器元数据。</summary>
	public IDataEntityPropertySequence Sequence { get; set; }

	/// <summary>获取或设置一个值，指示当前属性是否为主键。</summary>
	public bool IsPrimaryKey
	{
		get; set
		{
			if(field == value)
				return;

			field = value;

			//清除当前实体的主键缓存
			this.Entity?.Key = null;
		}
	}
	#endregion

	#region 重写属性
	/// <summary>获取一个值，指示数据实体属性是否为复合类型。该重写方法始终返回假(<c>False</c>)。</summary>
	public override bool IsComplex => false;

	/// <summary>获取一个值，指示数据实体属性是否为单值类型。该重写方法始终返回真(<c>True</c>)。</summary>
	public override bool IsSimplex => true;
	#endregion

	#region 重写方法
	public override string ToString()
	{
		var nullable = this.Nullable ? "NULL" : "NOT NULL";

		return this.Type.DbType switch
		{
			//处理小数类型
			DbType.Currency or
			DbType.Decimal or
			DbType.Double or
			DbType.Single => $"{this.Name} {this.Type}({this.Precision},{this.Scale}) [{nullable}]",

			//处理字符串或数组类型
			DbType.Binary or
			DbType.AnsiString or
			DbType.AnsiStringFixedLength or
			DbType.String or
			DbType.StringFixedLength => $"{this.Name} {this.Type}({this.Length}) [{nullable}]",

			_ => $"{this.Name} {this.Type} [{nullable}]",
		};
	}
	#endregion
}
