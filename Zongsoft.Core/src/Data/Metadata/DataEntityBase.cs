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
using System.Collections.Generic;

namespace Zongsoft.Data.Metadata;

/// <summary>
/// 表示数据实体的元数据类。
/// </summary>
public class DataEntityBase : IDataEntity, IEquatable<IDataEntity>, IEquatable<DataEntityBase>
{
	#region 成员字段
	private string _alias;
	#endregion

	#region 构造函数
	protected DataEntityBase(string @namespace, string name, string baseName, bool immutable = false)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		this.Namespace = @namespace;
		this.Name = name;
		this.Alias = null;
		this.QualifiedName = DataUtility.Qualify(this.Name, this.Namespace);
		this.BaseName = baseName;
		this.Immutable = immutable;
		this.Properties = new(this);
	}
	#endregion

	#region 公共属性
	/// <summary>获取所属命名空间。</summary>
	public string Namespace { get; }

	/// <summary>获取数据实体的名称。</summary>
	public string Name { get; }

	/// <summary>获取数据实体的限定名称。</summary>
	public string QualifiedName { get; }

	/// <summary>获取或设置数据实体的别名。</summary>
	/// <remarks>注意：如果 <see cref="Namespace"/> 属性不为空(<c>null</c>)或空字符串，则该属性默认为：<c>{Namespace}_{Name}</c>。</remarks>
	public string Alias { get => _alias; set => _alias = value ?? (string.IsNullOrEmpty(this.Namespace) ? value : $"{this.Namespace}_{this.Name}"); }

	/// <summary>获取或设置数据实体继承的父实体名。</summary>
	public string BaseName { get; set; }

	/// <summary>获取或设置数据实体支持的驱动。</summary>
	public string Driver { get; set; }

	/// <summary>获取或设置一个值，指示是否为不可变实体。</summary>
	public bool Immutable { get; set; }

	/// <summary>获取一个值，指示该实体是否定义了主键。</summary>
	public bool HasKey => this.Key != null && this.Key.Length > 0;

	/// <summary>获取数据实体的主键。</summary>
	public IDataEntitySimplexProperty[] Key
	{
		get => field ??= this.GetKey();
		internal set;
	}

	/// <summary>获取数据实体的属性元数据集合。</summary>
	public DataEntityPropertyCollection Properties { get; }
	#endregion

	#region 主键方法
	private IDataEntitySimplexProperty[] GetKey()
	{
		var keys = new List<IDataEntitySimplexProperty>(5);

		foreach(var property in this.Properties)
		{
			if(property.IsSimplex(out var simplex) && simplex.IsPrimaryKey)
				keys.Add(simplex);
		}

		return [.. keys];
	}

	public void SetKey(params IEnumerable<string> keys) => this.SetKey([.. keys]);
	public void SetKey(params ReadOnlySpan<string> keys)
	{
		if(keys.IsEmpty)
			return;

		foreach(var property in this.Properties)
		{
			if(property.IsSimplex(out var simplex))
				simplex.IsPrimaryKey = keys.Contains(property.Name);
		}
	}
	#endregion

	#region 重写方法
	public bool Equals(IDataEntity other) => other is not null && string.Equals(this.QualifiedName, other.QualifiedName);
	public bool Equals(DataEntityBase other) => other is not null && string.Equals(this.QualifiedName, other.QualifiedName);
	public override bool Equals(object obj) => obj is DataEntityBase other && this.Equals(other);
	public override int GetHashCode() => HashCode.Combine(this.QualifiedName);
	public override string ToString() => this.Immutable ? $"{this.QualifiedName}(immutability)" : this.QualifiedName;
	#endregion
}
