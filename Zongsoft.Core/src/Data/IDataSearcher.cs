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
using System.Collections;

namespace Zongsoft.Data;

/// <summary>
/// 表示数据搜索器的接口。
/// </summary>
public interface IDataSearcher
{
	/// <summary>获取数据搜索服务的名称，该名称亦为数据搜索接口的调用名。</summary>
	string Name { get; }

	/// <summary>获取数据搜索关键字的条件解析器。</summary>
	IDataSearcherConditioner Conditioner { get; }

	int Count(string keyword, IDataOptions options = null);
	bool Exists(string keyword, IDataOptions options = null);

	IEnumerable Search(string keyword, params Sorting[] sortings);
	IEnumerable Search(string keyword, IDataOptions options, params Sorting[] sortings);
	IEnumerable Search(string keyword, Paging paging, params Sorting[] sortings);
	IEnumerable Search(string keyword, string schema, params Sorting[] sortings);
	IEnumerable Search(string keyword, string schema, IDataOptions options, params Sorting[] sortings);
	IEnumerable Search(string keyword, string schema, Paging paging, params Sorting[] sortings);
	IEnumerable Search(string keyword, string schema, Paging paging, IDataOptions options, params Sorting[] sortings);
}
