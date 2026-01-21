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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Data;

public partial interface IDataService
{
	#region 存在方法
	ValueTask<bool> ExistsAsync(string key, DataExistsOptions options = null, CancellationToken cancellation = default);
	ValueTask<bool> ExistsAsync<TKey1>(TKey1 key1, DataExistsOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1>;
	ValueTask<bool> ExistsAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, DataExistsOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
	ValueTask<bool> ExistsAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, DataExistsOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
	ValueTask<bool> ExistsAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataExistsOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
	ValueTask<bool> ExistsAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataExistsOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
	ValueTask<bool> ExistsAsync(ICondition criteria, DataExistsOptions options = null, CancellationToken cancellation = default);
	#endregion

	#region 聚合方法
	ValueTask<TValue?> AggregateAsync<TValue>(DataAggregate aggregate, string key, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue>;
	ValueTask<TValue?> AggregateAsync<TValue>(DataAggregate aggregate, string key, DataAggregateOptions options, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue>;
	ValueTask<TValue?> AggregateAsync<TKey1, TValue>(DataAggregate aggregate, TKey1 key1, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TValue : struct, IEquatable<TValue>;
	ValueTask<TValue?> AggregateAsync<TKey1, TValue>(DataAggregate aggregate, TKey1 key1, DataAggregateOptions options, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TValue : struct, IEquatable<TValue>;
	ValueTask<TValue?> AggregateAsync<TKey1, TKey2, TValue>(DataAggregate aggregate, TKey1 key1, TKey2 key2, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TValue : struct, IEquatable<TValue>;
	ValueTask<TValue?> AggregateAsync<TKey1, TKey2, TValue>(DataAggregate aggregate, TKey1 key1, TKey2 key2, DataAggregateOptions options, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TValue : struct, IEquatable<TValue>;
	ValueTask<TValue?> AggregateAsync<TKey1, TKey2, TKey3, TValue>(DataAggregate aggregate, TKey1 key1, TKey2 key2, TKey3 key3, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TValue : struct, IEquatable<TValue>;
	ValueTask<TValue?> AggregateAsync<TKey1, TKey2, TKey3, TValue>(DataAggregate aggregate, TKey1 key1, TKey2 key2, TKey3 key3, DataAggregateOptions options, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TValue : struct, IEquatable<TValue>;
	ValueTask<TValue?> AggregateAsync<TKey1, TKey2, TKey3, TKey4, TValue>(DataAggregate aggregate, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TValue : struct, IEquatable<TValue>;
	ValueTask<TValue?> AggregateAsync<TKey1, TKey2, TKey3, TKey4, TValue>(DataAggregate aggregate, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataAggregateOptions options, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TValue : struct, IEquatable<TValue>;
	ValueTask<TValue?> AggregateAsync<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(DataAggregate aggregate, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5> where TValue : struct, IEquatable<TValue>;
	ValueTask<TValue?> AggregateAsync<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(DataAggregate aggregate, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataAggregateOptions options, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5> where TValue : struct, IEquatable<TValue>;
	ValueTask<TValue?> AggregateAsync<TValue>(DataAggregate aggregate, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue>;
	ValueTask<TValue?> AggregateAsync<TValue>(DataAggregate aggregate, DataAggregateOptions options, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue>;
	ValueTask<TValue?> AggregateAsync<TValue>(DataAggregate aggregate, ICondition criteria, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue>;
	ValueTask<TValue?> AggregateAsync<TValue>(DataAggregate aggregate, ICondition criteria, DataAggregateOptions options, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue>;
	#endregion

	#region 删除方法
	ValueTask<int> DeleteAsync(string key, DataDeleteOptions options = null, CancellationToken cancellation = default);
	ValueTask<int> DeleteAsync(string key, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default);

	ValueTask<int> DeleteAsync<TKey1>(TKey1 key1, DataDeleteOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1>;
	ValueTask<int> DeleteAsync<TKey1>(TKey1 key1, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1>;
	ValueTask<int> DeleteAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, DataDeleteOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
	ValueTask<int> DeleteAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
	ValueTask<int> DeleteAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, DataDeleteOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
	ValueTask<int> DeleteAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
	ValueTask<int> DeleteAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataDeleteOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
	ValueTask<int> DeleteAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
	ValueTask<int> DeleteAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataDeleteOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
	ValueTask<int> DeleteAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;

	ValueTask<int> DeleteAsync(ICondition criteria, DataDeleteOptions options = null, CancellationToken cancellation = default);
	ValueTask<int> DeleteAsync(ICondition criteria, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default);
	#endregion

	#region 插入方法
	ValueTask<int> InsertAsync(object data, DataInsertOptions options = null, CancellationToken cancellation = default);
	ValueTask<int> InsertAsync(object data, string schema, DataInsertOptions options = null, CancellationToken cancellation = default);

	ValueTask<int> InsertManyAsync(IEnumerable items, DataInsertOptions options = null, CancellationToken cancellation = default);
	ValueTask<int> InsertManyAsync(IEnumerable items, string schema, DataInsertOptions options = null, CancellationToken cancellation = default);

	ValueTask<int> InsertManyAsync(string key, IEnumerable items, DataInsertOptions options = null, CancellationToken cancellation = default);
	ValueTask<int> InsertManyAsync(string key, IEnumerable items, string schema, DataInsertOptions options = null, CancellationToken cancellation = default);
	#endregion

	#region 增改方法
	ValueTask<int> UpsertAsync(object data, DataUpsertOptions options = null, CancellationToken cancellation = default);
	ValueTask<int> UpsertAsync(object data, string schema, DataUpsertOptions options = null, CancellationToken cancellation = default);

	ValueTask<int> UpsertManyAsync(IEnumerable items, DataUpsertOptions options = null, CancellationToken cancellation = default);
	ValueTask<int> UpsertManyAsync(IEnumerable items, string schema, DataUpsertOptions options = null, CancellationToken cancellation = default);

	ValueTask<int> UpsertManyAsync(string key, IEnumerable items, DataUpsertOptions options = null, CancellationToken cancellation = default);
	ValueTask<int> UpsertManyAsync(string key, IEnumerable items, string schema, DataUpsertOptions options = null, CancellationToken cancellation = default);
	#endregion

	#region 更新方法
	ValueTask<int> UpdateAsync(string key, object data, DataUpdateOptions options = null, CancellationToken cancellation = default);
	ValueTask<int> UpdateAsync(string key, object data, string schema, DataUpdateOptions options = null, CancellationToken cancellation = default);
	ValueTask<int> UpdateManyAsync(string key, IEnumerable items, CancellationToken cancellation = default) => this.UpdateManyAsync(key, items, null, null, cancellation);
	ValueTask<int> UpdateManyAsync(string key, IEnumerable items, string schema, CancellationToken cancellation = default) => this.UpdateManyAsync(key, items, schema, null, cancellation);
	ValueTask<int> UpdateManyAsync(string key, IEnumerable items, DataUpdateOptions options, CancellationToken cancellation = default);
	ValueTask<int> UpdateManyAsync(string key, IEnumerable items, string schema, DataUpdateOptions options, CancellationToken cancellation = default);

	ValueTask<int> UpdateAsync<TKey1>(TKey1 key1, object data, DataUpdateOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1>;
	ValueTask<int> UpdateAsync<TKey1>(TKey1 key1, string schema, object data, DataUpdateOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1>;
	ValueTask<int> UpdateAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, object data, DataUpdateOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
	ValueTask<int> UpdateAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, object data, DataUpdateOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
	ValueTask<int> UpdateAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, object data, DataUpdateOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
	ValueTask<int> UpdateAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, object data, DataUpdateOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
	ValueTask<int> UpdateAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, object data, DataUpdateOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
	ValueTask<int> UpdateAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, object data, DataUpdateOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
	ValueTask<int> UpdateAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, object data, DataUpdateOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
	ValueTask<int> UpdateAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, object data, DataUpdateOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;

	ValueTask<int> UpdateAsync(object data, DataUpdateOptions options = null, CancellationToken cancellation = default);
	ValueTask<int> UpdateAsync(object data, string schema, DataUpdateOptions options = null, CancellationToken cancellation = default);
	ValueTask<int> UpdateAsync(object data, ICondition criteria, DataUpdateOptions options = null, CancellationToken cancellation = default);
	ValueTask<int> UpdateAsync(object data, ICondition criteria, string schema, DataUpdateOptions options = null, CancellationToken cancellation = default);
	#endregion

	#region 查询方法
	ValueTask<object> GetAsync(string key, params Sorting[] sortings);
	ValueTask<object> GetAsync(string key, Sorting[] sortings, CancellationToken cancellation = default);
	ValueTask<object> GetAsync(string key, DataSelectOptions options, params Sorting[] sortings);
	ValueTask<object> GetAsync(string key, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
	ValueTask<object> GetAsync(string key, Paging paging, params Sorting[] sortings);
	ValueTask<object> GetAsync(string key, Paging paging, Sorting[] sortings, CancellationToken cancellation = default);
	ValueTask<object> GetAsync(string key, Paging paging, DataSelectOptions options, params Sorting[] sortings);
	ValueTask<object> GetAsync(string key, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
	ValueTask<object> GetAsync(string key, string schema, params Sorting[] sortings);
	ValueTask<object> GetAsync(string key, string schema, Sorting[] sortings, CancellationToken cancellation = default);
	ValueTask<object> GetAsync(string key, string schema, DataSelectOptions options, params Sorting[] sortings);
	ValueTask<object> GetAsync(string key, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
	ValueTask<object> GetAsync(string key, string schema, Paging paging, params Sorting[] sortings);
	ValueTask<object> GetAsync(string key, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default);
	ValueTask<object> GetAsync(string key, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings);
	ValueTask<object> GetAsync(string key, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);

	ValueTask<object> GetAsync<TKey1>(TKey1 key1, params Sorting[] sortings) where TKey1 : IEquatable<TKey1>;
	ValueTask<object> GetAsync<TKey1>(TKey1 key1, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1>;
	ValueTask<object> GetAsync<TKey1>(TKey1 key1, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1>;
	ValueTask<object> GetAsync<TKey1>(TKey1 key1, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1>;
	ValueTask<object> GetAsync<TKey1>(TKey1 key1, Paging paging, params Sorting[] sortings) where TKey1 : IEquatable<TKey1>;
	ValueTask<object> GetAsync<TKey1>(TKey1 key1, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1>;
	ValueTask<object> GetAsync<TKey1>(TKey1 key1, Paging paging, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1>;
	ValueTask<object> GetAsync<TKey1>(TKey1 key1, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1>;
	ValueTask<object> GetAsync<TKey1>(TKey1 key1, string schema, params Sorting[] sortings) where TKey1 : IEquatable<TKey1>;
	ValueTask<object> GetAsync<TKey1>(TKey1 key1, string schema, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1>;
	ValueTask<object> GetAsync<TKey1>(TKey1 key1, string schema, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1>;
	ValueTask<object> GetAsync<TKey1>(TKey1 key1, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1>;
	ValueTask<object> GetAsync<TKey1>(TKey1 key1, string schema, Paging paging, params Sorting[] sortings) where TKey1 : IEquatable<TKey1>;
	ValueTask<object> GetAsync<TKey1>(TKey1 key1, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1>;
	ValueTask<object> GetAsync<TKey1>(TKey1 key1, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1>;
	ValueTask<object> GetAsync<TKey1>(TKey1 key1, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1>;

	ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
	ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
	ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
	ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
	ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, Paging paging, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
	ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
	ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, Paging paging, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
	ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
	ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
	ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
	ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
	ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
	ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Paging paging, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
	ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
	ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
	ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;

	ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, Paging paging, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, Paging paging, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Paging paging, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;

	ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, Paging paging, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, Paging paging, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, Paging paging, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;

	ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, Paging paging, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, Paging paging, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, Paging paging, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
	ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;

	IAsyncEnumerable<object> SelectAsync(CancellationToken cancellation = default);
	IAsyncEnumerable<object> SelectAsync(DataSelectOptions options, params Sorting[] sortings);
	IAsyncEnumerable<object> SelectAsync(DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
	IAsyncEnumerable<object> SelectAsync(ICondition criteria, params Sorting[] sortings);
	IAsyncEnumerable<object> SelectAsync(ICondition criteria, Sorting[] sortings, CancellationToken cancellation = default);
	IAsyncEnumerable<object> SelectAsync(ICondition criteria, DataSelectOptions options, params Sorting[] sortings);
	IAsyncEnumerable<object> SelectAsync(ICondition criteria, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
	IAsyncEnumerable<object> SelectAsync(ICondition criteria, Paging paging, params Sorting[] sortings);
	IAsyncEnumerable<object> SelectAsync(ICondition criteria, Paging paging, Sorting[] sortings, CancellationToken cancellation = default);
	IAsyncEnumerable<object> SelectAsync(ICondition criteria, Paging paging, DataSelectOptions options, params Sorting[] sortings);
	IAsyncEnumerable<object> SelectAsync(ICondition criteria, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
	IAsyncEnumerable<object> SelectAsync(ICondition criteria, string schema, params Sorting[] sortings);
	IAsyncEnumerable<object> SelectAsync(ICondition criteria, string schema, Sorting[] sortings, CancellationToken cancellation = default);
	IAsyncEnumerable<object> SelectAsync(ICondition criteria, string schema, DataSelectOptions options, params Sorting[] sortings);
	IAsyncEnumerable<object> SelectAsync(ICondition criteria, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
	IAsyncEnumerable<object> SelectAsync(ICondition criteria, string schema, Paging paging, params Sorting[] sortings);
	IAsyncEnumerable<object> SelectAsync(ICondition criteria, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default);
	IAsyncEnumerable<object> SelectAsync(ICondition criteria, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings);
	IAsyncEnumerable<object> SelectAsync(ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);

	IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, params Sorting[] sortings);
	IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, Sorting[] sortings, CancellationToken cancellation = default);
	IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, DataSelectOptions options, params Sorting[] sortings);
	IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
	IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, Paging paging, params Sorting[] sortings);
	IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, Paging paging, Sorting[] sortings, CancellationToken cancellation = default);
	IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, Paging paging, DataSelectOptions options, params Sorting[] sortings);
	IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
	IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, string schema, params Sorting[] sortings);
	IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, string schema, Sorting[] sortings, CancellationToken cancellation = default);
	IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, string schema, DataSelectOptions options, params Sorting[] sortings);
	IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
	IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, string schema, Paging paging, params Sorting[] sortings);
	IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default);
	IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings);
	IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
	IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, ICondition criteria, Paging paging, params Sorting[] sortings);
	IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, ICondition criteria, Paging paging, Sorting[] sortings, CancellationToken cancellation = default);
	IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema = null, params Sorting[] sortings);
	IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema, Sorting[] sortings, CancellationToken cancellation = default);
	IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema, DataSelectOptions options, params Sorting[] sortings);
	IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
	IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema, Paging paging, params Sorting[] sortings);
	IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default);
	IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings);
	IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
	#endregion
}

/*
 * 本接口是为了处理相关方法的泛型条件重载版本的条件参数类型匹配错误的问题。
 */
public partial interface IDataService
{
	ValueTask<bool> ExistsAsync(Condition criteria, DataExistsOptions options = null, CancellationToken cancellation = default) => this.ExistsAsync((ICondition)criteria, options, cancellation);
	ValueTask<bool> ExistsAsync(ConditionCollection criteria, DataExistsOptions options = null, CancellationToken cancellation = default) => this.ExistsAsync((ICondition)criteria, options, cancellation);

	ValueTask<TValue?> AggregateAsync<TValue>(DataAggregate aggregate, Condition criteria, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, (ICondition)criteria, null, cancellation);
	ValueTask<TValue?> AggregateAsync<TValue>(DataAggregate aggregate, Condition criteria, DataAggregateOptions options, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, (ICondition)criteria, options, cancellation);
	ValueTask<TValue?> AggregateAsync<TValue>(DataAggregate aggregate, ConditionCollection criteria, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, (ICondition)criteria, null, cancellation);
	ValueTask<TValue?> AggregateAsync<TValue>(DataAggregate aggregate, ConditionCollection criteria, DataAggregateOptions options, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, (ICondition)criteria, options, cancellation);

	ValueTask<int> DeleteAsync(Condition criteria, DataDeleteOptions options = null, CancellationToken cancellation = default) => this.DeleteAsync((ICondition)criteria, options, cancellation);
	ValueTask<int> DeleteAsync(Condition criteria, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default) => this.DeleteAsync((ICondition)criteria, schema, options, cancellation);
	ValueTask<int> DeleteAsync(ConditionCollection criteria, DataDeleteOptions options = null, CancellationToken cancellation = default) => this.DeleteAsync((ICondition)criteria, options, cancellation);
	ValueTask<int> DeleteAsync(ConditionCollection criteria, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default) => this.DeleteAsync((ICondition)criteria, schema, options, cancellation);

	ValueTask<int> UpdateAsync(object data, Condition criteria, DataUpdateOptions options = null, CancellationToken cancellation = default) => this.UpdateAsync(data, (ICondition)criteria, options, cancellation);
	ValueTask<int> UpdateAsync(object data, Condition criteria, string schema, DataUpdateOptions options = null, CancellationToken cancellation = default) => this.UpdateAsync(data, (ICondition)criteria, schema, options, cancellation);
	ValueTask<int> UpdateAsync(object data, ConditionCollection criteria, DataUpdateOptions options = null, CancellationToken cancellation = default) => this.UpdateAsync(data, (ICondition)criteria, options, cancellation);
	ValueTask<int> UpdateAsync(object data, ConditionCollection criteria, string schema, DataUpdateOptions options = null, CancellationToken cancellation = default) => this.UpdateAsync(data, (ICondition)criteria, schema, options, cancellation);
}
