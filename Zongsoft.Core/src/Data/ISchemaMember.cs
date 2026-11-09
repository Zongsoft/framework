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
using System.Reflection;
using System.Collections.Generic;

namespace Zongsoft.Data;

/// <summary>表示数据模式中的成员。</summary>
public interface ISchemaMember
{
	#region 公共属性
	/// <summary>获取模式成员的名称。</summary>
	string Name { get; }

	/// <summary>获取模式成员所在的父级路径，根成员的路径为空字符串。</summary>
	string Path { get; }

	/// <summary>获取包含当前成员名称的完整路径。</summary>
	string FullPath { get; }

	/// <summary>获取模式成员的父成员，根成员的父成员为空。</summary>
	ISchemaMember Parent { get; }

	/// <summary>获取模式成员对应的模型字段或属性，未绑定模型成员则为空。</summary>
	MemberInfo Member { get; }

	/// <summary>获取模式成员对应的数据实体属性，非持久化成员则为空。</summary>
	Metadata.IDataEntityProperty Property { get; }

	/// <summary>获取一个值，指示该模式成员是否忽略持久化。</summary>
	bool Ignored { get; }

	/// <summary>获取一对多模式成员的最大记录数，小于或等于零表示不限。</summary>
	int Limit { get; }

	/// <summary>获取一对多模式成员的排序规则集，未指定排序规则则为空。</summary>
	Sorting[] Sortings { get; }

	/// <summary>获取一个值，指示该模式成员是否包含子成员。</summary>
	bool HasChildren { get; }

	/// <summary>获取模式成员的子成员集。</summary>
	IEnumerable<ISchemaMember> Children { get; }
	#endregion
}

