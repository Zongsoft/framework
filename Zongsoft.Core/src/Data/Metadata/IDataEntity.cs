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

/// <summary>表示数据实体的元数据接口。</summary>
public interface IDataEntity
{
	#region 属性声明
	/// <summary>获取所属命名空间。</summary>
	string Namespace { get; }

	/// <summary>获取数据实体的名称。</summary>
	string Name { get; }

	/// <summary>获取数据实体的限定名称。</summary>
	string QualifiedName { get; }

	/// <summary>获取或设置数据实体映射的别名（表名）。</summary>
	string Alias { get; set; }

	/// <summary>获取或设置数据实体继承的父实体名。</summary>
	string BaseName { get; set; }

	/// <summary>获取或设置数据实体支持的驱动。</summary>
	string Driver { get; set; }

	/// <summary>获取或设置一个值，指示是否为不可变实体，默认为否(<c>False</c>)。</summary>
	/// <remarks>不可变实体只支持新增和删除操作。</remarks>
	bool Immutable { get; set; }

	/// <summary>获取一个值，指示该实体是否定义了主键。</summary>
	bool HasKey { get; }

	/// <summary>获取数据实体的主键。</summary>
	IDataEntitySimplexProperty[] Key { get; }

	/// <summary>获取数据实体的属性元数据集合。</summary>
	DataEntityPropertyCollection Properties { get; }
	#endregion
}
