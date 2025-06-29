﻿/*
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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Data.Metadata;

/// <summary>
/// 表示数据实体属性的元数据接口。
/// </summary>
public interface IDataEntityProperty : IEquatable<IDataEntityProperty>
{
	#region 属性定义
	/// <summary>获取所属的数据实体。</summary>
	IDataEntity Entity { get; }

	/// <summary>获取数据实体属性的名称。</summary>
	string Name { get; }

	/// <summary>获取数据实体属性的提示。</summary>
	string Hint { get; }

	/// <summary>获取一个值，指示数据实体属性是否为不可变属性，默认为假(<c>False</c>)。</summary>
	/// <remarks>
	/// 	<para>对于不可变简单属性：不能被修改(Update, Upsert)，但是新增(Insert)时可以设置其内容。</para>
	/// 	<para>对于不可变复合属性：不支持任何写操作(Delete, Insert, Update, Upsert)。</para>
	/// </remarks>
	bool Immutable { get; }

	/// <summary>获取一个值，指示数据实体属性是否为单值类型。</summary>
	bool IsSimplex { get; }

	/// <summary>获取一个值，指示数据实体属性是否为复合类型。</summary>
	bool IsComplex { get; }
	#endregion
}
