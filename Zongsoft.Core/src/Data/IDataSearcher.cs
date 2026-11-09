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

	#region 计数方法
	ValueTask<int> CountAsync(string keyword, CancellationToken cancellation) => this.CountAsync(keyword, default(IDataOptions), cancellation);
	ValueTask<int> CountAsync(string keyword, IDataOptions options = null, CancellationToken cancellation = default);
	#endregion

	#region 存在方法
	ValueTask<bool> ExistsAsync(string keyword, CancellationToken cancellation) => this.ExistsAsync(keyword, default(IDataOptions), cancellation);
	ValueTask<bool> ExistsAsync(string keyword, IDataOptions options = null, CancellationToken cancellation = default);
	#endregion

	#region 搜索方法
	IAsyncEnumerable<object> SearchAsync(string keyword, params Sorting[] sortings);
	IAsyncEnumerable<object> SearchAsync(string keyword, CancellationToken cancellation) => this.SearchAsync(keyword, default(Sorting[]), cancellation);
	IAsyncEnumerable<object> SearchAsync(string keyword, Sorting[] sortings, CancellationToken cancellation = default);

	IAsyncEnumerable<object> SearchAsync(string keyword, IDataOptions options, params Sorting[] sortings);
	IAsyncEnumerable<object> SearchAsync(string keyword, IDataOptions options, CancellationToken cancellation) => this.SearchAsync(keyword, options, default(Sorting[]), cancellation);
	IAsyncEnumerable<object> SearchAsync(string keyword, IDataOptions options, Sorting[] sortings, CancellationToken cancellation = default);

	IAsyncEnumerable<object> SearchAsync(string keyword, Paging paging, params Sorting[] sortings);
	IAsyncEnumerable<object> SearchAsync(string keyword, Paging paging, CancellationToken cancellation) => this.SearchAsync(keyword, paging, default(Sorting[]), cancellation);
	IAsyncEnumerable<object> SearchAsync(string keyword, Paging paging, Sorting[] sortings, CancellationToken cancellation = default);

	IAsyncEnumerable<object> SearchAsync(string keyword, string schema, params Sorting[] sortings);
	IAsyncEnumerable<object> SearchAsync(string keyword, string schema, CancellationToken cancellation) => this.SearchAsync(keyword, schema, default(Sorting[]), cancellation);
	IAsyncEnumerable<object> SearchAsync(string keyword, string schema, Sorting[] sortings, CancellationToken cancellation = default);

	IAsyncEnumerable<object> SearchAsync(string keyword, string schema, IDataOptions options, params Sorting[] sortings);
	IAsyncEnumerable<object> SearchAsync(string keyword, string schema, IDataOptions options, CancellationToken cancellation) => this.SearchAsync(keyword, schema, options, default(Sorting[]), cancellation);
	IAsyncEnumerable<object> SearchAsync(string keyword, string schema, IDataOptions options, Sorting[] sortings, CancellationToken cancellation = default);

	IAsyncEnumerable<object> SearchAsync(string keyword, string schema, Paging paging, params Sorting[] sortings);
	IAsyncEnumerable<object> SearchAsync(string keyword, string schema, Paging paging, CancellationToken cancellation) => this.SearchAsync(keyword, schema, paging, default(Sorting[]), cancellation);
	IAsyncEnumerable<object> SearchAsync(string keyword, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default);

	IAsyncEnumerable<object> SearchAsync(string keyword, string schema, Paging paging, IDataOptions options, params Sorting[] sortings);
	IAsyncEnumerable<object> SearchAsync(string keyword, string schema, Paging paging, IDataOptions options, CancellationToken cancellation) => this.SearchAsync(keyword, schema, paging, options, default(Sorting[]), cancellation);
	IAsyncEnumerable<object> SearchAsync(string keyword, string schema, Paging paging, IDataOptions options, Sorting[] sortings, CancellationToken cancellation = default);
	#endregion
}
