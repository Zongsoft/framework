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
	/// 表示数据访问的公共接口。
	/// </summary>
	/// <remarks>
	/// 	<para>关于“schema”查询参数的简要说明：</para>
	/// 	<para>表示要包含和排除的属性名列表，如果指定的是多个属性则属性名之间使用逗号(,)分隔；要排除的属性以感叹号(!)打头，单独一个星号(*)表示所有属性，单独一个感叹号(!)表示排除所有属性；如果未指定该参数则默认只会获取所有单值属性而不会获取导航属性。</para>
	/// </remarks>
	public interface IDataAccess
	{
		#region 事件定义
		event EventHandler<DataAccessErrorEventArgs> Error;
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

		#region 属性声明
		/// <summary>
		/// 获取数据访问的应用（子系统/业务模块）名。
		/// </summary>
		string Name { get; }

		/// <summary>
		/// 获取数据访问名映射器。
		/// </summary>
		IDataAccessNaming Naming { get; }

		/// <summary>
		/// 获取数据模式解析器。
		/// </summary>
		ISchemaParser Schema { get; }

		/// <summary>
		/// 获取或设置数据序号提供程序。
		/// </summary>
		Common.ISequence Sequence { get; set; }

		/// <summary>
		/// 获取数据访问器的元数据容器。
		/// </summary>
		Metadata.IDataMetadataContainer Metadata { get; }

		/// <summary>
		/// 获取或设置数据验证器。
		/// </summary>
		IDataValidator Validator { get; set; }

		/// <summary>
		/// 获取数据访问的过滤器集合。
		/// </summary>
		ICollection<IDataAccessFilter> Filters { get; }
		#endregion

		#region 执行方法
		IEnumerable<T> Execute<T>(string name, IDictionary<string, object> inParameters, IDataExecuteOptions options = null, Func<DataExecuteContextBase, bool> executing = null, Action<DataExecuteContextBase> executed = null);
		IEnumerable<T> Execute<T>(string name, IDictionary<string, object> inParameters, out IDictionary<string, object> outParameters, IDataExecuteOptions options = null, Func<DataExecuteContextBase, bool> executing = null, Action<DataExecuteContextBase> executed = null);

		object ExecuteScalar(string name, IDictionary<string, object> inParameters, IDataExecuteOptions options = null, Func<DataExecuteContextBase, bool> executing = null, Action<DataExecuteContextBase> executed = null);
		object ExecuteScalar(string name, IDictionary<string, object> inParameters, out IDictionary<string, object> outParameters, IDataExecuteOptions options = null, Func<DataExecuteContextBase, bool> executing = null, Action<DataExecuteContextBase> executed = null);
		#endregion

		#region 存在方法
		bool Exists<T>(ICondition criteria, IDataExistsOptions options = null, Func<DataExistContextBase, bool> existing = null, Action<DataExistContextBase> existed = null);
		bool Exists(string name, ICondition criteria, IDataExistsOptions options = null, Func<DataExistContextBase, bool> existing = null, Action<DataExistContextBase> existed = null);
		#endregion

		#region 聚合方法
		int Count<T>(ICondition criteria = null, string member = null, IDataAggregateOptions options = null);
		int Count(string name, ICondition criteria = null, string member = null, IDataAggregateOptions options = null);

		double? Aggregate<T>(DataAggregateFunction method, string member, ICondition criteria = null, IDataAggregateOptions options = null);
		double? Aggregate(string name, DataAggregateFunction method, string member, ICondition criteria = null, IDataAggregateOptions options = null);
		double? Aggregate(string name, DataAggregate aggregate, ICondition criteria = null, IDataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null);
		#endregion

		#region 递增方法
		long Increment<T>(string member, ICondition criteria);
		long Increment<T>(string member, ICondition criteria, IDataIncrementOptions options);
		long Increment<T>(string member, ICondition criteria, int interval);
		long Increment<T>(string member, ICondition criteria, int interval, IDataIncrementOptions options, Func<DataIncrementContextBase, bool> incrementing = null, Action<DataIncrementContextBase> incremented = null);

		long Increment(string name, string member, ICondition criteria);
		long Increment(string name, string member, ICondition criteria, IDataIncrementOptions options);
		long Increment(string name, string member, ICondition criteria, int interval);
		long Increment(string name, string member, ICondition criteria, int interval, IDataIncrementOptions options, Func<DataIncrementContextBase, bool> incrementing = null, Action<DataIncrementContextBase> incremented = null);

		long Decrement<T>(string member, ICondition criteria);
		long Decrement<T>(string member, ICondition criteria, IDataIncrementOptions options);
		long Decrement<T>(string member, ICondition criteria, int interval);
		long Decrement<T>(string member, ICondition criteria, int interval, IDataIncrementOptions options, Func<DataIncrementContextBase, bool> decrementing = null, Action<DataIncrementContextBase> decremented = null);

		long Decrement(string name, string member, ICondition criteria);
		long Decrement(string name, string member, ICondition criteria, IDataIncrementOptions options);
		long Decrement(string name, string member, ICondition criteria, int interval);
		long Decrement(string name, string member, ICondition criteria, int interval, IDataIncrementOptions options, Func<DataIncrementContextBase, bool> decrementing = null, Action<DataIncrementContextBase> decremented = null);
		#endregion

		#region 删除方法
		int Delete<T>(ICondition criteria, string schema = null);
		int Delete<T>(ICondition criteria, IDataDeleteOptions options);
		int Delete<T>(ICondition criteria, string schema, IDataDeleteOptions options, Func<DataDeleteContextBase, bool> deleting = null, Action<DataDeleteContextBase> deleted = null);

		int Delete(string name, ICondition criteria, string schema = null);
		int Delete(string name, ICondition criteria, IDataDeleteOptions options);
		int Delete(string name, ICondition criteria, string schema, IDataDeleteOptions options, Func<DataDeleteContextBase, bool> deleting = null, Action<DataDeleteContextBase> deleted = null);
		int Delete(string name, ICondition criteria, ISchema schema, IDataDeleteOptions options, Func<DataDeleteContextBase, bool> deleting = null, Action<DataDeleteContextBase> deleted = null);
		#endregion

		#region 插入方法
		int Insert<T>(T data);
		int Insert<T>(T data, IDataInsertOptions options);
		int Insert<T>(T data, string schema);
		int Insert<T>(T data, string schema, IDataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null);

		int Insert<T>(object data);
		int Insert<T>(object data, IDataInsertOptions options);
		int Insert<T>(object data, string schema);
		int Insert<T>(object data, string schema, IDataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null);

		int Insert(string name, object data);
		int Insert(string name, object data, IDataInsertOptions options);
		int Insert(string name, object data, string schema);
		int Insert(string name, object data, string schema, IDataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null);
		int Insert(string name, object data, ISchema schema, IDataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null);

		int InsertMany<T>(IEnumerable<T> items);
		int InsertMany<T>(IEnumerable<T> items, IDataInsertOptions options);
		int InsertMany<T>(IEnumerable<T> items, string schema);
		int InsertMany<T>(IEnumerable<T> items, string schema, IDataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null);

		int InsertMany<T>(IEnumerable items);
		int InsertMany<T>(IEnumerable items, IDataInsertOptions options);
		int InsertMany<T>(IEnumerable items, string schema);
		int InsertMany<T>(IEnumerable items, string schema, IDataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null);

		int InsertMany(string name, IEnumerable items);
		int InsertMany(string name, IEnumerable items, IDataInsertOptions options);
		int InsertMany(string name, IEnumerable items, string schema);
		int InsertMany(string name, IEnumerable items, string schema, IDataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null);
		int InsertMany(string name, IEnumerable items, ISchema schema, IDataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null);
		#endregion

		#region 增改方法
		int Upsert<T>(T data);
		int Upsert<T>(T data, IDataUpsertOptions options);
		int Upsert<T>(T data, string schema);
		int Upsert<T>(T data, string schema, IDataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null);

		int Upsert<T>(object data);
		int Upsert<T>(object data, IDataUpsertOptions options);
		int Upsert<T>(object data, string schema);
		int Upsert<T>(object data, string schema, IDataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null);

		int Upsert(string name, object data);
		int Upsert(string name, object data, IDataUpsertOptions options);
		int Upsert(string name, object data, string schema);
		int Upsert(string name, object data, string schema, IDataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null);
		int Upsert(string name, object data, ISchema schema, IDataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null);

		int UpsertMany<T>(IEnumerable<T> items);
		int UpsertMany<T>(IEnumerable<T> items, IDataUpsertOptions options);
		int UpsertMany<T>(IEnumerable<T> items, string schema);
		int UpsertMany<T>(IEnumerable<T> items, string schema, IDataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null);

		int UpsertMany<T>(IEnumerable items);
		int UpsertMany<T>(IEnumerable items, IDataUpsertOptions options);
		int UpsertMany<T>(IEnumerable items, string schema);
		int UpsertMany<T>(IEnumerable items, string schema, IDataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null);

		int UpsertMany(string name, IEnumerable items);
		int UpsertMany(string name, IEnumerable items, IDataUpsertOptions options);
		int UpsertMany(string name, IEnumerable items, string schema);
		int UpsertMany(string name, IEnumerable items, string schema, IDataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null);
		int UpsertMany(string name, IEnumerable items, ISchema schema, IDataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null);
		#endregion

		#region 更新方法
		int Update<T>(T data);
		int Update<T>(T data, IDataUpdateOptions options);
		int Update<T>(T data, string schema);
		int Update<T>(T data, string schema, IDataUpdateOptions options);
		int Update<T>(T data, ICondition criteria);
		int Update<T>(T data, ICondition criteria, IDataUpdateOptions options);
		int Update<T>(T data, ICondition criteria, string schema);
		int Update<T>(T data, ICondition criteria, string schema, IDataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null);

		int Update<T>(object data);
		int Update<T>(object data, IDataUpdateOptions options);
		int Update<T>(object data, string schema);
		int Update<T>(object data, string schema, IDataUpdateOptions options);
		int Update<T>(object data, ICondition criteria);
		int Update<T>(object data, ICondition criteria, IDataUpdateOptions options);
		int Update<T>(object data, ICondition criteria, string schema);
		int Update<T>(object data, ICondition criteria, string schema, IDataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null);

		int Update(string name, object data);
		int Update(string name, object data, IDataUpdateOptions options);
		int Update(string name, object data, string schema);
		int Update(string name, object data, string schema, IDataUpdateOptions options);
		int Update(string name, object data, ICondition criteria);
		int Update(string name, object data, ICondition criteria, IDataUpdateOptions options);
		int Update(string name, object data, ICondition criteria, string schema);
		int Update(string name, object data, ICondition criteria, string schema, IDataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null);
		int Update(string name, object data, ICondition criteria, ISchema schema, IDataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null);

		int UpdateMany<T>(IEnumerable<T> items);
		int UpdateMany<T>(IEnumerable<T> items, IDataUpdateOptions options);
		int UpdateMany<T>(IEnumerable<T> items, string schema);
		int UpdateMany<T>(IEnumerable<T> items, string schema, IDataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null);

		int UpdateMany<T>(IEnumerable items);
		int UpdateMany<T>(IEnumerable items, IDataUpdateOptions options);
		int UpdateMany<T>(IEnumerable items, string schema);
		int UpdateMany<T>(IEnumerable items, string schema, IDataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null);

		int UpdateMany(string name, IEnumerable items);
		int UpdateMany(string name, IEnumerable items, IDataUpdateOptions options);
		int UpdateMany(string name, IEnumerable items, string schema);
		int UpdateMany(string name, IEnumerable items, string schema, IDataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null);
		int UpdateMany(string name, IEnumerable items, ISchema schema, IDataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null);
		#endregion

		#region 查询方法
		IEnumerable<T> Select<T>(IDataSelectOptions options = null, params Sorting[] sortings);
		IEnumerable<T> Select<T>(ICondition criteria, params Sorting[] sortings);
		IEnumerable<T> Select<T>(ICondition criteria, IDataSelectOptions options, params Sorting[] sortings);
		IEnumerable<T> Select<T>(ICondition criteria, Paging paging, params Sorting[] sortings);
		IEnumerable<T> Select<T>(ICondition criteria, Paging paging, IDataSelectOptions options, params Sorting[] sortings);
		IEnumerable<T> Select<T>(ICondition criteria, string schema, params Sorting[] sortings);
		IEnumerable<T> Select<T>(ICondition criteria, string schema, IDataSelectOptions options, params Sorting[] sortings);
		IEnumerable<T> Select<T>(ICondition criteria, string schema, Paging paging, params Sorting[] sortings);
		IEnumerable<T> Select<T>(ICondition criteria, string schema, Paging paging, IDataSelectOptions options, params Sorting[] sortings);
		IEnumerable<T> Select<T>(ICondition criteria, string schema, Paging paging, IDataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected);

		IEnumerable<T> Select<T>(string name, IDataSelectOptions options = null, params Sorting[] sortings);
		IEnumerable<T> Select<T>(string name, ICondition criteria, params Sorting[] sortings);
		IEnumerable<T> Select<T>(string name, ICondition criteria, IDataSelectOptions options, params Sorting[] sortings);
		IEnumerable<T> Select<T>(string name, ICondition criteria, Paging paging, params Sorting[] sortings);
		IEnumerable<T> Select<T>(string name, ICondition criteria, Paging paging, IDataSelectOptions options, params Sorting[] sortings);
		IEnumerable<T> Select<T>(string name, ICondition criteria, string schema, params Sorting[] sortings);
		IEnumerable<T> Select<T>(string name, ICondition criteria, string schema, IDataSelectOptions options, params Sorting[] sortings);
		IEnumerable<T> Select<T>(string name, ICondition criteria, string schema, Paging paging, params Sorting[] sortings);
		IEnumerable<T> Select<T>(string name, ICondition criteria, string schema, Paging paging, IDataSelectOptions options, params Sorting[] sortings);
		IEnumerable<T> Select<T>(string name, ICondition criteria, string schema, Paging paging, IDataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected);
		IEnumerable<T> Select<T>(string name, ICondition criteria, ISchema schema, Paging paging, IDataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected);

		IEnumerable<T> Select<T>(string name, Grouping grouping, params Sorting[] sortings);
		IEnumerable<T> Select<T>(string name, Grouping grouping, IDataSelectOptions options, params Sorting[] sortings);
		IEnumerable<T> Select<T>(string name, Grouping grouping, Paging paging, IDataSelectOptions options = null, params Sorting[] sortings);
		IEnumerable<T> Select<T>(string name, Grouping grouping, string schema, params Sorting[] sortings);
		IEnumerable<T> Select<T>(string name, Grouping grouping, string schema, IDataSelectOptions options, params Sorting[] sortings);
		IEnumerable<T> Select<T>(string name, Grouping grouping, string schema, Paging paging, IDataSelectOptions options = null, params Sorting[] sortings);
		IEnumerable<T> Select<T>(string name, Grouping grouping, ICondition criteria, Paging paging, params Sorting[] sortings);
		IEnumerable<T> Select<T>(string name, Grouping grouping, ICondition criteria, string schema = null, params Sorting[] sortings);
		IEnumerable<T> Select<T>(string name, Grouping grouping, ICondition criteria, string schema, IDataSelectOptions options, params Sorting[] sortings);
		IEnumerable<T> Select<T>(string name, Grouping grouping, ICondition criteria, string schema, Paging paging, IDataSelectOptions options = null, params Sorting[] sortings);
		IEnumerable<T> Select<T>(string name, Grouping grouping, ICondition criteria, string schema, Paging paging, IDataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected);
		IEnumerable<T> Select<T>(string name, Grouping grouping, ICondition criteria, ISchema schema, Paging paging, IDataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected);
		#endregion
	}
}
