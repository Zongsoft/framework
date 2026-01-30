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
}
