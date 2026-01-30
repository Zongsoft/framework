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

namespace Zongsoft.Data;

partial class DataAccessBase
{
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
}
