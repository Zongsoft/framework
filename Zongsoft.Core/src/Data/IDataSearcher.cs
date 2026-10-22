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

/// <summary>表示数据搜索器的接口。</summary>
public interface IDataSearcher
{
	#region 公共属性
	/// <summary>获取数据搜索服务的名称，该名称亦为数据搜索接口的调用名。</summary>
	string Name { get; }

	/// <summary>获取数据搜索关键字的条件解析器。</summary>
	IDataSearcherConditioner Conditioner { get; }
	#endregion

	#region 异步方法
	ValueTask<int> CountAsync(string keyword, IDataOptions options = null, CancellationToken cancellation = default);
	ValueTask<bool> ExistsAsync(string keyword, IDataOptions options = null, CancellationToken cancellation = default);
	IAsyncEnumerable<object> SearchAsync(string keyword, params Sorting[] sortings);
	IAsyncEnumerable<object> SearchAsync(string keyword, Sorting[] sortings, CancellationToken cancellation = default);
	IAsyncEnumerable<object> SearchAsync(string keyword, IDataOptions options, params Sorting[] sortings);
	IAsyncEnumerable<object> SearchAsync(string keyword, IDataOptions options, Sorting[] sortings, CancellationToken cancellation = default);
	IAsyncEnumerable<object> SearchAsync(string keyword, Paging paging, params Sorting[] sortings);
	IAsyncEnumerable<object> SearchAsync(string keyword, Paging paging, Sorting[] sortings, CancellationToken cancellation = default);
	IAsyncEnumerable<object> SearchAsync(string keyword, string schema, params Sorting[] sortings);
	IAsyncEnumerable<object> SearchAsync(string keyword, string schema, Sorting[] sortings, CancellationToken cancellation = default);
	IAsyncEnumerable<object> SearchAsync(string keyword, string schema, IDataOptions options, params Sorting[] sortings);
	IAsyncEnumerable<object> SearchAsync(string keyword, string schema, IDataOptions options, Sorting[] sortings, CancellationToken cancellation = default);
	IAsyncEnumerable<object> SearchAsync(string keyword, string schema, Paging paging, params Sorting[] sortings);
	IAsyncEnumerable<object> SearchAsync(string keyword, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default);
	IAsyncEnumerable<object> SearchAsync(string keyword, string schema, Paging paging, IDataOptions options, params Sorting[] sortings);
	IAsyncEnumerable<object> SearchAsync(string keyword, string schema, Paging paging, IDataOptions options, Sorting[] sortings, CancellationToken cancellation = default);
	#endregion

	#region 便利取消
	ValueTask<int> CountAsync(string keyword, CancellationToken cancellation) => this.CountAsync(keyword, default(IDataOptions), cancellation);
	ValueTask<bool> ExistsAsync(string keyword, CancellationToken cancellation) => this.ExistsAsync(keyword, default(IDataOptions), cancellation);
	IAsyncEnumerable<object> SearchAsync(string keyword, CancellationToken cancellation) => this.SearchAsync(keyword, default(Sorting[]), cancellation);
	IAsyncEnumerable<object> SearchAsync(string keyword, IDataOptions options, CancellationToken cancellation) => this.SearchAsync(keyword, options, default(Sorting[]), cancellation);
	IAsyncEnumerable<object> SearchAsync(string keyword, Paging paging, CancellationToken cancellation) => this.SearchAsync(keyword, paging, default(Sorting[]), cancellation);
	IAsyncEnumerable<object> SearchAsync(string keyword, string schema, CancellationToken cancellation) => this.SearchAsync(keyword, schema, default(Sorting[]), cancellation);
	IAsyncEnumerable<object> SearchAsync(string keyword, string schema, IDataOptions options, CancellationToken cancellation) => this.SearchAsync(keyword, schema, options, default(Sorting[]), cancellation);
	IAsyncEnumerable<object> SearchAsync(string keyword, string schema, Paging paging, CancellationToken cancellation) => this.SearchAsync(keyword, schema, paging, default(Sorting[]), cancellation);
	IAsyncEnumerable<object> SearchAsync(string keyword, string schema, Paging paging, IDataOptions options, CancellationToken cancellation) => this.SearchAsync(keyword, schema, paging, options, default(Sorting[]), cancellation);
	#endregion
}
