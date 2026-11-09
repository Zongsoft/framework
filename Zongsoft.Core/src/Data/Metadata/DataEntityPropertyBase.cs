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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;

namespace Zongsoft.Data.Metadata;

/// <summary>表示数据实体属性的元数据抽象基类。</summary>
public abstract class DataEntityPropertyBase : IDataEntityProperty
{
	#region 构造函数
	protected DataEntityPropertyBase(string name, bool immutable) : this(null, name, immutable) { }
	protected DataEntityPropertyBase(DataEntityBase entity, string name, bool immutable)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		this.Entity = entity;
		this.Name = name.Trim();
		this.Immutable = immutable;
	}
	#endregion

	#region 公共属性
	/// <summary>获取所属的数据实体。</summary>
	public DataEntityBase Entity { get; internal protected set; }
	IDataEntity IDataEntityProperty.Entity => this.Entity;

	/// <summary>获取数据实体属性的名称。</summary>
	public string Name { get; }

	/// <summary>获取数据实体属性的别名。</summary>
	public string Alias { get; set; }

	/// <summary>获取或设置实体属性的提示。</summary>
	public string Hint { get; set; }

	/// <summary>
	/// 获取或设置数据实体属性是否为不可变属性。
	/// 注：简单属性默认为假(<c>False</c>)，复合属性默认为真(<c>True</c>)。
	/// </summary>
	public bool Immutable { get; set; }

	/// <summary>获取一个值，指示数据实体属性是否为单值类型。</summary>
	public abstract bool IsSimplex { get; }

	/// <summary>获取一个值，指示数据实体属性是否为复合类型。</summary>
	public abstract bool IsComplex { get; }
	#endregion

	#region 重写方法
	public override string ToString() => this.Entity == null ? $"{this.Name}" : $"{this.Entity.Name}.{this.Name}";
	#endregion
}
