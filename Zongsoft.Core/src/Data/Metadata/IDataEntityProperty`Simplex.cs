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

/// <summary>表示数据实体单值属性的元数据类。</summary>
public interface IDataEntitySimplexProperty : IDataEntityProperty
{
	#region 公共属性
	/// <summary>获取或设置数据实体属性的别名（字段名）。</summary>
	string Alias { get; set; }

	/// <summary>获取或设置数据实体属性的数据类型。</summary>
	DataType Type { get; set; }

	/// <summary>获取或设置一个值，指示当前属性是否为主键。</summary>
	bool IsPrimaryKey { get; set; }

	/// <summary>获取或设置文本或数组属性的最大长度。</summary>
	int Length { get; set; }

	/// <summary>获取或设置数值属性的精度。</summary>
	byte Precision { get; set; }

	/// <summary>获取或设置数值属性的小数点位数。</summary>
	byte Scale { get; set; }

	/// <summary>获取或设置默认值。</summary>
	object DefaultValue { get; set; }

	/// <summary>获取或设置属性是否允许为空。</summary>
	bool Nullable { get; set; }

	/// <summary>获取或设置属性是否可以参与排序。</summary>
	bool Sortable { get; set; }

	/// <summary>获取或设置数据序号器元数据。</summary>
	IDataEntityPropertySequence Sequence { get; set; }
	#endregion
}
