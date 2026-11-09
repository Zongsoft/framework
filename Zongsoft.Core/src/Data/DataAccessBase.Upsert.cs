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
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Data;

partial class DataAccessBase
{
	#region 公共方法
	public ValueTask<int> UpsertAsync<T>(T data, CancellationToken cancellation = default) => data == null ? ValueTask.FromResult(0) : this.UpsertAsync(this.GetName<T>(), data, string.Empty, null, null, null, cancellation);
	public ValueTask<int> UpsertAsync<T>(T data, DataUpsertOptions options, CancellationToken cancellation = default) => data == null ? ValueTask.FromResult(0) : this.UpsertAsync(this.GetName<T>(), data, string.Empty, options, null, null, cancellation);

	public ValueTask<int> UpsertAsync<T>(T data, string schema, CancellationToken cancellation = default) => data == null ? ValueTask.FromResult(0) : this.UpsertAsync(this.GetName<T>(), data, schema, null, null, null, cancellation);
	public ValueTask<int> UpsertAsync<T>(T data, string schema, DataUpsertOptions options, CancellationToken cancellation) => this.UpsertAsync<T>(data, schema, options, default(Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	public ValueTask<int> UpsertAsync<T>(T data, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting, CancellationToken cancellation) => this.UpsertAsync<T>(data, schema, options, upserting, default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	public ValueTask<int> UpsertAsync<T>(T data, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting = null, Func<DataUpsertContextBase, CancellationToken, ValueTask> upserted = null, CancellationToken cancellation = default) => data == null ? ValueTask.FromResult(0) : this.UpsertAsync(this.GetName<T>(), data, schema, options, upserting, upserted, cancellation);

	public ValueTask<int> UpsertAsync<T>(object data, CancellationToken cancellation = default) => data == null ? ValueTask.FromResult(0) : this.UpsertAsync(this.GetName<T>(), data, string.Empty, null, null, null, cancellation);
	public ValueTask<int> UpsertAsync<T>(object data, DataUpsertOptions options, CancellationToken cancellation = default) => data == null ? ValueTask.FromResult(0) : this.UpsertAsync(this.GetName<T>(), data, string.Empty, options, null, null, cancellation);

	public ValueTask<int> UpsertAsync<T>(object data, string schema, CancellationToken cancellation = default) => data == null ? ValueTask.FromResult(0) : this.UpsertAsync(this.GetName<T>(), data, schema, null, null, null, cancellation);
	public ValueTask<int> UpsertAsync<T>(object data, string schema, DataUpsertOptions options, CancellationToken cancellation) => this.UpsertAsync<T>(data, schema, options, default(Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	public ValueTask<int> UpsertAsync<T>(object data, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting, CancellationToken cancellation) => this.UpsertAsync<T>(data, schema, options, upserting, default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	public ValueTask<int> UpsertAsync<T>(object data, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting = null, Func<DataUpsertContextBase, CancellationToken, ValueTask> upserted = null, CancellationToken cancellation = default) => data == null ? ValueTask.FromResult(0) : this.UpsertAsync(this.GetName<T>(), data, schema, options, upserting, upserted, cancellation);

	public ValueTask<int> UpsertAsync(string name, object data, CancellationToken cancellation = default) => this.UpsertAsync(name, data, string.Empty, null, null, null, cancellation);
	public ValueTask<int> UpsertAsync(string name, object data, DataUpsertOptions options, CancellationToken cancellation = default) => this.UpsertAsync(name, data, string.Empty, options, null, null, cancellation);

	public ValueTask<int> UpsertAsync(string name, object data, string schema, CancellationToken cancellation = default) => this.UpsertAsync(name, data, schema, null, null, null, cancellation);
	public ValueTask<int> UpsertAsync(string name, object data, string schema, DataUpsertOptions options, CancellationToken cancellation) => this.UpsertAsync(name, data, schema, options, default(Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	public ValueTask<int> UpsertAsync(string name, object data, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting, CancellationToken cancellation) => this.UpsertAsync(name, data, schema, options, upserting, default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	public ValueTask<int> UpsertAsync(string name, object data, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting = null, Func<DataUpsertContextBase, CancellationToken, ValueTask> upserted = null, CancellationToken cancellation = default) => this.UpsertAsync(name, data, this.Schema.Parse(name, schema, data.GetType()), options, upserting, upserted, cancellation);

	public ValueTask<int> UpsertAsync(string name, object data, ISchema schema, DataUpsertOptions options, CancellationToken cancellation) => this.UpsertAsync(name, data, schema, options, default(Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	public ValueTask<int> UpsertAsync(string name, object data, ISchema schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting, CancellationToken cancellation) => this.UpsertAsync(name, data, schema, options, upserting, default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	public async ValueTask<int> UpsertAsync(string name, object data, ISchema schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting = null, Func<DataUpsertContextBase, CancellationToken, ValueTask> upserted = null, CancellationToken cancellation = default)
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		if(data == null)
			return 0;

		//创建数据访问上下文对象
		using var context = this.CreateUpsertContext(name, false, data, schema, options);

		//处理数据访问操作前的回调
		if(upserting != null && await upserting(context, cancellation))
			return context.Count;

		//激发“Upserting”事件，如果被中断则返回
		if(await this.OnUpsertingAsync(context, cancellation))
			return context.Count;

		//调用数据访问过滤器前事件
		await this.OnFilteringAsync(context, cancellation);

		//执行数据插入操作
		await this.OnUpsertAsync(context, cancellation);

		//调用数据访问过滤器后事件
		await this.OnFilteredAsync(context, cancellation);

		//激发“Upserted”事件
		await this.OnUpsertedAsync(context, cancellation);

		//处理数据访问操作后的回调
		if(upserted != null)
			await upserted(context, cancellation);

		var result = context.Count;

		//返回最终的结果
		return result;
	}

	public ValueTask<int> UpsertManyAsync<T>(IEnumerable<T> items, CancellationToken cancellation = default) => items == null ? ValueTask.FromResult(0) : this.UpsertManyAsync(this.GetName<T>(), items, string.Empty, null, null, null, cancellation);
	public ValueTask<int> UpsertManyAsync<T>(IEnumerable<T> items, DataUpsertOptions options, CancellationToken cancellation = default) => items == null ? ValueTask.FromResult(0) : this.UpsertManyAsync(this.GetName<T>(), items, string.Empty, options, null, null, cancellation);

	public ValueTask<int> UpsertManyAsync<T>(IEnumerable<T> items, string schema, CancellationToken cancellation = default) => items == null ? ValueTask.FromResult(0) : this.UpsertManyAsync(this.GetName<T>(), items, schema, null, null, null, cancellation);
	public ValueTask<int> UpsertManyAsync<T>(IEnumerable<T> items, string schema, DataUpsertOptions options, CancellationToken cancellation) => this.UpsertManyAsync<T>(items, schema, options, default(Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	public ValueTask<int> UpsertManyAsync<T>(IEnumerable<T> items, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting, CancellationToken cancellation) => this.UpsertManyAsync<T>(items, schema, options, upserting, default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	public ValueTask<int> UpsertManyAsync<T>(IEnumerable<T> items, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting = null, Func<DataUpsertContextBase, CancellationToken, ValueTask> upserted = null, CancellationToken cancellation = default) => items == null ? ValueTask.FromResult(0) : this.UpsertManyAsync(this.GetName<T>(), items, schema, options, upserting, upserted, cancellation);

	public ValueTask<int> UpsertManyAsync<T>(IEnumerable items, CancellationToken cancellation = default) => items == null ? ValueTask.FromResult(0) : this.UpsertManyAsync(this.GetName<T>(), items, string.Empty, null, null, null, cancellation);
	public ValueTask<int> UpsertManyAsync<T>(IEnumerable items, DataUpsertOptions options, CancellationToken cancellation = default) => items == null ? ValueTask.FromResult(0) : this.UpsertManyAsync(this.GetName<T>(), items, string.Empty, options, null, null, cancellation);

	public ValueTask<int> UpsertManyAsync<T>(IEnumerable items, string schema, CancellationToken cancellation = default) => items == null ? ValueTask.FromResult(0) : this.UpsertManyAsync(this.GetName<T>(), items, schema, null, null, null, cancellation);
	public ValueTask<int> UpsertManyAsync<T>(IEnumerable items, string schema, DataUpsertOptions options, CancellationToken cancellation) => this.UpsertManyAsync<T>(items, schema, options, default(Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	public ValueTask<int> UpsertManyAsync<T>(IEnumerable items, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting, CancellationToken cancellation) => this.UpsertManyAsync<T>(items, schema, options, upserting, default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	public ValueTask<int> UpsertManyAsync<T>(IEnumerable items, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting = null, Func<DataUpsertContextBase, CancellationToken, ValueTask> upserted = null, CancellationToken cancellation = default) => items == null ? ValueTask.FromResult(0) : this.UpsertManyAsync(this.GetName<T>(), items, schema, options, upserting, upserted, cancellation);

	public ValueTask<int> UpsertManyAsync(string name, IEnumerable items, CancellationToken cancellation = default) => this.UpsertManyAsync(name, items, string.Empty, null, null, null, cancellation);
	public ValueTask<int> UpsertManyAsync(string name, IEnumerable items, DataUpsertOptions options, CancellationToken cancellation = default) => this.UpsertManyAsync(name, items, string.Empty, options, null, null, cancellation);

	public ValueTask<int> UpsertManyAsync(string name, IEnumerable items, string schema, CancellationToken cancellation = default) => this.UpsertManyAsync(name, items, schema, null, null, null, cancellation);
	public ValueTask<int> UpsertManyAsync(string name, IEnumerable items, string schema, DataUpsertOptions options, CancellationToken cancellation) => this.UpsertManyAsync(name, items, schema, options, default(Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	public ValueTask<int> UpsertManyAsync(string name, IEnumerable items, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting, CancellationToken cancellation) => this.UpsertManyAsync(name, items, schema, options, upserting, default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	public ValueTask<int> UpsertManyAsync(string name, IEnumerable items, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting = null, Func<DataUpsertContextBase, CancellationToken, ValueTask> upserted = null, CancellationToken cancellation = default) => this.UpsertManyAsync(name, items, this.Schema.Parse(name, schema, Common.TypeExtension.GetElementType(items.GetType())), options, upserting, upserted, cancellation);

	public ValueTask<int> UpsertManyAsync(string name, IEnumerable items, ISchema schema, DataUpsertOptions options, CancellationToken cancellation) => this.UpsertManyAsync(name, items, schema, options, default(Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	public ValueTask<int> UpsertManyAsync(string name, IEnumerable items, ISchema schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting, CancellationToken cancellation) => this.UpsertManyAsync(name, items, schema, options, upserting, default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	public ValueTask<int> UpsertManyAsync(string name, IEnumerable items, ISchema schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting = null, Func<DataUpsertContextBase, CancellationToken, ValueTask> upserted = null, CancellationToken cancellation = default) => this.UpsertManyCoreAsync(name, items, schema, options, upserting, upserted, cancellation);

	public ValueTask<int> UpsertManyAsync<T>(IAsyncEnumerable<T> items, CancellationToken cancellation = default) => items == null ? ValueTask.FromResult(0) : this.UpsertManyAsync(this.GetName<T>(), items, string.Empty, null, null, null, cancellation);
	public ValueTask<int> UpsertManyAsync<T>(IAsyncEnumerable<T> items, DataUpsertOptions options, CancellationToken cancellation = default) => items == null ? ValueTask.FromResult(0) : this.UpsertManyAsync(this.GetName<T>(), items, string.Empty, options, null, null, cancellation);

	public ValueTask<int> UpsertManyAsync<T>(IAsyncEnumerable<T> items, string schema, CancellationToken cancellation = default) => items == null ? ValueTask.FromResult(0) : this.UpsertManyAsync(this.GetName<T>(), items, schema, null, null, null, cancellation);
	public ValueTask<int> UpsertManyAsync<T>(IAsyncEnumerable<T> items, string schema, DataUpsertOptions options, CancellationToken cancellation) => this.UpsertManyAsync<T>(items, schema, options, default(Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	public ValueTask<int> UpsertManyAsync<T>(IAsyncEnumerable<T> items, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting, CancellationToken cancellation) => this.UpsertManyAsync<T>(items, schema, options, upserting, default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	public ValueTask<int> UpsertManyAsync<T>(IAsyncEnumerable<T> items, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting = null, Func<DataUpsertContextBase, CancellationToken, ValueTask> upserted = null, CancellationToken cancellation = default) => items == null ? ValueTask.FromResult(0) : this.UpsertManyAsync(this.GetName<T>(), items, schema, options, upserting, upserted, cancellation);

	public ValueTask<int> UpsertManyAsync<T>(string name, IAsyncEnumerable<T> items, CancellationToken cancellation = default) => this.UpsertManyAsync<T>(name, items, string.Empty, null, null, null, cancellation);
	public ValueTask<int> UpsertManyAsync<T>(string name, IAsyncEnumerable<T> items, DataUpsertOptions options, CancellationToken cancellation = default) => this.UpsertManyAsync<T>(name, items, string.Empty, options, null, null, cancellation);

	public ValueTask<int> UpsertManyAsync<T>(string name, IAsyncEnumerable<T> items, string schema, CancellationToken cancellation = default) => this.UpsertManyAsync<T>(name, items, schema, null, null, null, cancellation);
	public ValueTask<int> UpsertManyAsync<T>(string name, IAsyncEnumerable<T> items, string schema, DataUpsertOptions options, CancellationToken cancellation) => this.UpsertManyAsync<T>(name, items, schema, options, default(Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	public ValueTask<int> UpsertManyAsync<T>(string name, IAsyncEnumerable<T> items, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting, CancellationToken cancellation) => this.UpsertManyAsync<T>(name, items, schema, options, upserting, default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	public ValueTask<int> UpsertManyAsync<T>(string name, IAsyncEnumerable<T> items, string schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting = null, Func<DataUpsertContextBase, CancellationToken, ValueTask> upserted = null, CancellationToken cancellation = default) => this.UpsertManyAsync<T>(name, items, this.Schema.Parse(name, schema, typeof(T)), options, upserting, upserted, cancellation);

	public ValueTask<int> UpsertManyAsync<T>(string name, IAsyncEnumerable<T> items, ISchema schema, DataUpsertOptions options, CancellationToken cancellation) => this.UpsertManyAsync<T>(name, items, schema, options, default(Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	public ValueTask<int> UpsertManyAsync<T>(string name, IAsyncEnumerable<T> items, ISchema schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting, CancellationToken cancellation) => this.UpsertManyAsync<T>(name, items, schema, options, upserting, default(Func<DataUpsertContextBase, CancellationToken, ValueTask>), cancellation);
	public ValueTask<int> UpsertManyAsync<T>(string name, IAsyncEnumerable<T> items, ISchema schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting = null, Func<DataUpsertContextBase, CancellationToken, ValueTask> upserted = null, CancellationToken cancellation = default) => this.UpsertManyCoreAsync(name, items, schema, options, upserting, upserted, cancellation);
	#endregion

	#region 保护方法
	protected abstract ValueTask OnUpsertAsync(DataUpsertContextBase context, CancellationToken cancellation);
	#endregion

	#region 私有方法
	private async ValueTask<int> UpsertManyCoreAsync(string name, object items, ISchema schema, DataUpsertOptions options, Func<DataUpsertContextBase, CancellationToken, ValueTask<bool>> upserting = null, Func<DataUpsertContextBase, CancellationToken, ValueTask> upserted = null, CancellationToken cancellation = default)
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		if(items == null)
			return 0;

		//创建数据访问上下文对象
		using var context = this.CreateUpsertContext(name, true, items, schema, options);

		//处理数据访问操作前的回调
		if(upserting != null && await upserting(context, cancellation))
			return context.Count;

		//激发“Upserting”事件，如果被中断则返回
		if(await this.OnUpsertingAsync(context, cancellation))
			return context.Count;

		//调用数据访问过滤器前事件
		await this.OnFilteringAsync(context, cancellation);

		//执行数据增改操作
		await this.OnUpsertAsync(context, cancellation);

		//调用数据访问过滤器后事件
		await this.OnFilteredAsync(context, cancellation);

		//激发“Upserted”事件
		await this.OnUpsertedAsync(context, cancellation);

		//处理数据访问操作后的回调
		if(upserted != null)
			await upserted(context, cancellation);

		var result = context.Count;

		//返回最终的结果
		return result;
	}
	#endregion
}
