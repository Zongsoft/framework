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
	public partial interface IDataService
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

		/// <summary>获取当前服务的安全主体。</summary>
		System.Security.Claims.ClaimsPrincipal Principal { get; }

		/// <summary>获取数据服务注解。</summary>
		DataServiceAttribute Attribute { get; }

		/// <summary>获取父数据服务。</summary>
		IDataService Service { get; }

		/// <summary>获取数据访问接口。</summary>
		IDataAccess DataAccess { get; }

		/// <summary>获取数据服务验证器。</summary>
		IDataServiceValidator Validator { get; }

		/// <summary>获取服务提供程序。</summary>
		IServiceProvider ServiceProvider { get; }
		#endregion

		#region 执行方法
		IEnumerable<T> Execute<T>(string name, IDictionary<string, object> inParameters, DataExecuteOptions options = null);
		IEnumerable<T> Execute<T>(string name, IDictionary<string, object> inParameters, out IDictionary<string, object> outParameters, DataExecuteOptions options = null);

		object ExecuteScalar(string name, IDictionary<string, object> inParameters, DataExecuteOptions options = null);
		object ExecuteScalar(string name, IDictionary<string, object> inParameters, out IDictionary<string, object> outParameters, DataExecuteOptions options = null);
		#endregion

		#region 存在方法
		bool Exists(string key, DataExistsOptions options = null);
		bool Exists<TKey1>(TKey1 key1, DataExistsOptions options = null) where TKey1 : IEquatable<TKey1>;
		bool Exists<TKey1, TKey2>(TKey1 key1, TKey2 key2, DataExistsOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		bool Exists<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, DataExistsOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		bool Exists<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataExistsOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		bool Exists<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataExistsOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;

		bool Exists(ICondition criteria, DataExistsOptions options = null);
		#endregion

		#region 聚合方法
		int Count(string key, string member = null, DataAggregateOptions options = null);
		int Count<TKey1>(TKey1 key1, string member = null, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1>;
		int Count<TKey1, TKey2>(TKey1 key1, TKey2 key2, string member = null, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		int Count<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string member = null, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		int Count<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string member = null, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		int Count<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string member = null, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		int Count(ICondition criteria = null, string member = null, DataAggregateOptions options = null);

		TValue? Aggregate<TValue>(DataAggregateFunction function, string member, string key, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue>;
		TValue? Aggregate<TKey1, TValue>(DataAggregateFunction function, string member, TKey1 key1, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TValue : struct, IEquatable<TValue>;
		TValue? Aggregate<TKey1, TKey2, TValue>(DataAggregateFunction function, string member, TKey1 key1, TKey2 key2, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TValue : struct, IEquatable<TValue>;
		TValue? Aggregate<TKey1, TKey2, TKey3, TValue>(DataAggregateFunction function, string member, TKey1 key1, TKey2 key2, TKey3 key3, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TValue : struct, IEquatable<TValue>;
		TValue? Aggregate<TKey1, TKey2, TKey3, TKey4, TValue>(DataAggregateFunction function, string member, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TValue : struct, IEquatable<TValue>;
		TValue? Aggregate<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(DataAggregateFunction function, string member, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataAggregateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5> where TValue : struct, IEquatable<TValue>;
		TValue? Aggregate<TValue>(DataAggregateFunction function, string member, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue>;
		#endregion

		#region 递增方法
		long Increment(string member, ICondition criteria, DataIncrementOptions options);
		long Increment(string member, ICondition criteria, int interval = 1, DataIncrementOptions options = null);

		long Decrement(string member, ICondition criteria, DataIncrementOptions options);
		long Decrement(string member, ICondition criteria, int interval = 1, DataIncrementOptions options = null);
		#endregion

		#region 删除方法
		int Delete(string key, DataDeleteOptions options = null);
		int Delete(string key, string schema, DataDeleteOptions options = null);

		int Delete<TKey1>(TKey1 key1, DataDeleteOptions options = null) where TKey1 : IEquatable<TKey1>;
		int Delete<TKey1>(TKey1 key1, string schema, DataDeleteOptions options = null) where TKey1 : IEquatable<TKey1>;
		int Delete<TKey1, TKey2>(TKey1 key1, TKey2 key2, DataDeleteOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		int Delete<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, DataDeleteOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		int Delete<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, DataDeleteOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		int Delete<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, DataDeleteOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		int Delete<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataDeleteOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		int Delete<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, DataDeleteOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		int Delete<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataDeleteOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		int Delete<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, DataDeleteOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;

		int Delete(ICondition criteria, DataDeleteOptions options = null);
		int Delete(ICondition criteria, string schema, DataDeleteOptions options = null);
		#endregion

		#region 插入方法
		int Insert(object data, DataInsertOptions options = null);
		int Insert(object data, string schema, DataInsertOptions options = null);

		int InsertMany(IEnumerable items, DataInsertOptions options = null);
		int InsertMany(IEnumerable items, string schema, DataInsertOptions options = null);
		#endregion

		#region 增改方法
		int Upsert(object data, DataUpsertOptions options = null);
		int Upsert(object data, string schema, DataUpsertOptions options = null);

		int UpsertMany(IEnumerable items, DataUpsertOptions options = null);
		int UpsertMany(IEnumerable items, string schema, DataUpsertOptions options = null);

		int UpsertMany(string key, IEnumerable items, DataUpsertOptions options = null);
		int UpsertMany(string key, IEnumerable items, bool reset, DataUpsertOptions options = null);
		int UpsertMany(string key, IEnumerable items, string schema, DataUpsertOptions options = null);
		int UpsertMany(string key, IEnumerable items, string schema, bool reset, DataUpsertOptions options = null);

		int UpsertMany<TKey1>(TKey1 key1, IEnumerable items, DataUpsertOptions options = null) where TKey1 : IEquatable<TKey1>;
		int UpsertMany<TKey1>(TKey1 key1, IEnumerable items, bool reset, DataUpsertOptions options = null) where TKey1 : IEquatable<TKey1>;
		int UpsertMany<TKey1>(TKey1 key1, IEnumerable items, string schema, DataUpsertOptions options = null) where TKey1 : IEquatable<TKey1>;
		int UpsertMany<TKey1>(TKey1 key1, IEnumerable items, string schema, bool reset, DataUpsertOptions options = null) where TKey1 : IEquatable<TKey1>;

		int UpsertMany<TKey1, TKey2>(TKey1 key1, TKey2 key2, IEnumerable items, DataUpsertOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		int UpsertMany<TKey1, TKey2>(TKey1 key1, TKey2 key2, IEnumerable items, bool reset, DataUpsertOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		int UpsertMany<TKey1, TKey2>(TKey1 key1, TKey2 key2, IEnumerable items, string schema, DataUpsertOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		int UpsertMany<TKey1, TKey2>(TKey1 key1, TKey2 key2, IEnumerable items, string schema, bool reset, DataUpsertOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;

		int UpsertMany<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, IEnumerable items, DataUpsertOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		int UpsertMany<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, IEnumerable items, bool reset, DataUpsertOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		int UpsertMany<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, IEnumerable items, string schema, DataUpsertOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		int UpsertMany<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, IEnumerable items, string schema, bool reset, DataUpsertOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;

		int UpsertMany<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, IEnumerable items, DataUpsertOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		int UpsertMany<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, IEnumerable items, bool reset, DataUpsertOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		int UpsertMany<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, IEnumerable items, string schema, DataUpsertOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		int UpsertMany<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, IEnumerable items, string schema, bool reset, DataUpsertOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;

		int UpsertMany<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, IEnumerable items, DataUpsertOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		int UpsertMany<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, IEnumerable items, bool reset, DataUpsertOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		int UpsertMany<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, IEnumerable items, string schema, DataUpsertOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		int UpsertMany<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, IEnumerable items, string schema, bool reset, DataUpsertOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		#endregion

		#region 更新方法
		int Update(string key, object data, DataUpdateOptions options = null);
		int Update(string key, object data, string schema, DataUpdateOptions options = null);

		int Update<TKey1>(TKey1 key1, object data, DataUpdateOptions options = null) where TKey1 : IEquatable<TKey1>;
		int Update<TKey1>(TKey1 key1, string schema, object data, DataUpdateOptions options = null) where TKey1 : IEquatable<TKey1>;
		int Update<TKey1, TKey2>(TKey1 key1, TKey2 key2, object data, DataUpdateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		int Update<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, object data, DataUpdateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		int Update<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, object data, DataUpdateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		int Update<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, object data, DataUpdateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		int Update<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, object data, DataUpdateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		int Update<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, object data, DataUpdateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		int Update<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, object data, DataUpdateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		int Update<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, object data, DataUpdateOptions options = null) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;

		int Update(object data, DataUpdateOptions options = null);
		int Update(object data, string schema, DataUpdateOptions options = null);
		int Update(object data, ICondition criteria, DataUpdateOptions options = null);
		int Update(object data, ICondition criteria, string schema, DataUpdateOptions options = null);

		int UpdateMany(IEnumerable items, DataUpdateOptions options = null);
		int UpdateMany(IEnumerable items, string schema, DataUpdateOptions options = null);
		#endregion

		#region 查询方法
		object Get(string key, params Sorting[] sortings);
		object Get(string key, DataSelectOptions options, params Sorting[] sortings);
		object Get(string key, Paging paging, params Sorting[] sortings);
		object Get(string key, Paging paging, DataSelectOptions options, params Sorting[] sortings);
		object Get(string key, string schema, params Sorting[] sortings);
		object Get(string key, string schema, DataSelectOptions options, params Sorting[] sortings);
		object Get(string key, string schema, Paging paging, params Sorting[] sortings);
		object Get(string key, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings);

		object Get<TKey1>(TKey1 key1, params Sorting[] sortings) where TKey1 : IEquatable<TKey1>;
		object Get<TKey1>(TKey1 key1, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1>;
		object Get<TKey1>(TKey1 key1, Paging paging, params Sorting[] sortings) where TKey1 : IEquatable<TKey1>;
		object Get<TKey1>(TKey1 key1, Paging paging, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1>;
		object Get<TKey1>(TKey1 key1, string schema, params Sorting[] sortings) where TKey1 : IEquatable<TKey1>;
		object Get<TKey1>(TKey1 key1, string schema, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1>;
		object Get<TKey1>(TKey1 key1, string schema, Paging paging, params Sorting[] sortings) where TKey1 : IEquatable<TKey1>;
		object Get<TKey1>(TKey1 key1, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1>;

		object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, Paging paging, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, Paging paging, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Paging paging, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;
		object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2>;

		object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, Paging paging, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, Paging paging, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Paging paging, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;
		object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3>;

		object Get<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		object Get<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		object Get<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, Paging paging, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		object Get<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, Paging paging, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		object Get<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		object Get<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		object Get<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, Paging paging, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;
		object Get<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4>;

		object Get<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		object Get<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		object Get<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, Paging paging, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		object Get<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, Paging paging, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		object Get<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		object Get<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		object Get<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, Paging paging, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;
		object Get<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5>;

		IEnumerable Select(DataSelectOptions options = null, params Sorting[] sortings);
		IEnumerable Select(ICondition criteria, params Sorting[] sortings);
		IEnumerable Select(ICondition criteria, DataSelectOptions options, params Sorting[] sortings);
		IEnumerable Select(ICondition criteria, Paging paging, params Sorting[] sortings);
		IEnumerable Select(ICondition criteria, Paging paging, DataSelectOptions options, params Sorting[] sortings);
		IEnumerable Select(ICondition criteria, string schema, params Sorting[] sortings);
		IEnumerable Select(ICondition criteria, string schema, DataSelectOptions options, params Sorting[] sortings);
		IEnumerable Select(ICondition criteria, string schema, Paging paging, params Sorting[] sortings);
		IEnumerable Select(ICondition criteria, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings);

		IEnumerable<T> Select<T>(Grouping grouping, params Sorting[] sortings);
		IEnumerable<T> Select<T>(Grouping grouping, DataSelectOptions options, params Sorting[] sortings);
		IEnumerable<T> Select<T>(Grouping grouping, Paging paging, params Sorting[] sortings);
		IEnumerable<T> Select<T>(Grouping grouping, Paging paging, DataSelectOptions options, params Sorting[] sortings);
		IEnumerable<T> Select<T>(Grouping grouping, string schema, params Sorting[] sortings);
		IEnumerable<T> Select<T>(Grouping grouping, string schema, DataSelectOptions options, params Sorting[] sortings);
		IEnumerable<T> Select<T>(Grouping grouping, string schema, Paging paging, params Sorting[] sortings);
		IEnumerable<T> Select<T>(Grouping grouping, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings);
		IEnumerable<T> Select<T>(Grouping grouping, ICondition criteria, Paging paging, params Sorting[] sortings);
		IEnumerable<T> Select<T>(Grouping grouping, ICondition criteria, string schema = null, params Sorting[] sortings);
		IEnumerable<T> Select<T>(Grouping grouping, ICondition criteria, string schema, DataSelectOptions options, params Sorting[] sortings);
		IEnumerable<T> Select<T>(Grouping grouping, ICondition criteria, string schema, Paging paging, params Sorting[] sortings);
		IEnumerable<T> Select<T>(Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings);
		#endregion
	}

	/*
	 * 本接口是为了处理相关方法的泛型条件重载版本的条件参数类型匹配错误的问题。
	 */
	public partial interface IDataService
	{
		bool Exists(Condition criteria, DataExistsOptions options = null) => this.Exists((ICondition)criteria, options);
		bool Exists(ConditionCollection criteria, DataExistsOptions options = null) => this.Exists((ICondition)criteria, options);

		int Count(Condition criteria = null, string member = null, DataAggregateOptions options = null) => this.Count((ICondition)criteria, member, options);
		int Count(ConditionCollection criteria = null, string member = null, DataAggregateOptions options = null) => this.Count((ICondition)criteria, member, options);

		TValue? Aggregate<TValue>(DataAggregateFunction function, string member, Condition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => this.Aggregate<TValue>(function, member, (ICondition)criteria, options);
		TValue? Aggregate<TValue>(DataAggregateFunction function, string member, ConditionCollection criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => this.Aggregate<TValue>(function, member, (ICondition)criteria, options);

		int Delete(Condition criteria, DataDeleteOptions options = null) => this.Delete((ICondition)criteria, options);
		int Delete(Condition criteria, string schema, DataDeleteOptions options = null) => this.Delete((ICondition)criteria, schema, options);
		int Delete(ConditionCollection criteria, DataDeleteOptions options = null) => this.Delete((ICondition)criteria, options);
		int Delete(ConditionCollection criteria, string schema, DataDeleteOptions options = null) => this.Delete((ICondition)criteria, schema, options);

		int Update(object data, Condition criteria, DataUpdateOptions options = null) => this.Update(data, (ICondition)criteria, options);
		int Update(object data, Condition criteria, string schema, DataUpdateOptions options = null) => this.Update(data, (ICondition)criteria, schema, options);
		int Update(object data, ConditionCollection criteria, DataUpdateOptions options = null) => this.Update(data, (ICondition)criteria, options);
		int Update(object data, ConditionCollection criteria, string schema, DataUpdateOptions options = null) => this.Update(data, (ICondition)criteria, schema, options);
	}
}
