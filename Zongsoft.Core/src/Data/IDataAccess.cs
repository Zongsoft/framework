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

using Microsoft.Extensions.Primitives;

namespace Zongsoft.Data;

/// <summary>表示数据访问的公共接口。</summary>
/// <remarks>
/// 	<para>关于“schema”查询参数的简要说明：</para>
/// 	<para>表示要包含和排除的属性名列表，如果指定的是多个属性则属性名之间使用逗号(,)分隔；要排除的属性以感叹号(!)打头，单独一个星号(*)表示所有属性，单独一个感叹号(!)表示排除所有属性；如果未指定该参数则默认只会获取所有单值属性而不会获取导航属性。</para>
/// </remarks>
public interface IDataAccess : IAsyncDisposable
{
	#region 事件定义
	event Func<object, DataAccessErrorEventArgs, CancellationToken, ValueTask> Error;
	event Func<object, DataExecutedEventArgs, CancellationToken, ValueTask> Executed;
	event Func<object, DataExecutingEventArgs, CancellationToken, ValueTask> Executing;
	event Func<object, DataExistedEventArgs, CancellationToken, ValueTask> Existed;
	event Func<object, DataExistingEventArgs, CancellationToken, ValueTask> Existing;
	event Func<object, DataAggregatedEventArgs, CancellationToken, ValueTask> Aggregated;
	event Func<object, DataAggregatingEventArgs, CancellationToken, ValueTask> Aggregating;
	event Func<object, DataDeletedEventArgs, CancellationToken, ValueTask> Deleted;
	event Func<object, DataDeletingEventArgs, CancellationToken, ValueTask> Deleting;
	event Func<object, DataImportedEventArgs, CancellationToken, ValueTask> Imported;
	event Func<object, DataImportingEventArgs, CancellationToken, ValueTask> Importing;
	event Func<object, DataInsertedEventArgs, CancellationToken, ValueTask> Inserted;
	event Func<object, DataInsertingEventArgs, CancellationToken, ValueTask> Inserting;
	event Func<object, DataUpsertedEventArgs, CancellationToken, ValueTask> Upserted;
	event Func<object, DataUpsertingEventArgs, CancellationToken, ValueTask> Upserting;
	event Func<object, DataUpdatedEventArgs, CancellationToken, ValueTask> Updated;
	event Func<object, DataUpdatingEventArgs, CancellationToken, ValueTask> Updating;
	event Func<object, DataSelectedEventArgs, CancellationToken, ValueTask> Selected;
	event Func<object, DataSelectingEventArgs, CancellationToken, ValueTask> Selecting;
	#endregion

	#region 属性声明
	/// <summary>获取数据访问的应用（子系统/业务模块）名。</summary>
	string Name { get; }
	/// <summary>获取数据模式解析器。</summary>
	ISchemaParser Schema { get; }
	/// <summary>获取或设置数据序号程序。</summary>
	IDataSequencer Sequencer { get; set; }
	/// <summary>获取数据访问的过滤器集合。</summary>
	ICollection<object> Filters { get; }
	/// <summary>获取一个值，指示当前对象是否已被处置。</summary>
	bool IsDisposed { get; }
	/// <summary>获取当前对象的处置通知令牌。</summary>
	IChangeToken Disposed { get; }
	#endregion

	#region 导入方法
	ValueTask<int> ImportAsync(string name, IEnumerable data, CancellationToken cancellation) => this.ImportAsync(name, data, Array.Empty<string>(), null, cancellation);
	ValueTask<int> ImportAsync(string name, IEnumerable data, DataImportOptions options = null, CancellationToken cancellation = default) => this.ImportAsync(name, data, Array.Empty<string>(), options, cancellation);

