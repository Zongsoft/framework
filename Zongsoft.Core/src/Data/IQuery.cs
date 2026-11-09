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

namespace Zongsoft.Data;

/// <summary>表示查询定义的接口。</summary>
public interface IQuery
{
	/// <summary>获取数据查询的名称。</summary>
	string Name { get; }

	/// <summary>获取查询要返回的结果集元素类型。</summary>
	Type ModelType { get; }

	/// <summary>获取或设置查询操作的条件。</summary>
	ICondition Criteria { get; set; }

	/// <summary>获取或设置查询操作的结果数据模式（即查询结果的形状结构）。</summary>
	ISchema Schema { get; set; }

	/// <summary>获取或设置查询操作的分组。</summary>
	Grouping Grouping { get; set; }

	/// <summary>获取或设置查询操作的分页设置。</summary>
	Paging Paging { get; set; }

	/// <summary>获取或设置查询操作的排序设置。</summary>
	Sorting[] Sortings { get; set; }
}
