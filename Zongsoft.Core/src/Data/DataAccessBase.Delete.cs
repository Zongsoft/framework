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
	#region 公共方法
	public ValueTask<int> DeleteAsync<T>(ICondition criteria, string schema = null, CancellationToken cancellation = default) => this.DeleteAsync(this.GetName<T>(), criteria, schema, null, null, null, cancellation);
	public ValueTask<int> DeleteAsync<T>(ICondition criteria, DataDeleteOptions options, CancellationToken cancellation = default) => this.DeleteAsync(this.GetName<T>(), criteria, string.Empty, options, null, null, cancellation);
	public ValueTask<int> DeleteAsync<T>(ICondition criteria, string schema, DataDeleteOptions options, Func<DataDeleteContextBase, CancellationToken, ValueTask<bool>> deleting = null, Func<DataDeleteContextBase, CancellationToken, ValueTask> deleted = null, CancellationToken cancellation = default) => this.DeleteAsync(this.GetName<T>(), criteria, schema, options, deleting, deleted, cancellation);
	public ValueTask<int> DeleteAsync(string name, ICondition criteria, string schema = null, CancellationToken cancellation = default) => this.DeleteAsync(name, criteria, schema, null, null, null, cancellation);
	public ValueTask<int> DeleteAsync(string name, ICondition criteria, DataDeleteOptions options, CancellationToken cancellation = default) => this.DeleteAsync(name, criteria, string.Empty, options, null, null, cancellation);
	public ValueTask<int> DeleteAsync(string name, ICondition criteria, string schema, DataDeleteOptions options, Func<DataDeleteContextBase, CancellationToken, ValueTask<bool>> deleting = null, Func<DataDeleteContextBase, CancellationToken, ValueTask> deleted = null, CancellationToken cancellation = default) => this.DeleteAsync(name, criteria, this.Schema.Parse(name, schema), options, deleting, deleted, cancellation);
	public async ValueTask<int> DeleteAsync(string name, ICondition criteria, ISchema schema, DataDeleteOptions options, Func<DataDeleteContextBase, CancellationToken, ValueTask<bool>> deleting = null, Func<DataDeleteContextBase, CancellationToken, ValueTask> deleted = null, CancellationToken cancellation = default)
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		//创建数据访问上下文对象
		using var context = this.CreateDeleteContext(name, criteria, schema, options);

		//处理数据访问操作前的回调
		if(deleting != null && await deleting(context, cancellation))
			return context.Count;

		//激发“Deleting”事件，如果被中断则返回
		if(await this.OnDeletingAsync(context, cancellation))
			return context.Count;

		//调用数据访问过滤器前事件
		await this.OnFilteringAsync(context, cancellation);

		//执行数据删除操作
		await this.OnDeleteAsync(context, cancellation);

		//调用数据访问过滤器后事件
		await this.OnFilteredAsync(context, cancellation);

		//激发“Deleted”事件
		await this.OnDeletedAsync(context, cancellation);

		//处理数据访问操作后的回调
		if(deleted != null)
			await deleted(context, cancellation);

		var result = context.Count;

		//返回最终的结果
		return result;
	}
	#endregion

	#region 保护方法
	protected abstract ValueTask OnDeleteAsync(DataDeleteContextBase context, CancellationToken cancellation);
	#endregion

	#region 便利取消
	public ValueTask<int> DeleteAsync<T>(ICondition criteria, CancellationToken cancellation) => this.DeleteAsync<T>(criteria, default(string), cancellation);
	public ValueTask<int> DeleteAsync<T>(ICondition criteria, string schema, DataDeleteOptions options, Func<DataDeleteContextBase, CancellationToken, ValueTask<bool>> deleting, CancellationToken cancellation) => this.DeleteAsync<T>(criteria, schema, options, deleting, default(Func<DataDeleteContextBase, CancellationToken, ValueTask>), cancellation);
	public ValueTask<int> DeleteAsync<T>(ICondition criteria, string schema, DataDeleteOptions options, CancellationToken cancellation) => this.DeleteAsync<T>(criteria, schema, options, default(Func<DataDeleteContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataDeleteContextBase, CancellationToken, ValueTask>), cancellation);
	public ValueTask<int> DeleteAsync(string name, ICondition criteria, CancellationToken cancellation) => this.DeleteAsync(name, criteria, default(string), cancellation);
	public ValueTask<int> DeleteAsync(string name, ICondition criteria, string schema, DataDeleteOptions options, Func<DataDeleteContextBase, CancellationToken, ValueTask<bool>> deleting, CancellationToken cancellation) => this.DeleteAsync(name, criteria, schema, options, deleting, default(Func<DataDeleteContextBase, CancellationToken, ValueTask>), cancellation);
	public ValueTask<int> DeleteAsync(string name, ICondition criteria, string schema, DataDeleteOptions options, CancellationToken cancellation) => this.DeleteAsync(name, criteria, schema, options, default(Func<DataDeleteContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataDeleteContextBase, CancellationToken, ValueTask>), cancellation);
	public ValueTask<int> DeleteAsync(string name, ICondition criteria, ISchema schema, DataDeleteOptions options, Func<DataDeleteContextBase, CancellationToken, ValueTask<bool>> deleting, CancellationToken cancellation) => this.DeleteAsync(name, criteria, schema, options, deleting, default(Func<DataDeleteContextBase, CancellationToken, ValueTask>), cancellation);
	public ValueTask<int> DeleteAsync(string name, ICondition criteria, ISchema schema, DataDeleteOptions options, CancellationToken cancellation) => this.DeleteAsync(name, criteria, schema, options, default(Func<DataDeleteContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataDeleteContextBase, CancellationToken, ValueTask>), cancellation);
	#endregion
}
