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
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Data
{
	/// <summary>
	/// 表示数据服务的接口。
	/// </summary>
	public interface IDataService
	{
		#region 事件定义
		event EventHandler<DataExecutedEventArgs> Executed;
		event EventHandler<DataExecutingEventArgs> Executing;
		event EventHandler<DataExistedEventArgs> Existed;
		event EventHandler<DataExistingEventArgs> Existing;
		event EventHandler<DataAggregatedEventArgs> Aggregated;
		event EventHandler<DataAggregatingEventArgs> Aggregating;
		event EventHandler<DataIncrementedEventArgs> Incremented;
		event EventHandler<DataIncrementingEventArgs> Incrementing;
		event EventHandler<DataDeletedEventArgs> Deleted;
		event EventHandler<DataDeletingEventArgs> Deleting;
		event EventHandler<DataInsertedEventArgs> Inserted;
		event EventHandler<DataInsertingEventArgs> Inserting;
		event EventHandler<DataUpsertedEventArgs> Upserted;
		event EventHandler<DataUpsertingEventArgs> Upserting;
		event EventHandler<DataUpdatedEventArgs> Updated;
		event EventHandler<DataUpdatingEventArgs> Updating;
		event EventHandler<DataSelectedEventArgs> Selected;
		event EventHandler<DataSelectingEventArgs> Selecting;
		#endregion

		#region 属性定义
		/// <summary>获取数据服务的名称，该名称亦为数据访问接口的调用名。</summary>
		string Name { get; }

		/// <summary>获取一个值，指示是否支持删除操作。</summary>
		bool CanDelete { get; }

		/// <summary>获取一个值，指示是否支持新增操作。</summary>
		bool CanInsert { get; }

		/// <summary>获取一个值，指示是否支持修改操作。</summary>
		bool CanUpdate { get; }

		/// <summary>获取一个值，指示是否支持增改操作。</summary>
		bool CanUpsert { get; }

		/// <summary>获取数据服务注解。</summary>
		DataServiceAttribute Attribute { get; }

		/// <summary>获取数据访问接口。</summary>
		IDataAccess DataAccess { get; }

		/// <summary>获取数据服务验证器。</summary>
		IDataServiceValidator Validator { get; }

		/// <summary>获取服务提供程序。</summary>
		IServiceProvider ServiceProvider { get; }
		#endregion

		#region 执行方法
		IEnumerable<T> Execute<T>(string name, IDictionary<string, object> inParameters, IDataExecuteOptions options = null);
		IEnumerable<T> Execute<T>(string name, IDictionary<string, object> inParameters, out IDictionary<string, object> outParameters, IDataExecuteOptions options = null);

		object ExecuteScalar(string name, IDictionary<string, object> inParameters, IDataExecuteOptions options = null);
		object ExecuteScalar(string name, IDictionary<string, object> inParameters, out IDictionary<string, object> outParameters, IDataExecuteOptions options = null);
		#endregion

		#region 存在方法
		bool Exists(string key, IDataExistsOptions options = null);
		bool Exists<TKey1>(TKey1 key1, IDataExistsOptions options = null) where TKey1 : IEquatable<TKey1>;
		bool Exists<TKey1, TKey2>(TKey1 key1, TKey2 key2, IDataExistsOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		bool Exists<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, IDataExistsOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		bool Exists<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, IDataExistsOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		bool Exists<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, IDataExistsOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;

		bool Exists(ICondition criteria, IDataExistsOptions options = null);
		#endregion

		#region 聚合方法
		int Count(string key, string member = null, IDataAggregateOptions options = null);
		int Count<TKey1>(TKey1 key1, string member = null, IDataAggregateOptions options = null) where TKey1 : IEquatable<TKey1>;
		int Count<TKey1, TKey2>(TKey1 key1, TKey2 key2, string member = null, IDataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		int Count<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string member = null, IDataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		int Count<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string member = null, IDataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		int Count<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string member = null, IDataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		int Count(ICondition criteria = null, string member = null, IDataAggregateOptions options = null);

		double? Aggregate(DataAggregateFunction function, string member, string key, IDataAggregateOptions options = null);
		double? Aggregate<TKey1>(DataAggregateFunction function, string member, TKey1 key1, IDataAggregateOptions options = null) where TKey1 : IEquatable<TKey1>;
		double? Aggregate<TKey1, TKey2>(DataAggregateFunction function, string member, TKey1 key1, TKey2 key2, IDataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		double? Aggregate<TKey1, TKey2, TKey3>(DataAggregateFunction function, string member, TKey1 key1, TKey2 key2, TKey3 key3, IDataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		double? Aggregate<TKey1, TKey2, TKey3, TKey4>(DataAggregateFunction function, string member, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, IDataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		double? Aggregate<TKey1, TKey2, TKey3, TKey4, TKey5>(DataAggregateFunction function, string member, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, IDataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		double? Aggregate(DataAggregateFunction function, string member, ICondition criteria = null, IDataAggregateOptions options = null);
		#endregion

		#region 递增方法
		long Increment(string member, ICondition criteria, IDataIncrementOptions options);
		long Increment(string member, ICondition criteria, int interval = 1, IDataIncrementOptions options = null);

		long Decrement(string member, ICondition criteria, IDataIncrementOptions options);
		long Decrement(string member, ICondition criteria, int interval = 1, IDataIncrementOptions options = null);
		#endregion

		#region 删除方法
		int Delete(string key, IDataDeleteOptions options = null);
		int Delete(string key, string schema, IDataDeleteOptions options = null);

		int Delete<TKey1>(TKey1 key1, IDataDeleteOptions options = null) where TKey1 : IEquatable<TKey1>;
		int Delete<TKey1>(TKey1 key1, string schema, IDataDeleteOptions options = null) where TKey1 : IEquatable<TKey1>;
		int Delete<TKey1, TKey2>(TKey1 key1, TKey2 key2, IDataDeleteOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		int Delete<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, IDataDeleteOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		int Delete<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, IDataDeleteOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		int Delete<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, IDataDeleteOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		int Delete<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, IDataDeleteOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		int Delete<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, IDataDeleteOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		int Delete<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, IDataDeleteOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		int Delete<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, IDataDeleteOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;

		int Delete(ICondition criteria, IDataDeleteOptions options = null);
		int Delete(ICondition criteria, string schema, IDataDeleteOptions options = null);
		#endregion

		#region 插入方法
		int Insert(object data, IDataInsertOptions options = null);
		int Insert(object data, string schema, IDataInsertOptions options = null);

		int InsertMany(IEnumerable items, IDataInsertOptions options = null);
		int InsertMany(IEnumerable items, string schema, IDataInsertOptions options = null);
		#endregion

		#region 复写方法
		int Upsert(object data, IDataUpsertOptions options = null);
		int Upsert(object data, string schema, IDataUpsertOptions options = null);

		int UpsertMany(IEnumerable items, IDataUpsertOptions options = null);
		int UpsertMany(IEnumerable items, string schema, IDataUpsertOptions options = null);
		#endregion

		#region 更新方法
		int Update(string key, object data, IDataUpdateOptions options = null);
		int Update(string key, object data, string schema, IDataUpdateOptions options = null);

		int Update<TKey1>(TKey1 key1, object data, IDataUpdateOptions options = null) where TKey1 : IEquatable<TKey1>;
		int Update<TKey1>(TKey1 key1, string schema, object data, IDataUpdateOptions options = null) where TKey1 : IEquatable<TKey1>;
		int Update<TKey1, TKey2>(TKey1 key1, TKey2 key2, object data, IDataUpdateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		int Update<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, object data, IDataUpdateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		int Update<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, object data, IDataUpdateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		int Update<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, object data, IDataUpdateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		int Update<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, object data, IDataUpdateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		int Update<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, object data, IDataUpdateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		int Update<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, object data, IDataUpdateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		int Update<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, object data, IDataUpdateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;

		int Update(object data, IDataUpdateOptions options = null);
		int Update(object data, string schema, IDataUpdateOptions options = null);
		int Update(object data, ICondition criteria, IDataUpdateOptions options = null);
		int Update(object data, ICondition criteria, string schema, IDataUpdateOptions options = null);

		int UpdateMany(IEnumerable items, IDataUpdateOptions options = null);
		int UpdateMany(IEnumerable items, string schema, IDataUpdateOptions options = null);
		#endregion

		#region 查询方法
		object Get(string key, params Sorting[] sortings);
		object Get(string key, IDataSelectOptions options, params Sorting[] sortings);
		object Get(string key, Paging paging, params Sorting[] sortings);
		object Get(string key, Paging paging, IDataSelectOptions options, params Sorting[] sortings);
		object Get(string key, string schema, params Sorting[] sortings);
		object Get(string key, string schema, IDataSelectOptions options, params Sorting[] sortings);
		object Get(string key, string schema, Paging paging, params Sorting[] sortings);
		object Get(string key, string schema, Paging paging, IDataSelectOptions options, params Sorting[] sortings);

		object Get<TKey1>(TKey1 key1, params Sorting[] sortings) where TKey1 : IEquatable<TKey1>;
		object Get<TKey1>(TKey1 key1, IDataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1>;
		object Get<TKey1>(TKey1 key1, Paging paging, params Sorting[] sortings) where TKey1 : IEquatable<TKey1>;
		object Get<TKey1>(TKey1 key1, Paging paging, IDataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1>;
		object Get<TKey1>(TKey1 key1, string schema, params Sorting[] sortings) where TKey1 : IEquatable<TKey1>;
		object Get<TKey1>(TKey1 key1, string schema, IDataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1>;
		object Get<TKey1>(TKey1 key1, string schema, Paging paging, params Sorting[] sortings) where TKey1 : IEquatable<TKey1>;
		object Get<TKey1>(TKey1 key1, string schema, Paging paging, IDataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1>;

		object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, IDataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, Paging paging, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, Paging paging, IDataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, IDataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Paging paging, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Paging paging, IDataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;

		object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, IDataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, Paging paging, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, Paging paging, IDataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, IDataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Paging paging, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Paging paging, IDataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;

		object Get<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		object Get<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, IDataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		object Get<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, Paging paging, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		object Get<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, Paging paging, IDataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		object Get<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		object Get<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, IDataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		object Get<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, Paging paging, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		object Get<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, Paging paging, IDataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;

		object Get<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		object Get<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, IDataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		object Get<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, Paging paging, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		object Get<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, Paging paging, IDataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		object Get<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		object Get<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, IDataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		object Get<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, Paging paging, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		object Get<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, Paging paging, IDataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;

		IEnumerable Select(IDataSelectOptions options = null, params Sorting[] sortings);
		IEnumerable Select(ICondition criteria, params Sorting[] sortings);
		IEnumerable Select(ICondition criteria, IDataSelectOptions options, params Sorting[] sortings);
		IEnumerable Select(ICondition criteria, Paging paging, params Sorting[] sortings);
		IEnumerable Select(ICondition criteria, Paging paging, IDataSelectOptions options, params Sorting[] sortings);
		IEnumerable Select(ICondition criteria, string schema, params Sorting[] sortings);
		IEnumerable Select(ICondition criteria, string schema, IDataSelectOptions options, params Sorting[] sortings);
		IEnumerable Select(ICondition criteria, string schema, Paging paging, params Sorting[] sortings);
		IEnumerable Select(ICondition criteria, string schema, Paging paging, IDataSelectOptions options, params Sorting[] sortings);

		IEnumerable<T> Select<T>(Grouping grouping, params Sorting[] sortings);
		IEnumerable<T> Select<T>(Grouping grouping, IDataSelectOptions options, params Sorting[] sortings);
		IEnumerable<T> Select<T>(Grouping grouping, Paging paging, params Sorting[] sortings);
		IEnumerable<T> Select<T>(Grouping grouping, Paging paging, IDataSelectOptions options, params Sorting[] sortings);
		IEnumerable<T> Select<T>(Grouping grouping, string schema, params Sorting[] sortings);
		IEnumerable<T> Select<T>(Grouping grouping, string schema, IDataSelectOptions options, params Sorting[] sortings);
		IEnumerable<T> Select<T>(Grouping grouping, string schema, Paging paging, params Sorting[] sortings);
		IEnumerable<T> Select<T>(Grouping grouping, string schema, Paging paging, IDataSelectOptions options, params Sorting[] sortings);
		IEnumerable<T> Select<T>(Grouping grouping, ICondition criteria, Paging paging, params Sorting[] sortings);
		IEnumerable<T> Select<T>(Grouping grouping, ICondition criteria, string schema = null, params Sorting[] sortings);
		IEnumerable<T> Select<T>(Grouping grouping, ICondition criteria, string schema, IDataSelectOptions options, params Sorting[] sortings);
		IEnumerable<T> Select<T>(Grouping grouping, ICondition criteria, string schema, Paging paging, params Sorting[] sortings);
		IEnumerable<T> Select<T>(Grouping grouping, ICondition criteria, string schema, Paging paging, IDataSelectOptions options, params Sorting[] sortings);
		#endregion
	}
}
