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
using System.Reflection;
using System.Collections.Generic;

namespace Zongsoft.Data;

/// <summary>表示数据模式中的成员。</summary>
public interface ISchemaMember
{
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
}

