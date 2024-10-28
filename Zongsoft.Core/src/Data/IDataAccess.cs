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
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
		event EventHandler<DataDeletedEventArgs> Deleted;
		event EventHandler<DataDeletingEventArgs> Deleting;
		event EventHandler<DataImportedEventArgs> Imported;
		event EventHandler<DataImportingEventArgs> Importing;
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
		/// <summary>获取数据访问的应用（子系统/业务模块）名。</summary>
		string Name { get; }

		/// <summary>获取数据访问名映射器。</summary>
		IDataAccessNaming Naming { get; }

		/// <summary>获取数据模式解析器。</summary>
		ISchemaParser Schema { get; }

		/// <summary>获取或设置数据序号提供程序。</summary>
		Common.ISequence Sequence { get; set; }

		/// <summary>获取数据访问器的元数据容器。</summary>
		Metadata.IDataMetadataContainer Metadata { get; }

		/// <summary>获取数据访问的过滤器集合。</summary>
		ICollection<object> Filters { get; }
		#endregion

		#region 导入方法
		int Import(string name, IEnumerable data, DataImportOptions options = null) => this.Import(name, data, Array.Empty<string>(), options);
		int Import(string name, IEnumerable data, string members, DataImportOptions options = null) => this.Import(name, data, string.IsNullOrEmpty(members) ? null : members.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries), options);
		int Import(string name, IEnumerable data, IEnumerable<string> members, DataImportOptions options = null);
		ValueTask<int> ImportAsync(string name, IEnumerable data, CancellationToken cancellation = default) => this.ImportAsync(name, data, Array.Empty<string>(), null, cancellation);
		ValueTask<int> ImportAsync(string name, IEnumerable data, DataImportOptions options, CancellationToken cancellation = default) => this.ImportAsync(name, data, Array.Empty<string>(), options, cancellation);
		ValueTask<int> ImportAsync(string name, IEnumerable data, string members, DataImportOptions options, CancellationToken cancellation = default) => this.ImportAsync(name, data, string.IsNullOrEmpty(members) ? null : members.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries), options, cancellation);
		ValueTask<int> ImportAsync(string name, IEnumerable data, IEnumerable<string> members, DataImportOptions options, CancellationToken cancellation = default);

		int Import<T>(IEnumerable<T> data, DataImportOptions options = null) => this.Import<T>(data, Array.Empty<string>(), options);
		int Import<T>(IEnumerable<T> data, string members, DataImportOptions options = null) => this.Import<T>(data, string.IsNullOrEmpty(members) ? null : members.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries), options);
		int Import<T>(IEnumerable<T> data, IEnumerable<string> members, DataImportOptions options = null);
		ValueTask<int> ImportAsync<T>(IEnumerable<T> data, CancellationToken cancellation = default) => this.ImportAsync(data, Array.Empty<string>(), null, cancellation);
		ValueTask<int> ImportAsync<T>(IEnumerable<T> data, DataImportOptions options, CancellationToken cancellation = default) => this.ImportAsync<T>(data, Array.Empty<string>(), options, cancellation);
		ValueTask<int> ImportAsync<T>(IEnumerable<T> data, string members, DataImportOptions options, CancellationToken cancellation = default) => this.ImportAsync<T>(data, string.IsNullOrEmpty(members) ? null : members.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries), options, cancellation);
		ValueTask<int> ImportAsync<T>(IEnumerable<T> data, IEnumerable<string> members, DataImportOptions options, CancellationToken cancellation = default);
		#endregion

		#region 执行方法
		IEnumerable<T> Execute<T>(string name, IDictionary<string, object> inParameters, DataExecuteOptions options = null, Func<DataExecuteContextBase, bool> executing = null, Action<DataExecuteContextBase> executed = null);
		IEnumerable<T> Execute<T>(string name, IDictionary<string, object> inParameters, out IDictionary<string, object> outParameters, DataExecuteOptions options = null, Func<DataExecuteContextBase, bool> executing = null, Action<DataExecuteContextBase> executed = null);
		IAsyncEnumerable<T> ExecuteAsync<T>(string name, IDictionary<string, object> inParameters, DataExecuteOptions options = null, Func<DataExecuteContextBase, bool> executing = null, Action<DataExecuteContextBase> executed = null, CancellationToken cancellation = default);

		object ExecuteScalar(string name, IDictionary<string, object> inParameters, DataExecuteOptions options = null, Func<DataExecuteContextBase, bool> executing = null, Action<DataExecuteContextBase> executed = null);
		object ExecuteScalar(string name, IDictionary<string, object> inParameters, out IDictionary<string, object> outParameters, DataExecuteOptions options = null, Func<DataExecuteContextBase, bool> executing = null, Action<DataExecuteContextBase> executed = null);
		ValueTask<object> ExecuteScalarAsync(string name, IDictionary<string, object> inParameters, DataExecuteOptions options = null, Func<DataExecuteContextBase, bool> executing = null, Action<DataExecuteContextBase> executed = null, CancellationToken cancellation = default);
		#endregion

		#region 存在方法
		bool Exists<T>(ICondition criteria, DataExistsOptions options = null, Func<DataExistContextBase, bool> existing = null, Action<DataExistContextBase> existed = null);
		bool Exists(string name, ICondition criteria, DataExistsOptions options = null, Func<DataExistContextBase, bool> existing = null, Action<DataExistContextBase> existed = null);

		ValueTask<bool> ExistsAsync<T>(ICondition criteria, DataExistsOptions options = null, Func<DataExistContextBase, bool> existing = null, Action<DataExistContextBase> existed = null, CancellationToken cancellation = default);
		ValueTask<bool> ExistsAsync(string name, ICondition criteria, DataExistsOptions options = null, Func<DataExistContextBase, bool> existing = null, Action<DataExistContextBase> existed = null, CancellationToken cancellation = default);
		#endregion

		#region 聚合方法
		int Count<T>(ICondition criteria = null, string member = null, DataAggregateOptions options = null);
		int Count(string name, ICondition criteria = null, string member = null, DataAggregateOptions options = null);

		TValue? Aggregate<T, TValue>(DataAggregateFunction function, string member, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue>;
		TValue? Aggregate<T, TValue>(DataAggregate aggregate, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null) where TValue : struct, IEquatable<TValue>;

		TValue? Aggregate<TValue>(string name, DataAggregateFunction function, string member, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue>;
		TValue? Aggregate<TValue>(string name, DataAggregate aggregate, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null) where TValue : struct, IEquatable<TValue>;

		ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregateFunction function, string member, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue>;
		ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregate aggregate, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue>;

		ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregateFunction function, string member, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue>;
		ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregate aggregate, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue>;
		#endregion

		#region 删除方法
		int Delete<T>(ICondition criteria, string schema = null);
		int Delete<T>(ICondition criteria, DataDeleteOptions options);
		int Delete<T>(ICondition criteria, string schema, DataDeleteOptions options, Func<DataDeleteContextBase, bool> deleting = null, Action<DataDeleteContextBase> deleted = null);

		int Delete(string name, ICondition criteria, string schema = null);
		int Delete(string name, ICondition criteria, DataDeleteOptions options);
		int Delete(string name, ICondition criteria, string schema, DataDeleteOptions options, Func<DataDeleteContextBase, bool> deleting = null, Action<DataDeleteContextBase> deleted = null);
		int Delete(string name, ICondition criteria, ISchema schema, DataDeleteOptions options, Func<DataDeleteContextBase, bool> deleting = null, Action<DataDeleteContextBase> deleted = null);

		ValueTask<int> DeleteAsync<T>(ICondition criteria, string schema = null, CancellationToken cancellation = default);
		ValueTask<int> DeleteAsync<T>(ICondition criteria, DataDeleteOptions options, CancellationToken cancellation = default);
		ValueTask<int> DeleteAsync<T>(ICondition criteria, string schema, DataDeleteOptions options, Func<DataDeleteContextBase, bool> deleting = null, Action<DataDeleteContextBase> deleted = null, CancellationToken cancellation = default);

		ValueTask<int> DeleteAsync(string name, ICondition criteria, string schema = null, CancellationToken cancellation = default);
		ValueTask<int> DeleteAsync(string name, ICondition criteria, DataDeleteOptions options, CancellationToken cancellation = default);
		ValueTask<int> DeleteAsync(string name, ICondition criteria, string schema, DataDeleteOptions options, Func<DataDeleteContextBase, bool> deleting = null, Action<DataDeleteContextBase> deleted = null, CancellationToken cancellation = default);
		ValueTask<int> DeleteAsync(string name, ICondition criteria, ISchema schema, DataDeleteOptions options, Func<DataDeleteContextBase, bool> deleting = null, Action<DataDeleteContextBase> deleted = null, CancellationToken cancellation = default);
		#endregion

		#region 插入方法
		int Insert<T>(T data);
		int Insert<T>(T data, DataInsertOptions options);
		int Insert<T>(T data, string schema);
		int Insert<T>(T data, string schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null);

		int Insert<T>(object data);
		int Insert<T>(object data, DataInsertOptions options);
		int Insert<T>(object data, string schema);
		int Insert<T>(object data, string schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null);

		int Insert(string name, object data);
		int Insert(string name, object data, DataInsertOptions options);
		int Insert(string name, object data, string schema);
		int Insert(string name, object data, string schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null);
		int Insert(string name, object data, ISchema schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null);

		ValueTask<int> InsertAsync<T>(T data, CancellationToken cancellation = default);
		ValueTask<int> InsertAsync<T>(T data, DataInsertOptions options, CancellationToken cancellation = default);
		ValueTask<int> InsertAsync<T>(T data, string schema, CancellationToken cancellation = default);
		ValueTask<int> InsertAsync<T>(T data, string schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null, CancellationToken cancellation = default);

		ValueTask<int> InsertAsync<T>(object data, CancellationToken cancellation = default);
		ValueTask<int> InsertAsync<T>(object data, DataInsertOptions options, CancellationToken cancellation = default);
		ValueTask<int> InsertAsync<T>(object data, string schema, CancellationToken cancellation = default);
		ValueTask<int> InsertAsync<T>(object data, string schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null, CancellationToken cancellation = default);

		ValueTask<int> InsertAsync(string name, object data, CancellationToken cancellation = default);
		ValueTask<int> InsertAsync(string name, object data, DataInsertOptions options, CancellationToken cancellation = default);
		ValueTask<int> InsertAsync(string name, object data, string schema, CancellationToken cancellation = default);
		ValueTask<int> InsertAsync(string name, object data, string schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null, CancellationToken cancellation = default);
		ValueTask<int> InsertAsync(string name, object data, ISchema schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null, CancellationToken cancellation = default);

		int InsertMany<T>(IEnumerable<T> items);
		int InsertMany<T>(IEnumerable<T> items, DataInsertOptions options);
		int InsertMany<T>(IEnumerable<T> items, string schema);
		int InsertMany<T>(IEnumerable<T> items, string schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null);

		int InsertMany<T>(IEnumerable items);
		int InsertMany<T>(IEnumerable items, DataInsertOptions options);
		int InsertMany<T>(IEnumerable items, string schema);
		int InsertMany<T>(IEnumerable items, string schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null);

		int InsertMany(string name, IEnumerable items);
		int InsertMany(string name, IEnumerable items, DataInsertOptions options);
		int InsertMany(string name, IEnumerable items, string schema);
		int InsertMany(string name, IEnumerable items, string schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null);
		int InsertMany(string name, IEnumerable items, ISchema schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null);

		ValueTask<int> InsertManyAsync<T>(IEnumerable<T> items, CancellationToken cancellation = default);
		ValueTask<int> InsertManyAsync<T>(IEnumerable<T> items, DataInsertOptions options, CancellationToken cancellation = default);
		ValueTask<int> InsertManyAsync<T>(IEnumerable<T> items, string schema, CancellationToken cancellation = default);
		ValueTask<int> InsertManyAsync<T>(IEnumerable<T> items, string schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null, CancellationToken cancellation = default);

		ValueTask<int> InsertManyAsync<T>(IEnumerable items, CancellationToken cancellation = default);
		ValueTask<int> InsertManyAsync<T>(IEnumerable items, DataInsertOptions options, CancellationToken cancellation = default);
		ValueTask<int> InsertManyAsync<T>(IEnumerable items, string schema, CancellationToken cancellation = default);
		ValueTask<int> InsertManyAsync<T>(IEnumerable items, string schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null, CancellationToken cancellation = default);

		ValueTask<int> InsertManyAsync(string name, IEnumerable items, CancellationToken cancellation = default);
		ValueTask<int> InsertManyAsync(string name, IEnumerable items, DataInsertOptions options, CancellationToken cancellation = default);
		ValueTask<int> InsertManyAsync(string name, IEnumerable items, string schema, CancellationToken cancellation = default);
		ValueTask<int> InsertManyAsync(string name, IEnumerable items, string schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null, CancellationToken cancellation = default);
		ValueTask<int> InsertManyAsync(string name, IEnumerable items, ISchema schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null, CancellationToken cancellation = default);
		#endregion

		#region 增改方法
		int Upsert<T>(T data);
		int Upsert<T>(T data, DataUpsertOptions options);
		int Upsert<T>(T data, string schema);
		int Upsert<T>(T data, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null);

		int Upsert<T>(object data);
		int Upsert<T>(object data, DataUpsertOptions options);
		int Upsert<T>(object data, string schema);
		int Upsert<T>(object data, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null);

		int Upsert(string name, object data);
		int Upsert(string name, object data, DataUpsertOptions options);
		int Upsert(string name, object data, string schema);
		int Upsert(string name, object data, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null);
		int Upsert(string name, object data, ISchema schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null);

		ValueTask<int> UpsertAsync<T>(T data, CancellationToken cancellation = default);
		ValueTask<int> UpsertAsync<T>(T data, DataUpsertOptions options, CancellationToken cancellation = default);
		ValueTask<int> UpsertAsync<T>(T data, string schema, CancellationToken cancellation = default);
		ValueTask<int> UpsertAsync<T>(T data, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null, CancellationToken cancellation = default);

		ValueTask<int> UpsertAsync<T>(object data, CancellationToken cancellation = default);
		ValueTask<int> UpsertAsync<T>(object data, DataUpsertOptions options, CancellationToken cancellation = default);
		ValueTask<int> UpsertAsync<T>(object data, string schema, CancellationToken cancellation = default);
		ValueTask<int> UpsertAsync<T>(object data, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null, CancellationToken cancellation = default);

		ValueTask<int> UpsertAsync(string name, object data, CancellationToken cancellation = default);
		ValueTask<int> UpsertAsync(string name, object data, DataUpsertOptions options, CancellationToken cancellation = default);
		ValueTask<int> UpsertAsync(string name, object data, string schema, CancellationToken cancellation = default);
		ValueTask<int> UpsertAsync(string name, object data, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null, CancellationToken cancellation = default);
		ValueTask<int> UpsertAsync(string name, object data, ISchema schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null, CancellationToken cancellation = default);

		int UpsertMany<T>(IEnumerable<T> items);
		int UpsertMany<T>(IEnumerable<T> items, DataUpsertOptions options);
		int UpsertMany<T>(IEnumerable<T> items, string schema);
		int UpsertMany<T>(IEnumerable<T> items, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null);

		int UpsertMany<T>(IEnumerable items);
		int UpsertMany<T>(IEnumerable items, DataUpsertOptions options);
		int UpsertMany<T>(IEnumerable items, string schema);
		int UpsertMany<T>(IEnumerable items, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null);

		int UpsertMany(string name, IEnumerable items);
		int UpsertMany(string name, IEnumerable items, DataUpsertOptions options);
		int UpsertMany(string name, IEnumerable items, string schema);
		int UpsertMany(string name, IEnumerable items, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null);
		int UpsertMany(string name, IEnumerable items, ISchema schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null);

		ValueTask<int> UpsertManyAsync<T>(IEnumerable<T> items, CancellationToken cancellation = default);
		ValueTask<int> UpsertManyAsync<T>(IEnumerable<T> items, DataUpsertOptions options, CancellationToken cancellation = default);
		ValueTask<int> UpsertManyAsync<T>(IEnumerable<T> items, string schema, CancellationToken cancellation = default);
		ValueTask<int> UpsertManyAsync<T>(IEnumerable<T> items, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null, CancellationToken cancellation = default);

		ValueTask<int> UpsertManyAsync<T>(IEnumerable items, CancellationToken cancellation = default);
		ValueTask<int> UpsertManyAsync<T>(IEnumerable items, DataUpsertOptions options, CancellationToken cancellation = default);
		ValueTask<int> UpsertManyAsync<T>(IEnumerable items, string schema, CancellationToken cancellation = default);
		ValueTask<int> UpsertManyAsync<T>(IEnumerable items, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null, CancellationToken cancellation = default);

		ValueTask<int> UpsertManyAsync(string name, IEnumerable items, CancellationToken cancellation = default);
		ValueTask<int> UpsertManyAsync(string name, IEnumerable items, DataUpsertOptions options, CancellationToken cancellation = default);
		ValueTask<int> UpsertManyAsync(string name, IEnumerable items, string schema, CancellationToken cancellation = default);
		ValueTask<int> UpsertManyAsync(string name, IEnumerable items, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null, CancellationToken cancellation = default);
		ValueTask<int> UpsertManyAsync(string name, IEnumerable items, ISchema schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null, CancellationToken cancellation = default);
		#endregion

		#region 更新方法
		int Update<T>(T data);
		int Update<T>(T data, DataUpdateOptions options);
		int Update<T>(T data, string schema);
		int Update<T>(T data, string schema, DataUpdateOptions options);
		int Update<T>(T data, ICondition criteria);
		int Update<T>(T data, ICondition criteria, DataUpdateOptions options);
		int Update<T>(T data, ICondition criteria, string schema);
		int Update<T>(T data, ICondition criteria, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null);

		int Update<T>(object data);
		int Update<T>(object data, DataUpdateOptions options);
		int Update<T>(object data, string schema);
		int Update<T>(object data, string schema, DataUpdateOptions options);
		int Update<T>(object data, ICondition criteria);
		int Update<T>(object data, ICondition criteria, DataUpdateOptions options);
		int Update<T>(object data, ICondition criteria, string schema);
		int Update<T>(object data, ICondition criteria, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null);

		int Update(string name, object data);
		int Update(string name, object data, DataUpdateOptions options);
		int Update(string name, object data, string schema);
		int Update(string name, object data, string schema, DataUpdateOptions options);
		int Update(string name, object data, ICondition criteria);
		int Update(string name, object data, ICondition criteria, DataUpdateOptions options);
		int Update(string name, object data, ICondition criteria, string schema);
		int Update(string name, object data, ICondition criteria, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null);
		int Update(string name, object data, ICondition criteria, ISchema schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null);

		ValueTask<int> UpdateAsync<T>(T data, CancellationToken cancellation = default);
		ValueTask<int> UpdateAsync<T>(T data, DataUpdateOptions options, CancellationToken cancellation = default);
		ValueTask<int> UpdateAsync<T>(T data, string schema, CancellationToken cancellation = default);
		ValueTask<int> UpdateAsync<T>(T data, string schema, DataUpdateOptions options, CancellationToken cancellation = default);
		ValueTask<int> UpdateAsync<T>(T data, ICondition criteria, CancellationToken cancellation = default);
		ValueTask<int> UpdateAsync<T>(T data, ICondition criteria, DataUpdateOptions options, CancellationToken cancellation = default);
		ValueTask<int> UpdateAsync<T>(T data, ICondition criteria, string schema, CancellationToken cancellation = default);
		ValueTask<int> UpdateAsync<T>(T data, ICondition criteria, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null, CancellationToken cancellation = default);

		ValueTask<int> UpdateAsync<T>(object data, CancellationToken cancellation = default);
		ValueTask<int> UpdateAsync<T>(object data, DataUpdateOptions options, CancellationToken cancellation = default);
		ValueTask<int> UpdateAsync<T>(object data, string schema, CancellationToken cancellation = default);
		ValueTask<int> UpdateAsync<T>(object data, string schema, DataUpdateOptions options, CancellationToken cancellation = default);
		ValueTask<int> UpdateAsync<T>(object data, ICondition criteria, CancellationToken cancellation = default);
		ValueTask<int> UpdateAsync<T>(object data, ICondition criteria, DataUpdateOptions options, CancellationToken cancellation = default);
		ValueTask<int> UpdateAsync<T>(object data, ICondition criteria, string schema, CancellationToken cancellation = default);
		ValueTask<int> UpdateAsync<T>(object data, ICondition criteria, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null, CancellationToken cancellation = default);

		ValueTask<int> UpdateAsync(string name, object data, CancellationToken cancellation = default);
		ValueTask<int> UpdateAsync(string name, object data, DataUpdateOptions options, CancellationToken cancellation = default);
		ValueTask<int> UpdateAsync(string name, object data, string schema, CancellationToken cancellation = default);
		ValueTask<int> UpdateAsync(string name, object data, string schema, DataUpdateOptions options, CancellationToken cancellation = default);
		ValueTask<int> UpdateAsync(string name, object data, ICondition criteria, CancellationToken cancellation = default);
		ValueTask<int> UpdateAsync(string name, object data, ICondition criteria, DataUpdateOptions options, CancellationToken cancellation = default);
		ValueTask<int> UpdateAsync(string name, object data, ICondition criteria, string schema, CancellationToken cancellation = default);
		ValueTask<int> UpdateAsync(string name, object data, ICondition criteria, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null, CancellationToken cancellation = default);
		ValueTask<int> UpdateAsync(string name, object data, ICondition criteria, ISchema schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null, CancellationToken cancellation = default);

		int UpdateMany<T>(IEnumerable<T> items);
		int UpdateMany<T>(IEnumerable<T> items, DataUpdateOptions options);
		int UpdateMany<T>(IEnumerable<T> items, string schema);
		int UpdateMany<T>(IEnumerable<T> items, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null);

		int UpdateMany<T>(IEnumerable items);
		int UpdateMany<T>(IEnumerable items, DataUpdateOptions options);
		int UpdateMany<T>(IEnumerable items, string schema);
		int UpdateMany<T>(IEnumerable items, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null);

		int UpdateMany(string name, IEnumerable items);
		int UpdateMany(string name, IEnumerable items, DataUpdateOptions options);
		int UpdateMany(string name, IEnumerable items, string schema);
		int UpdateMany(string name, IEnumerable items, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null);
		int UpdateMany(string name, IEnumerable items, ISchema schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null);

		ValueTask<int> UpdateManyAsync<T>(IEnumerable<T> items, CancellationToken cancellation = default);
		ValueTask<int> UpdateManyAsync<T>(IEnumerable<T> items, DataUpdateOptions options, CancellationToken cancellation = default);
		ValueTask<int> UpdateManyAsync<T>(IEnumerable<T> items, string schema, CancellationToken cancellation = default);
		ValueTask<int> UpdateManyAsync<T>(IEnumerable<T> items, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null, CancellationToken cancellation = default);

		ValueTask<int> UpdateManyAsync<T>(IEnumerable items, CancellationToken cancellation = default);
		ValueTask<int> UpdateManyAsync<T>(IEnumerable items, DataUpdateOptions options, CancellationToken cancellation = default);
		ValueTask<int> UpdateManyAsync<T>(IEnumerable items, string schema, CancellationToken cancellation = default);
		ValueTask<int> UpdateManyAsync<T>(IEnumerable items, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null, CancellationToken cancellation = default);

		ValueTask<int> UpdateManyAsync(string name, IEnumerable items, CancellationToken cancellation = default);
		ValueTask<int> UpdateManyAsync(string name, IEnumerable items, DataUpdateOptions options, CancellationToken cancellation = default);
		ValueTask<int> UpdateManyAsync(string name, IEnumerable items, string schema, CancellationToken cancellation = default);
		ValueTask<int> UpdateManyAsync(string name, IEnumerable items, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null, CancellationToken cancellation = default);
		ValueTask<int> UpdateManyAsync(string name, IEnumerable items, ISchema schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null, CancellationToken cancellation = default);
		#endregion

		#region 查询方法
		IEnumerable<T> Select<T>(DataSelectOptions options = null, params Sorting[] sortings);
		IEnumerable<T> Select<T>(ICondition criteria, params Sorting[] sortings);
		IEnumerable<T> Select<T>(ICondition criteria, DataSelectOptions options, params Sorting[] sortings);
		IEnumerable<T> Select<T>(ICondition criteria, Paging paging, params Sorting[] sortings);
		IEnumerable<T> Select<T>(ICondition criteria, Paging paging, DataSelectOptions options, params Sorting[] sortings);
		IEnumerable<T> Select<T>(ICondition criteria, string schema, params Sorting[] sortings);
		IEnumerable<T> Select<T>(ICondition criteria, string schema, DataSelectOptions options, params Sorting[] sortings);
		IEnumerable<T> Select<T>(ICondition criteria, string schema, Paging paging, params Sorting[] sortings);
		IEnumerable<T> Select<T>(ICondition criteria, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings);
		IEnumerable<T> Select<T>(ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected);

		IEnumerable<T> Select<T>(string name, DataSelectOptions options = null, params Sorting[] sortings);
		IEnumerable<T> Select<T>(string name, ICondition criteria, params Sorting[] sortings);
		IEnumerable<T> Select<T>(string name, ICondition criteria, DataSelectOptions options, params Sorting[] sortings);
		IEnumerable<T> Select<T>(string name, ICondition criteria, Paging paging, params Sorting[] sortings);
		IEnumerable<T> Select<T>(string name, ICondition criteria, Paging paging, DataSelectOptions options, params Sorting[] sortings);
		IEnumerable<T> Select<T>(string name, ICondition criteria, string schema, params Sorting[] sortings);
		IEnumerable<T> Select<T>(string name, ICondition criteria, string schema, DataSelectOptions options, params Sorting[] sortings);
		IEnumerable<T> Select<T>(string name, ICondition criteria, string schema, Paging paging, params Sorting[] sortings);
		IEnumerable<T> Select<T>(string name, ICondition criteria, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings);
		IEnumerable<T> Select<T>(string name, ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected);
		IEnumerable<T> Select<T>(string name, ICondition criteria, ISchema schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected);

		IEnumerable<T> Select<T>(string name, Grouping grouping, params Sorting[] sortings);
		IEnumerable<T> Select<T>(string name, Grouping grouping, DataSelectOptions options, params Sorting[] sortings);
		IEnumerable<T> Select<T>(string name, Grouping grouping, Paging paging, DataSelectOptions options = null, params Sorting[] sortings);
		IEnumerable<T> Select<T>(string name, Grouping grouping, string schema, params Sorting[] sortings);
		IEnumerable<T> Select<T>(string name, Grouping grouping, string schema, DataSelectOptions options, params Sorting[] sortings);
		IEnumerable<T> Select<T>(string name, Grouping grouping, string schema, Paging paging, DataSelectOptions options = null, params Sorting[] sortings);
		IEnumerable<T> Select<T>(string name, Grouping grouping, ICondition criteria, Paging paging, params Sorting[] sortings);
		IEnumerable<T> Select<T>(string name, Grouping grouping, ICondition criteria, string schema = null, params Sorting[] sortings);
		IEnumerable<T> Select<T>(string name, Grouping grouping, ICondition criteria, string schema, DataSelectOptions options, params Sorting[] sortings);
		IEnumerable<T> Select<T>(string name, Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options = null, params Sorting[] sortings);
		IEnumerable<T> Select<T>(string name, Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected);
		IEnumerable<T> Select<T>(string name, Grouping grouping, ICondition criteria, ISchema schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected);

		IAsyncEnumerable<T> SelectAsync<T>(CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(DataSelectOptions options, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, Sorting[] sortings, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, DataSelectOptions options, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, Paging paging, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, Paging paging, Sorting[] sortings, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, Paging paging, DataSelectOptions options, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, Sorting[] sortings, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, DataSelectOptions options, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, Paging paging, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, Paging paging, DataSelectOptions options, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected, CancellationToken cancellation = default);

		IAsyncEnumerable<T> SelectAsync<T>(string name, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, DataSelectOptions options, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, Sorting[] sortings, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, DataSelectOptions options, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, Paging paging, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, Paging paging, Sorting[] sortings, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, Paging paging, DataSelectOptions options, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, Sorting[] sortings, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, DataSelectOptions options, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, Paging paging, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, Paging paging, DataSelectOptions options, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, ISchema schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected, CancellationToken cancellation = default);

		IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, Sorting[] sortings, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, DataSelectOptions options, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, Paging paging, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, Paging paging, DataSelectOptions options, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, Sorting[] sortings, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, DataSelectOptions options, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, Paging paging, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, Paging paging, DataSelectOptions options, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, Paging paging, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, Paging paging, Sorting[] sortings, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, Sorting[] sortings, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, DataSelectOptions options, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected, CancellationToken cancellation = default);
		IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, ISchema schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected, CancellationToken cancellation = default);
		#endregion
	}
}
