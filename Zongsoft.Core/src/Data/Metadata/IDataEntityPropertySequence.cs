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

/// <summary>表示数据实体属性序号器的元数据接口。</summary>
public interface IDataEntityPropertySequence
{
	/// <summary>获取序号所属的数据属性元素。</summary>
	IDataEntitySimplexProperty Property { get; }

	/// <summary>获取序号器的名称。</summary>
	string Name { get; }

	/// <summary>获取序号器的种子数。</summary>
	int Seed { get; }

	/// <summary>获取序号器的递增量，默认为 <c>1</c>。</summary>
	int Interval { get; }

	/// <summary>获取一个值，指示是否采用数据库内置序号方案。</summary>
	bool IsBuiltin { get; }

	/// <summary>获取一个值，指示是否采用外置序号器方案。</summary>
	bool IsExternal { get; }

	/// <summary>获取序号的引用的属性数组。</summary>
	IDataEntitySimplexProperty[] References { get; }

	/// <summary>获取一个值，指示是否有引用的属性。</summary>
	bool HasReferences => this.References != null && this.References.Length > 0;
}
