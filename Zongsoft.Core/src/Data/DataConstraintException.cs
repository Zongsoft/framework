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

namespace Zongsoft.Data;

/// <summary>
/// 表示数据约束失败的异常类。
/// </summary>
public class DataConstraintException : DataAccessException
{
	#region 构造函数
	public DataConstraintException(string driverName, int code, DataConstraintKind kind, string name, string value, Actor principal, Exception innerException = null) : this(driverName, code, kind, name, value, principal, null, innerException) { }
	public DataConstraintException(string driverName, int code, DataConstraintKind kind, string name, string value, Actor principal, Actor foreigner, Exception innerException = null) : base(driverName, code, Properties.Resources.DataConstraintException_Message, innerException)
	{
		this.Kind = kind;
		this.Name = name;
		this.Value = value;
		this.Principal = principal;
		this.Foreigner = foreigner;
	}
	#endregion

	#region 公共属性
	/// <summary>获取数据约束的名称。</summary>
	public string Name { get; }
	/// <summary>获取数据约束冲突的值。</summary>
	public string Value { get; }
	/// <summary>获取数据约束的类型。</summary>
	public DataConstraintKind Kind { get; }
	/// <summary>获取数据约束的主表。</summary>
	public Actor Principal { get; }
	/// <summary>获取数据约束的外表。</summary>
	public Actor Foreigner { get; }
	#endregion

	public sealed class Actor
	{
		#region 构造函数
		public Actor(string name, params Field[] fields) : this(name, null, fields) { }
		public Actor(string name, string description, params Field[] fields)
		{
			if(string.IsNullOrEmpty(name))
				throw new ArgumentNullException(nameof(name));

			this.Name = name;
			this.Description = description;
			this.Fields = fields ?? [];
		}
		#endregion

		#region 公共属性
		/// <summary>获取数据约束的表名。</summary>
		public string Name { get; }
		/// <summary>获取或设置数据约束的描述。</summary>
		public string Description { get; set; }
		/// <summary>获取或设置数据约束的字段集。</summary>
		public Field[] Fields { get; set; }
		#endregion

		#region 重写方法
		public bool Equals(Actor other) => other is not null && string.Equals(this.Name, other.Name);
		public override bool Equals(object obj) => this.Equals(obj as Actor);
		public override int GetHashCode() => this.Name.GetHashCode();
		public override string ToString() => this.Name;
		#endregion

		public sealed class Field
		{
			#region 构造函数
			public Field(string name, Type type, string label = null, string description = null) : this(name, type, 0, false, label, description) { }
			public Field(string name, Type type, int length, string label = null, string description = null) : this(name, type, length, false, label, description) { }
			public Field(string name, Type type, bool nullable = false, string label = null, string description = null) : this(name, type, 0, nullable, label, description) { }
			public Field(string name, Type type, int length, bool nullable = false, string label = null, string description = null)
			{
				if(string.IsNullOrEmpty(name))
					throw new ArgumentNullException(nameof(name));

				this.Name = name;
				this.Type = type;
				this.Length = length;
				this.Nullable = nullable;
				this.Label = label;
				this.Description = description;
			}
			#endregion

			#region 公共属性
			/// <summary>获取字段名称。</summary>
			public string Name { get; }
			/// <summary>获取字段类型。</summary>
			public Type Type { get; }
			/// <summary>获取字段类型。</summary>
			public int Length { get; }
			/// <summary>获取字段是否可空。</summary>
			public bool Nullable { get; }
			/// <summary>获取或设置字段标题。</summary>
			public string Label { get; set; }
			/// <summary>获取或设置字段描述。</summary>
			public string Description { get; set; }
			#endregion

			#region 重写方法
			public bool Equals(Field other) => other is not null && string.Equals(this.Name, other.Name);
			public override bool Equals(object obj) => this.Equals(obj as Field);
			public override int GetHashCode() => this.Name.GetHashCode();
			public override string ToString() => this.Length > 0 ?
				$"{this.Name}@{Common.TypeAlias.GetAlias(this.Type)}({this.Length})" :
				$"{this.Name}@{Common.TypeAlias.GetAlias(this.Type)}";
			#endregion
		}
	}
}
