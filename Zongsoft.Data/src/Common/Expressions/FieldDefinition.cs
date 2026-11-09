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
using System.Collections.ObjectModel;

namespace Zongsoft.Data.Common.Expressions;

public class FieldDefinition : Expression
{
	#region 构造函数
	public FieldDefinition(string name, DataType type, bool nullable = true)
	{
		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		this.Name = name;
		this.Type = type;
		this.Nullable = nullable;
	}
	#endregion

	#region 公共属性
	/// <summary>获取字段的名称。</summary>
	public string Name { get; }

	/// <summary>获取或设置字段的数据类型。</summary>
	public DataType Type { get; set; }

	/// <summary>获取或设置字段是否允许为空。</summary>
	public bool Nullable { get; set; }

	/// <summary>获取或设置数值字段的最大长度。</summary>
	public int Length { get; set; }

	/// <summary>获取或设置数值字段的精度。</summary>
	public byte Precision { get; set; }

	/// <summary>获取或设置数值字段的小数点位数。</summary>
	public byte Scale { get; set; }
	#endregion

	#region 重写方法
	public override string ToString() => $"{this.Name} {this.Type} {(this.Nullable ? "NULL" : "NOT NULL")}";
	#endregion
}

public class FieldDefinitionCollection() : KeyedCollection<string, FieldDefinition>(StringComparer.OrdinalIgnoreCase)
{
	protected override string GetKeyForItem(FieldDefinition field) => field.Name;
}
