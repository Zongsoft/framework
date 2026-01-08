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

using Zongsoft.Common;
using Zongsoft.Services;

namespace Zongsoft.Data;

/// <summary>
/// 表示数据访问的抽象基类。
/// </summary>
[System.Reflection.DefaultMember(nameof(Filters))]
public abstract class DataAccessBase : IDataAccess, IDisposable
{
	#region 事件定义
	public event EventHandler<DataAccessErrorEventArgs> Error;
	public event EventHandler<DataExecutedEventArgs> Executed;
	public event EventHandler<DataExecutingEventArgs> Executing;
	public event EventHandler<DataExistedEventArgs> Existed;
	public event EventHandler<DataExistingEventArgs> Existing;
	public event EventHandler<DataAggregatedEventArgs> Aggregated;
	public event EventHandler<DataAggregatingEventArgs> Aggregating;
	public event EventHandler<DataImportedEventArgs> Imported;
	public event EventHandler<DataImportingEventArgs> Importing;
	public event EventHandler<DataDeletedEventArgs> Deleted;
	public event EventHandler<DataDeletingEventArgs> Deleting;
	public event EventHandler<DataInsertedEventArgs> Inserted;
	public event EventHandler<DataInsertingEventArgs> Inserting;
	public event EventHandler<DataUpsertedEventArgs> Upserted;
	public event EventHandler<DataUpsertingEventArgs> Upserting;
	public event EventHandler<DataUpdatedEventArgs> Updated;
	public event EventHandler<DataUpdatingEventArgs> Updating;
	public event EventHandler<DataSelectedEventArgs> Selected;
	public event EventHandler<DataSelectingEventArgs> Selecting;
	#endregion

	#region 常量定义
	private const int DISPOSED = -1;
	private const int DISPOSING = 1;
	#endregion

	#region 私有变量
	private volatile int _disposing;
	#endregion

	#region 成员字段
	private string _name;
	private ISchemaParser _schema;
	private IDataSequencer _sequencer;
	private DataAccessFilterCollection _filters;
	#endregion

	#region 构造函数
	protected DataAccessBase(string name)
	{
		_name = name ?? string.Empty;
		_filters = new DataAccessFilterCollection();
	}
	#endregion

	#region 公共属性
	/// <summary>获取数据访问器的名称，通常为应用、模块名。</summary>
	public string Name
	{
		get => _name;
		set => _name = value ?? string.Empty;
	}

	/// <summary>获取数据模式解析器。</summary>
	public ISchemaParser Schema => _schema ??= this.CreateSchema();

	/// <summary>获取或设置数据序号生成程序。</summary>
	public IDataSequencer Sequencer
	{
		get => _sequencer ??= this.CreateSequencer();
		set => _sequencer = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>获取数据访问过滤器集合。</summary>
	public ICollection<object> Filters => _filters;
	#endregion

	#region 处置通知
	public bool IsDisposed => _disposing == DISPOSED;

	private CancellationTokenSource _cancellation = new();
	/// <summary>获取当前对象的处置通知令牌。</summary>
	public IChangeToken Disposed => Notification.GetToken(_cancellation);
	#endregion

	#region 导入方法
	public int Import<T>(IEnumerable<T> data, IEnumerable<string> members, DataImportOptions options = null) =>
		data == null ? 0 : this.Import(this.GetName<T>(), data, members, options);
	int IDataAccess.Import(string name, IEnumerable data, IEnumerable<string> members, DataImportOptions options) =>
		data == null ? 0 : this.Import(name, data, members, options);
	public int Import(string name, IEnumerable data, IEnumerable<string> members, DataImportOptions options, Func<DataImportContextBase, bool> importing = null, Action<DataImportContextBase> imported = null)
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		if(data == null)
			return 0;

		//创建数据访问上下文对象
		var context = this.CreateImportContext(name, data, members, options);

		//处理数据访问操作前的回调
		if(importing != null && importing(context))
			return context.Count;

		//激发“Importing”事件，如果被中断则返回
		if(this.OnImporting(context))
			return context.Count;

		//调用数据访问过滤器前事件
		this.OnFiltering(context);

		//执行数据导入操作
		this.OnImport(context);

		//调用数据访问过滤器后事件
		this.OnFiltered(context);

		//激发“Imported”事件
		this.OnImported(context);

		//处理数据访问操作后的回调
		imported?.Invoke(context);

		var result = context.Count;

		//处置上下文资源
		context.Dispose();

		//返回最终的结果
		return result;
	}

	public ValueTask<int> ImportAsync<T>(IEnumerable<T> data, IEnumerable<string> members, DataImportOptions options, CancellationToken cancellation = default) =>
		data == null ? ValueTask.FromResult(0) : this.ImportAsync(this.GetName<T>(), data, members, options, cancellation);
	ValueTask<int> IDataAccess.ImportAsync(string name, IEnumerable data, IEnumerable<string> members, DataImportOptions options, CancellationToken cancellation) =>
		data == null ? ValueTask.FromResult(0) : this.ImportAsync(name, data, members, options, cancellation, null, null);
	public async ValueTask<int> ImportAsync(string name, IEnumerable data, IEnumerable<string> members, DataImportOptions options, CancellationToken cancellation, Func<DataImportContextBase, bool> importing = null, Action<DataImportContextBase> imported = null)
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		if(data == null)
			return 0;

		//创建数据访问上下文对象
		var context = this.CreateImportContext(name, data, members, options);

		//处理数据访问操作前的回调
		if(importing != null && importing(context))
			return context.Count;

		//激发“Importing”事件，如果被中断则返回
		if(this.OnImporting(context))
			return context.Count;

		//调用数据访问过滤器前事件
		this.OnFiltering(context);

		//执行数据导入操作
		await this.OnImportAsync(context, cancellation);

		//调用数据访问过滤器后事件
		this.OnFiltered(context);

		//激发“Imported”事件
		this.OnImported(context);

		//处理数据访问操作后的回调
		imported?.Invoke(context);

		var result = context.Count;

		//处置上下文资源
		context.Dispose();

