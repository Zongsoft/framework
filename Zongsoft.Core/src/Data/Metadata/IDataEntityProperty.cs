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

/// <summary>表示数据实体属性的元数据接口。</summary>
public interface IDataEntityProperty
{
	#region 属性定义
	/// <summary>获取所属的数据实体。</summary>
	IDataEntity Entity { get; }

	/// <summary>获取数据实体属性的名称。</summary>
	string Name { get; }

	/// <summary>获取或设置数据实体属性的提示。</summary>
	string Hint { get; set; }

	/// <summary>获取或设置一个值，指示数据实体属性是否为不可变属性，默认为假(<c>False</c>)。</summary>
	/// <remarks>
	/// 	<para>对于不可变简单属性：不能被修改(Update, Upsert)，但是新增(Insert)时可以设置其内容。</para>
	/// 	<para>对于不可变复合属性：不支持任何写操作(Delete, Insert, Update, Upsert)。</para>
	/// </remarks>
	bool Immutable { get; set; }

	/// <summary>获取一个值，指示数据实体属性是否为单值类型。</summary>
	bool IsSimplex { get; }

	/// <summary>获取一个值，指示数据实体属性是否为复合类型。</summary>
	bool IsComplex { get; }
	#endregion
}
