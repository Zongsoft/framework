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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Zongsoft.Data;

partial class DataAccessBase
{
	#region 公共方法
	public ValueTask<int> ExecuteAsync(string name, CancellationToken cancellation) => this.ExecuteAsync(name, null, null, null, null, cancellation);
	public ValueTask<int> ExecuteAsync(string name, DataExecuteOptions options, CancellationToken cancellation) => this.ExecuteAsync(name, null, options, null, null, cancellation);
	public ValueTask<int> ExecuteAsync(string name, DataExecuteOptions options, Func<DataExecuteContextBase, CancellationToken, ValueTask<bool>> executing, CancellationToken cancellation) => this.ExecuteAsync(name, options, executing, default(Func<DataExecuteContextBase, CancellationToken, ValueTask>), cancellation);
	public ValueTask<int> ExecuteAsync(string name, DataExecuteOptions options = null, Func<DataExecuteContextBase, CancellationToken, ValueTask<bool>> executing = null, Func<DataExecuteContextBase, CancellationToken, ValueTask> executed = null, CancellationToken cancellation = default) => this.ExecuteAsync(name, null, options, executing, executed, cancellation);

	public ValueTask<int> ExecuteAsync(string name, IEnumerable<Parameter> parameters, CancellationToken cancellation) => this.ExecuteAsync(name, parameters, null, null, null, cancellation);
	public ValueTask<int> ExecuteAsync(string name, IEnumerable<Parameter> parameters, DataExecuteOptions options, CancellationToken cancellation) => this.ExecuteAsync(name, parameters, options, null, null, cancellation);
	public ValueTask<int> ExecuteAsync(string name, IEnumerable<Parameter> parameters, DataExecuteOptions options, Func<DataExecuteContextBase, CancellationToken, ValueTask<bool>> executing, CancellationToken cancellation) => this.ExecuteAsync(name, parameters, options, executing, default(Func<DataExecuteContextBase, CancellationToken, ValueTask>), cancellation);
	public async ValueTask<int> ExecuteAsync(string name, IEnumerable<Parameter> parameters, DataExecuteOptions options = null, Func<DataExecuteContextBase, CancellationToken, ValueTask<bool>> executing = null, Func<DataExecuteContextBase, CancellationToken, ValueTask> executed = null, CancellationToken cancellation = default)
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		//创建数据访问上下文对象
		using var context = this.CreateExecuteContext(name, false, null, parameters, options);

		//处理数据访问操作前的回调
		if(executing != null && await executing(context, cancellation))
			return GetRecordsAffected(context.Result);

		//激发“Executing”事件，如果被中断则返回
		if(await this.OnExecutingAsync(context, cancellation))
			return GetRecordsAffected(context.Result);

		//调用数据访问过滤器前事件
		await this.OnFilteringAsync(context, cancellation);

		//执行数据操作方法
		await this.OnExecuteAsync(context, cancellation);

		//调用数据访问过滤器后事件
		await this.OnFilteredAsync(context, cancellation);

		//激发“Executed”事件
		await this.OnExecutedAsync(context, cancellation);

		//处理数据访问操作后的回调
		if(executed != null)
			await executed(context, cancellation);

		var result = GetRecordsAffected(context.Result);

