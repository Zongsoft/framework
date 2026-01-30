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
using System.Collections.Generic;

namespace Zongsoft.Data;

partial class DataAccessBase
{
	public int Execute(string name, DataExecuteOptions options = null, Func<DataExecuteContextBase, bool> executing = null, Action<DataExecuteContextBase> executed = null) => this.Execute(name, null, options, executing, executed);
	public int Execute(string name, IEnumerable<Parameter> parameters, DataExecuteOptions options = null, Func<DataExecuteContextBase, bool> executing = null, Action<DataExecuteContextBase> executed = null)
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		//创建数据访问上下文对象
		var context = this.CreateExecuteContext(name, false, null, parameters, options);

		//处理数据访问操作前的回调
		if(executing != null && executing(context))
		{
			//返回委托回调的结果
			return GetRecordsAffected(context.Result);
		}

		//激发“Executing”事件，如果被中断则返回
		if(this.OnExecuting(context))
		{
			//返回事件执行后的结果
			return GetRecordsAffected(context.Result);
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

		var result = GetRecordsAffected(context.Result);

		//处置上下文资源
		context.Dispose();

		//返回最终的结果
		return result;
	}

	public ValueTask<int> ExecuteAsync(string name, CancellationToken cancellation = default) => this.ExecuteAsync(name, null, null, null, null, cancellation);
	public ValueTask<int> ExecuteAsync(string name, DataExecuteOptions options, CancellationToken cancellation = default) => this.ExecuteAsync(name, null, options, null, null, cancellation);
	public ValueTask<int> ExecuteAsync(string name, DataExecuteOptions options, Func<DataExecuteContextBase, bool> executing, Action<DataExecuteContextBase> executed, CancellationToken cancellation = default) => this.ExecuteAsync(name, null, options, executing, executed, cancellation);
	public ValueTask<int> ExecuteAsync(string name, IEnumerable<Parameter> parameters, CancellationToken cancellation = default) => this.ExecuteAsync(name, parameters, null, null, null, cancellation);
	public ValueTask<int> ExecuteAsync(string name, IEnumerable<Parameter> parameters, DataExecuteOptions options, CancellationToken cancellation = default) => this.ExecuteAsync(name, parameters, options, null, null, cancellation);
	public async ValueTask<int> ExecuteAsync(string name, IEnumerable<Parameter> parameters, DataExecuteOptions options, Func<DataExecuteContextBase, bool> executing, Action<DataExecuteContextBase> executed, CancellationToken cancellation = default)
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		//创建数据访问上下文对象
		var context = this.CreateExecuteContext(name, false, null, parameters, options);

		//处理数据访问操作前的回调
		if(executing != null && executing(context))
			return GetRecordsAffected(context.Result);

		//激发“Executing”事件，如果被中断则返回
		if(this.OnExecuting(context))
			return GetRecordsAffected(context.Result);

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

		var result = GetRecordsAffected(context.Result);

		//处置上下文资源
		context.Dispose();

		//返回最终的结果
		return result;
	}

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
	public async IAsyncEnumerable<T> ExecuteAsync<T>(string name, IEnumerable<Parameter> parameters, DataExecuteOptions options, Func<DataExecuteContextBase, bool> executing, Action<DataExecuteContextBase> executed, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellation = default)
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
}
