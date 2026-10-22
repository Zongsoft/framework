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
	public ValueTask<bool> ExistsAsync<T>(ICondition criteria, CancellationToken cancellation) => this.ExistsAsync<T>(criteria, default(DataExistsOptions), default(Func<DataExistContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataExistContextBase, CancellationToken, ValueTask>), cancellation);
	public ValueTask<bool> ExistsAsync<T>(ICondition criteria, DataExistsOptions options, CancellationToken cancellation) => this.ExistsAsync<T>(criteria, options, default(Func<DataExistContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataExistContextBase, CancellationToken, ValueTask>), cancellation);
	public ValueTask<bool> ExistsAsync<T>(ICondition criteria, DataExistsOptions options, Func<DataExistContextBase, CancellationToken, ValueTask<bool>> existing, CancellationToken cancellation) => this.ExistsAsync<T>(criteria, options, existing, default(Func<DataExistContextBase, CancellationToken, ValueTask>), cancellation);
	public ValueTask<bool> ExistsAsync<T>(ICondition criteria, DataExistsOptions options = null, Func<DataExistContextBase, CancellationToken, ValueTask<bool>> existing = null, Func<DataExistContextBase, CancellationToken, ValueTask> existed = null, CancellationToken cancellation = default) => this.ExistsAsync(this.GetName<T>(), criteria, options, existing, existed, cancellation);

	public ValueTask<bool> ExistsAsync(string name, ICondition criteria, CancellationToken cancellation) => this.ExistsAsync(name, criteria, default(DataExistsOptions), default(Func<DataExistContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataExistContextBase, CancellationToken, ValueTask>), cancellation);
	public ValueTask<bool> ExistsAsync(string name, ICondition criteria, DataExistsOptions options, CancellationToken cancellation) => this.ExistsAsync(name, criteria, options, default(Func<DataExistContextBase, CancellationToken, ValueTask<bool>>), default(Func<DataExistContextBase, CancellationToken, ValueTask>), cancellation);
	public ValueTask<bool> ExistsAsync(string name, ICondition criteria, DataExistsOptions options, Func<DataExistContextBase, CancellationToken, ValueTask<bool>> existing, CancellationToken cancellation) => this.ExistsAsync(name, criteria, options, existing, default(Func<DataExistContextBase, CancellationToken, ValueTask>), cancellation);
	public async ValueTask<bool> ExistsAsync(string name, ICondition criteria, DataExistsOptions options = null, Func<DataExistContextBase, CancellationToken, ValueTask<bool>> existing = null, Func<DataExistContextBase, CancellationToken, ValueTask> existed = null, CancellationToken cancellation = default)
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		//创建数据访问上下文对象
		using var context = this.CreateExistContext(name, criteria, options);

		//处理数据访问操作前的回调
		if(existing != null && await existing(context, cancellation))
			return context.Result;

		//激发“Existing”事件，如果被中断则返回
		if(await this.OnExistingAsync(context, cancellation))
			return context.Result;

		//调用数据访问过滤器前事件
		await this.OnFilteringAsync(context, cancellation);

		//执行存在操作方法
		await this.OnExistsAsync(context, cancellation);

		//调用数据访问过滤器后事件
		await this.OnFilteredAsync(context, cancellation);

		//激发“Existed”事件
		await this.OnExistedAsync(context, cancellation);

		//处理数据访问操作后的回调
		if(existed != null)
			await existed(context, cancellation);

		var result = context.Result;

		//返回最终的结果
		return result;
	}
	#endregion

	#region 保护方法
	protected abstract ValueTask OnExistsAsync(DataExistContextBase context, CancellationToken cancellation);
	#endregion
}
