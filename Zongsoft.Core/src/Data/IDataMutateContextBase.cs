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

namespace Zongsoft.Data;

/// <summary>
/// 表示写入操作上下文的基础接口。
/// </summary>
public interface IDataMutateContextBase : IDataAccessContextBase
{
	/// <summary>获取写入操作对应的实体。</summary>
	Metadata.IDataEntity Entity { get; }

	/// <summary>获取或设置写入操作的受影响记录数。</summary>
	int Count { get; set; }

	/// <summary>获取或设置写入操作的数据。</summary>
	object Data { get; set; }

	/// <summary>获取写入操作的选项。</summary>
	IDataMutateOptions Options { get; }

	/// <summary>获取写入操作的验证器。</summary>
	IDataValidator Validator { get; }
}