		//返回最终的结果
		return result;

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		static int GetRecordsAffected(object value) => value == null || System.Convert.IsDBNull(value) ? 0 : (int)System.Convert.ChangeType(value, typeof(int));
	}

	public IAsyncEnumerable<T> ExecuteAsync<T>(string name, CancellationToken cancellation) => this.ExecuteAsync<T>(name, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> ExecuteAsync<T>(string name, DataExecuteOptions options, CancellationToken cancellation) => this.ExecuteAsync<T>(name, null, options, null, null, cancellation);
	public IAsyncEnumerable<T> ExecuteAsync<T>(string name, DataExecuteOptions options, Func<DataExecuteContextBase, CancellationToken, ValueTask<bool>> executing, CancellationToken cancellation) => this.ExecuteAsync<T>(name, options, executing, default(Func<DataExecuteContextBase, CancellationToken, ValueTask>), cancellation);
	public IAsyncEnumerable<T> ExecuteAsync<T>(string name, DataExecuteOptions options = null, Func<DataExecuteContextBase, CancellationToken, ValueTask<bool>> executing = null, Func<DataExecuteContextBase, CancellationToken, ValueTask> executed = null, CancellationToken cancellation = default) => this.ExecuteAsync<T>(name, null, options, executing, executed, cancellation);

	public IAsyncEnumerable<T> ExecuteAsync<T>(string name, IEnumerable<Parameter> parameters, CancellationToken cancellation) => this.ExecuteAsync<T>(name, parameters, null, null, null, cancellation);
	public IAsyncEnumerable<T> ExecuteAsync<T>(string name, IEnumerable<Parameter> parameters, DataExecuteOptions options, CancellationToken cancellation) => this.ExecuteAsync<T>(name, parameters, options, null, null, cancellation);
	public IAsyncEnumerable<T> ExecuteAsync<T>(string name, IEnumerable<Parameter> parameters, DataExecuteOptions options, Func<DataExecuteContextBase, CancellationToken, ValueTask<bool>> executing, CancellationToken cancellation) => this.ExecuteAsync<T>(name, parameters, options, executing, default(Func<DataExecuteContextBase, CancellationToken, ValueTask>), cancellation);
	public async IAsyncEnumerable<T> ExecuteAsync<T>(string name, IEnumerable<Parameter> parameters, DataExecuteOptions options = null, Func<DataExecuteContextBase, CancellationToken, ValueTask<bool>> executing = null, Func<DataExecuteContextBase, CancellationToken, ValueTask> executed = null, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellation = default)
	{
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		IAsyncEnumerable<T> result;

		using(var context = this.CreateExecuteContext(name, false, typeof(T), parameters, options))
		{
			if(!(executing != null && await executing(context, cancellation)) && !await this.OnExecutingAsync(context, cancellation))
			{
				await this.OnFilteringAsync(context, cancellation);
				await this.OnExecuteAsync(context, cancellation);
				await this.OnFilteredAsync(context, cancellation);
				await this.OnExecutedAsync(context, cancellation);

				if(executed != null)
					await executed(context, cancellation);
			}

			result = ToAsyncEnumerable<T>(context.Result, cancellation);
		}

		await foreach(var item in result.WithCancellation(cancellation))
			yield return item;
	}

	public ValueTask<object> ExecuteScalarAsync(string name, CancellationToken cancellation) => this.ExecuteScalarAsync(name, null, null, null, null, cancellation);
	public ValueTask<object> ExecuteScalarAsync(string name, DataExecuteOptions options, CancellationToken cancellation) => this.ExecuteScalarAsync(name, null, options, null, null, cancellation);
	public ValueTask<object> ExecuteScalarAsync(string name, DataExecuteOptions options, Func<DataExecuteContextBase, CancellationToken, ValueTask<bool>> executing, CancellationToken cancellation) => this.ExecuteScalarAsync(name, options, executing, default(Func<DataExecuteContextBase, CancellationToken, ValueTask>), cancellation);
	public ValueTask<object> ExecuteScalarAsync(string name, DataExecuteOptions options = null, Func<DataExecuteContextBase, CancellationToken, ValueTask<bool>> executing = null, Func<DataExecuteContextBase, CancellationToken, ValueTask> executed = null, CancellationToken cancellation = default) => this.ExecuteScalarAsync(name, null, options, executing, executed, cancellation);

	public ValueTask<object> ExecuteScalarAsync(string name, IEnumerable<Parameter> parameters, CancellationToken cancellation) => this.ExecuteScalarAsync(name, parameters, null, null, null, cancellation);
	public ValueTask<object> ExecuteScalarAsync(string name, IEnumerable<Parameter> parameters, DataExecuteOptions options, CancellationToken cancellation) => this.ExecuteScalarAsync(name, parameters, options, null, null, cancellation);
	public ValueTask<object> ExecuteScalarAsync(string name, IEnumerable<Parameter> parameters, DataExecuteOptions options, Func<DataExecuteContextBase, CancellationToken, ValueTask<bool>> executing, CancellationToken cancellation) => this.ExecuteScalarAsync(name, parameters, options, executing, default(Func<DataExecuteContextBase, CancellationToken, ValueTask>), cancellation);
	public async ValueTask<object> ExecuteScalarAsync(string name, IEnumerable<Parameter> parameters, DataExecuteOptions options = null, Func<DataExecuteContextBase, CancellationToken, ValueTask<bool>> executing = null, Func<DataExecuteContextBase, CancellationToken, ValueTask> executed = null, CancellationToken cancellation = default)
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		//创建数据访问上下文对象
		using var context = this.CreateExecuteContext(name, true, typeof(object), parameters, options);

		//处理数据访问操作前的回调
		if(executing != null && await executing(context, cancellation))
			return context.Result;

		//激发“Executing”事件，如果被中断则返回
		if(await this.OnExecutingAsync(context, cancellation))
			return context.Result;

		//调用数据访问过滤器前事件
		await this.OnFilteringAsync(context, cancellation);

		//执行数据操作方法
		await this.OnExecuteAsync(context, cancellation);

		//调用数据访问过滤器后事件
		await this.OnFilteredAsync(context, cancellation);

		//激发“Executed”事件
		await this.OnExecutedAsync(context, cancellation);

		//处理数据访问操作后的回调
		if(executed != null)
			await executed(context, cancellation);

		var result = context.Result;

		//返回最终的结果
		return result;
	}
	#endregion

	#region 保护方法
	protected abstract ValueTask OnExecuteAsync(DataExecuteContextBase context, CancellationToken cancellation);
	#endregion
}
