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
 * Copyright (C) 2010-2022 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Data
{
	public partial interface IDataService
	{
		#region 执行方法
		Task<IEnumerable<T>> ExecuteAsync<T>(string name, IDictionary<string, object> inParameters, DataExecuteOptions options = null, CancellationToken cancellation = default);
		Task<object> ExecuteScalarAsync(string name, IDictionary<string, object> inParameters, DataExecuteOptions options = null, CancellationToken cancellation = default);
		#endregion

		#region 存在方法
		Task<bool> ExistsAsync(string key, DataExistsOptions options = null, CancellationToken cancellation = default);
		Task<bool> ExistsAsync<TKey1>(TKey1 key1, DataExistsOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1>;
		Task<bool> ExistsAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, DataExistsOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		Task<bool> ExistsAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, DataExistsOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		Task<bool> ExistsAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataExistsOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		Task<bool> ExistsAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataExistsOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		Task<bool> ExistsAsync(ICondition criteria, DataExistsOptions options = null, CancellationToken cancellation = default);
		#endregion

		#region 聚合方法
		Task<int> CountAsync(string key, string member = null, DataAggregateOptions options = null, CancellationToken cancellation = default);
		Task<int> CountAsync<TKey1>(TKey1 key1, string member = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1>;
		Task<int> CountAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string member = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		Task<int> CountAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string member = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		Task<int> CountAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string member = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		Task<int> CountAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string member = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		Task<int> CountAsync(ICondition criteria = null, string member = null, DataAggregateOptions options = null, CancellationToken cancellation = default);

		Task<TValue?> AggregateAsync<TValue>(DataAggregateFunction function, string member, string key, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue>;
		Task<TValue?> AggregateAsync<TKey1, TValue>(DataAggregateFunction function, string member, TKey1 key1, DataAggregateOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TValue : struct, IEquatable<TValue>;
		Task<TValue?> AggregateAsync<TKey1, TKey2, TValue>(DataAggregateFunction function, string member, TKey1 key1, TKey2 key2, DataAggregateOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TValue : struct, IEquatable<TValue>;
		Task<TValue?> AggregateAsync<TKey1, TKey2, TKey3, TValue>(DataAggregateFunction function, string member, TKey1 key1, TKey2 key2, TKey3 key3, DataAggregateOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TValue : struct, IEquatable<TValue>;
		Task<TValue?> AggregateAsync<TKey1, TKey2, TKey3, TKey4, TValue>(DataAggregateFunction function, string member, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataAggregateOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TValue : struct, IEquatable<TValue>;
		Task<TValue?> AggregateAsync<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(DataAggregateFunction function, string member, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataAggregateOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5> where TValue : struct, IEquatable<TValue>;
		Task<TValue?> AggregateAsync<TValue>(DataAggregateFunction function, string member, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue>;
		#endregion

		#region 递增方法
		Task<long> IncrementAsync(string member, ICondition criteria, DataIncrementOptions options, CancellationToken cancellation = default);
		Task<long> IncrementAsync(string member, ICondition criteria, int interval = 1, DataIncrementOptions options = null, CancellationToken cancellation = default);

		Task<long> DecrementAsync(string member, ICondition criteria, DataIncrementOptions options, CancellationToken cancellation = default);
		Task<long> DecrementAsync(string member, ICondition criteria, int interval = 1, DataIncrementOptions options = null, CancellationToken cancellation = default);
		#endregion

		#region 删除方法
		Task<int> DeleteAsync(string key, DataDeleteOptions options = null, CancellationToken cancellation = default);
		Task<int> DeleteAsync(string key, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default);

		Task<int> DeleteAsync<TKey1>(TKey1 key1, DataDeleteOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1>;
		Task<int> DeleteAsync<TKey1>(TKey1 key1, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1>;
		Task<int> DeleteAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, DataDeleteOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		Task<int> DeleteAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		Task<int> DeleteAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, DataDeleteOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		Task<int> DeleteAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		Task<int> DeleteAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataDeleteOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		Task<int> DeleteAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		Task<int> DeleteAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataDeleteOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		Task<int> DeleteAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;

		Task<int> DeleteAsync(ICondition criteria, DataDeleteOptions options = null, CancellationToken cancellation = default);
		Task<int> DeleteAsync(ICondition criteria, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default);
		#endregion

		#region 插入方法
		Task<int> InsertAsync(object data, DataInsertOptions options = null, CancellationToken cancellation = default);
		Task<int> InsertAsync(object data, string schema, DataInsertOptions options = null, CancellationToken cancellation = default);

		Task<int> InsertManyAsync(IEnumerable items, DataInsertOptions options = null, CancellationToken cancellation = default);
		Task<int> InsertManyAsync(IEnumerable items, string schema, DataInsertOptions options = null, CancellationToken cancellation = default);

		Task<int> InsertManyAsync(string key, IEnumerable items, DataInsertOptions options = null, CancellationToken cancellation = default);
		Task<int> InsertManyAsync(string key, IEnumerable items, string schema, DataInsertOptions options = null, CancellationToken cancellation = default);
		#endregion

		#region 增改方法
		Task<int> UpsertAsync(object data, DataUpsertOptions options = null, CancellationToken cancellation = default);
		Task<int> UpsertAsync(object data, string schema, DataUpsertOptions options = null, CancellationToken cancellation = default);

		Task<int> UpsertManyAsync(IEnumerable items, DataUpsertOptions options = null, CancellationToken cancellation = default);
		Task<int> UpsertManyAsync(IEnumerable items, string schema, DataUpsertOptions options = null, CancellationToken cancellation = default);

		Task<int> UpsertManyAsync(string key, IEnumerable items, DataUpsertOptions options = null, CancellationToken cancellation = default);
		Task<int> UpsertManyAsync(string key, IEnumerable items, string schema, DataUpsertOptions options = null, CancellationToken cancellation = default);
		#endregion

		#region 更新方法
		Task<int> UpdateAsync(string key, object data, DataUpdateOptions options = null, CancellationToken cancellation = default);
		Task<int> UpdateAsync(string key, object data, string schema, DataUpdateOptions options = null, CancellationToken cancellation = default);

		Task<int> UpdateAsync<TKey1>(TKey1 key1, object data, DataUpdateOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1>;
		Task<int> UpdateAsync<TKey1>(TKey1 key1, string schema, object data, DataUpdateOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1>;
		Task<int> UpdateAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, object data, DataUpdateOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		Task<int> UpdateAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, object data, DataUpdateOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		Task<int> UpdateAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, object data, DataUpdateOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		Task<int> UpdateAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, object data, DataUpdateOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		Task<int> UpdateAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, object data, DataUpdateOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		Task<int> UpdateAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, object data, DataUpdateOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		Task<int> UpdateAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, object data, DataUpdateOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		Task<int> UpdateAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, object data, DataUpdateOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;

		Task<int> UpdateAsync(object data, DataUpdateOptions options = null, CancellationToken cancellation = default);
		Task<int> UpdateAsync(object data, string schema, DataUpdateOptions options = null, CancellationToken cancellation = default);
		Task<int> UpdateAsync(object data, ICondition criteria, DataUpdateOptions options = null, CancellationToken cancellation = default);
		Task<int> UpdateAsync(object data, ICondition criteria, string schema, DataUpdateOptions options = null, CancellationToken cancellation = default);

		Task<int> UpdateManyAsync(IEnumerable items, DataUpdateOptions options = null, CancellationToken cancellation = default);
		Task<int> UpdateManyAsync(IEnumerable items, string schema, DataUpdateOptions options = null, CancellationToken cancellation = default);

		Task<int> UpdateManyAsync(string key, IEnumerable items, DataUpdateOptions options = null, CancellationToken cancellation = default);
		Task<int> UpdateManyAsync(string key, IEnumerable items, string schema, DataUpdateOptions options = null, CancellationToken cancellation = default);
		#endregion

		#region 查询方法
		Task<object> GetAsync(string key, params Sorting[] sortings);
		Task<object> GetAsync(string key, Sorting[] sortings, CancellationToken cancellation = default);
		Task<object> GetAsync(string key, DataSelectOptions options, params Sorting[] sortings);
		Task<object> GetAsync(string key, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
		Task<object> GetAsync(string key, Paging paging, params Sorting[] sortings);
		Task<object> GetAsync(string key, Paging paging, Sorting[] sortings, CancellationToken cancellation = default);
		Task<object> GetAsync(string key, Paging paging, DataSelectOptions options, params Sorting[] sortings);
		Task<object> GetAsync(string key, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
		Task<object> GetAsync(string key, string schema, params Sorting[] sortings);
		Task<object> GetAsync(string key, string schema, Sorting[] sortings, CancellationToken cancellation = default);
		Task<object> GetAsync(string key, string schema, DataSelectOptions options, params Sorting[] sortings);
		Task<object> GetAsync(string key, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
		Task<object> GetAsync(string key, string schema, Paging paging, params Sorting[] sortings);
		Task<object> GetAsync(string key, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default);
		Task<object> GetAsync(string key, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings);
		Task<object> GetAsync(string key, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);

		Task<object> GetAsync<TKey1>(TKey1 key1, params Sorting[] sortings) where TKey1 : IEquatable<TKey1>;
		Task<object> GetAsync<TKey1>(TKey1 key1, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1>;
		Task<object> GetAsync<TKey1>(TKey1 key1, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1>;
		Task<object> GetAsync<TKey1>(TKey1 key1, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1>;
		Task<object> GetAsync<TKey1>(TKey1 key1, Paging paging, params Sorting[] sortings) where TKey1 : IEquatable<TKey1>;
		Task<object> GetAsync<TKey1>(TKey1 key1, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1>;
		Task<object> GetAsync<TKey1>(TKey1 key1, Paging paging, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1>;
		Task<object> GetAsync<TKey1>(TKey1 key1, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1>;
		Task<object> GetAsync<TKey1>(TKey1 key1, string schema, params Sorting[] sortings) where TKey1 : IEquatable<TKey1>;
		Task<object> GetAsync<TKey1>(TKey1 key1, string schema, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1>;
		Task<object> GetAsync<TKey1>(TKey1 key1, string schema, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1>;
		Task<object> GetAsync<TKey1>(TKey1 key1, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1>;
		Task<object> GetAsync<TKey1>(TKey1 key1, string schema, Paging paging, params Sorting[] sortings) where TKey1 : IEquatable<TKey1>;
		Task<object> GetAsync<TKey1>(TKey1 key1, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1>;
		Task<object> GetAsync<TKey1>(TKey1 key1, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1>;
		Task<object> GetAsync<TKey1>(TKey1 key1, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1>;

		Task<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		Task<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		Task<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		Task<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		Task<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, Paging paging, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		Task<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		Task<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, Paging paging, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		Task<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		Task<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		Task<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		Task<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		Task<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		Task<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Paging paging, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		Task<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		Task<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		Task<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;

		Task<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		Task<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		Task<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		Task<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		Task<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, Paging paging, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		Task<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		Task<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, Paging paging, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		Task<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		Task<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		Task<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		Task<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		Task<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		Task<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Paging paging, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		Task<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		Task<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		Task<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;

		Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, Paging paging, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, Paging paging, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, Paging paging, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;

		Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, Paging paging, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, Paging paging, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, Paging paging, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;

		Task<IEnumerable> SelectAsync(CancellationToken cancellation = default);
		Task<IEnumerable> SelectAsync(DataSelectOptions options, params Sorting[] sortings);
		Task<IEnumerable> SelectAsync(DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
		Task<IEnumerable> SelectAsync(ICondition criteria, params Sorting[] sortings);
		Task<IEnumerable> SelectAsync(ICondition criteria, Sorting[] sortings, CancellationToken cancellation = default);
		Task<IEnumerable> SelectAsync(ICondition criteria, DataSelectOptions options, params Sorting[] sortings);
		Task<IEnumerable> SelectAsync(ICondition criteria, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
		Task<IEnumerable> SelectAsync(ICondition criteria, Paging paging, params Sorting[] sortings);
		Task<IEnumerable> SelectAsync(ICondition criteria, Paging paging, Sorting[] sortings, CancellationToken cancellation = default);
		Task<IEnumerable> SelectAsync(ICondition criteria, Paging paging, DataSelectOptions options, params Sorting[] sortings);
		Task<IEnumerable> SelectAsync(ICondition criteria, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
		Task<IEnumerable> SelectAsync(ICondition criteria, string schema, params Sorting[] sortings);
		Task<IEnumerable> SelectAsync(ICondition criteria, string schema, Sorting[] sortings, CancellationToken cancellation = default);
		Task<IEnumerable> SelectAsync(ICondition criteria, string schema, DataSelectOptions options, params Sorting[] sortings);
		Task<IEnumerable> SelectAsync(ICondition criteria, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
		Task<IEnumerable> SelectAsync(ICondition criteria, string schema, Paging paging, params Sorting[] sortings);
		Task<IEnumerable> SelectAsync(ICondition criteria, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default);
		Task<IEnumerable> SelectAsync(ICondition criteria, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings);
		Task<IEnumerable> SelectAsync(ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);

		Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, params Sorting[] sortings);
		Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, Sorting[] sortings, CancellationToken cancellation = default);
		Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, DataSelectOptions options, params Sorting[] sortings);
		Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
		Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, Paging paging, params Sorting[] sortings);
		Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, Paging paging, Sorting[] sortings, CancellationToken cancellation = default);
		Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, Paging paging, DataSelectOptions options, params Sorting[] sortings);
		Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
		Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, string schema, params Sorting[] sortings);
		Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, string schema, Sorting[] sortings, CancellationToken cancellation = default);
		Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, string schema, DataSelectOptions options, params Sorting[] sortings);
		Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
		Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, string schema, Paging paging, params Sorting[] sortings);
		Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default);
		Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings);
		Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
		Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, ICondition criteria, Paging paging, params Sorting[] sortings);
		Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, ICondition criteria, Paging paging, Sorting[] sortings, CancellationToken cancellation = default);
		Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema = null, params Sorting[] sortings);
		Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema, Sorting[] sortings, CancellationToken cancellation = default);
		Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema, DataSelectOptions options, params Sorting[] sortings);
		Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
		Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema, Paging paging, params Sorting[] sortings);
		Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default);
		Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings);
		Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
		#endregion
	}

	/*
	 * 本接口是为了处理相关方法的泛型条件重载版本的条件参数类型匹配错误的问题。
	 */
	public partial interface IDataService
	{
		Task<bool> ExistsAsync(Condition criteria, DataExistsOptions options = null, CancellationToken cancellation = default) => this.ExistsAsync((ICondition)criteria, options, cancellation);
		Task<bool> ExistsAsync(ConditionCollection criteria, DataExistsOptions options = null, CancellationToken cancellation = default) => this.ExistsAsync((ICondition)criteria, options, cancellation);

		Task<TValue?> AggregateAsync<TValue>(DataAggregateFunction function, string member, Condition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(function, member, (ICondition)criteria, options, cancellation);
		Task<TValue?> AggregateAsync<TValue>(DataAggregateFunction function, string member, ConditionCollection criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(function, member, (ICondition)criteria, options, cancellation);

		Task<int> DeleteAsync(Condition criteria, DataDeleteOptions options = null, CancellationToken cancellation = default) => this.DeleteAsync((ICondition)criteria, options, cancellation);
		Task<int> DeleteAsync(Condition criteria, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default) => this.DeleteAsync((ICondition)criteria, schema, options, cancellation);
		Task<int> DeleteAsync(ConditionCollection criteria, DataDeleteOptions options = null, CancellationToken cancellation = default) => this.DeleteAsync((ICondition)criteria, options, cancellation);
		Task<int> DeleteAsync(ConditionCollection criteria, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default) => this.DeleteAsync((ICondition)criteria, schema, options, cancellation);

		Task<int> UpdateAsync(object data, Condition criteria, DataUpdateOptions options = null, CancellationToken cancellation = default) => this.UpdateAsync(data, (ICondition)criteria, options, cancellation);
		Task<int> UpdateAsync(object data, Condition criteria, string schema, DataUpdateOptions options = null, CancellationToken cancellation = default) => this.UpdateAsync(data, (ICondition)criteria, schema, options, cancellation);
		Task<int> UpdateAsync(object data, ConditionCollection criteria, DataUpdateOptions options = null, CancellationToken cancellation = default) => this.UpdateAsync(data, (ICondition)criteria, options, cancellation);
		Task<int> UpdateAsync(object data, ConditionCollection criteria, string schema, DataUpdateOptions options = null, CancellationToken cancellation = default) => this.UpdateAsync(data, (ICondition)criteria, schema, options, cancellation);
	}
}
