/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
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
using System.Collections.Generic;

namespace Zongsoft.Data
{
	/// <summary>
	/// 表示数据搜索器的泛型接口。
	/// </summary>
	/// <typeparam name="TModel">关于搜索服务对应的数据模型类型。</typeparam>
	public interface IDataSearcher<TModel> : IDataSearcher
	{
		new IEnumerable<TModel> Search(string keyword, string filter = null, params Sorting[] sortings);
		new IEnumerable<TModel> Search(string keyword, IDataOptions options, string filter = null, params Sorting[] sortings);
		new IEnumerable<TModel> Search(string keyword, Paging paging, string filter = null, params Sorting[] sortings);
		new IEnumerable<TModel> Search(string keyword, string schema, string filter = null, params Sorting[] sortings);
		new IEnumerable<TModel> Search(string keyword, string schema, IDataOptions options, string filter = null, params Sorting[] sortings);
		new IEnumerable<TModel> Search(string keyword, string schema, Paging paging, string filter = null, params Sorting[] sortings);
		new IEnumerable<TModel> Search(string keyword, string schema, Paging paging, IDataOptions options, string filter = null, params Sorting[] sortings);
	}
}
