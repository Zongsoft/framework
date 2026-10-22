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

/// <summary>表示数据服务的泛型接口。</summary>
/// <typeparam name="TModel">关于数据服务对应的数据模型类型。</typeparam>
public interface IDataService<TModel> : IDataService
{
	#region 事件定义
	event Func<object, DataGettedEventArgs<TModel>, CancellationToken, ValueTask> Getted;
	event Func<object, DataGettingEventArgs<TModel>, CancellationToken, ValueTask> Getting;
	#endregion

	#region 属性定义
	/// <summary>获取数据搜索器对象。</summary>
	IDataSearcher<TModel> Searcher { get; }

	/// <summary>获取数据服务授权验证器。</summary>
	IDataServiceAuthorizer<TModel> Authorizer { get; }

	/// <summary>获取数据服务操作验证器。</summary>
	new IDataServiceValidator<TModel> Validator { get; }

	/// <summary>获取数据服务过滤器集合。</summary>
	ICollection<IDataServiceFilter<TModel>> Filters { get; }
	#endregion

	#region 异步查询
	new IAsyncEnumerable<TModel> SelectAsync(CancellationToken cancellation);
	new IAsyncEnumerable<TModel> SelectAsync(DataSelectOptions options = null, params Sorting[] sortings);
	new IAsyncEnumerable<TModel> SelectAsync(DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
	new IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, params Sorting[] sortings);
	new IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, Sorting[] sortings, CancellationToken cancellation = default);
	new IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, DataSelectOptions options, params Sorting[] sortings);
	new IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
	new IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, Paging paging, params Sorting[] sortings);
	new IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, Paging paging, Sorting[] sortings, CancellationToken cancellation = default);
	new IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, Paging paging, DataSelectOptions options, params Sorting[] sortings);
	new IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
	new IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, string schema, params Sorting[] sortings);
	new IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, string schema, Sorting[] sortings, CancellationToken cancellation = default);
	new IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, string schema, DataSelectOptions options, params Sorting[] sortings);
	new IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
	new IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, string schema, Paging paging, params Sorting[] sortings);
	new IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default);
	new IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings);
	new IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
	#endregion

	#region 便利取消
	new IAsyncEnumerable<TModel> SelectAsync(DataSelectOptions options, CancellationToken cancellation) => this.SelectAsync(options, default(Sorting[]), cancellation);
	new IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, CancellationToken cancellation) => this.SelectAsync(criteria, default(Sorting[]), cancellation);
	new IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, DataSelectOptions options, CancellationToken cancellation) => this.SelectAsync(criteria, options, default(Sorting[]), cancellation);
	new IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, Paging paging, CancellationToken cancellation) => this.SelectAsync(criteria, paging, default(Sorting[]), cancellation);
	new IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, Paging paging, DataSelectOptions options, CancellationToken cancellation) => this.SelectAsync(criteria, paging, options, default(Sorting[]), cancellation);
	new IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, string schema, CancellationToken cancellation) => this.SelectAsync(criteria, schema, default(Sorting[]), cancellation);
	new IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, string schema, DataSelectOptions options, CancellationToken cancellation) => this.SelectAsync(criteria, schema, options, default(Sorting[]), cancellation);
	new IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, string schema, Paging paging, CancellationToken cancellation) => this.SelectAsync(criteria, schema, paging, default(Sorting[]), cancellation);
	new IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, string schema, Paging paging, DataSelectOptions options, CancellationToken cancellation) => this.SelectAsync(criteria, schema, paging, options, default(Sorting[]), cancellation);
	#endregion
	#region 排序重载
	new IAsyncEnumerable<TModel> SelectAsync(Sorting[] sortings, CancellationToken cancellation) => this.SelectAsync(default(DataSelectOptions), sortings, cancellation);
	#endregion
}