	ValueTask<int> ImportAsync(string name, IEnumerable data, string members, CancellationToken cancellation) => this.ImportAsync(name, data, members, default(DataImportOptions), cancellation);
	ValueTask<int> ImportAsync(string name, IEnumerable data, string members, DataImportOptions options = null, CancellationToken cancellation = default) => this.ImportAsync(name, data, string.IsNullOrEmpty(members) ? null : members.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries), options, cancellation);

	ValueTask<int> ImportAsync(string name, IEnumerable data, IEnumerable<string> members, CancellationToken cancellation) => this.ImportAsync(name, data, members, default(DataImportOptions), cancellation);
	ValueTask<int> ImportAsync(string name, IEnumerable data, IEnumerable<string> members, DataImportOptions options = null, CancellationToken cancellation = default);

	ValueTask<int> ImportAsync<T>(IEnumerable<T> data, CancellationToken cancellation) => this.ImportAsync(data, Array.Empty<string>(), null, cancellation);
	ValueTask<int> ImportAsync<T>(IEnumerable<T> data, DataImportOptions options = null, CancellationToken cancellation = default) => this.ImportAsync<T>(data, Array.Empty<string>(), options, cancellation);

	ValueTask<int> ImportAsync<T>(IEnumerable<T> data, string members, CancellationToken cancellation) => this.ImportAsync<T>(data, members, default(DataImportOptions), cancellation);
	ValueTask<int> ImportAsync<T>(IEnumerable<T> data, string members, DataImportOptions options = null, CancellationToken cancellation = default) => this.ImportAsync<T>(data, string.IsNullOrEmpty(members) ? null : members.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries), options, cancellation);

	ValueTask<int> ImportAsync<T>(IEnumerable<T> data, IEnumerable<string> members, CancellationToken cancellation) => this.ImportAsync<T>(data, members, default(DataImportOptions), cancellation);
	ValueTask<int> ImportAsync<T>(IEnumerable<T> data, IEnumerable<string> members, DataImportOptions options = null, CancellationToken cancellation = default);

	ValueTask<int> ImportAsync<T>(string name, IAsyncEnumerable<T> data, CancellationToken cancellation) => this.ImportAsync<T>(name, data, Array.Empty<string>(), null, cancellation);
	ValueTask<int> ImportAsync<T>(string name, IAsyncEnumerable<T> data, DataImportOptions options = null, CancellationToken cancellation = default) => this.ImportAsync<T>(name, data, Array.Empty<string>(), options, cancellation);

	ValueTask<int> ImportAsync<T>(string name, IAsyncEnumerable<T> data, string members, CancellationToken cancellation) => this.ImportAsync<T>(name, data, members, default(DataImportOptions), cancellation);
	ValueTask<int> ImportAsync<T>(string name, IAsyncEnumerable<T> data, string members, DataImportOptions options = null, CancellationToken cancellation = default) => this.ImportAsync<T>(name, data, string.IsNullOrEmpty(members) ? null : members.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries), options, cancellation);

	ValueTask<int> ImportAsync<T>(string name, IAsyncEnumerable<T> data, IEnumerable<string> members, CancellationToken cancellation) => this.ImportAsync<T>(name, data, members, default(DataImportOptions), cancellation);
	ValueTask<int> ImportAsync<T>(string name, IAsyncEnumerable<T> data, IEnumerable<string> members, DataImportOptions options = null, CancellationToken cancellation = default);
	ValueTask<int> ImportAsync<T>(string name, IAsyncEnumerable<T> data, IEnumerable<string> members, DataImportOptions options, Func<DataImportContextBase, CancellationToken, ValueTask<bool>> importing, Func<DataImportContextBase, CancellationToken, ValueTask> imported, CancellationToken cancellation = default);

	ValueTask<int> ImportAsync<T>(IAsyncEnumerable<T> data, CancellationToken cancellation) => this.ImportAsync<T>(data, Array.Empty<string>(), null, cancellation);
	ValueTask<int> ImportAsync<T>(IAsyncEnumerable<T> data, DataImportOptions options = null, CancellationToken cancellation = default) => this.ImportAsync<T>(data, Array.Empty<string>(), options, cancellation);

	ValueTask<int> ImportAsync<T>(IAsyncEnumerable<T> data, string members, CancellationToken cancellation) => this.ImportAsync<T>(data, members, default(DataImportOptions), cancellation);
	ValueTask<int> ImportAsync<T>(IAsyncEnumerable<T> data, string members, DataImportOptions options = null, CancellationToken cancellation = default) => this.ImportAsync<T>(data, string.IsNullOrEmpty(members) ? null : members.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries), options, cancellation);

	ValueTask<int> ImportAsync<T>(IAsyncEnumerable<T> data, IEnumerable<string> members, CancellationToken cancellation) => this.ImportAsync<T>(data, members, default(DataImportOptions), cancellation);
	ValueTask<int> ImportAsync<T>(IAsyncEnumerable<T> data, IEnumerable<string> members, DataImportOptions options = null, CancellationToken cancellation = default);
	#endregion

	#region 执行方法
	ValueTask<int> ExecuteAsync(string name, CancellationToken cancellation);
	ValueTask<int> ExecuteAsync(string name, DataExecuteOptions options, CancellationToken cancellation);
	ValueTask<int> ExecuteAsync(string name, DataExecuteOptions options, Func<DataExecuteContextBase, CancellationToken, ValueTask<bool>> executing, CancellationToken cancellation) => this.ExecuteAsync(name, options, executing, default(Func<DataExecuteContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> ExecuteAsync(string name, DataExecuteOptions options = null, Func<DataExecuteContextBase, CancellationToken, ValueTask<bool>> executing = null, Func<DataExecuteContextBase, CancellationToken, ValueTask> executed = null, CancellationToken cancellation = default);

	ValueTask<int> ExecuteAsync(string name, IEnumerable<Parameter> parameters, CancellationToken cancellation);
	ValueTask<int> ExecuteAsync(string name, IEnumerable<Parameter> parameters, DataExecuteOptions options, CancellationToken cancellation);
	ValueTask<int> ExecuteAsync(string name, IEnumerable<Parameter> parameters, DataExecuteOptions options, Func<DataExecuteContextBase, CancellationToken, ValueTask<bool>> executing, CancellationToken cancellation) => this.ExecuteAsync(name, parameters, options, executing, default(Func<DataExecuteContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> ExecuteAsync(string name, IEnumerable<Parameter> parameters, DataExecuteOptions options = null, Func<DataExecuteContextBase, CancellationToken, ValueTask<bool>> executing = null, Func<DataExecuteContextBase, CancellationToken, ValueTask> executed = null, CancellationToken cancellation = default);

	IAsyncEnumerable<T> ExecuteAsync<T>(string name, CancellationToken cancellation);
	IAsyncEnumerable<T> ExecuteAsync<T>(string name, DataExecuteOptions options, CancellationToken cancellation);
	IAsyncEnumerable<T> ExecuteAsync<T>(string name, DataExecuteOptions options, Func<DataExecuteContextBase, CancellationToken, ValueTask<bool>> executing, CancellationToken cancellation) => this.ExecuteAsync<T>(name, options, executing, default(Func<DataExecuteContextBase, CancellationToken, ValueTask>), cancellation);
	IAsyncEnumerable<T> ExecuteAsync<T>(string name, DataExecuteOptions options = null, Func<DataExecuteContextBase, CancellationToken, ValueTask<bool>> executing = null, Func<DataExecuteContextBase, CancellationToken, ValueTask> executed = null, CancellationToken cancellation = default);

	IAsyncEnumerable<T> ExecuteAsync<T>(string name, IEnumerable<Parameter> parameters, CancellationToken cancellation);
	IAsyncEnumerable<T> ExecuteAsync<T>(string name, IEnumerable<Parameter> parameters, DataExecuteOptions options, CancellationToken cancellation);
	IAsyncEnumerable<T> ExecuteAsync<T>(string name, IEnumerable<Parameter> parameters, DataExecuteOptions options, Func<DataExecuteContextBase, CancellationToken, ValueTask<bool>> executing, CancellationToken cancellation) => this.ExecuteAsync<T>(name, parameters, options, executing, default(Func<DataExecuteContextBase, CancellationToken, ValueTask>), cancellation);
	IAsyncEnumerable<T> ExecuteAsync<T>(string name, IEnumerable<Parameter> parameters, DataExecuteOptions options = null, Func<DataExecuteContextBase, CancellationToken, ValueTask<bool>> executing = null, Func<DataExecuteContextBase, CancellationToken, ValueTask> executed = null, CancellationToken cancellation = default);

	ValueTask<object> ExecuteScalarAsync(string name, CancellationToken cancellation);
	ValueTask<object> ExecuteScalarAsync(string name, DataExecuteOptions options, CancellationToken cancellation);
	ValueTask<object> ExecuteScalarAsync(string name, DataExecuteOptions options, Func<DataExecuteContextBase, CancellationToken, ValueTask<bool>> executing, CancellationToken cancellation) => this.ExecuteScalarAsync(name, options, executing, default(Func<DataExecuteContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<object> ExecuteScalarAsync(string name, DataExecuteOptions options = null, Func<DataExecuteContextBase, CancellationToken, ValueTask<bool>> executing = null, Func<DataExecuteContextBase, CancellationToken, ValueTask> executed = null, CancellationToken cancellation = default);

	ValueTask<object> ExecuteScalarAsync(string name, IEnumerable<Parameter> parameters, CancellationToken cancellation);
	ValueTask<object> ExecuteScalarAsync(string name, IEnumerable<Parameter> parameters, DataExecuteOptions options, CancellationToken cancellation);
	ValueTask<object> ExecuteScalarAsync(string name, IEnumerable<Parameter> parameters, DataExecuteOptions options, Func<DataExecuteContextBase, CancellationToken, ValueTask<bool>> executing, CancellationToken cancellation) => this.ExecuteScalarAsync(name, parameters, options, executing, default(Func<DataExecuteContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<object> ExecuteScalarAsync(string name, IEnumerable<Parameter> parameters, DataExecuteOptions options = null, Func<DataExecuteContextBase, CancellationToken, ValueTask<bool>> executing = null, Func<DataExecuteContextBase, CancellationToken, ValueTask> executed = null, CancellationToken cancellation = default);
	#endregion

	#region 存在方法
	ValueTask<bool> ExistsAsync<T>(ICondition criteria, CancellationToken cancellation) => this.ExistsAsync<T>(criteria, default(DataExistsOptions), default(Func<DataExistContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataExistContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<bool> ExistsAsync<T>(ICondition criteria, DataExistsOptions options, CancellationToken cancellation) => this.ExistsAsync<T>(criteria, options, default(Func<DataExistContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataExistContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<bool> ExistsAsync<T>(ICondition criteria, DataExistsOptions options, Func<DataExistContextBase, CancellationToken, ValueTask<bool>> existing, CancellationToken cancellation) => this.ExistsAsync<T>(criteria, options, existing, default(Func<DataExistContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<bool> ExistsAsync<T>(ICondition criteria, DataExistsOptions options = null, Func<DataExistContextBase, CancellationToken, ValueTask<bool>> existing = null, Func<DataExistContextBase, CancellationToken, ValueTask> existed = null, CancellationToken cancellation = default);

	ValueTask<bool> ExistsAsync(string name, ICondition criteria, CancellationToken cancellation) => this.ExistsAsync(name, criteria, default(DataExistsOptions), default(Func<DataExistContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataExistContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<bool> ExistsAsync(string name, ICondition criteria, DataExistsOptions options, CancellationToken cancellation) => this.ExistsAsync(name, criteria, options, default(Func<DataExistContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataExistContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<bool> ExistsAsync(string name, ICondition criteria, DataExistsOptions options, Func<DataExistContextBase, CancellationToken, ValueTask<bool>> existing, CancellationToken cancellation) => this.ExistsAsync(name, criteria, options, existing, default(Func<DataExistContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<bool> ExistsAsync(string name, ICondition criteria, DataExistsOptions options = null, Func<DataExistContextBase, CancellationToken, ValueTask<bool>> existing = null, Func<DataExistContextBase, CancellationToken, ValueTask> existed = null, CancellationToken cancellation = default);
	#endregion

	#region 聚合方法
	ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregateFunction function, string member, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<T, TValue>(function, member, default(ICondition), default(DataAggregateOptions), cancellation);
	ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregateFunction function, string member, ICondition criteria, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<T, TValue>(function, member, criteria, default(DataAggregateOptions), cancellation);
	ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregateFunction function, string member, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue>;

	ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregateFunction function, string member, string alias, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<T, TValue>(function, member, alias, default(ICondition), default(DataAggregateOptions), cancellation);
	ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregateFunction function, string member, string alias, ICondition criteria, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<T, TValue>(function, member, alias, criteria, default(DataAggregateOptions), cancellation);
	ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregateFunction function, string member, string alias, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue>;

	ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregateFunction function, string member, bool distinct, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<T, TValue>(function, member, distinct, default(ICondition), default(DataAggregateOptions), cancellation);
	ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregateFunction function, string member, bool distinct, ICondition criteria, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<T, TValue>(function, member, distinct, criteria, default(DataAggregateOptions), cancellation);
	ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregateFunction function, string member, bool distinct, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue>;

	ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregateFunction function, string member, bool distinct, string alias, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<T, TValue>(function, member, distinct, alias, default(ICondition), default(DataAggregateOptions), cancellation);
	ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregateFunction function, string member, bool distinct, string alias, ICondition criteria, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<T, TValue>(function, member, distinct, alias, criteria, default(DataAggregateOptions), cancellation);
	ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregateFunction function, string member, bool distinct, string alias, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue>;

	ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregate aggregate, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<T, TValue>(aggregate, default(ICondition), default(DataAggregateOptions), default(Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataAggregateContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregate aggregate, ICondition criteria, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<T, TValue>(aggregate, criteria, default(DataAggregateOptions), default(Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataAggregateContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregate aggregate, ICondition criteria, DataAggregateOptions options, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<T, TValue>(aggregate, criteria, options, default(Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataAggregateContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregate aggregate, ICondition criteria, DataAggregateOptions options, Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>> aggregating, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<T, TValue>(aggregate, criteria, options, aggregating, default(Func<DataAggregateContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregate aggregate, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>> aggregating = null, Func<DataAggregateContextBase, CancellationToken, ValueTask> aggregated = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue>;

	ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregateFunction function, string member, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(name, function, member, default(ICondition), default(DataAggregateOptions), cancellation);
	ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregateFunction function, string member, ICondition criteria, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(name, function, member, criteria, default(DataAggregateOptions), cancellation);
	ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregateFunction function, string member, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue>;

	ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregateFunction function, string member, string alias, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(name, function, member, alias, default(ICondition), default(DataAggregateOptions), cancellation);
	ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregateFunction function, string member, string alias, ICondition criteria, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(name, function, member, alias, criteria, default(DataAggregateOptions), cancellation);
	ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregateFunction function, string member, string alias, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue>;

	ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregateFunction function, string member, bool distinct, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(name, function, member, distinct, default(ICondition), default(DataAggregateOptions), cancellation);
	ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregateFunction function, string member, bool distinct, ICondition criteria, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(name, function, member, distinct, criteria, default(DataAggregateOptions), cancellation);
	ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregateFunction function, string member, bool distinct, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue>;

	ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregateFunction function, string member, bool distinct, string alias, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(name, function, member, distinct, alias, default(ICondition), default(DataAggregateOptions), cancellation);
	ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregateFunction function, string member, bool distinct, string alias, ICondition criteria, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(name, function, member, distinct, alias, criteria, default(DataAggregateOptions), cancellation);
	ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregateFunction function, string member, bool distinct, string alias, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue>;

	ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregate aggregate, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(name, aggregate, default(ICondition), default(DataAggregateOptions), default(Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataAggregateContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregate aggregate, ICondition criteria, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(name, aggregate, criteria, default(DataAggregateOptions), default(Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataAggregateContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregate aggregate, ICondition criteria, DataAggregateOptions options, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(name, aggregate, criteria, options, default(Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataAggregateContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregate aggregate, ICondition criteria, DataAggregateOptions options, Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>> aggregating, CancellationToken cancellation) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(name, aggregate, criteria, options, aggregating, default(Func<DataAggregateContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregate aggregate, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, CancellationToken, ValueTask<bool>> aggregating = null, Func<DataAggregateContextBase, CancellationToken, ValueTask> aggregated = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue>;
	#endregion

	#region 删除方法
	ValueTask<int> DeleteAsync<T>(ICondition criteria, string schema = null, CancellationToken cancellation = default);
	ValueTask<int> DeleteAsync<T>(ICondition criteria, string schema, DataDeleteOptions options, CancellationToken cancellation) => this.DeleteAsync<T>(criteria, schema, options, default(Func<DataDeleteContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataDeleteContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> DeleteAsync<T>(ICondition criteria, string schema, DataDeleteOptions options, Func<DataDeleteContextBase, CancellationToken, ValueTask<bool>> deleting, CancellationToken cancellation) => this.DeleteAsync<T>(criteria, schema, options, deleting, default(Func<DataDeleteContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> DeleteAsync<T>(ICondition criteria, string schema, DataDeleteOptions options, Func<DataDeleteContextBase, CancellationToken, ValueTask<bool>> deleting = null, Func<DataDeleteContextBase, CancellationToken, ValueTask> deleted = null, CancellationToken cancellation = default);

	ValueTask<int> DeleteAsync<T>(ICondition criteria, CancellationToken cancellation) => this.DeleteAsync<T>(criteria, default(string), cancellation);
	ValueTask<int> DeleteAsync<T>(ICondition criteria, DataDeleteOptions options, CancellationToken cancellation = default);

	ValueTask<int> DeleteAsync(string name, ICondition criteria, string schema = null, CancellationToken cancellation = default);
	ValueTask<int> DeleteAsync(string name, ICondition criteria, string schema, DataDeleteOptions options, CancellationToken cancellation) => this.DeleteAsync(name, criteria, schema, options, default(Func<DataDeleteContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataDeleteContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> DeleteAsync(string name, ICondition criteria, string schema, DataDeleteOptions options, Func<DataDeleteContextBase, CancellationToken, ValueTask<bool>> deleting, CancellationToken cancellation) => this.DeleteAsync(name, criteria, schema, options, deleting, default(Func<DataDeleteContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> DeleteAsync(string name, ICondition criteria, string schema, DataDeleteOptions options, Func<DataDeleteContextBase, CancellationToken, ValueTask<bool>> deleting = null, Func<DataDeleteContextBase, CancellationToken, ValueTask> deleted = null, CancellationToken cancellation = default);

	ValueTask<int> DeleteAsync(string name, ICondition criteria, CancellationToken cancellation) => this.DeleteAsync(name, criteria, default(string), cancellation);
	ValueTask<int> DeleteAsync(string name, ICondition criteria, DataDeleteOptions options, CancellationToken cancellation = default);

	ValueTask<int> DeleteAsync(string name, ICondition criteria, ISchema schema, DataDeleteOptions options, CancellationToken cancellation) => this.DeleteAsync(name, criteria, schema, options, default(Func<DataDeleteContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataDeleteContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> DeleteAsync(string name, ICondition criteria, ISchema schema, DataDeleteOptions options, Func<DataDeleteContextBase, CancellationToken, ValueTask<bool>> deleting, CancellationToken cancellation) => this.DeleteAsync(name, criteria, schema, options, deleting, default(Func<DataDeleteContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> DeleteAsync(string name, ICondition criteria, ISchema schema, DataDeleteOptions options, Func<DataDeleteContextBase, CancellationToken, ValueTask<bool>> deleting = null, Func<DataDeleteContextBase, CancellationToken, ValueTask> deleted = null, CancellationToken cancellation = default);
	#endregion

	#region 插入方法
	ValueTask<int> InsertAsync<T>(T data, CancellationToken cancellation = default);
	ValueTask<int> InsertAsync<T>(T data, DataInsertOptions options, CancellationToken cancellation = default);

	ValueTask<int> InsertAsync<T>(T data, string schema, CancellationToken cancellation = default);
	ValueTask<int> InsertAsync<T>(T data, string schema, DataInsertOptions options, CancellationToken cancellation) => this.InsertAsync<T>(data, schema, options, default(Func<DataInsertContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataInsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> InsertAsync<T>(T data, string schema, DataInsertOptions options, Func<DataInsertContextBase, CancellationToken, ValueTask<bool>> inserting, CancellationToken cancellation) => this.InsertAsync<T>(data, schema, options, inserting, default(Func<DataInsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> InsertAsync<T>(T data, string schema, DataInsertOptions options, Func<DataInsertContextBase, CancellationToken, ValueTask<bool>> inserting = null, Func<DataInsertContextBase, CancellationToken, ValueTask> inserted = null, CancellationToken cancellation = default);

	ValueTask<int> InsertAsync<T>(object data, CancellationToken cancellation = default);
	ValueTask<int> InsertAsync<T>(object data, DataInsertOptions options, CancellationToken cancellation = default);

	ValueTask<int> InsertAsync<T>(object data, string schema, CancellationToken cancellation = default);
	ValueTask<int> InsertAsync<T>(object data, string schema, DataInsertOptions options, CancellationToken cancellation) => this.InsertAsync<T>(data, schema, options, default(Func<DataInsertContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataInsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> InsertAsync<T>(object data, string schema, DataInsertOptions options, Func<DataInsertContextBase, CancellationToken, ValueTask<bool>> inserting, CancellationToken cancellation) => this.InsertAsync<T>(data, schema, options, inserting, default(Func<DataInsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> InsertAsync<T>(object data, string schema, DataInsertOptions options, Func<DataInsertContextBase, CancellationToken, ValueTask<bool>> inserting = null, Func<DataInsertContextBase, CancellationToken, ValueTask> inserted = null, CancellationToken cancellation = default);

	ValueTask<int> InsertAsync(string name, object data, CancellationToken cancellation = default);
	ValueTask<int> InsertAsync(string name, object data, DataInsertOptions options, CancellationToken cancellation = default);

	ValueTask<int> InsertAsync(string name, object data, string schema, CancellationToken cancellation = default);
	ValueTask<int> InsertAsync(string name, object data, string schema, DataInsertOptions options, CancellationToken cancellation) => this.InsertAsync(name, data, schema, options, default(Func<DataInsertContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataInsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> InsertAsync(string name, object data, string schema, DataInsertOptions options, Func<DataInsertContextBase, CancellationToken, ValueTask<bool>> inserting, CancellationToken cancellation) => this.InsertAsync(name, data, schema, options, inserting, default(Func<DataInsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> InsertAsync(string name, object data, string schema, DataInsertOptions options, Func<DataInsertContextBase, CancellationToken, ValueTask<bool>> inserting = null, Func<DataInsertContextBase, CancellationToken, ValueTask> inserted = null, CancellationToken cancellation = default);

	ValueTask<int> InsertAsync(string name, object data, ISchema schema, DataInsertOptions options, CancellationToken cancellation) => this.InsertAsync(name, data, schema, options, default(Func<DataInsertContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataInsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> InsertAsync(string name, object data, ISchema schema, DataInsertOptions options, Func<DataInsertContextBase, CancellationToken, ValueTask<bool>> inserting, CancellationToken cancellation) => this.InsertAsync(name, data, schema, options, inserting, default(Func<DataInsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> InsertAsync(string name, object data, ISchema schema, DataInsertOptions options, Func<DataInsertContextBase, CancellationToken, ValueTask<bool>> inserting = null, Func<DataInsertContextBase, CancellationToken, ValueTask> inserted = null, CancellationToken cancellation = default);

	ValueTask<int> InsertManyAsync<T>(IEnumerable<T> items, CancellationToken cancellation = default);
	ValueTask<int> InsertManyAsync<T>(IEnumerable<T> items, DataInsertOptions options, CancellationToken cancellation = default);

	ValueTask<int> InsertManyAsync<T>(IEnumerable<T> items, string schema, CancellationToken cancellation = default);
	ValueTask<int> InsertManyAsync<T>(IEnumerable<T> items, string schema, DataInsertOptions options, CancellationToken cancellation) => this.InsertManyAsync<T>(items, schema, options, default(Func<DataInsertContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataInsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> InsertManyAsync<T>(IEnumerable<T> items, string schema, DataInsertOptions options, Func<DataInsertContextBase, CancellationToken, ValueTask<bool>> inserting, CancellationToken cancellation) => this.InsertManyAsync<T>(items, schema, options, inserting, default(Func<DataInsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> InsertManyAsync<T>(IEnumerable<T> items, string schema, DataInsertOptions options, Func<DataInsertContextBase, CancellationToken, ValueTask<bool>> inserting = null, Func<DataInsertContextBase, CancellationToken, ValueTask> inserted = null, CancellationToken cancellation = default);

	ValueTask<int> InsertManyAsync<T>(IEnumerable items, CancellationToken cancellation = default);
	ValueTask<int> InsertManyAsync<T>(IEnumerable items, DataInsertOptions options, CancellationToken cancellation = default);

	ValueTask<int> InsertManyAsync<T>(IEnumerable items, string schema, CancellationToken cancellation = default);
	ValueTask<int> InsertManyAsync<T>(IEnumerable items, string schema, DataInsertOptions options, CancellationToken cancellation) => this.InsertManyAsync<T>(items, schema, options, default(Func<DataInsertContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataInsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> InsertManyAsync<T>(IEnumerable items, string schema, DataInsertOptions options, Func<DataInsertContextBase, CancellationToken, ValueTask<bool>> inserting, CancellationToken cancellation) => this.InsertManyAsync<T>(items, schema, options, inserting, default(Func<DataInsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> InsertManyAsync<T>(IEnumerable items, string schema, DataInsertOptions options, Func<DataInsertContextBase, CancellationToken, ValueTask<bool>> inserting = null, Func<DataInsertContextBase, CancellationToken, ValueTask> inserted = null, CancellationToken cancellation = default);

	ValueTask<int> InsertManyAsync(string name, IEnumerable items, CancellationToken cancellation = default);
	ValueTask<int> InsertManyAsync(string name, IEnumerable items, DataInsertOptions options, CancellationToken cancellation = default);

	ValueTask<int> InsertManyAsync(string name, IEnumerable items, string schema, CancellationToken cancellation = default);
	ValueTask<int> InsertManyAsync(string name, IEnumerable items, string schema, DataInsertOptions options, CancellationToken cancellation) => this.InsertManyAsync(name, items, schema, options, default(Func<DataInsertContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataInsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> InsertManyAsync(string name, IEnumerable items, string schema, DataInsertOptions options, Func<DataInsertContextBase, CancellationToken, ValueTask<bool>> inserting, CancellationToken cancellation) => this.InsertManyAsync(name, items, schema, options, inserting, default(Func<DataInsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> InsertManyAsync(string name, IEnumerable items, string schema, DataInsertOptions options, Func<DataInsertContextBase, CancellationToken, ValueTask<bool>> inserting = null, Func<DataInsertContextBase, CancellationToken, ValueTask> inserted = null, CancellationToken cancellation = default);

	ValueTask<int> InsertManyAsync(string name, IEnumerable items, ISchema schema, DataInsertOptions options, CancellationToken cancellation) => this.InsertManyAsync(name, items, schema, options, default(Func<DataInsertContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataInsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> InsertManyAsync(string name, IEnumerable items, ISchema schema, DataInsertOptions options, Func<DataInsertContextBase, CancellationToken, ValueTask<bool>> inserting, CancellationToken cancellation) => this.InsertManyAsync(name, items, schema, options, inserting, default(Func<DataInsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> InsertManyAsync(string name, IEnumerable items, ISchema schema, DataInsertOptions options, Func<DataInsertContextBase, CancellationToken, ValueTask<bool>> inserting = null, Func<DataInsertContextBase, CancellationToken, ValueTask> inserted = null, CancellationToken cancellation = default);

	ValueTask<int> InsertManyAsync<T>(IAsyncEnumerable<T> items, CancellationToken cancellation = default);
	ValueTask<int> InsertManyAsync<T>(IAsyncEnumerable<T> items, DataInsertOptions options, CancellationToken cancellation = default);

	ValueTask<int> InsertManyAsync<T>(IAsyncEnumerable<T> items, string schema, CancellationToken cancellation = default);
	ValueTask<int> InsertManyAsync<T>(IAsyncEnumerable<T> items, string schema, DataInsertOptions options, CancellationToken cancellation) => this.InsertManyAsync<T>(items, schema, options, default(Func<DataInsertContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataInsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> InsertManyAsync<T>(IAsyncEnumerable<T> items, string schema, DataInsertOptions options, Func<DataInsertContextBase, CancellationToken, ValueTask<bool>> inserting, CancellationToken cancellation) => this.InsertManyAsync<T>(items, schema, options, inserting, default(Func<DataInsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> InsertManyAsync<T>(IAsyncEnumerable<T> items, string schema, DataInsertOptions options, Func<DataInsertContextBase, CancellationToken, ValueTask<bool>> inserting = null, Func<DataInsertContextBase, CancellationToken, ValueTask> inserted = null, CancellationToken cancellation = default);

	ValueTask<int> InsertManyAsync<T>(string name, IAsyncEnumerable<T> items, CancellationToken cancellation = default);
	ValueTask<int> InsertManyAsync<T>(string name, IAsyncEnumerable<T> items, DataInsertOptions options, CancellationToken cancellation = default);

	ValueTask<int> InsertManyAsync<T>(string name, IAsyncEnumerable<T> items, string schema, CancellationToken cancellation = default);
	ValueTask<int> InsertManyAsync<T>(string name, IAsyncEnumerable<T> items, string schema, DataInsertOptions options, CancellationToken cancellation) => this.InsertManyAsync<T>(name, items, schema, options, default(Func<DataInsertContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataInsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> InsertManyAsync<T>(string name, IAsyncEnumerable<T> items, string schema, DataInsertOptions options, Func<DataInsertContextBase, CancellationToken, ValueTask<bool>> inserting, CancellationToken cancellation) => this.InsertManyAsync<T>(name, items, schema, options, inserting, default(Func<DataInsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> InsertManyAsync<T>(string name, IAsyncEnumerable<T> items, string schema, DataInsertOptions options, Func<DataInsertContextBase, CancellationToken, ValueTask<bool>> inserting = null, Func<DataInsertContextBase, CancellationToken, ValueTask> inserted = null, CancellationToken cancellation = default);

	ValueTask<int> InsertManyAsync<T>(string name, IAsyncEnumerable<T> items, ISchema schema, DataInsertOptions options, CancellationToken cancellation) => this.InsertManyAsync<T>(name, items, schema, options, default(Func<DataInsertContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataInsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> InsertManyAsync<T>(string name, IAsyncEnumerable<T> items, ISchema schema, DataInsertOptions options, Func<DataInsertContextBase, CancellationToken, ValueTask<bool>> inserting, CancellationToken cancellation) => this.InsertManyAsync<T>(name, items, schema, options, inserting, default(Func<DataInsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> InsertManyAsync<T>(string name, IAsyncEnumerable<T> items, ISchema schema, DataInsertOptions options, Func<DataInsertContextBase, CancellationToken, ValueTask<bool>> inserting = null, Func<DataInsertContextBase, CancellationToken, ValueTask> inserted = null, CancellationToken cancellation = default);
	#endregion

	#region 增改方法
	ValueTask<int> UpsertAsync<T>(T data, CancellationToken cancellation = default);
	ValueTask<int> UpsertAsync<T>(T data, DataUpsertOptions options, CancellationToken cancellation = default);

	ValueTask<int> UpsertAsync<T>(T data, string schema, CancellationToken cancellation = default);
	ValueTask<int> UpsertAsync<T>(T data, string schema, DataUpsertOptions options, CancellationToken cancellation) => this.UpsertAsync<T>(data, schema, options, default(Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> UpsertAsync<T>(T data, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting, CancellationToken cancellation) => this.UpsertAsync<T>(data, schema, options, upserting, default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> UpsertAsync<T>(T data, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting = null, Func<DataUpsertContextBase, CancellationToken, ValueTask> upserted = null, CancellationToken cancellation = default);

	ValueTask<int> UpsertAsync<T>(object data, CancellationToken cancellation = default);
	ValueTask<int> UpsertAsync<T>(object data, DataUpsertOptions options, CancellationToken cancellation = default);

	ValueTask<int> UpsertAsync<T>(object data, string schema, CancellationToken cancellation = default);
	ValueTask<int> UpsertAsync<T>(object data, string schema, DataUpsertOptions options, CancellationToken cancellation) => this.UpsertAsync<T>(data, schema, options, default(Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> UpsertAsync<T>(object data, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting, CancellationToken cancellation) => this.UpsertAsync<T>(data, schema, options, upserting, default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> UpsertAsync<T>(object data, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting = null, Func<DataUpsertContextBase, CancellationToken, ValueTask> upserted = null, CancellationToken cancellation = default);

	ValueTask<int> UpsertAsync(string name, object data, CancellationToken cancellation = default);
	ValueTask<int> UpsertAsync(string name, object data, DataUpsertOptions options, CancellationToken cancellation = default);

	ValueTask<int> UpsertAsync(string name, object data, string schema, CancellationToken cancellation = default);
	ValueTask<int> UpsertAsync(string name, object data, string schema, DataUpsertOptions options, CancellationToken cancellation) => this.UpsertAsync(name, data, schema, options, default(Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> UpsertAsync(string name, object data, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting, CancellationToken cancellation) => this.UpsertAsync(name, data, schema, options, upserting, default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> UpsertAsync(string name, object data, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting = null, Func<DataUpsertContextBase, CancellationToken, ValueTask> upserted = null, CancellationToken cancellation = default);

	ValueTask<int> UpsertAsync(string name, object data, ISchema schema, DataUpsertOptions options, CancellationToken cancellation) => this.UpsertAsync(name, data, schema, options, default(Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> UpsertAsync(string name, object data, ISchema schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting, CancellationToken cancellation) => this.UpsertAsync(name, data, schema, options, upserting, default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> UpsertAsync(string name, object data, ISchema schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting = null, Func<DataUpsertContextBase, CancellationToken, ValueTask> upserted = null, CancellationToken cancellation = default);

	ValueTask<int> UpsertManyAsync<T>(IEnumerable<T> items, CancellationToken cancellation = default);
	ValueTask<int> UpsertManyAsync<T>(IEnumerable<T> items, DataUpsertOptions options, CancellationToken cancellation = default);

	ValueTask<int> UpsertManyAsync<T>(IEnumerable<T> items, string schema, CancellationToken cancellation = default);
	ValueTask<int> UpsertManyAsync<T>(IEnumerable<T> items, string schema, DataUpsertOptions options, CancellationToken cancellation) => this.UpsertManyAsync<T>(items, schema, options, default(Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> UpsertManyAsync<T>(IEnumerable<T> items, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting, CancellationToken cancellation) => this.UpsertManyAsync<T>(items, schema, options, upserting, default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> UpsertManyAsync<T>(IEnumerable<T> items, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting = null, Func<DataUpsertContextBase, CancellationToken, ValueTask> upserted = null, CancellationToken cancellation = default);

	ValueTask<int> UpsertManyAsync<T>(IEnumerable items, CancellationToken cancellation = default);
	ValueTask<int> UpsertManyAsync<T>(IEnumerable items, DataUpsertOptions options, CancellationToken cancellation = default);

	ValueTask<int> UpsertManyAsync<T>(IEnumerable items, string schema, CancellationToken cancellation = default);
	ValueTask<int> UpsertManyAsync<T>(IEnumerable items, string schema, DataUpsertOptions options, CancellationToken cancellation) => this.UpsertManyAsync<T>(items, schema, options, default(Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> UpsertManyAsync<T>(IEnumerable items, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting, CancellationToken cancellation) => this.UpsertManyAsync<T>(items, schema, options, upserting, default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> UpsertManyAsync<T>(IEnumerable items, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting = null, Func<DataUpsertContextBase, CancellationToken, ValueTask> upserted = null, CancellationToken cancellation = default);

	ValueTask<int> UpsertManyAsync(string name, IEnumerable items, CancellationToken cancellation = default);
	ValueTask<int> UpsertManyAsync(string name, IEnumerable items, DataUpsertOptions options, CancellationToken cancellation = default);

	ValueTask<int> UpsertManyAsync(string name, IEnumerable items, string schema, CancellationToken cancellation = default);
	ValueTask<int> UpsertManyAsync(string name, IEnumerable items, string schema, DataUpsertOptions options, CancellationToken cancellation) => this.UpsertManyAsync(name, items, schema, options, default(Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> UpsertManyAsync(string name, IEnumerable items, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting, CancellationToken cancellation) => this.UpsertManyAsync(name, items, schema, options, upserting, default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> UpsertManyAsync(string name, IEnumerable items, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting = null, Func<DataUpsertContextBase, CancellationToken, ValueTask> upserted = null, CancellationToken cancellation = default);

	ValueTask<int> UpsertManyAsync(string name, IEnumerable items, ISchema schema, DataUpsertOptions options, CancellationToken cancellation) => this.UpsertManyAsync(name, items, schema, options, default(Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> UpsertManyAsync(string name, IEnumerable items, ISchema schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting, CancellationToken cancellation) => this.UpsertManyAsync(name, items, schema, options, upserting, default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> UpsertManyAsync(string name, IEnumerable items, ISchema schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting = null, Func<DataUpsertContextBase, CancellationToken, ValueTask> upserted = null, CancellationToken cancellation = default);

	ValueTask<int> UpsertManyAsync<T>(IAsyncEnumerable<T> items, CancellationToken cancellation = default);
	ValueTask<int> UpsertManyAsync<T>(IAsyncEnumerable<T> items, DataUpsertOptions options, CancellationToken cancellation = default);

	ValueTask<int> UpsertManyAsync<T>(IAsyncEnumerable<T> items, string schema, CancellationToken cancellation = default);
	ValueTask<int> UpsertManyAsync<T>(IAsyncEnumerable<T> items, string schema, DataUpsertOptions options, CancellationToken cancellation) => this.UpsertManyAsync<T>(items, schema, options, default(Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> UpsertManyAsync<T>(IAsyncEnumerable<T> items, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting, CancellationToken cancellation) => this.UpsertManyAsync<T>(items, schema, options, upserting, default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> UpsertManyAsync<T>(IAsyncEnumerable<T> items, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting = null, Func<DataUpsertContextBase, CancellationToken, ValueTask> upserted = null, CancellationToken cancellation = default);

	ValueTask<int> UpsertManyAsync<T>(string name, IAsyncEnumerable<T> items, CancellationToken cancellation = default);
	ValueTask<int> UpsertManyAsync<T>(string name, IAsyncEnumerable<T> items, DataUpsertOptions options, CancellationToken cancellation = default);

	ValueTask<int> UpsertManyAsync<T>(string name, IAsyncEnumerable<T> items, string schema, CancellationToken cancellation = default);
	ValueTask<int> UpsertManyAsync<T>(string name, IAsyncEnumerable<T> items, string schema, DataUpsertOptions options, CancellationToken cancellation) => this.UpsertManyAsync<T>(name, items, schema, options, default(Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> UpsertManyAsync<T>(string name, IAsyncEnumerable<T> items, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting, CancellationToken cancellation) => this.UpsertManyAsync<T>(name, items, schema, options, upserting, default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> UpsertManyAsync<T>(string name, IAsyncEnumerable<T> items, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting = null, Func<DataUpsertContextBase, CancellationToken, ValueTask> upserted = null, CancellationToken cancellation = default);

	ValueTask<int> UpsertManyAsync<T>(string name, IAsyncEnumerable<T> items, ISchema schema, DataUpsertOptions options, CancellationToken cancellation) => this.UpsertManyAsync<T>(name, items, schema, options, default(Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> UpsertManyAsync<T>(string name, IAsyncEnumerable<T> items, ISchema schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting, CancellationToken cancellation) => this.UpsertManyAsync<T>(name, items, schema, options, upserting, default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> UpsertManyAsync<T>(string name, IAsyncEnumerable<T> items, ISchema schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting = null, Func<DataUpsertContextBase, CancellationToken, ValueTask> upserted = null, CancellationToken cancellation = default);
	#endregion

	#region 更新方法
	ValueTask<int> UpdateAsync<T>(T data, CancellationToken cancellation = default);
	ValueTask<int> UpdateAsync<T>(T data, DataUpdateOptions options, CancellationToken cancellation = default);
	ValueTask<int> UpdateAsync<T>(T data, ICondition criteria, CancellationToken cancellation = default);
	ValueTask<int> UpdateAsync<T>(T data, ICondition criteria, DataUpdateOptions options, CancellationToken cancellation = default);

	ValueTask<int> UpdateAsync<T>(T data, string schema, CancellationToken cancellation = default);
	ValueTask<int> UpdateAsync<T>(T data, string schema, DataUpdateOptions options, CancellationToken cancellation = default);
	ValueTask<int> UpdateAsync<T>(T data, ICondition criteria, string schema, CancellationToken cancellation = default);
	ValueTask<int> UpdateAsync<T>(T data, ICondition criteria, string schema, DataUpdateOptions options, CancellationToken cancellation) => this.UpdateAsync<T>(data, criteria, schema, options, default(Func<DataUpdateContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataUpdateContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> UpdateAsync<T>(T data, ICondition criteria, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, CancellationToken, ValueTask<bool>> updating, CancellationToken cancellation) => this.UpdateAsync<T>(data, criteria, schema, options, updating, default(Func<DataUpdateContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> UpdateAsync<T>(T data, ICondition criteria, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, CancellationToken, ValueTask<bool>> updating = null, Func<DataUpdateContextBase, CancellationToken, ValueTask> updated = null, CancellationToken cancellation = default);

	ValueTask<int> UpdateAsync<T>(object data, CancellationToken cancellation = default);
	ValueTask<int> UpdateAsync<T>(object data, DataUpdateOptions options, CancellationToken cancellation = default);
	ValueTask<int> UpdateAsync<T>(object data, ICondition criteria, CancellationToken cancellation = default);
	ValueTask<int> UpdateAsync<T>(object data, ICondition criteria, DataUpdateOptions options, CancellationToken cancellation = default);

	ValueTask<int> UpdateAsync<T>(object data, string schema, CancellationToken cancellation = default);
	ValueTask<int> UpdateAsync<T>(object data, string schema, DataUpdateOptions options, CancellationToken cancellation = default);
	ValueTask<int> UpdateAsync<T>(object data, ICondition criteria, string schema, CancellationToken cancellation = default);
	ValueTask<int> UpdateAsync<T>(object data, ICondition criteria, string schema, DataUpdateOptions options, CancellationToken cancellation) => this.UpdateAsync<T>(data, criteria, schema, options, default(Func<DataUpdateContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataUpdateContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> UpdateAsync<T>(object data, ICondition criteria, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, CancellationToken, ValueTask<bool>> updating, CancellationToken cancellation) => this.UpdateAsync<T>(data, criteria, schema, options, updating, default(Func<DataUpdateContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> UpdateAsync<T>(object data, ICondition criteria, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, CancellationToken, ValueTask<bool>> updating = null, Func<DataUpdateContextBase, CancellationToken, ValueTask> updated = null, CancellationToken cancellation = default);

	ValueTask<int> UpdateAsync(string name, object data, CancellationToken cancellation = default);
	ValueTask<int> UpdateAsync(string name, object data, DataUpdateOptions options, CancellationToken cancellation = default);
	ValueTask<int> UpdateAsync(string name, object data, ICondition criteria, CancellationToken cancellation = default);
	ValueTask<int> UpdateAsync(string name, object data, ICondition criteria, DataUpdateOptions options, CancellationToken cancellation = default);

	ValueTask<int> UpdateAsync(string name, object data, string schema, CancellationToken cancellation = default);
	ValueTask<int> UpdateAsync(string name, object data, string schema, DataUpdateOptions options, CancellationToken cancellation = default);
	ValueTask<int> UpdateAsync(string name, object data, ICondition criteria, string schema, CancellationToken cancellation = default);
	ValueTask<int> UpdateAsync(string name, object data, ICondition criteria, string schema, DataUpdateOptions options, CancellationToken cancellation) => this.UpdateAsync(name, data, criteria, schema, options, default(Func<DataUpdateContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataUpdateContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> UpdateAsync(string name, object data, ICondition criteria, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, CancellationToken, ValueTask<bool>> updating, CancellationToken cancellation) => this.UpdateAsync(name, data, criteria, schema, options, updating, default(Func<DataUpdateContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> UpdateAsync(string name, object data, ICondition criteria, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, CancellationToken, ValueTask<bool>> updating = null, Func<DataUpdateContextBase, CancellationToken, ValueTask> updated = null, CancellationToken cancellation = default);

	ValueTask<int> UpdateAsync(string name, object data, ICondition criteria, ISchema schema, DataUpdateOptions options, CancellationToken cancellation) => this.UpdateAsync(name, data, criteria, schema, options, default(Func<DataUpdateContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataUpdateContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> UpdateAsync(string name, object data, ICondition criteria, ISchema schema, DataUpdateOptions options, Func<DataUpdateContextBase, CancellationToken, ValueTask<bool>> updating, CancellationToken cancellation) => this.UpdateAsync(name, data, criteria, schema, options, updating, default(Func<DataUpdateContextBase, CancellationToken, ValueTask>), cancellation);
	ValueTask<int> UpdateAsync(string name, object data, ICondition criteria, ISchema schema, DataUpdateOptions options, Func<DataUpdateContextBase, CancellationToken, ValueTask<bool>> updating = null, Func<DataUpdateContextBase, CancellationToken, ValueTask> updated = null, CancellationToken cancellation = default);
	#endregion

	#region 查询方法
	IAsyncEnumerable<T> SelectAsync<T>(CancellationToken cancellation);
	IAsyncEnumerable<T> SelectAsync<T>(DataSelectOptions options = null, params Sorting[] sortings) => this.SelectAsync<T>(options, sortings, CancellationToken.None);
	IAsyncEnumerable<T> SelectAsync<T>(DataSelectOptions options, CancellationToken cancellation);
	IAsyncEnumerable<T> SelectAsync<T>(Sorting[] sortings, CancellationToken cancellation) => this.SelectAsync<T>(default(DataSelectOptions), sortings, cancellation);
	IAsyncEnumerable<T> SelectAsync<T>(DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);

	IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, params Sorting[] sortings) => this.SelectAsync<T>(criteria, sortings, CancellationToken.None);
	IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, CancellationToken cancellation);
	IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, Sorting[] sortings, CancellationToken cancellation = default);
	IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync<T>(criteria, options, sortings, CancellationToken.None);
	IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, DataSelectOptions options, CancellationToken cancellation);
	IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);

	IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, Paging paging, params Sorting[] sortings) => this.SelectAsync<T>(criteria, paging, sortings, CancellationToken.None);
	IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, Paging paging, CancellationToken cancellation);
	IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, Paging paging, Sorting[] sortings, CancellationToken cancellation = default);
	IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, Paging paging, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync<T>(criteria, paging, options, sortings, CancellationToken.None);
	IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, Paging paging, DataSelectOptions options, CancellationToken cancellation);
	IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);

	IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, params Sorting[] sortings) => this.SelectAsync<T>(criteria, schema, sortings, CancellationToken.None);
	IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, CancellationToken cancellation);
	IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, Sorting[] sortings, CancellationToken cancellation = default);
	IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync<T>(criteria, schema, options, sortings, CancellationToken.None);
	IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, DataSelectOptions options, CancellationToken cancellation);
	IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);

	IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, Paging paging, params Sorting[] sortings) => this.SelectAsync<T>(criteria, schema, paging, sortings, CancellationToken.None);
	IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, Paging paging, CancellationToken cancellation);
	IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default);
	IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync<T>(criteria, schema, paging, options, sortings, CancellationToken.None);
	IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, Paging paging, DataSelectOptions options, CancellationToken cancellation);
	IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
	IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, CancellationToken, ValueTask<bool>> selecting, Func<DataSelectContextBase, CancellationToken, ValueTask> selected, CancellationToken cancellation = default);

	IAsyncEnumerable<T> SelectAsync<T>(string name, CancellationToken cancellation);
	IAsyncEnumerable<T> SelectAsync<T>(string name, DataSelectOptions options = null, params Sorting[] sortings) => this.SelectAsync<T>(name, options, sortings, CancellationToken.None);
	IAsyncEnumerable<T> SelectAsync<T>(string name, DataSelectOptions options, CancellationToken cancellation);
	IAsyncEnumerable<T> SelectAsync<T>(string name, Sorting[] sortings, CancellationToken cancellation) => this.SelectAsync<T>(name, default(DataSelectOptions), sortings, cancellation);
	IAsyncEnumerable<T> SelectAsync<T>(string name, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);

	IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, params Sorting[] sortings) => this.SelectAsync<T>(name, criteria, sortings, CancellationToken.None);
	IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, CancellationToken cancellation);
	IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, Sorting[] sortings, CancellationToken cancellation = default);
	IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync<T>(name, criteria, options, sortings, CancellationToken.None);
	IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, DataSelectOptions options, CancellationToken cancellation);
	IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);

	IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, Paging paging, params Sorting[] sortings) => this.SelectAsync<T>(name, criteria, paging, sortings, CancellationToken.None);
	IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, Paging paging, CancellationToken cancellation);
	IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, Paging paging, Sorting[] sortings, CancellationToken cancellation = default);
	IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, Paging paging, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync<T>(name, criteria, paging, options, sortings, CancellationToken.None);
	IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, Paging paging, DataSelectOptions options, CancellationToken cancellation);
	IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);

	IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, params Sorting[] sortings) => this.SelectAsync<T>(name, criteria, schema, sortings, CancellationToken.None);
	IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, CancellationToken cancellation);
	IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, Sorting[] sortings, CancellationToken cancellation = default);
	IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync<T>(name, criteria, schema, options, sortings, CancellationToken.None);
	IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, DataSelectOptions options, CancellationToken cancellation);
	IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);

	IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, Paging paging, params Sorting[] sortings) => this.SelectAsync<T>(name, criteria, schema, paging, sortings, CancellationToken.None);
	IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, Paging paging, CancellationToken cancellation);
	IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default);
	IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync<T>(name, criteria, schema, paging, options, sortings, CancellationToken.None);
	IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, Paging paging, DataSelectOptions options, CancellationToken cancellation);
	IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
	IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, CancellationToken, ValueTask<bool>> selecting, Func<DataSelectContextBase, CancellationToken, ValueTask> selected, CancellationToken cancellation = default);

	IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, ISchema schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, CancellationToken, ValueTask<bool>> selecting, Func<DataSelectContextBase, CancellationToken, ValueTask> selected, CancellationToken cancellation = default);

	IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, params Sorting[] sortings) => this.SelectAsync<T>(name, grouping, sortings, CancellationToken.None);
	IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, CancellationToken cancellation);
	IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, Sorting[] sortings, CancellationToken cancellation = default);
	IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync<T>(name, grouping, options, sortings, CancellationToken.None);
	IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, DataSelectOptions options, CancellationToken cancellation);
	IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);

	IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, Paging paging, CancellationToken cancellation);
	IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, Paging paging, DataSelectOptions options = null, params Sorting[] sortings) => this.SelectAsync<T>(name, grouping, paging, options, sortings, CancellationToken.None);
	IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, Paging paging, DataSelectOptions options, CancellationToken cancellation);
	IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, Paging paging, Sorting[] sortings, CancellationToken cancellation) => this.SelectAsync<T>(name, grouping, paging, default(DataSelectOptions), sortings, cancellation);
	IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);

	IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, params Sorting[] sortings) => this.SelectAsync<T>(name, grouping, schema, sortings, CancellationToken.None);
	IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, CancellationToken cancellation);
	IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, Sorting[] sortings, CancellationToken cancellation = default);
	IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync<T>(name, grouping, schema, options, sortings, CancellationToken.None);
	IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, DataSelectOptions options, CancellationToken cancellation);
	IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);

	IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, Paging paging, CancellationToken cancellation);
	IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, Paging paging, DataSelectOptions options = null, params Sorting[] sortings) => this.SelectAsync<T>(name, grouping, schema, paging, options, sortings, CancellationToken.None);
	IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, Paging paging, DataSelectOptions options, CancellationToken cancellation);
	IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation) => this.SelectAsync<T>(name, grouping, schema, paging, default(DataSelectOptions), sortings, cancellation);
	IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);

	IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, CancellationToken cancellation);
	IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, Sorting[] sortings, CancellationToken cancellation) => this.SelectAsync<T>(name, grouping, criteria, default(string), sortings, cancellation);

	IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, Paging paging, params Sorting[] sortings) => this.SelectAsync<T>(name, grouping, criteria, paging, sortings, CancellationToken.None);
	IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, Paging paging, CancellationToken cancellation);
	IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, Paging paging, Sorting[] sortings, CancellationToken cancellation = default);

	IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema = null, params Sorting[] sortings) => this.SelectAsync<T>(name, grouping, criteria, schema, sortings, CancellationToken.None);
	IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, CancellationToken cancellation);
	IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, Sorting[] sortings, CancellationToken cancellation = default);
	IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync<T>(name, grouping, criteria, schema, options, sortings, CancellationToken.None);
	IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, DataSelectOptions options, CancellationToken cancellation);
	IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);

	IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, Paging paging, CancellationToken cancellation) => this.SelectAsync<T>(name, grouping, criteria, schema, paging, null, null, cancellation);
	IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options = null, params Sorting[] sortings) => this.SelectAsync<T>(name, grouping, criteria, schema, paging, options, sortings, CancellationToken.None);
	IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options, CancellationToken cancellation);
	IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation) => this.SelectAsync<T>(name, grouping, criteria, schema, paging, default(DataSelectOptions), sortings, cancellation);
	IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default);
	IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, CancellationToken, ValueTask<bool>> selecting, Func<DataSelectContextBase, CancellationToken, ValueTask> selected, CancellationToken cancellation = default);

	IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, ISchema schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, CancellationToken, ValueTask<bool>> selecting, Func<DataSelectContextBase, CancellationToken, ValueTask> selected, CancellationToken cancellation = default);
	#endregion
}
