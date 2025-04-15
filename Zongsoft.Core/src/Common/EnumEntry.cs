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

namespace Zongsoft.Common;

/// <summary>
/// 表示枚举项的描述。
/// </summary>
public readonly struct EnumEntry : IFormattable, IFormatProvider, IEquatable<EnumEntry>
{
	#region 构造函数
	public EnumEntry(Type type, string name, object value, string alias, string description)
	{
		this.Type = type ?? throw new ArgumentNullException(nameof(type));
		this.Name = name;
		this.Value = value;
		this.Alias = alias;
		this.Description = description;
	}
	#endregion

	#region 公共字段
	/// <summary>获取枚举项的名称。</summary>
	public readonly string Name;

	/// <summary>获取枚举的类型。</summary>
	public readonly Type Type;

	/// <summary>当前描述的枚举项值，该值有可能为枚举项的值也可能是对应的基元类型值。</summary>
	public readonly object Value;

	/// <summary>获取枚举项的别名，如果未定义建议创建者设置为枚举项的名称。</summary>
	/// <remarks>枚举项的别名由<seealso cref="Components.AliasAttribute"/>自定义特性指定。</remarks>
	public readonly string Alias;

	/// <summary>当前描述枚举项的描述文本，如果未定义建议创建者设置为枚举项的名称。</summary>
	/// <remarks>枚举项的描述由<seealso cref="System.ComponentModel.DescriptionAttribute"/>自定义特性指定。</remarks>
	public readonly string Description;
	#endregion

	#region 重写方法
	public bool Equals(EnumEntry entry) => this.Type == entry.Type && object.Equals(this.Value, entry.Value);
	public override bool Equals(object obj) => obj is EnumEntry other && this.Equals(other);
	public override int GetHashCode() => HashCode.Combine(this.Type, this.Value);
	public override string ToString() => this.Type == null || this.Value == null || string.IsNullOrEmpty(this.Name) ? string.Empty : $"{this.Type.Name}.{this.Name}={this.Value}";
	#endregion

	#region 符号重写
	public static bool operator ==(EnumEntry left, EnumEntry right) => left.Equals(right);
	public static bool operator !=(EnumEntry left, EnumEntry right) => !(left == right);
	#endregion

	#region 格式方法
	public string ToString(string format)
	{
		if(string.IsNullOrEmpty(format))
			return this.ToString();

		return format.Trim().ToLowerInvariant() switch
		{
			"n" or "name" => this.Name,
			"a" or "alias" => this.Alias,
			"d" or "description" => this.Description,
			"f" or "full" or "fullname" => this.ToString(),
			_ => this.Value.ToString(),
		};
	}

	string IFormattable.ToString(string format, IFormatProvider formatProvider) => this.ToString(format);
	object IFormatProvider.GetFormat(Type formatType) => formatType == typeof(ICustomFormatter) ? this : null;
	#endregion
}