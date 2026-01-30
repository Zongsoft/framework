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

namespace Zongsoft.Data;

partial class DataAccessBase
{
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
}
