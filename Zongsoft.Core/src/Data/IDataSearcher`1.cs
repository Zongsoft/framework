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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.Data;

/// <summary>表示数据搜索器的泛型接口。</summary>
/// <typeparam name="TModel">关于搜索服务对应的数据模型类型。</typeparam>
public interface IDataSearcher<TModel> : IDataSearcher
{

	#region 异步方法
	new IAsyncEnumerable<TModel> SearchAsync(string keyword, params Sorting[] sortings);
	new IAsyncEnumerable<TModel> SearchAsync(string keyword, Sorting[] sortings, CancellationToken cancellation = default);
	new IAsyncEnumerable<TModel> SearchAsync(string keyword, IDataOptions options, params Sorting[] sortings);
	new IAsyncEnumerable<TModel> SearchAsync(string keyword, IDataOptions options, Sorting[] sortings, CancellationToken cancellation = default);
	new IAsyncEnumerable<TModel> SearchAsync(string keyword, Paging paging, params Sorting[] sortings);
	new IAsyncEnumerable<TModel> SearchAsync(string keyword, Paging paging, Sorting[] sortings, CancellationToken cancellation = default);
	new IAsyncEnumerable<TModel> SearchAsync(string keyword, string schema, params Sorting[] sortings);
	new IAsyncEnumerable<TModel> SearchAsync(string keyword, string schema, Sorting[] sortings, CancellationToken cancellation = default);
	new IAsyncEnumerable<TModel> SearchAsync(string keyword, string schema, IDataOptions options, params Sorting[] sortings);
	new IAsyncEnumerable<TModel> SearchAsync(string keyword, string schema, IDataOptions options, Sorting[] sortings, CancellationToken cancellation = default);
	new IAsyncEnumerable<TModel> SearchAsync(string keyword, string schema, Paging paging, params Sorting[] sortings);
	new IAsyncEnumerable<TModel> SearchAsync(string keyword, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default);
	new IAsyncEnumerable<TModel> SearchAsync(string keyword, string schema, Paging paging, IDataOptions options, params Sorting[] sortings);
	new IAsyncEnumerable<TModel> SearchAsync(string keyword, string schema, Paging paging, IDataOptions options, Sorting[] sortings, CancellationToken cancellation = default);
	#endregion

	#region 便利取消
	new IAsyncEnumerable<TModel> SearchAsync(string keyword, CancellationToken cancellation) => this.SearchAsync(keyword, default(Sorting[]), cancellation);
	new IAsyncEnumerable<TModel> SearchAsync(string keyword, IDataOptions options, CancellationToken cancellation) => this.SearchAsync(keyword, options, default(Sorting[]), cancellation);
	new IAsyncEnumerable<TModel> SearchAsync(string keyword, Paging paging, CancellationToken cancellation) => this.SearchAsync(keyword, paging, default(Sorting[]), cancellation);
	new IAsyncEnumerable<TModel> SearchAsync(string keyword, string schema, CancellationToken cancellation) => this.SearchAsync(keyword, schema, default(Sorting[]), cancellation);
	new IAsyncEnumerable<TModel> SearchAsync(string keyword, string schema, IDataOptions options, CancellationToken cancellation) => this.SearchAsync(keyword, schema, options, default(Sorting[]), cancellation);
	new IAsyncEnumerable<TModel> SearchAsync(string keyword, string schema, Paging paging, CancellationToken cancellation) => this.SearchAsync(keyword, schema, paging, default(Sorting[]), cancellation);
	new IAsyncEnumerable<TModel> SearchAsync(string keyword, string schema, Paging paging, IDataOptions options, CancellationToken cancellation) => this.SearchAsync(keyword, schema, paging, options, default(Sorting[]), cancellation);
	#endregion
}
