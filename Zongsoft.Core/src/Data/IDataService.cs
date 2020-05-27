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
		event EventHandler<DataCountedEventArgs> Counted;
		event EventHandler<DataCountingEventArgs> Counting;
		event EventHandler<DataExecutedEventArgs> Executed;
		event EventHandler<DataExecutingEventArgs> Executing;
		event EventHandler<DataExistedEventArgs> Existed;
		event EventHandler<DataExistingEventArgs> Existing;
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
		/// <summary>
		/// 获取数据服务的名称，该名称亦为数据访问接口的调用名。
		/// </summary>
		string Name { get; }

		/// <summary>
		/// 获取一个值，指示是否支持删除操作。
		/// </summary>
		bool CanDelete { get; }

		/// <summary>
		/// 获取一个值，指示是否支持新增操作。
		/// </summary>
		bool CanInsert { get; }

		/// <summary>
		/// 获取一个值，指示是否支持修改操作。
		/// </summary>
		bool CanUpdate { get; }

		/// <summary>
		/// 获取数据访问接口。
		/// </summary>
		IDataAccess DataAccess { get; }
		#endregion

		#region 执行方法
		IEnumerable<T> Execute<T>(string name, IDictionary<string, object> inParameters, IDictionary<string, object> states = null);
		IEnumerable<T> Execute<T>(string name, IDictionary<string, object> inParameters, out IDictionary<string, object> outParameters, IDictionary<string, object> states = null);

		object ExecuteScalar(string name, IDictionary<string, object> inParameters, IDictionary<string, object> states = null);
		object ExecuteScalar(string name, IDictionary<string, object> inParameters, out IDictionary<string, object> outParameters, IDictionary<string, object> states = null);
		#endregion

		#region 存在方法
		bool Exists(ICondition condition, IDictionary<string, object> states = null);

		bool Exists<TKey>(TKey key, IDictionary<string, object> states = null);
		bool Exists<TKey1, TKey2>(TKey1 key1, TKey2 key2, IDictionary<string, object> states = null);
		bool Exists<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, IDictionary<string, object> states = null);
		#endregion

		#region 计数方法
		int Count(ICondition condition, IDictionary<string, object> states);
		int Count(ICondition condition, string member);
		int Count(ICondition condition, string member = null, IDictionary<string, object> states = null);

		int Count<TKey>(TKey key, string member = null, IDictionary<string, object> states = null);
		int Count<TKey1, TKey2>(TKey1 key1, TKey2 key2, string member = null, IDictionary<string, object> states = null);
		int Count<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string member = null, IDictionary<string, object> states = null);
		#endregion

		#region 递增方法
		long Increment(string member, ICondition condition, IDictionary<string, object> states);
		long Increment(string member, ICondition condition, int interval);
		long Increment(string member, ICondition condition, int interval = 1, IDictionary<string, object> states = null);

		long Decrement(string member, ICondition condition, IDictionary<string, object> states);
		long Decrement(string member, ICondition condition, int interval);
		long Decrement(string member, ICondition condition, int interval = 1, IDictionary<string, object> states = null);
		#endregion

		#region 删除方法
		int Delete<TKey>(TKey key, string schema = null);
		int Delete<TKey>(TKey key, string schema, IDictionary<string, object> states);
		int Delete<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema = null);
		int Delete<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, IDictionary<string, object> states);
		int Delete<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema = null);
		int Delete<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, IDictionary<string, object> states);

		int Delete(ICondition condition, string schema = null);
		int Delete(ICondition condition, string schema, IDictionary<string, object> states);
		#endregion

		#region 插入方法
		int Insert(object data);
		int Insert(object data, IDictionary<string, object> states);
		int Insert(object data, string schema);
		int Insert(object data, string schema, IDictionary<string, object> states);

		int InsertMany(IEnumerable items);
		int InsertMany(IEnumerable items, IDictionary<string, object> states);
		int InsertMany(IEnumerable items, string schema);
		int InsertMany(IEnumerable items, string schema, IDictionary<string, object> states);
		#endregion

		#region 复写方法
		int Upsert(object data);
		int Upsert(object data, IDictionary<string, object> states);
		int Upsert(object data, string schema);
		int Upsert(object data, string schema, IDictionary<string, object> states);

		int UpsertMany(IEnumerable items);
		int UpsertMany(IEnumerable items, IDictionary<string, object> states);
		int UpsertMany(IEnumerable items, string schema);
		int UpsertMany(IEnumerable items, string schema, IDictionary<string, object> states);
		#endregion

		#region 更新方法
		int Update<TKey>(object data, TKey key, IDictionary<string, object> states = null);
		int Update<TKey>(object data, TKey key, string schema, IDictionary<string, object> states = null);
		int Update<TKey1, TKey2>(object data, TKey1 key1, TKey2 key2, IDictionary<string, object> states = null);
		int Update<TKey1, TKey2>(object data, TKey1 key1, TKey2 key2, string schema, IDictionary<string, object> states = null);
		int Update<TKey1, TKey2, TKey3>(object data, TKey1 key1, TKey2 key2, TKey3 key3, IDictionary<string, object> states = null);
		int Update<TKey1, TKey2, TKey3>(object data, TKey1 key1, TKey2 key2, TKey3 key3, string schema, IDictionary<string, object> states = null);

		int Update(object data, IDictionary<string, object> states = null);
		int Update(object data, string schema, IDictionary<string, object> states = null);
		int Update(object data, ICondition condition, IDictionary<string, object> states = null);
		int Update(object data, ICondition condition, string schema, IDictionary<string, object> states = null);

		int UpdateMany(IEnumerable items, IDictionary<string, object> states = null);
		int UpdateMany(IEnumerable items, string schema, IDictionary<string, object> states = null);
		#endregion

		#region 查询方法
		object Get<TKey>(TKey key, params Sorting[] sortings);
		object Get<TKey>(TKey key, IDictionary<string, object> states, params Sorting[] sortings);
		object Get<TKey>(TKey key, Paging paging, params Sorting[] sortings);
		object Get<TKey>(TKey key, string schema, params Sorting[] sortings);
		object Get<TKey>(TKey key, string schema, IDictionary<string, object> states, params Sorting[] sortings);
		object Get<TKey>(TKey key, string schema, Paging paging, params Sorting[] sortings);
		object Get<TKey>(TKey key, string schema, Paging paging, IDictionary<string, object> states, params Sorting[] sortings);
		object Get<TKey>(TKey key, string schema, Paging paging, IDictionary<string, object> states, out IPageable pageable, params Sorting[] sortings);

		object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, params Sorting[] sortings);
		object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, IDictionary<string, object> states, params Sorting[] sortings);
		object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, Paging paging, params Sorting[] sortings);
		object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, params Sorting[] sortings);
		object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, IDictionary<string, object> states, params Sorting[] sortings);
		object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Paging paging, params Sorting[] sortings);
		object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Paging paging, IDictionary<string, object> states, params Sorting[] sortings);
		object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Paging paging, IDictionary<string, object> states, out IPageable pageable, params Sorting[] sortings);

		object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, params Sorting[] sortings);
		object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, IDictionary<string, object> states, params Sorting[] sortings);
		object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, Paging paging, params Sorting[] sortings);
		object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, params Sorting[] sortings);
		object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, IDictionary<string, object> states, params Sorting[] sortings);
		object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Paging paging, params Sorting[] sortings);
		object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Paging paging, IDictionary<string, object> states, params Sorting[] sortings);
		object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Paging paging, IDictionary<string, object> states, out IPageable pageable, params Sorting[] sortings);

		IEnumerable Select(IDictionary<string, object> states = null, params Sorting[] sortings);
		IEnumerable Select(ICondition condition, params Sorting[] sortings);
		IEnumerable Select(ICondition condition, IDictionary<string, object> states, params Sorting[] sortings);
		IEnumerable Select(ICondition condition, Paging paging, params Sorting[] sortings);
		IEnumerable Select(ICondition condition, Paging paging, IDictionary<string, object> states, params Sorting[] sortings);
		IEnumerable Select(ICondition condition, string schema, params Sorting[] sortings);
		IEnumerable Select(ICondition condition, string schema, IDictionary<string, object> states, params Sorting[] sortings);
		IEnumerable Select(ICondition condition, string schema, Paging paging, params Sorting[] sortings);
		IEnumerable Select(ICondition condition, string schema, Paging paging, IDictionary<string, object> states, params Sorting[] sortings);

		IEnumerable<T> Select<T>(Grouping grouping, params Sorting[] sortings);
		IEnumerable<T> Select<T>(Grouping grouping, IDictionary<string, object> states, params Sorting[] sortings);
		IEnumerable<T> Select<T>(Grouping grouping, Paging paging, IDictionary<string, object> states = null, params Sorting[] sortings);
		IEnumerable<T> Select<T>(Grouping grouping, string schema, params Sorting[] sortings);
		IEnumerable<T> Select<T>(Grouping grouping, string schema, IDictionary<string, object> states, params Sorting[] sortings);
		IEnumerable<T> Select<T>(Grouping grouping, string schema, Paging paging, IDictionary<string, object> states = null, params Sorting[] sortings);
		IEnumerable<T> Select<T>(Grouping grouping, ICondition condition, string schema = null, params Sorting[] sortings);
		IEnumerable<T> Select<T>(Grouping grouping, ICondition condition, string schema, IDictionary<string, object> states, params Sorting[] sortings);
		IEnumerable<T> Select<T>(Grouping grouping, ICondition condition, string schema, Paging paging, IDictionary<string, object> states = null, params Sorting[] sortings);
		#endregion
	}
}