		//返回最终的结果
		return result;
	}

	protected abstract void OnImport(DataImportContextBase context);
	protected abstract ValueTask OnImportAsync(DataImportContextBase context, CancellationToken cancellation = default);
	#endregion

	#region 执行方法
	public IEnumerable<T> Execute<T>(string name, DataExecuteOptions options = null, Func<DataExecuteContextBase, bool> executing = null, Action<DataExecuteContextBase> executed = null) => this.Execute<T>(name, null, options, executing, executed);
	public IEnumerable<T> Execute<T>(string name, IEnumerable<Parameter> parameters, DataExecuteOptions options = null, Func<DataExecuteContextBase, bool> executing = null, Action<DataExecuteContextBase> executed = null)
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		//创建数据访问上下文对象
		var context = this.CreateExecuteContext(name, false, typeof(T), parameters, options);

		//处理数据访问操作前的回调
		if(executing != null && executing(context))
		{
			//返回委托回调的结果
			return context.Result as IEnumerable<T>;
		}

		//激发“Executing”事件，如果被中断则返回
		if(this.OnExecuting(context))
		{
			//返回事件执行后的结果
			return context.Result as IEnumerable<T>;
		}

		//调用数据访问过滤器前事件
		this.OnFiltering(context);

		//执行数据操作方法
		this.OnExecute(context);

		//调用数据访问过滤器后事件
		this.OnFiltered(context);

		//激发“Executed”事件
		this.OnExecuted(context);

		//处理数据访问操作后的回调
		executed?.Invoke(context);

		var result = ToEnumerable<T>(context.Result);

		//处置上下文资源
		context.Dispose();

		//返回最终的结果
		return result;
	}

	public IAsyncEnumerable<T> ExecuteAsync<T>(string name, CancellationToken cancellation = default) => this.ExecuteAsync<T>(name, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> ExecuteAsync<T>(string name, DataExecuteOptions options, CancellationToken cancellation = default) => this.ExecuteAsync<T>(name, null, options, null, null, cancellation);
	public IAsyncEnumerable<T> ExecuteAsync<T>(string name, DataExecuteOptions options, Func<DataExecuteContextBase, bool> executing, Action<DataExecuteContextBase> executed, CancellationToken cancellation = default) => this.ExecuteAsync<T>(name, null, options, executing, executed, cancellation);
	public IAsyncEnumerable<T> ExecuteAsync<T>(string name, IEnumerable<Parameter> parameters, CancellationToken cancellation = default) => this.ExecuteAsync<T>(name, parameters, null, null, null, cancellation);
	public IAsyncEnumerable<T> ExecuteAsync<T>(string name, IEnumerable<Parameter> parameters, DataExecuteOptions options, CancellationToken cancellation = default) => this.ExecuteAsync<T>(name, parameters, options, null, null, cancellation);
	public async IAsyncEnumerable<T> ExecuteAsync<T>(string name, IEnumerable<Parameter> parameters, DataExecuteOptions options, Func<DataExecuteContextBase, bool> executing, Action<DataExecuteContextBase> executed, [System.Runtime.CompilerServices.EnumeratorCancellation]CancellationToken cancellation = default)
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		//创建数据访问上下文对象
		var context = this.CreateExecuteContext(name, false, typeof(T), parameters, options);

		//处理数据访问操作前的回调
		if(executing != null && executing(context))
		{
			if(context.Result is IAsyncEnumerable<T> asyncEnumerable)
			{
				await using var iterator = asyncEnumerable.GetAsyncEnumerator(cancellation);
				while(await iterator.MoveNextAsync())
					yield return iterator.Current;
			}
			else if(context.Result is IEnumerable<T> enumerable)
			{
				var iterator = enumerable.GetEnumerator();
				while(iterator.MoveNext())
					yield return iterator.Current;
			}

			yield break;
		}

		//激发“Executing”事件，如果被中断则返回
		if(this.OnExecuting(context))
		{
			if(context.Result is IAsyncEnumerable<T> asyncEnumerable)
			{
				await using var iterator = asyncEnumerable.GetAsyncEnumerator(cancellation);
				while(await iterator.MoveNextAsync())
					yield return iterator.Current;
			}
			else if(context.Result is IEnumerable<T> enumerable)
			{
				var iterator = enumerable.GetEnumerator();
				while(iterator.MoveNext())
					yield return iterator.Current;
			}

			yield break;
		}

		//调用数据访问过滤器前事件
		this.OnFiltering(context);

		//执行数据操作方法
		await this.OnExecuteAsync(context, cancellation);

		//调用数据访问过滤器后事件
		this.OnFiltered(context);

		//激发“Executed”事件
		this.OnExecuted(context);

		//处理数据访问操作后的回调
		executed?.Invoke(context);

		var result = ToAsyncEnumerable<T>(context.Result, cancellation);

		//处置上下文资源
		context.Dispose();

		//返回最终的结果
		await using var enumerator = result.GetAsyncEnumerator(cancellation);
		while(await enumerator.MoveNextAsync())
			yield return enumerator.Current;
	}

	public object ExecuteScalar(string name, DataExecuteOptions options = null, Func<DataExecuteContextBase, bool> executing = null, Action<DataExecuteContextBase> executed = null) => this.ExecuteScalar(name, null, options, executing, executed);
	public object ExecuteScalar(string name, IEnumerable<Parameter> parameters, DataExecuteOptions options = null, Func<DataExecuteContextBase, bool> executing = null, Action<DataExecuteContextBase> executed = null)
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		//创建数据访问上下文对象
		var context = this.CreateExecuteContext(name, true, typeof(object), parameters, options);

		//处理数据访问操作前的回调
		if(executing != null && executing(context))
		{
			//返回委托回调的结果
			return context.Result;
		}

		//激发“Executing”事件，如果被中断则返回
		if(this.OnExecuting(context))
		{
			//返回事件执行后的结果
			return context.Result;
		}

		//调用数据访问过滤器前事件
		this.OnFiltering(context);

		//执行数据操作方法
		this.OnExecute(context);

		//调用数据访问过滤器后事件
		this.OnFiltered(context);

		//激发“Executed”事件
		this.OnExecuted(context);

		//处理数据访问操作后的回调
		executed?.Invoke(context);

		var result = context.Result;

		//处置上下文资源
		context.Dispose();

		//返回最终的结果
		return result;
	}

	public ValueTask<object> ExecuteScalarAsync(string name, CancellationToken cancellation = default) => this.ExecuteScalarAsync(name, null, null, null, null, cancellation);
	public ValueTask<object> ExecuteScalarAsync(string name, DataExecuteOptions options, CancellationToken cancellation = default) => this.ExecuteScalarAsync(name, null, options, null, null, cancellation);
	public ValueTask<object> ExecuteScalarAsync(string name, DataExecuteOptions options, Func<DataExecuteContextBase, bool> executing, Action<DataExecuteContextBase> executed, CancellationToken cancellation = default) => this.ExecuteScalarAsync(name, null, options, executing, executed, cancellation);
	public ValueTask<object> ExecuteScalarAsync(string name, IEnumerable<Parameter> parameters, CancellationToken cancellation = default) => this.ExecuteScalarAsync(name, parameters, null, null, null, cancellation);
	public ValueTask<object> ExecuteScalarAsync(string name, IEnumerable<Parameter> parameters, DataExecuteOptions options, CancellationToken cancellation = default) => this.ExecuteScalarAsync(name, parameters, options, null, null, cancellation);
	public async ValueTask<object> ExecuteScalarAsync(string name, IEnumerable<Parameter> parameters, DataExecuteOptions options, Func<DataExecuteContextBase, bool> executing, Action<DataExecuteContextBase> executed, CancellationToken cancellation = default)
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		//创建数据访问上下文对象
		var context = this.CreateExecuteContext(name, true, typeof(object), parameters, options);

		//处理数据访问操作前的回调
		if(executing != null && executing(context))
			return context.Result;

		//激发“Executing”事件，如果被中断则返回
		if(this.OnExecuting(context))
			return context.Result;

		//调用数据访问过滤器前事件
		this.OnFiltering(context);

		//执行数据操作方法
		await this.OnExecuteAsync(context, cancellation);

		//调用数据访问过滤器后事件
		this.OnFiltered(context);

		//激发“Executed”事件
		this.OnExecuted(context);

		//处理数据访问操作后的回调
		executed?.Invoke(context);

		var result = context.Result;

		//处置上下文资源
		context.Dispose();

		//返回最终的结果
		return result;
	}

	protected abstract void OnExecute(DataExecuteContextBase context);
	protected abstract ValueTask OnExecuteAsync(DataExecuteContextBase context, CancellationToken cancellation);
	#endregion

	#region 存在方法
	public bool Exists<T>(ICondition criteria, DataExistsOptions options = null, Func<DataExistContextBase, bool> existing = null, Action<DataExistContextBase> existed = null) => this.Exists(this.GetName<T>(), criteria, options, existing, existed);
	public bool Exists(string name, ICondition criteria, DataExistsOptions options = null, Func<DataExistContextBase, bool> existing = null, Action<DataExistContextBase> existed = null)
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		//创建数据访问上下文对象
		var context = this.CreateExistContext(name, criteria, options);

		//处理数据访问操作前的回调
		if(existing != null && existing(context))
			return context.Result;

		//激发“Existing”事件，如果被中断则返回
		if(this.OnExisting(context))
			return context.Result;

		//调用数据访问过滤器前事件
		this.OnFiltering(context);

		//执行存在操作方法
		this.OnExists(context);

		//调用数据访问过滤器后事件
		this.OnFiltered(context);

		//激发“Existed”事件
		this.OnExisted(context);

		//处理数据访问操作后的回调
		if(existed != null)
			existed(context);

		var result = context.Result;

		//处置上下文资源
		context.Dispose();

		//返回最终的结果
		return result;
	}

	public ValueTask<bool> ExistsAsync<T>(ICondition criteria, DataExistsOptions options = null, Func<DataExistContextBase, bool> existing = null, Action<DataExistContextBase> existed = null, CancellationToken cancellation = default) => this.ExistsAsync(this.GetName<T>(), criteria, options, existing, existed, cancellation);
	public async ValueTask<bool> ExistsAsync(string name, ICondition criteria, DataExistsOptions options = null, Func<DataExistContextBase, bool> existing = null, Action<DataExistContextBase> existed = null, CancellationToken cancellation = default)
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		//创建数据访问上下文对象
		var context = this.CreateExistContext(name, criteria, options);

		//处理数据访问操作前的回调
		if(existing != null && existing(context))
			return context.Result;

		//激发“Existing”事件，如果被中断则返回
		if(this.OnExisting(context))
			return context.Result;

		//调用数据访问过滤器前事件
		this.OnFiltering(context);

		//执行存在操作方法
		await this.OnExistsAsync(context, cancellation);

		//调用数据访问过滤器后事件
		this.OnFiltered(context);

		//激发“Existed”事件
		this.OnExisted(context);

		//处理数据访问操作后的回调
		if(existed != null)
			existed(context);

		var result = context.Result;

		//处置上下文资源
		context.Dispose();

		//返回最终的结果
		return result;
	}

	protected abstract void OnExists(DataExistContextBase context);
	protected abstract ValueTask OnExistsAsync(DataExistContextBase context, CancellationToken cancellation);
	#endregion

	#region 聚合方法
	public TValue? Aggregate<T, TValue>(DataAggregateFunction function, string member, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => this.Aggregate<TValue>(this.GetName<T>(), new DataAggregate(function, member), criteria, options, null, null);
	public TValue? Aggregate<T, TValue>(DataAggregateFunction function, string member, string alias, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => this.Aggregate<TValue>(this.GetName<T>(), new DataAggregate(function, member, alias), criteria, options, null, null);
	public TValue? Aggregate<T, TValue>(DataAggregateFunction function, string member, bool distinct, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => this.Aggregate<TValue>(this.GetName<T>(), new DataAggregate(function, member, distinct), criteria, options, null, null);
	public TValue? Aggregate<T, TValue>(DataAggregateFunction function, string member, bool distinct, string alias, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => this.Aggregate<TValue>(this.GetName<T>(), new DataAggregate(function, member, distinct, alias), criteria, options, null, null);
	public TValue? Aggregate<T, TValue>(DataAggregate aggregate, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null) where TValue : struct, IEquatable<TValue> => this.Aggregate<TValue>(this.GetName<T>(), aggregate, criteria, options, aggregating, aggregated);

	public TValue? Aggregate<TValue>(string name, DataAggregateFunction function, string member, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => this.Aggregate<TValue>(name, new DataAggregate(function, member), criteria, options, null, null);
	public TValue? Aggregate<TValue>(string name, DataAggregateFunction function, string member, string alias, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => this.Aggregate<TValue>(name, new DataAggregate(function, member, alias), criteria, options, null, null);
	public TValue? Aggregate<TValue>(string name, DataAggregateFunction function, string member, bool distinct, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => this.Aggregate<TValue>(name, new DataAggregate(function, member, distinct), criteria, options, null, null);
	public TValue? Aggregate<TValue>(string name, DataAggregateFunction function, string member, bool distinct, string alias, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => this.Aggregate<TValue>(name, new DataAggregate(function, member, distinct, alias), criteria, options, null, null);
	public TValue? Aggregate<TValue>(string name, DataAggregate aggregate, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null) where TValue : struct, IEquatable<TValue>
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		//创建数据访问上下文对象
		var context = this.CreateAggregateContext(name, aggregate, criteria, options);

		//处理数据访问操作前的回调
		if(aggregating != null && aggregating(context))
			return context.GetValue<TValue>();

		//激发“Aggregating”事件，如果被中断则返回
		if(this.OnAggregating(context))
			return context.GetValue<TValue>();

		//调用数据访问过滤器前事件
		this.OnFiltering(context);

		//执行聚合操作方法
		this.OnAggregate(context);

		//调用数据访问过滤器后事件
		this.OnFiltered(context);

		//激发“Aggregated”事件
		this.OnAggregated(context);

		//处理数据访问操作后的回调
		if(aggregated != null)
			aggregated(context);

		var value = context.GetValue<TValue>();

		//处置上下文资源
		context.Dispose();

		//返回最终的结果
		return value;
	}

	public ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregateFunction function, string member, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(this.GetName<T>(), new DataAggregate(function, member), criteria, options, null, null, cancellation);
	public ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregateFunction function, string member, string alias, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(this.GetName<T>(), new DataAggregate(function, member, alias), criteria, options, null, null, cancellation);
	public ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregateFunction function, string member, bool distinct, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(this.GetName<T>(), new DataAggregate(function, member, distinct), criteria, options, null, null, cancellation);
	public ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregateFunction function, string member, bool distinct, string alias, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(this.GetName<T>(), new DataAggregate(function, member, distinct, alias), criteria, options, null, null, cancellation);
	public ValueTask<TValue?> AggregateAsync<T, TValue>(DataAggregate aggregate, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(this.GetName<T>(), aggregate, criteria, options, aggregating, aggregated, cancellation);

	public ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregateFunction function, string member, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(name, new DataAggregate(function, member), criteria, options, null, null, cancellation);
	public ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregateFunction function, string member, string alias, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(name, new DataAggregate(function, member, alias), criteria, options, null, null, cancellation);
	public ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregateFunction function, string member, bool distinct, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(name, new DataAggregate(function, member, distinct), criteria, options, null, null, cancellation);
	public ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregateFunction function, string member, bool distinct, string alias, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(name, new DataAggregate(function, member, distinct, alias), criteria, options, null, null, cancellation);
	public async ValueTask<TValue?> AggregateAsync<TValue>(string name, DataAggregate aggregate, ICondition criteria = null, DataAggregateOptions options = null, Func<DataAggregateContextBase, bool> aggregating = null, Action<DataAggregateContextBase> aggregated = null, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue>
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		//创建数据访问上下文对象
		var context = this.CreateAggregateContext(name, aggregate, criteria, options);

		//处理数据访问操作前的回调
		if(aggregating != null && aggregating(context))
			return context.GetValue<TValue>();

		//激发“Aggregating”事件，如果被中断则返回
		if(this.OnAggregating(context))
			return context.GetValue<TValue>();

		//调用数据访问过滤器前事件
		this.OnFiltering(context);

		//执行聚合操作方法
		await this.OnAggregateAsync(context, cancellation);

		//调用数据访问过滤器后事件
		this.OnFiltered(context);

		//激发“Aggregated”事件
		this.OnAggregated(context);

		//处理数据访问操作后的回调
		if(aggregated != null)
			aggregated(context);

		var value = context.GetValue<TValue>();

		//处置上下文资源
		context.Dispose();

		//返回最终的结果
		return value;
	}

	protected abstract void OnAggregate(DataAggregateContextBase context);
	protected abstract ValueTask OnAggregateAsync(DataAggregateContextBase context, CancellationToken cancellation);
	#endregion

	#region 删除方法
	public int Delete<T>(ICondition criteria, string schema = null) => this.Delete(this.GetName<T>(), criteria, schema, null, null, null);
	public int Delete<T>(ICondition criteria, DataDeleteOptions options) => this.Delete(this.GetName<T>(), criteria, string.Empty, options, null, null);
	public int Delete<T>(ICondition criteria, string schema, DataDeleteOptions options, Func<DataDeleteContextBase, bool> deleting = null, Action<DataDeleteContextBase> deleted = null) => this.Delete(this.GetName<T>(), criteria, schema, options, deleting, deleted);
	public int Delete(string name, ICondition criteria, string schema = null) => this.Delete(name, criteria, schema, null, null, null);
	public int Delete(string name, ICondition criteria, DataDeleteOptions options) => this.Delete(name, criteria, string.Empty, options, null, null);
	public int Delete(string name, ICondition criteria, string schema, DataDeleteOptions options, Func<DataDeleteContextBase, bool> deleting = null, Action<DataDeleteContextBase> deleted = null) => this.Delete(name, criteria, this.Schema.Parse(name, schema), options, deleting, deleted);
	public int Delete(string name, ICondition criteria, ISchema schema, DataDeleteOptions options, Func<DataDeleteContextBase, bool> deleting = null, Action<DataDeleteContextBase> deleted = null)
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		//创建数据访问上下文对象
		var context = this.CreateDeleteContext(name, criteria, schema, options);

		//处理数据访问操作前的回调
		if(deleting != null && deleting(context))
			return context.Count;

		//激发“Deleting”事件，如果被中断则返回
		if(this.OnDeleting(context))
			return context.Count;

		//调用数据访问过滤器前事件
		this.OnFiltering(context);

		//执行数据删除操作
		this.OnDelete(context);

		//调用数据访问过滤器后事件
		this.OnFiltered(context);

		//激发“Deleted”事件
		this.OnDeleted(context);

		//处理数据访问操作后的回调
		if(deleted != null)
			deleted(context);

		var result = context.Count;

		//处置上下文资源
		context.Dispose();

		//返回最终的结果
		return result;
	}

	public ValueTask<int> DeleteAsync<T>(ICondition criteria, string schema = null, CancellationToken cancellation = default) => this.DeleteAsync(this.GetName<T>(), criteria, schema, null, null, null, cancellation);
	public ValueTask<int> DeleteAsync<T>(ICondition criteria, DataDeleteOptions options, CancellationToken cancellation = default) => this.DeleteAsync(this.GetName<T>(), criteria, String.Empty, options, null, null, cancellation);
	public ValueTask<int> DeleteAsync<T>(ICondition criteria, string schema, DataDeleteOptions options, Func<DataDeleteContextBase, bool> deleting = null, Action<DataDeleteContextBase> deleted = null, CancellationToken cancellation = default) => this.DeleteAsync(this.GetName<T>(), criteria, schema, options, deleting, deleted, cancellation);
	public ValueTask<int> DeleteAsync(string name, ICondition criteria, string schema = null, CancellationToken cancellation = default) => this.DeleteAsync(name, criteria, schema, null, null, null, cancellation);
	public ValueTask<int> DeleteAsync(string name, ICondition criteria, DataDeleteOptions options, CancellationToken cancellation = default) => this.DeleteAsync(name, criteria, string.Empty, options, null, null, cancellation);
	public ValueTask<int> DeleteAsync(string name, ICondition criteria, string schema, DataDeleteOptions options, Func<DataDeleteContextBase, bool> deleting = null, Action<DataDeleteContextBase> deleted = null, CancellationToken cancellation = default) => this.DeleteAsync(name, criteria, this.Schema.Parse(name, schema), options, deleting, deleted, cancellation);
	public async ValueTask<int> DeleteAsync(string name, ICondition criteria, ISchema schema, DataDeleteOptions options, Func<DataDeleteContextBase, bool> deleting = null, Action<DataDeleteContextBase> deleted = null, CancellationToken cancellation = default)
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		//创建数据访问上下文对象
		var context = this.CreateDeleteContext(name, criteria, schema, options);

		//处理数据访问操作前的回调
		if(deleting != null && deleting(context))
			return context.Count;

		//激发“Deleting”事件，如果被中断则返回
		if(this.OnDeleting(context))
			return context.Count;

		//调用数据访问过滤器前事件
		this.OnFiltering(context);

		//执行数据删除操作
		await this.OnDeleteAsync(context, cancellation);

		//调用数据访问过滤器后事件
		this.OnFiltered(context);

		//激发“Deleted”事件
		this.OnDeleted(context);

		//处理数据访问操作后的回调
		if(deleted != null)
			deleted(context);

		var result = context.Count;

		//处置上下文资源
		context.Dispose();

		//返回最终的结果
		return result;
	}

	protected abstract void OnDelete(DataDeleteContextBase context);
	protected abstract ValueTask OnDeleteAsync(DataDeleteContextBase context, CancellationToken cancellation);
	#endregion

	#region 插入方法
	public int Insert<T>(T data) => data == null ? 0 : this.Insert(this.GetName<T>(), data, string.Empty, null, null, null);
	public int Insert<T>(T data, DataInsertOptions options) => data == null ? 0 : this.Insert(this.GetName<T>(), data, string.Empty, options, null, null);
	public int Insert<T>(T data, string schema) => data == null ? 0 : this.Insert(this.GetName<T>(), data, schema, null, null, null);
	public int Insert<T>(T data, string schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null) => data == null ? 0 : this.Insert(this.GetName<T>(), data, schema, options, inserting, inserted);
	public int Insert<T>(object data) => data == null ? 0 : this.Insert(this.GetName<T>(), data, string.Empty, null, null, null);
	public int Insert<T>(object data, DataInsertOptions options) => data == null ? 0 : this.Insert(this.GetName<T>(), data, string.Empty, options, null, null);
	public int Insert<T>(object data, string schema) => data == null ? 0 : this.Insert(this.GetName<T>(), data, schema, null, null, null);
	public int Insert<T>(object data, string schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null) => data == null ? 0 : this.Insert(this.GetName<T>(), data, schema, options, inserting, inserted);

	public int Insert(string name, object data) => this.Insert(name, data, string.Empty, null, null, null);
	public int Insert(string name, object data, DataInsertOptions options) => this.Insert(name, data, string.Empty, options, null, null);
	public int Insert(string name, object data, string schema) => this.Insert(name, data, schema, null, null, null);
	public int Insert(string name, object data, string schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null) => this.Insert(name, data, this.Schema.Parse(name, schema, data.GetType()), options, inserting, inserted);
	public int Insert(string name, object data, ISchema schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null)
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		if(data == null)
			return 0;

		//创建数据访问上下文对象
		var context = this.CreateInsertContext(name, false, data, schema, options);

		//处理数据访问操作前的回调
		if(inserting != null && inserting(context))
			return context.Count;

		//激发“Inserting”事件，如果被中断则返回
		if(this.OnInserting(context))
			return context.Count;

		//调用数据访问过滤器前事件
		this.OnFiltering(context);

		//执行数据插入操作
		this.OnInsert(context);

		//调用数据访问过滤器后事件
		this.OnFiltered(context);

		//激发“Inserted”事件
		this.OnInserted(context);

		//处理数据访问操作后的回调
		if(inserted != null)
			inserted(context);

		var result = context.Count;

		//处置上下文资源
		context.Dispose();

		//返回最终的结果
		return result;
	}

	public ValueTask<int> InsertAsync<T>(T data, CancellationToken cancellation = default) => data == null ? ValueTask.FromResult(0) : this.InsertAsync(this.GetName<T>(), data, string.Empty, null, null, null, cancellation);
	public ValueTask<int> InsertAsync<T>(T data, DataInsertOptions options, CancellationToken cancellation = default) => data == null ? ValueTask.FromResult(0) : this.InsertAsync(this.GetName<T>(), data, string.Empty, options, null, null, cancellation);
	public ValueTask<int> InsertAsync<T>(T data, string schema, CancellationToken cancellation = default) => data == null ? ValueTask.FromResult(0) : this.InsertAsync(this.GetName<T>(), data, schema, null, null, null, cancellation);
	public ValueTask<int> InsertAsync<T>(T data, string schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null, CancellationToken cancellation = default) => data == null ? ValueTask.FromResult(0) : this.InsertAsync(this.GetName<T>(), data, schema, options, inserting, inserted, cancellation);
	public ValueTask<int> InsertAsync<T>(object data, CancellationToken cancellation = default) => data == null ? ValueTask.FromResult(0) : this.InsertAsync(this.GetName<T>(), data, string.Empty, null, null, null, cancellation);
	public ValueTask<int> InsertAsync<T>(object data, DataInsertOptions options, CancellationToken cancellation = default) => data == null ? ValueTask.FromResult(0) : this.InsertAsync(this.GetName<T>(), data, string.Empty, options, null, null, cancellation);
	public ValueTask<int> InsertAsync<T>(object data, string schema, CancellationToken cancellation = default) => data == null ? ValueTask.FromResult(0) : this.InsertAsync(this.GetName<T>(), data, schema, null, null, null, cancellation);
	public ValueTask<int> InsertAsync<T>(object data, string schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null, CancellationToken cancellation = default) => data == null ? ValueTask.FromResult(0) : this.InsertAsync(this.GetName<T>(), data, schema, options, inserting, inserted, cancellation);

	public ValueTask<int> InsertAsync(string name, object data, CancellationToken cancellation = default) => this.InsertAsync(name, data, string.Empty, null, null, null, cancellation);
	public ValueTask<int> InsertAsync(string name, object data, DataInsertOptions options, CancellationToken cancellation = default) => this.InsertAsync(name, data, string.Empty, options, null, null, cancellation);
	public ValueTask<int> InsertAsync(string name, object data, string schema, CancellationToken cancellation = default) => this.InsertAsync(name, data, schema, null, null, null, cancellation);
	public ValueTask<int> InsertAsync(string name, object data, string schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null, CancellationToken cancellation = default) => this.InsertAsync(name, data, this.Schema.Parse(name, schema, data.GetType()), options, inserting, inserted, cancellation);
	public async ValueTask<int> InsertAsync(string name, object data, ISchema schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null, CancellationToken cancellation = default)
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		if(data == null)
			return 0;

		//创建数据访问上下文对象
		var context = this.CreateInsertContext(name, false, data, schema, options);

		//处理数据访问操作前的回调
		if(inserting != null && inserting(context))
			return context.Count;

		//激发“Inserting”事件，如果被中断则返回
		if(this.OnInserting(context))
			return context.Count;

		//调用数据访问过滤器前事件
		this.OnFiltering(context);

		//执行数据插入操作
		await this.OnInsertAsync(context, cancellation);

		//调用数据访问过滤器后事件
		this.OnFiltered(context);

		//激发“Inserted”事件
		this.OnInserted(context);

		//处理数据访问操作后的回调
		if(inserted != null)
			inserted(context);

		var result = context.Count;

		//处置上下文资源
		context.Dispose();

		//返回最终的结果
		return result;
	}

	public int InsertMany<T>(IEnumerable<T> items) => items == null ? 0 : this.InsertMany(this.GetName<T>(), items, string.Empty, null, null, null);
	public int InsertMany<T>(IEnumerable<T> items, DataInsertOptions options) => items == null ? 0 : this.InsertMany(this.GetName<T>(), items, string.Empty, options, null, null);
	public int InsertMany<T>(IEnumerable<T> items, string schema) => items == null ? 0 : this.InsertMany(this.GetName<T>(), items, schema, null, null, null);
	public int InsertMany<T>(IEnumerable<T> items, string schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null) => items == null ? 0 : this.InsertMany(this.GetName<T>(), items, schema, options, inserting, inserted);
	public int InsertMany<T>(IEnumerable items) => items == null ? 0 : this.InsertMany(this.GetName<T>(), items, string.Empty, null, null, null);
	public int InsertMany<T>(IEnumerable items, DataInsertOptions options) => items == null ? 0 : this.InsertMany(this.GetName<T>(), items, string.Empty, options, null, null);
	public int InsertMany<T>(IEnumerable items, string schema) => items == null ? 0 : this.InsertMany(this.GetName<T>(), items, schema, null, null, null);
	public int InsertMany<T>(IEnumerable items, string schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null) => items == null ? 0 : this.InsertMany(this.GetName<T>(), items, schema, options, inserting, inserted);
	public int InsertMany(string name, IEnumerable items) => this.InsertMany(name, items, string.Empty, null, null, null);
	public int InsertMany(string name, IEnumerable items, DataInsertOptions options) => this.InsertMany(name, items, string.Empty, options, null, null);
	public int InsertMany(string name, IEnumerable items, string schema) => this.InsertMany(name, items, schema, null, null, null);
	public int InsertMany(string name, IEnumerable items, string schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null) => this.InsertMany(name, items, this.Schema.Parse(name, schema, Common.TypeExtension.GetElementType(items.GetType())), options, inserting, inserted);
	public int InsertMany(string name, IEnumerable items, ISchema schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null)
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		if(items == null)
			return 0;

		//创建数据访问上下文对象
		var context = this.CreateInsertContext(name, true, items, schema, options);

		//处理数据访问操作前的回调
		if(inserting != null && inserting(context))
			return context.Count;

		//激发“Inserting”事件，如果被中断则返回
		if(this.OnInserting(context))
			return context.Count;

		//调用数据访问过滤器前事件
		this.OnFiltering(context);

		//执行数据插入操作
		this.OnInsert(context);

		//调用数据访问过滤器后事件
		this.OnFiltered(context);

		//激发“Inserted”事件
		this.OnInserted(context);

		//处理数据访问操作后的回调
		if(inserted != null)
			inserted(context);

		var result = context.Count;

		//处置上下文资源
		context.Dispose();

		//返回最终的结果
		return result;
	}

	public ValueTask<int> InsertManyAsync<T>(IEnumerable<T> items, CancellationToken cancellation = default) => items == null ? ValueTask.FromResult(0) : this.InsertManyAsync(this.GetName<T>(), items, string.Empty, null, null, null, cancellation);
	public ValueTask<int> InsertManyAsync<T>(IEnumerable<T> items, DataInsertOptions options, CancellationToken cancellation = default) => items == null ? ValueTask.FromResult(0) : this.InsertManyAsync(this.GetName<T>(), items, string.Empty, options, null, null, cancellation);
	public ValueTask<int> InsertManyAsync<T>(IEnumerable<T> items, string schema, CancellationToken cancellation = default) => items == null ? ValueTask.FromResult(0) : this.InsertManyAsync(this.GetName<T>(), items, schema, null, null, null, cancellation);
	public ValueTask<int> InsertManyAsync<T>(IEnumerable<T> items, string schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null, CancellationToken cancellation = default) => items == null ? ValueTask.FromResult(0) : this.InsertManyAsync(this.GetName<T>(), items, schema, options, inserting, inserted, cancellation);
	public ValueTask<int> InsertManyAsync<T>(IEnumerable items, CancellationToken cancellation = default) => items == null ? ValueTask.FromResult(0) : this.InsertManyAsync(this.GetName<T>(), items, string.Empty, null, null, null, cancellation);
	public ValueTask<int> InsertManyAsync<T>(IEnumerable items, DataInsertOptions options, CancellationToken cancellation = default) => items == null ? ValueTask.FromResult(0) : this.InsertManyAsync(this.GetName<T>(), items, string.Empty, options, null, null, cancellation);
	public ValueTask<int> InsertManyAsync<T>(IEnumerable items, string schema, CancellationToken cancellation = default) => items == null ? ValueTask.FromResult(0) : this.InsertManyAsync(this.GetName<T>(), items, schema, null, null, null, cancellation);
	public ValueTask<int> InsertManyAsync<T>(IEnumerable items, string schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null, CancellationToken cancellation = default) => items == null ? ValueTask.FromResult(0) : this.InsertManyAsync(this.GetName<T>(), items, schema, options, inserting, inserted, cancellation);
	public ValueTask<int> InsertManyAsync(string name, IEnumerable items, CancellationToken cancellation = default) => this.InsertManyAsync(name, items, string.Empty, null, null, null, cancellation);
	public ValueTask<int> InsertManyAsync(string name, IEnumerable items, DataInsertOptions options, CancellationToken cancellation = default) => this.InsertManyAsync(name, items, string.Empty, options, null, null, cancellation);
	public ValueTask<int> InsertManyAsync(string name, IEnumerable items, string schema, CancellationToken cancellation = default) => this.InsertManyAsync(name, items, schema, null, null, null, cancellation);
	public ValueTask<int> InsertManyAsync(string name, IEnumerable items, string schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null, CancellationToken cancellation = default) => this.InsertManyAsync(name, items, this.Schema.Parse(name, schema, Common.TypeExtension.GetElementType(items.GetType())), options, inserting, inserted, cancellation);
	public async ValueTask<int> InsertManyAsync(string name, IEnumerable items, ISchema schema, DataInsertOptions options, Func<DataInsertContextBase, bool> inserting = null, Action<DataInsertContextBase> inserted = null, CancellationToken cancellation = default)
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		if(items == null)
			return 0;

		//创建数据访问上下文对象
		var context = this.CreateInsertContext(name, true, items, schema, options);

		//处理数据访问操作前的回调
		if(inserting != null && inserting(context))
			return context.Count;

		//激发“Inserting”事件，如果被中断则返回
		if(this.OnInserting(context))
			return context.Count;

		//调用数据访问过滤器前事件
		this.OnFiltering(context);

		//执行数据插入操作
		await this.OnInsertAsync(context, cancellation);

		//调用数据访问过滤器后事件
		this.OnFiltered(context);

		//激发“Inserted”事件
		this.OnInserted(context);

		//处理数据访问操作后的回调
		if(inserted != null)
			inserted(context);

		var result = context.Count;

		//处置上下文资源
		context.Dispose();

		//返回最终的结果
		return result;
	}

	protected abstract void OnInsert(DataInsertContextBase context);
	protected abstract ValueTask OnInsertAsync(DataInsertContextBase context, CancellationToken cancellation);
	#endregion

	#region 增改方法
	public int Upsert<T>(T data) => data == null ? 0 : this.Upsert(this.GetName<T>(), data, string.Empty, null, null, null);
	public int Upsert<T>(T data, DataUpsertOptions options) => data == null ? 0 : this.Upsert(this.GetName<T>(), data, string.Empty, options, null, null);
	public int Upsert<T>(T data, string schema) => data == null ? 0 : this.Upsert(this.GetName<T>(), data, schema, null, null, null);
	public int Upsert<T>(T data, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null) => data == null ? 0 : this.Upsert(this.GetName<T>(), data, schema, options, upserting, upserted);

	public int Upsert<T>(object data) => data == null ? 0 : this.Upsert(this.GetName<T>(), data, string.Empty, null, null, null);
	public int Upsert<T>(object data, DataUpsertOptions options) => data == null ? 0 : this.Upsert(this.GetName<T>(), data, string.Empty, options, null, null);
	public int Upsert<T>(object data, string schema) => data == null ? 0 : this.Upsert(this.GetName<T>(), data, schema, null, null, null);
	public int Upsert<T>(object data, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null) => data == null ? 0 : this.Upsert(this.GetName<T>(), data, schema, options, upserting, upserted);

	public int Upsert(string name, object data) => this.Upsert(name, data, string.Empty, null, null, null);
	public int Upsert(string name, object data, DataUpsertOptions options) => this.Upsert(name, data, string.Empty, options, null, null);
	public int Upsert(string name, object data, string schema) => this.Upsert(name, data, schema, null, null, null);
	public int Upsert(string name, object data, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null) => this.Upsert(name, data, this.Schema.Parse(name, schema, data.GetType()), options, upserting, upserted);
	public int Upsert(string name, object data, ISchema schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null)
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		if(data == null)
			return 0;

		//创建数据访问上下文对象
		var context = this.CreateUpsertContext(name, false, data, schema, options);

		//处理数据访问操作前的回调
		if(upserting != null && upserting(context))
			return context.Count;

		//激发“Upserting”事件，如果被中断则返回
		if(this.OnUpserting(context))
			return context.Count;

		//调用数据访问过滤器前事件
		this.OnFiltering(context);

		//执行数据插入操作
		this.OnUpsert(context);

		//调用数据访问过滤器后事件
		this.OnFiltered(context);

		//激发“Upserted”事件
		this.OnUpserted(context);

		//处理数据访问操作后的回调
		if(upserted != null)
			upserted(context);

		var result = context.Count;

		//处置上下文资源
		context.Dispose();

		//返回最终的结果
		return result;
	}

	public ValueTask<int> UpsertAsync<T>(T data, CancellationToken cancellation = default) => data == null ? ValueTask.FromResult(0) : this.UpsertAsync(this.GetName<T>(), data, string.Empty, null, null, null, cancellation);
	public ValueTask<int> UpsertAsync<T>(T data, DataUpsertOptions options, CancellationToken cancellation = default) => data == null ? ValueTask.FromResult(0) : this.UpsertAsync(this.GetName<T>(), data, string.Empty, options, null, null, cancellation);
	public ValueTask<int> UpsertAsync<T>(T data, string schema, CancellationToken cancellation = default) => data == null ? ValueTask.FromResult(0) : this.UpsertAsync(this.GetName<T>(), data, schema, null, null, null, cancellation);
	public ValueTask<int> UpsertAsync<T>(T data, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null, CancellationToken cancellation = default) => data == null ? ValueTask.FromResult(0) : this.UpsertAsync(this.GetName<T>(), data, schema, options, upserting, upserted, cancellation);
	public ValueTask<int> UpsertAsync<T>(object data, CancellationToken cancellation = default) => data == null ? ValueTask.FromResult(0) : this.UpsertAsync(this.GetName<T>(), data, string.Empty, null, null, null, cancellation);
	public ValueTask<int> UpsertAsync<T>(object data, DataUpsertOptions options, CancellationToken cancellation = default) => data == null ? ValueTask.FromResult(0) : this.UpsertAsync(this.GetName<T>(), data, string.Empty, options, null, null, cancellation);
	public ValueTask<int> UpsertAsync<T>(object data, string schema, CancellationToken cancellation = default) => data == null ? ValueTask.FromResult(0) : this.UpsertAsync(this.GetName<T>(), data, schema, null, null, null, cancellation);
	public ValueTask<int> UpsertAsync<T>(object data, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null, CancellationToken cancellation = default) => data == null ? ValueTask.FromResult(0) : this.UpsertAsync(this.GetName<T>(), data, schema, options, upserting, upserted, cancellation);

	public ValueTask<int> UpsertAsync(string name, object data, CancellationToken cancellation = default) => this.UpsertAsync(name, data, string.Empty, null, null, null, cancellation);
	public ValueTask<int> UpsertAsync(string name, object data, DataUpsertOptions options, CancellationToken cancellation = default) => this.UpsertAsync(name, data, string.Empty, options, null, null, cancellation);
	public ValueTask<int> UpsertAsync(string name, object data, string schema, CancellationToken cancellation = default) => this.UpsertAsync(name, data, schema, null, null, null, cancellation);
	public ValueTask<int> UpsertAsync(string name, object data, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null, CancellationToken cancellation = default) => this.UpsertAsync(name, data, this.Schema.Parse(name, schema, data.GetType()), options, upserting, upserted, cancellation);

	public async ValueTask<int> UpsertAsync(string name, object data, ISchema schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null, CancellationToken cancellation = default)
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		if(data == null)
			return 0;

		//创建数据访问上下文对象
		var context = this.CreateUpsertContext(name, false, data, schema, options);

		//处理数据访问操作前的回调
		if(upserting != null && upserting(context))
			return context.Count;

		//激发“Upserting”事件，如果被中断则返回
		if(this.OnUpserting(context))
			return context.Count;

		//调用数据访问过滤器前事件
		this.OnFiltering(context);

		//执行数据插入操作
		await this.OnUpsertAsync(context, cancellation);

		//调用数据访问过滤器后事件
		this.OnFiltered(context);

		//激发“Upserted”事件
		this.OnUpserted(context);

		//处理数据访问操作后的回调
		if(upserted != null)
			upserted(context);

		var result = context.Count;

		//处置上下文资源
		context.Dispose();

		//返回最终的结果
		return result;
	}

	public int UpsertMany<T>(IEnumerable<T> items) => items == null ? 0 : this.UpsertMany(this.GetName<T>(), items, string.Empty, null, null, null);
	public int UpsertMany<T>(IEnumerable<T> items, DataUpsertOptions options) => items == null ? 0 : this.UpsertMany(this.GetName<T>(), items, string.Empty, options, null, null);
	public int UpsertMany<T>(IEnumerable<T> items, string schema) => items == null ? 0 : this.UpsertMany(this.GetName<T>(), items, schema, null, null, null);
	public int UpsertMany<T>(IEnumerable<T> items, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null) => items == null ? 0 : this.UpsertMany(this.GetName<T>(), items, schema, options, upserting, upserted);
	public int UpsertMany<T>(IEnumerable items) => items == null ? 0 : this.UpsertMany(this.GetName<T>(), items, string.Empty, null, null, null);
	public int UpsertMany<T>(IEnumerable items, DataUpsertOptions options) => items == null ? 0 : this.UpsertMany(this.GetName<T>(), items, string.Empty, options, null, null);
	public int UpsertMany<T>(IEnumerable items, string schema) => items == null ? 0 : this.UpsertMany(this.GetName<T>(), items, schema, null, null, null);
	public int UpsertMany<T>(IEnumerable items, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null) => items == null ? 0 : this.UpsertMany(this.GetName<T>(), items, schema, options, upserting, upserted);

	public int UpsertMany(string name, IEnumerable items) => this.UpsertMany(name, items, string.Empty, null, null, null);
	public int UpsertMany(string name, IEnumerable items, DataUpsertOptions options) => this.UpsertMany(name, items, string.Empty, options, null, null);
	public int UpsertMany(string name, IEnumerable items, string schema) => this.UpsertMany(name, items, schema, null, null, null);
	public int UpsertMany(string name, IEnumerable items, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null) => this.UpsertMany(name, items, this.Schema.Parse(name, schema, Common.TypeExtension.GetElementType(items.GetType())), options, upserting, upserted);
	public int UpsertMany(string name, IEnumerable items, ISchema schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null)
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		if(items == null)
			return 0;

		//创建数据访问上下文对象
		var context = this.CreateUpsertContext(name, true, items, schema, options);

		//处理数据访问操作前的回调
		if(upserting != null && upserting(context))
			return context.Count;

		//激发“Upserting”事件，如果被中断则返回
		if(this.OnUpserting(context))
			return context.Count;

		//调用数据访问过滤器前事件
		this.OnFiltering(context);

		//执行数据增改操作
		this.OnUpsert(context);

		//调用数据访问过滤器后事件
		this.OnFiltered(context);

		//激发“Upserted”事件
		this.OnUpserted(context);

		//处理数据访问操作后的回调
		if(upserted != null)
			upserted(context);

		var result = context.Count;

		//处置上下文资源
		context.Dispose();

		//返回最终的结果
		return result;
	}

	public ValueTask<int> UpsertManyAsync<T>(IEnumerable<T> items, CancellationToken cancellation = default) => items == null ? ValueTask.FromResult(0) : this.UpsertManyAsync(this.GetName<T>(), items, string.Empty, null, null, null, cancellation);
	public ValueTask<int> UpsertManyAsync<T>(IEnumerable<T> items, DataUpsertOptions options, CancellationToken cancellation = default) => items == null ? ValueTask.FromResult(0) : this.UpsertManyAsync(this.GetName<T>(), items, string.Empty, options, null, null, cancellation);
	public ValueTask<int> UpsertManyAsync<T>(IEnumerable<T> items, string schema, CancellationToken cancellation = default) => items == null ? ValueTask.FromResult(0) : this.UpsertManyAsync(this.GetName<T>(), items, schema, null, null, null, cancellation);
	public ValueTask<int> UpsertManyAsync<T>(IEnumerable<T> items, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null, CancellationToken cancellation = default) => items == null ? ValueTask.FromResult(0) : this.UpsertManyAsync(this.GetName<T>(), items, schema, options, upserting, upserted, cancellation);

	public ValueTask<int> UpsertManyAsync<T>(IEnumerable items, CancellationToken cancellation = default) => items == null ? ValueTask.FromResult(0) : this.UpsertManyAsync(this.GetName<T>(), items, string.Empty, null, null, null, cancellation);
	public ValueTask<int> UpsertManyAsync<T>(IEnumerable items, DataUpsertOptions options, CancellationToken cancellation = default) => items == null ? ValueTask.FromResult(0) : this.UpsertManyAsync(this.GetName<T>(), items, string.Empty, options, null, null, cancellation);
	public ValueTask<int> UpsertManyAsync<T>(IEnumerable items, string schema, CancellationToken cancellation = default) => items == null ? ValueTask.FromResult(0) : this.UpsertManyAsync(this.GetName<T>(), items, schema, null, null, null, cancellation);
	public ValueTask<int> UpsertManyAsync<T>(IEnumerable items, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null, CancellationToken cancellation = default) => items == null ? ValueTask.FromResult(0) : this.UpsertManyAsync(this.GetName<T>(), items, schema, options, upserting, upserted, cancellation);

	public ValueTask<int> UpsertManyAsync(string name, IEnumerable items, CancellationToken cancellation = default) => this.UpsertManyAsync(name, items, string.Empty, null, null, null, cancellation);
	public ValueTask<int> UpsertManyAsync(string name, IEnumerable items, DataUpsertOptions options, CancellationToken cancellation = default) => this.UpsertManyAsync(name, items, string.Empty, options, null, null, cancellation);
	public ValueTask<int> UpsertManyAsync(string name, IEnumerable items, string schema, CancellationToken cancellation = default) => this.UpsertManyAsync(name, items, schema, null, null, null, cancellation);
	public ValueTask<int> UpsertManyAsync(string name, IEnumerable items, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null, CancellationToken cancellation = default) => this.UpsertManyAsync(name, items, this.Schema.Parse(name, schema, Common.TypeExtension.GetElementType(items.GetType())), options, upserting, upserted, cancellation);
	public async ValueTask<int> UpsertManyAsync(string name, IEnumerable items, ISchema schema, DataUpsertOptions options, Func<DataUpsertContextBase, bool> upserting = null, Action<DataUpsertContextBase> upserted = null, CancellationToken cancellation = default)
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		if(items == null)
			return 0;

		//创建数据访问上下文对象
		var context = this.CreateUpsertContext(name, true, items, schema, options);

		//处理数据访问操作前的回调
		if(upserting != null && upserting(context))
			return context.Count;

		//激发“Upserting”事件，如果被中断则返回
		if(this.OnUpserting(context))
			return context.Count;

		//调用数据访问过滤器前事件
		this.OnFiltering(context);

		//执行数据增改操作
		await this.OnUpsertAsync(context, cancellation);

		//调用数据访问过滤器后事件
		this.OnFiltered(context);

		//激发“Upserted”事件
		this.OnUpserted(context);

		//处理数据访问操作后的回调
		if(upserted != null)
			upserted(context);

		var result = context.Count;

		//处置上下文资源
		context.Dispose();

		//返回最终的结果
		return result;
	}

	protected abstract void OnUpsert(DataUpsertContextBase context);
	protected abstract ValueTask OnUpsertAsync(DataUpsertContextBase context, CancellationToken cancellation);
	#endregion

	#region 更新方法
	public int Update<T>(T data) => data == null ? 0 : this.Update(this.GetName<T>(), data, null, string.Empty, null, null, null);
	public int Update<T>(T data, DataUpdateOptions options) => data == null ? 0 : this.Update(this.GetName<T>(), data, null, string.Empty, options, null, null);
	public int Update<T>(T data, string schema) => data == null ? 0 : this.Update(this.GetName<T>(), data, null, schema, null, null, null);
	public int Update<T>(T data, string schema, DataUpdateOptions options) => data == null ? 0 : this.Update(this.GetName<T>(), data, null, schema, options, null, null);
	public int Update<T>(T data, ICondition criteria) => data == null ? 0 : this.Update(this.GetName<T>(), data, criteria, string.Empty, null, null, null);
	public int Update<T>(T data, ICondition criteria, DataUpdateOptions options) => data == null ? 0 : this.Update(this.GetName<T>(), data, criteria, string.Empty, options, null, null);
	public int Update<T>(T data, ICondition criteria, string schema) => data == null ? 0 : this.Update(this.GetName<T>(), data, criteria, schema, null, null, null);
	public int Update<T>(T data, ICondition criteria, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null) => data == null ? 0 : this.Update(this.GetName<T>(), data, criteria, schema, options, updating, updated);

	public int Update<T>(object data) => data == null ? 0 : this.Update(this.GetName<T>(), data, null, string.Empty, null, null, null);
	public int Update<T>(object data, DataUpdateOptions options) => data == null ? 0 : this.Update(this.GetName<T>(), data, null, string.Empty, options, null, null);
	public int Update<T>(object data, string schema) => data == null ? 0 : this.Update(this.GetName<T>(), data, null, schema, null, null, null);
	public int Update<T>(object data, string schema, DataUpdateOptions options) => data == null ? 0 : this.Update(this.GetName<T>(), data, null, schema, options, null, null);
	public int Update<T>(object data, ICondition criteria) => data == null ? 0 : this.Update(this.GetName<T>(), data, criteria, string.Empty, null, null, null);
	public int Update<T>(object data, ICondition criteria, DataUpdateOptions options) => data == null ? 0 : this.Update(this.GetName<T>(), data, criteria, string.Empty, options, null, null);
	public int Update<T>(object data, ICondition criteria, string schema) => data == null ? 0 : this.Update(this.GetName<T>(), data, criteria, schema, null, null, null);
	public int Update<T>(object data, ICondition criteria, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null) => data == null ? 0 : this.Update(this.GetName<T>(), data, criteria, schema, options, updating, updated);

	public int Update(string name, object data) => this.Update(name, data, null, string.Empty, null, null, null);
	public int Update(string name, object data, DataUpdateOptions options) => this.Update(name, data, null, string.Empty, options, null, null);
	public int Update(string name, object data, string schema) => this.Update(name, data, null, schema, null, null, null);
	public int Update(string name, object data, string schema, DataUpdateOptions options) => this.Update(name, data, null, schema, options, null, null);
	public int Update(string name, object data, ICondition criteria) => this.Update(name, data, criteria, string.Empty, null, null, null);
	public int Update(string name, object data, ICondition criteria, DataUpdateOptions options) => this.Update(name, data, criteria, string.Empty, options, null, null);
	public int Update(string name, object data, ICondition criteria, string schema) => this.Update(name, data, criteria, schema, null, null, null);
	public int Update(string name, object data, ICondition criteria, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null) => this.Update(name, data, criteria, this.Schema.Parse(name, schema, data.GetType()), options, updating, updated);
	public int Update(string name, object data, ICondition criteria, ISchema schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null)
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		if(data == null)
			return 0;

		//创建数据访问上下文对象
		var context = this.CreateUpdateContext(name, data, criteria, schema, options);

		//处理数据访问操作前的回调
		if(updating != null && updating(context))
			return context.Count;

		//激发“Updating”事件，如果被中断则返回
		if(this.OnUpdating(context))
			return context.Count;

		//调用数据访问过滤器前事件
		this.OnFiltering(context);

		//执行数据更新操作
		this.OnUpdate(context);

		//调用数据访问过滤器后事件
		this.OnFiltered(context);

		//激发“Updated”事件
		this.OnUpdated(context);

		//处理数据访问操作后的回调
		if(updated != null)
			updated(context);

		var result = context.Count;

		//处置上下文资源
		context.Dispose();

		//返回最终的结果
		return result;
	}

	public ValueTask<int> UpdateAsync<T>(T data, CancellationToken cancellation = default) =>
		data == null ? ValueTask.FromResult(0) : this.UpdateAsync(this.GetName<T>(), data, null, string.Empty, null, null, null, cancellation);
	public ValueTask<int> UpdateAsync<T>(T data, DataUpdateOptions options, CancellationToken cancellation = default) =>
		data == null ? ValueTask.FromResult(0) : this.UpdateAsync(this.GetName<T>(), data, null, string.Empty, options, null, null, cancellation);
	public ValueTask<int> UpdateAsync<T>(T data, string schema, CancellationToken cancellation = default) =>
		data == null ? ValueTask.FromResult(0) : this.UpdateAsync(this.GetName<T>(), data, null, schema, null, null, null, cancellation);
	public ValueTask<int> UpdateAsync<T>(T data, string schema, DataUpdateOptions options, CancellationToken cancellation = default) =>
		data == null ? ValueTask.FromResult(0) : this.UpdateAsync(this.GetName<T>(), data, null, schema, options, null, null, cancellation);
	public ValueTask<int> UpdateAsync<T>(T data, ICondition criteria, CancellationToken cancellation = default) =>
		data == null ? ValueTask.FromResult(0) : this.UpdateAsync(this.GetName<T>(), data, criteria, string.Empty, null, null, null, cancellation);
	public ValueTask<int> UpdateAsync<T>(T data, ICondition criteria, DataUpdateOptions options, CancellationToken cancellation = default) =>
		data == null ? ValueTask.FromResult(0) : this.UpdateAsync(this.GetName<T>(), data, criteria, string.Empty, options, null, null, cancellation);
	public ValueTask<int> UpdateAsync<T>(T data, ICondition criteria, string schema, CancellationToken cancellation = default) =>
		data == null ? ValueTask.FromResult(0) : this.UpdateAsync(this.GetName<T>(), data, criteria, schema, null, null, null, cancellation);
	public ValueTask<int> UpdateAsync<T>(T data, ICondition criteria, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null, CancellationToken cancellation = default) =>
		data == null ? ValueTask.FromResult(0) : this.UpdateAsync(this.GetName<T>(), data, criteria, schema, options, updating, updated, cancellation);
	public ValueTask<int> UpdateAsync<T>(object data, CancellationToken cancellation = default) =>
		data == null ? ValueTask.FromResult(0) : this.UpdateAsync(this.GetName<T>(), data, null, string.Empty, null, null, null, cancellation);
	public ValueTask<int> UpdateAsync<T>(object data, DataUpdateOptions options, CancellationToken cancellation = default) =>
		data == null ? ValueTask.FromResult(0) : this.UpdateAsync(this.GetName<T>(), data, null, string.Empty, options, null, null, cancellation);
	public ValueTask<int> UpdateAsync<T>(object data, string schema, CancellationToken cancellation = default) =>
		data == null ? ValueTask.FromResult(0) : this.UpdateAsync(this.GetName<T>(), data, null, schema, null, null, null, cancellation);
	public ValueTask<int> UpdateAsync<T>(object data, string schema, DataUpdateOptions options, CancellationToken cancellation = default) =>
		data == null ? ValueTask.FromResult(0) : this.UpdateAsync(this.GetName<T>(), data, null, schema, options, null, null, cancellation);
	public ValueTask<int> UpdateAsync<T>(object data, ICondition criteria, CancellationToken cancellation = default) =>
		data == null ? ValueTask.FromResult(0) : this.UpdateAsync(this.GetName<T>(), data, criteria, string.Empty, null, null, null, cancellation);
	public ValueTask<int> UpdateAsync<T>(object data, ICondition criteria, DataUpdateOptions options, CancellationToken cancellation = default) =>
		data == null ? ValueTask.FromResult(0) : this.UpdateAsync(this.GetName<T>(), data, criteria, string.Empty, options, null, null, cancellation);
	public ValueTask<int> UpdateAsync<T>(object data, ICondition criteria, string schema, CancellationToken cancellation = default) =>
		data == null ? ValueTask.FromResult(0) : this.UpdateAsync(this.GetName<T>(), data, criteria, schema, null, null, null, cancellation);
	public ValueTask<int> UpdateAsync<T>(object data, ICondition criteria, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null, CancellationToken cancellation = default) =>
		data == null ? ValueTask.FromResult(0) : this.UpdateAsync(this.GetName<T>(), data, criteria, schema, options, updating, updated, cancellation);

	public ValueTask<int> UpdateAsync(string name, object data, CancellationToken cancellation = default) =>
		this.UpdateAsync(name, data, null, string.Empty, null, null, null, cancellation);
	public ValueTask<int> UpdateAsync(string name, object data, DataUpdateOptions options, CancellationToken cancellation = default) =>
		this.UpdateAsync(name, data, null, string.Empty, options, null, null, cancellation);
	public ValueTask<int> UpdateAsync(string name, object data, string schema, CancellationToken cancellation = default) =>
		this.UpdateAsync(name, data, null, schema, null, null, null, cancellation);
	public ValueTask<int> UpdateAsync(string name, object data, string schema, DataUpdateOptions options, CancellationToken cancellation = default) =>
		this.UpdateAsync(name, data, null, schema, options, null, null, cancellation);
	public ValueTask<int> UpdateAsync(string name, object data, ICondition criteria, CancellationToken cancellation = default) =>
		this.UpdateAsync(name, data, criteria, string.Empty, null, null, null, cancellation);
	public ValueTask<int> UpdateAsync(string name, object data, ICondition criteria, DataUpdateOptions options, CancellationToken cancellation = default) =>
		this.UpdateAsync(name, data, criteria, string.Empty, options, null, null, cancellation);
	public ValueTask<int> UpdateAsync(string name, object data, ICondition criteria, string schema, CancellationToken cancellation = default) =>
		this.UpdateAsync(name, data, criteria, schema, null, null, null, cancellation);
	public ValueTask<int> UpdateAsync(string name, object data, ICondition criteria, string schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null, CancellationToken cancellation = default) =>
		this.UpdateAsync(name, data, criteria, this.Schema.Parse(name, schema, data.GetType()), options, updating, updated, cancellation);
	public async ValueTask<int> UpdateAsync(string name, object data, ICondition criteria, ISchema schema, DataUpdateOptions options, Func<DataUpdateContextBase, bool> updating = null, Action<DataUpdateContextBase> updated = null, CancellationToken cancellation = default)
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		if(data == null)
			return 0;

		//创建数据访问上下文对象
		var context = this.CreateUpdateContext(name, data, criteria, schema, options);

		//处理数据访问操作前的回调
		if(updating != null && updating(context))
			return context.Count;

		//激发“Updating”事件，如果被中断则返回
		if(this.OnUpdating(context))
			return context.Count;

		//调用数据访问过滤器前事件
		this.OnFiltering(context);

		//执行数据更新操作
		await this.OnUpdateAsync(context, cancellation);

		//调用数据访问过滤器后事件
		this.OnFiltered(context);

		//激发“Updated”事件
		this.OnUpdated(context);

		//处理数据访问操作后的回调
		if(updated != null)
			updated(context);

		var result = context.Count;

		//处置上下文资源
		context.Dispose();

		//返回最终的结果
		return result;
	}

	protected abstract void OnUpdate(DataUpdateContextBase context);
	protected abstract ValueTask OnUpdateAsync(DataUpdateContextBase context, CancellationToken cancellation);
	#endregion

	#region 普通查询
	public IEnumerable<T> Select<T>(DataSelectOptions options = null, params Sorting[] sortings) =>
		this.Select<T>(this.GetName<T>(), null, string.Empty, null, options, sortings, null, null);
	public IEnumerable<T> Select<T>(ICondition criteria, params Sorting[] sortings) =>
		this.Select<T>(this.GetName<T>(), criteria, string.Empty, null, null, sortings, null, null);
	public IEnumerable<T> Select<T>(ICondition criteria, DataSelectOptions options, params Sorting[] sortings) =>
		this.Select<T>(this.GetName<T>(), criteria, string.Empty, null, options, sortings, null, null);
	public IEnumerable<T> Select<T>(ICondition criteria, Paging paging, params Sorting[] sortings) =>
		this.Select<T>(this.GetName<T>(), criteria, string.Empty, paging, null, sortings, null, null);
	public IEnumerable<T> Select<T>(ICondition criteria, Paging paging, DataSelectOptions options, params Sorting[] sortings) =>
		this.Select<T>(this.GetName<T>(), criteria, string.Empty, paging, options, sortings, null, null);
	public IEnumerable<T> Select<T>(ICondition criteria, string schema, params Sorting[] sortings) =>
		this.Select<T>(this.GetName<T>(), criteria, schema, null, null, sortings, null, null);
	public IEnumerable<T> Select<T>(ICondition criteria, string schema, DataSelectOptions options, params Sorting[] sortings) =>
		this.Select<T>(this.GetName<T>(), criteria, schema, null, options, sortings, null, null);
	public IEnumerable<T> Select<T>(ICondition criteria, string schema, Paging paging, params Sorting[] sortings) =>
		this.Select<T>(this.GetName<T>(), criteria, schema, paging, null, sortings, null, null);
	public IEnumerable<T> Select<T>(ICondition criteria, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings) =>
		this.Select<T>(this.GetName<T>(), criteria, schema, paging, options, sortings, null, null);
	public IEnumerable<T> Select<T>(ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected) =>
		this.Select<T>(this.GetName<T>(), criteria, schema, paging, options, sortings, selecting, selected);

	public IEnumerable<T> Select<T>(string name, DataSelectOptions options = null, params Sorting[] sortings) =>
		this.Select<T>(name, null, string.Empty, null, options, sortings, null, null);
	public IEnumerable<T> Select<T>(string name, ICondition criteria, params Sorting[] sortings) =>
		this.Select<T>(name, criteria, string.Empty, null, null, sortings, null, null);
	public IEnumerable<T> Select<T>(string name, ICondition criteria, DataSelectOptions options, params Sorting[] sortings) =>
		this.Select<T>(name, criteria, string.Empty, null, options, sortings, null, null);
	public IEnumerable<T> Select<T>(string name, ICondition criteria, Paging paging, params Sorting[] sortings) =>
		this.Select<T>(name, criteria, string.Empty, paging, null, sortings, null, null);
	public IEnumerable<T> Select<T>(string name, ICondition criteria, Paging paging, DataSelectOptions options, params Sorting[] sortings) =>
		this.Select<T>(name, criteria, string.Empty, paging, options, sortings, null, null);
	public IEnumerable<T> Select<T>(string name, ICondition criteria, string schema, params Sorting[] sortings) =>
		this.Select<T>(name, criteria, schema, null, null, sortings, null, null);
	public IEnumerable<T> Select<T>(string name, ICondition criteria, string schema, DataSelectOptions options, params Sorting[] sortings) =>
		this.Select<T>(name, criteria, schema, null, options, sortings, null, null);
	public IEnumerable<T> Select<T>(string name, ICondition criteria, string schema, Paging paging, params Sorting[] sortings) =>
		this.Select<T>(name, criteria, schema, paging, null, sortings, null, null);
	public IEnumerable<T> Select<T>(string name, ICondition criteria, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings) =>
		this.Select<T>(name, criteria, schema, paging, options, sortings, null, null);
	public IEnumerable<T> Select<T>(string name, ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected) =>
		this.Select<T>(name, criteria, this.Schema.Parse(name, schema, typeof(T)), paging, options, sortings, selecting, selected);
	public IEnumerable<T> Select<T>(string name, ICondition criteria, ISchema schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected)
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		//创建数据访问上下文对象
		var context = this.CreateSelectContext(name, typeof(T), criteria, null, schema, paging, sortings, options);

		//执行查询方法
		return this.Select<T>(context, selecting, selected);
	}

	public IAsyncEnumerable<T> SelectAsync<T>(CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), null, string.Empty, null, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(DataSelectOptions options, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), null, string.Empty, null, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), null, string.Empty, null, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, string.Empty, null, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, string.Empty, null, null, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, DataSelectOptions options, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, string.Empty, null, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, string.Empty, null, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, Paging paging, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, string.Empty, paging, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, string.Empty, paging, null, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, Paging paging, DataSelectOptions options, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, string.Empty, paging, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, string.Empty, paging, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, schema, null, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, schema, null, null, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, DataSelectOptions options, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, schema, null, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, schema, null, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, Paging paging, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, schema, paging, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, schema, paging, null, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, Paging paging, DataSelectOptions options, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, schema, paging, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, schema, paging, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, schema, paging, options, sortings, selecting, selected, cancellation);

	public IAsyncEnumerable<T> SelectAsync<T>(string name, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, null, string.Empty, null, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, DataSelectOptions options, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, null, string.Empty, null, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, null, string.Empty, null, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, criteria, string.Empty, null, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, criteria, string.Empty, null, null, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, DataSelectOptions options, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, criteria, string.Empty, null, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, criteria, string.Empty, null, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, Paging paging, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, criteria, string.Empty, paging, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, criteria, string.Empty, paging, null, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, Paging paging, DataSelectOptions options, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, criteria, string.Empty, paging, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, criteria, string.Empty, paging, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, criteria, schema, null, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, criteria, schema, null, null, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, DataSelectOptions options, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, criteria, schema, null, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, criteria, schema, null, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, Paging paging, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, criteria, schema, paging, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, criteria, schema, paging, null, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, Paging paging, DataSelectOptions options, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, criteria, schema, paging, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, criteria, schema, paging, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected, CancellationToken cancellation = default) =>
		 this.SelectAsync<T>(name, criteria, this.Schema.Parse(name, schema, typeof(T)), paging, options, sortings, selecting, selected, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, ISchema schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected, CancellationToken cancellation = default)
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		//创建数据访问上下文对象
		var context = this.CreateSelectContext(name, typeof(T), criteria, null, schema, paging, sortings, options);

		//执行查询方法
		return this.SelectAsync<T>(context, selecting, selected, cancellation);
	}
	#endregion

	#region 分组查询
	public IEnumerable<T> Select<T>(string name, Grouping grouping, params Sorting[] sortings) =>
		this.Select<T>(name, grouping, null, string.Empty, null, null, sortings, null, null);
	public IEnumerable<T> Select<T>(string name, Grouping grouping, DataSelectOptions options, params Sorting[] sortings) =>
		this.Select<T>(name, grouping, null, string.Empty, null, options, sortings, null, null);
	public IEnumerable<T> Select<T>(string name, Grouping grouping, Paging paging, DataSelectOptions options = null, params Sorting[] sortings) =>
		this.Select<T>(name, grouping, null, string.Empty, paging, options, sortings, null, null);
	public IEnumerable<T> Select<T>(string name, Grouping grouping, string schema, params Sorting[] sortings) =>
		this.Select<T>(name, grouping, null, schema, null, null, sortings, null, null);
	public IEnumerable<T> Select<T>(string name, Grouping grouping, string schema, DataSelectOptions options, params Sorting[] sortings) =>
		this.Select<T>(name, grouping, null, schema, null, options, sortings, null, null);
	public IEnumerable<T> Select<T>(string name, Grouping grouping, string schema, Paging paging, DataSelectOptions options = null, params Sorting[] sortings) =>
		this.Select<T>(name, grouping, null, schema, paging, options, sortings, null, null);
	public IEnumerable<T> Select<T>(string name, Grouping grouping, ICondition criteria, Paging paging, params Sorting[] sortings) =>
		this.Select<T>(name, grouping, criteria, (ISchema)null, paging, null, sortings, null, null);
	public IEnumerable<T> Select<T>(string name, Grouping grouping, ICondition criteria, string schema, params Sorting[] sortings) =>
		this.Select<T>(name, grouping, criteria, schema, null, null, sortings, null, null);
	public IEnumerable<T> Select<T>(string name, Grouping grouping, ICondition criteria, string schema, DataSelectOptions options, params Sorting[] sortings) =>
		this.Select<T>(name, grouping, criteria, schema, null, options, sortings, null, null);
	public IEnumerable<T> Select<T>(string name, Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options = null, params Sorting[] sortings) =>
		this.Select<T>(name, grouping, criteria, schema, paging, options, sortings, null, null);
	public IEnumerable<T> Select<T>(string name, Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected) =>
		this.Select<T>(name, grouping, criteria, string.IsNullOrWhiteSpace(schema) ? null : this.Schema.Parse(name, schema, typeof(T)), paging, options, sortings, selecting, selected);
	public IEnumerable<T> Select<T>(string name, Grouping grouping, ICondition criteria, ISchema schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected)
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		//创建数据访问上下文对象
		var context = this.CreateSelectContext(name, typeof(T), criteria, grouping, schema, paging, sortings, options);

		//执行查询方法
		return this.Select<T>(context, selecting, selected);
	}

	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, null, string.Empty, null, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, null, string.Empty, null, null, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, DataSelectOptions options, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, null, string.Empty, null, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, null, string.Empty, null, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, Paging paging, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, null, string.Empty, paging, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, Paging paging, DataSelectOptions options, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, null, string.Empty, paging, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, null, string.Empty, paging, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, null, schema, null, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, null, schema, null, null, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, DataSelectOptions options, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, null, schema, null, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, null, schema, null, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, Paging paging, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, null, schema, paging, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, Paging paging, DataSelectOptions options, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, null, schema, paging, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		  this.SelectAsync<T>(name, grouping, null, schema, paging, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, criteria, (ISchema)null, null, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, Paging paging, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, criteria, (ISchema)null, paging, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, criteria, (ISchema)null, paging, null, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, criteria, schema, null, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, criteria, schema, null, null, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, DataSelectOptions options, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, criteria, schema, null, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, criteria, schema, null, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, criteria, schema, paging, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, criteria, schema, paging, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, criteria, string.IsNullOrWhiteSpace(schema) ? null : this.Schema.Parse(name, schema, typeof(T)), paging, options, sortings, selecting, selected, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, ISchema schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected, CancellationToken cancellation = default)
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		//创建数据访问上下文对象
		var context = this.CreateSelectContext(name, typeof(T), criteria, grouping, schema, paging, sortings, options);

		//执行查询方法
		return this.SelectAsync<T>(context, selecting, selected, cancellation);
	}
	#endregion

	#region 查询处理
	private IEnumerable<T> Select<T>(DataSelectContextBase context, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected)
	{
		//确实是否已处置
		this.EnsureDisposed();

		//处理数据访问操作前的回调
		if(selecting != null && selecting(context))
			return context.Result as IEnumerable<T>;

		//激发“Selecting”事件，如果被中断则返回
		if(this.OnSelecting(context))
			return context.Result as IEnumerable<T>;

		//调用数据访问过滤器前事件
		this.OnFiltering(context);

		//执行数据查询操作
		this.OnSelect(context);

		//调用数据访问过滤器后事件
		this.OnFiltered(context);

		//激发“Selected”事件
		this.OnSelected(context);

		//处理数据访问操作后的回调
		if(selected != null)
			selected(context);

		var result = ToEnumerable<T>(context.Result);

		//处置上下文资源
		context.Dispose();

		//返回最终的结果
		return result;
	}

	private IAsyncEnumerable<T> SelectAsync<T>(DataSelectContextBase context, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected, CancellationToken cancellation)
	{
		//确实是否已处置
		this.EnsureDisposed();

		//处理数据访问操作前的回调
		if(selecting != null && selecting(context))
			return ToAsyncEnumerable<T>(context.Result, cancellation);

		//激发“Selecting”事件，如果被中断则返回
		if(this.OnSelecting(context))
			return ToAsyncEnumerable<T>(context.Result, cancellation);

		//调用数据访问过滤器前事件
		this.OnFiltering(context);

		//执行数据查询操作
		this.OnSelectAsync(context, cancellation).AsTask().GetAwaiter().GetResult();

		//调用数据访问过滤器后事件
		this.OnFiltered(context);

		//激发“Selected”事件
		this.OnSelected(context);

		//处理数据访问操作后的回调
		selected?.Invoke(context);

		var result = ToAsyncEnumerable<T>(context.Result, cancellation);

		//处置上下文资源
		context.Dispose();

		return result;
	}

	protected abstract void OnSelect(DataSelectContextBase context);
	protected abstract ValueTask OnSelectAsync(DataSelectContextBase context, CancellationToken cancellation);
	#endregion

	#region 虚拟方法
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private string GetName<T>() => this.GetName(typeof(T));
	protected virtual string GetName(Type type)
	{
		var name = Model.Naming.Get(type);

		if(string.IsNullOrEmpty(name))
			throw new InvalidOperationException($"Missing data access name mapping of the '{type.FullName}' type.");

		return name;
	}

	protected virtual void OnFiltering(IDataAccessContextBase context) => _filters.InvokeFiltering(context);
	protected virtual void OnFiltered(IDataAccessContextBase context) => _filters.InvokeFiltered(context);
	#endregion

	#region 抽象方法
	protected abstract ISchemaParser CreateSchema();
	protected abstract IDataSequencer CreateSequencer();
	protected abstract DataExecuteContextBase CreateExecuteContext(string name, bool isScalar, Type resultType, IEnumerable<Parameter> parameters, IDataExecuteOptions options);
	protected abstract DataExistContextBase CreateExistContext(string name, ICondition criteria, IDataExistsOptions options);
	protected abstract DataAggregateContextBase CreateAggregateContext(string name, DataAggregate aggregate, ICondition criteria, IDataAggregateOptions options);
	protected abstract DataImportContextBase CreateImportContext(string name, IEnumerable data, IEnumerable<string> members, IDataImportOptions options);
	protected abstract DataDeleteContextBase CreateDeleteContext(string name, ICondition criteria, ISchema schema, IDataDeleteOptions options);
	protected abstract DataInsertContextBase CreateInsertContext(string name, bool isMultiple, object data, ISchema schema, IDataInsertOptions options);
	protected abstract DataUpsertContextBase CreateUpsertContext(string name, bool isMultiple, object data, ISchema schema, IDataUpsertOptions options);
	protected abstract DataUpdateContextBase CreateUpdateContext(string name, object data, ICondition criteria, ISchema schema, IDataUpdateOptions options);
	protected abstract DataSelectContextBase CreateSelectContext(string name, Type entityType, ICondition criteria, Grouping grouping, ISchema schema, Paging paging, Sorting[] sortings, IDataSelectOptions options);
	#endregion

	#region 激发事件
	protected virtual void OnError(DataAccessErrorEventArgs args) => this.Error?.Invoke(this, args);
	protected virtual void OnExecuted(DataExecuteContextBase context) => this.Executed?.Invoke(this, new DataExecutedEventArgs(context));
	protected virtual bool OnExecuting(DataExecuteContextBase context)
	{
		var e = this.Executing;
		if(e == null)
			return false;

		var args = new DataExecutingEventArgs(context);
		e(this, args);
		return args.Cancel;
	}

	protected virtual void OnExisted(DataExistContextBase context) => this.Existed?.Invoke(this, new DataExistedEventArgs(context));
	protected virtual bool OnExisting(DataExistContextBase context)
	{
		var e = this.Existing;
		if(e == null)
			return false;

		var args = new DataExistingEventArgs(context);
		e(this, args);
		return args.Cancel;
	}

	protected virtual void OnAggregated(DataAggregateContextBase context) => this.Aggregated?.Invoke(this, new DataAggregatedEventArgs(context));
	protected virtual bool OnAggregating(DataAggregateContextBase context)
	{
		var e = this.Aggregating;
		if(e == null)
			return false;

		var args = new DataAggregatingEventArgs(context);
		e(this, args);
		return args.Cancel;
	}

	protected virtual void OnImported(DataImportContextBase context) => this.Imported?.Invoke(this, new DataImportedEventArgs(context));
	protected virtual bool OnImporting(DataImportContextBase context)
	{
		var e = this.Importing;
		if(e == null)
			return false;

		var args = new DataImportingEventArgs(context);
		e(this, args);
		return args.Cancel;
	}

	protected virtual void OnDeleted(DataDeleteContextBase context) => this.Deleted?.Invoke(this, new DataDeletedEventArgs(context));
	protected virtual bool OnDeleting(DataDeleteContextBase context)
	{
		var e = this.Deleting;
		if(e == null)
			return false;

		var args = new DataDeletingEventArgs(context);
		e(this, args);
		return args.Cancel;
	}

	protected virtual void OnInserted(DataInsertContextBase context) => this.Inserted?.Invoke(this, new DataInsertedEventArgs(context));
	protected virtual bool OnInserting(DataInsertContextBase context)
	{
		var e = this.Inserting;
		if(e == null)
			return false;

		var args = new DataInsertingEventArgs(context);
		e(this, args);
		return args.Cancel;
	}

	protected virtual void OnUpserted(DataUpsertContextBase context) => this.Upserted?.Invoke(this, new DataUpsertedEventArgs(context));
	protected virtual bool OnUpserting(DataUpsertContextBase context)
	{
		var e = this.Upserting;
		if(e == null)
			return false;

		var args = new DataUpsertingEventArgs(context);
		e(this, args);
		return args.Cancel;
	}

	protected virtual void OnUpdated(DataUpdateContextBase context) => this.Updated?.Invoke(this, new DataUpdatedEventArgs(context));
	protected virtual bool OnUpdating(DataUpdateContextBase context)
	{
		var e = this.Updating;
		if(e == null)
			return false;

		var args = new DataUpdatingEventArgs(context);
		e(this, args);
		return args.Cancel;
	}

	protected virtual void OnSelected(DataSelectContextBase context) => this.Selected?.Invoke(this, new DataSelectedEventArgs(context));
	protected virtual bool OnSelecting(DataSelectContextBase context)
	{
		var e = this.Selecting;
		if(e == null)
			return false;

		var args = new DataSelectingEventArgs(context);
		e(this, args);
		return args.Cancel;
	}
	#endregion

	#region 处置方法
	public void Dispose()
	{
		var disposing = Interlocked.CompareExchange(ref _disposing, DISPOSING, 0);
		if(disposing != 0)
			return;

		try
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}
		finally
		{
			_disposing = DISPOSED;
		}
	}

	protected virtual void Dispose(bool disposing)
	{
		var cancellation = Interlocked.Exchange(ref _cancellation, null);

		if(cancellation != null)
		{
			cancellation.Cancel();
			cancellation.Dispose();
		}
	}
	#endregion

	#region 私有方法
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private static IEnumerable<T> ToEnumerable<T>(object result) => Collections.Enumerable.Enumerate<T>(result);
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private static IAsyncEnumerable<T> ToAsyncEnumerable<T>(object result, CancellationToken cancellation) => Collections.Enumerable.EnumerateAsync<T>(result, cancellation);
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private void EnsureDisposed()
	{
		if(this.IsDisposed)
			throw new ObjectDisposedException($"[{this.GetType().Name}]{_name}");
	}
	#endregion

	#region 嵌套子类
	protected abstract class DataSequencerBase(DataAccessBase accessor) : IDataSequencer
	{
		#region 成员字段
		private ISequenceBase _sequence;
		private readonly DataAccessBase _accessor = accessor ?? throw new ArgumentNullException(nameof(accessor));
		#endregion

		#region 公共属性
		public string Name => _accessor.Name;
		public DataAccessBase Accessor => _accessor;
		public ISequenceBase Sequence
		{
			get => _sequence ??= Common.Sequence.Variate(this.GetSequence());
			set => _sequence = value ?? throw new ArgumentNullException(nameof(value));
		}
		#endregion

		#region 公共方法
		public long Increase(string name, string field, int interval = 1) => this.Increase(GetProperty(name, field), interval);
		public abstract long Increase(Metadata.IDataEntitySimplexProperty property, int interval = 1);

		public ValueTask<long> IncreaseAsync(string name, string field, CancellationToken cancellation = default) => this.IncreaseAsync(name, field, 1, cancellation);
		public ValueTask<long> IncreaseAsync(string name, string field, int interval, CancellationToken cancellation = default) => this.IncreaseAsync(GetProperty(name, field), interval, cancellation);

		public ValueTask<long> IncreaseAsync(Metadata.IDataEntitySimplexProperty property, CancellationToken cancellation = default) => this.IncreaseAsync(property, 1, cancellation);
		public abstract ValueTask<long> IncreaseAsync(Metadata.IDataEntitySimplexProperty property, int interval, CancellationToken cancellation = default);
		#endregion

		#region 虚拟方法
		protected virtual ISequenceBase GetSequence()
		{
			var provider = ApplicationContext.Current.Services.ResolveRequired<IServiceProvider<ISequenceBase>>();

			var sequence = provider.GetService(this.Name);
			if(sequence == null && !string.IsNullOrEmpty(this.Name))
				sequence = provider.GetService(string.Empty);

			return sequence ?? throw new InvalidOperationException($"Missing required Sequence service.");
		}
		#endregion

		#region 私有方法
		private static Metadata.IDataEntitySimplexProperty GetProperty(string name, string field)
		{
			if(!Mapping.Entities.TryGetValue(name, out var entity))
				throw new InvalidOperationException($"The entity specified with the name '{name}' does not exist.");

			if(!entity.Properties.TryGetValue(field, out var property))
				throw new InvalidOperationException($"The '{name}' entity does not contains the '{field}' property.");

			if(!property.IsSimplex)
				throw new InvalidOperationException($"This '{property.Entity.Name}.{property.Name}' property is not a simplex property and does not support sequence feature.");

			return (Metadata.IDataEntitySimplexProperty)property;
		}
		#endregion
	}
	#endregion
}
