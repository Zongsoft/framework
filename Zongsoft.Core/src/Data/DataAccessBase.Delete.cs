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
}
