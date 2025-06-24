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
using System.Collections.Generic;

namespace Zongsoft.Data.Metadata;

/// <summary>
/// 表示数据命令的元数据类。
/// </summary>
public class DataCommandBase : IDataCommand, IEquatable<IDataCommand>, IEquatable<DataCommandBase>
{
	#region 成员字段
	private string _alias;
	#endregion

	#region 构造函数
	protected DataCommandBase(string @namespace, string name, string alias = null)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		this.Namespace = @namespace;
		this.Name = name;
		this.Alias = alias;
		this.QualifiedName = string.IsNullOrEmpty(@namespace) ? name.ToLowerInvariant() : $"{@namespace.ToLowerInvariant()}.{name.ToLowerInvariant()}";
		this.Parameters = new();
	}
	#endregion

	#region 公共属性
	/// <summary>获取所属命名空间。</summary>
	public string Namespace { get; }

	/// <summary>获取数据命令的名称。</summary>
	public string Name { get; }

	/// <summary>获取数据命令的限定名称。</summary>
	public string QualifiedName { get; }

	/// <summary>获取或设置数据命令的类型。</summary>
	public DataCommandType Type { get; set; }

	/// <summary>获取或设置数据命令的别名（表名、存储过程名）。</summary>
	/// <remarks>注意：如果 <see cref="Namespace"/> 属性不为空(<c>null</c>)或空字符串，则该属性默认为：<c>{Namespace}_{Name}</c>。</remarks>
	public string Alias { get => _alias; set => _alias = value ?? (string.IsNullOrEmpty(this.Namespace) ? value : $"{this.Namespace}_{this.Name}"); }

	/// <summary>获取或设置数据命令支持的驱动。</summary>
	public string Driver { get; set; }

	/// <summary>获取或设置数据命令的变化性。</summary>
	public DataCommandMutability Mutability { get; set; }

	/// <summary>获取数据命令的参数集合。</summary>
	public DataCommandParameterCollection Parameters { get; }

	/// <summary>获取数据命令的脚本对象。</summary>
	public IDataCommandScriptor Scriptor { get; protected set; }
	#endregion

	#region 重写方法
	public bool Equals(IDataCommand other) => other is not null && string.Equals(this.QualifiedName, other.QualifiedName);
	public bool Equals(DataCommandBase other) => other is not null && string.Equals(this.QualifiedName, other.QualifiedName);
	public override bool Equals(object obj) => obj is DataCommandBase other && this.Equals(other);
	public override int GetHashCode() => HashCode.Combine(this.QualifiedName);
	public override string ToString()
	{
		var qualifiedName = $"{this.QualifiedName}({(this.Parameters.Count > 0 ? "..." : null)})";

		if(this.Mutability != DataCommandMutability.None)
			qualifiedName += $"!{this.Mutability}";

		return qualifiedName;
	}
	#endregion
}
