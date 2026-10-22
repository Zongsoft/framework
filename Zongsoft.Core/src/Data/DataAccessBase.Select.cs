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
using System.Runtime.CompilerServices;

namespace Zongsoft.Data;

partial class DataAccessBase
{
	#region 普通查询
	public IAsyncEnumerable<T> SelectAsync<T>(CancellationToken cancellation) =>
		this.SelectAsync<T>(this.GetName<T>(), null, string.Empty, null, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), null, string.Empty, null, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, string.Empty, null, null, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, string.Empty, null, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, string.Empty, paging, null, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, string.Empty, paging, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, schema, null, null, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, schema, null, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, schema, paging, null, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, schema, paging, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, CancellationToken, ValueTask<bool>> selecting, Func<DataSelectContextBase, CancellationToken, ValueTask> selected, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, schema, paging, options, sortings, selecting, selected, cancellation);

	public IAsyncEnumerable<T> SelectAsync<T>(string name, CancellationToken cancellation) =>
		this.SelectAsync<T>(name, null, string.Empty, null, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, null, string.Empty, null, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, criteria, string.Empty, null, null, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, criteria, string.Empty, null, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, criteria, string.Empty, paging, null, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, criteria, string.Empty, paging, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, criteria, schema, null, null, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, criteria, schema, null, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, criteria, schema, paging, null, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, criteria, schema, paging, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, CancellationToken, ValueTask<bool>> selecting, Func<DataSelectContextBase, CancellationToken, ValueTask> selected, CancellationToken cancellation = default) =>
		 this.SelectAsync<T>(name, criteria, this.Schema.Parse(name, schema, typeof(T)), paging, options, sortings, selecting, selected, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, ISchema schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, CancellationToken, ValueTask<bool>> selecting, Func<DataSelectContextBase, CancellationToken, ValueTask> selected, CancellationToken cancellation = default)
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
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, null, string.Empty, null, null, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, null, string.Empty, null, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, Paging paging, CancellationToken cancellation) =>
		this.SelectAsync<T>(name, grouping, null, string.Empty, paging, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, null, string.Empty, paging, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, null, schema, null, null, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, null, schema, null, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, Paging paging, CancellationToken cancellation) =>
		this.SelectAsync<T>(name, grouping, null, schema, paging, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		  this.SelectAsync<T>(name, grouping, null, schema, paging, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, CancellationToken cancellation) =>
		this.SelectAsync<T>(name, grouping, criteria, (ISchema)null, null, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, criteria, (ISchema)null, paging, null, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, criteria, schema, null, null, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, criteria, schema, null, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, criteria, schema, paging, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, CancellationToken, ValueTask<bool>> selecting, Func<DataSelectContextBase, CancellationToken, ValueTask> selected, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, criteria, string.IsNullOrWhiteSpace(schema) ? null : this.Schema.Parse(name, schema, typeof(T)), paging, options, sortings, selecting, selected, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, ISchema schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, CancellationToken, ValueTask<bool>> selecting, Func<DataSelectContextBase, CancellationToken, ValueTask> selected, CancellationToken cancellation = default)
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
	private IAsyncEnumerable<T> SelectAsync<T>(DataSelectContextBase context, Func<DataSelectContextBase, CancellationToken, ValueTask<bool>> selecting, Func<DataSelectContextBase, CancellationToken, ValueTask> selected, CancellationToken cancellation = default)
	{
		this.EnsureDisposed();
		var ownsContext = true;

		try
		{
			cancellation.ThrowIfCancellationRequested();
			var preparation = this.PrepareSelectAsync(context, selecting, cancellation);
			ValueTask<IAsyncEnumerable<T>> operation;

			//已完成的短路保持原结果身份；不等待尚未完成的准备操作。
			if(preparation.IsCompleted)
			{
				if(preparation.Result)
					return ToAsyncEnumerable<T>(context.Result, cancellation);

				operation = this.CompleteSelectAsync<T>(context, ValueTask.FromResult(false), selected, cancellation);
			}

			else
				operation = this.CompleteSelectAsync<T>(context, preparation, selected, cancellation);

			var result = new DataAsyncEnumerable<T>(
				operation,
				cancellation,
				context.Paging == null || !context.Paging.IsPaged());

			ownsContext = false;
			return result;
		}
		finally
		{
			if(ownsContext)
				context.Dispose();
		}
	}

	private async ValueTask<bool> PrepareSelectAsync(DataSelectContextBase context, Func<DataSelectContextBase, CancellationToken, ValueTask<bool>> selecting, CancellationToken cancellation)
	{
		if(selecting != null && await selecting(context, cancellation))
			return true;

		if(await this.OnSelectingAsync(context, cancellation))
			return true;

		await this.OnFilteringAsync(context, cancellation);
		return false;
	}

	private async ValueTask<IAsyncEnumerable<T>> CompleteSelectAsync<T>(DataSelectContextBase context, ValueTask<bool> preparation, Func<DataSelectContextBase, CancellationToken, ValueTask> selected, CancellationToken cancellation)
	{
		try
		{
			if(!await preparation)
			{
				await this.OnSelectAsync(context, cancellation);
				await this.OnFilteredAsync(context, cancellation);
				await this.OnSelectedAsync(context, cancellation);

				if(selected != null)
					await selected(context, cancellation);
			}

			return ToAsyncEnumerable<T>(context.Result, cancellation);
		}
		finally
		{
			context.Dispose();
		}
	}

	protected abstract ValueTask OnSelectAsync(DataSelectContextBase context, CancellationToken cancellation);
	#endregion

	#region 便利重载
	public IAsyncEnumerable<T> SelectAsync<T>(DataSelectOptions options = null, params Sorting[] sortings) => this.SelectAsync<T>(options, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, params Sorting[] sortings) => this.SelectAsync<T>(criteria, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync<T>(criteria, options, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, Paging paging, params Sorting[] sortings) => this.SelectAsync<T>(criteria, paging, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, Paging paging, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync<T>(criteria, paging, options, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, params Sorting[] sortings) => this.SelectAsync<T>(criteria, schema, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync<T>(criteria, schema, options, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, Paging paging, params Sorting[] sortings) => this.SelectAsync<T>(criteria, schema, paging, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync<T>(criteria, schema, paging, options, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, DataSelectOptions options = null, params Sorting[] sortings) => this.SelectAsync<T>(name, options, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, params Sorting[] sortings) => this.SelectAsync<T>(name, criteria, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync<T>(name, criteria, options, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, Paging paging, params Sorting[] sortings) => this.SelectAsync<T>(name, criteria, paging, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, Paging paging, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync<T>(name, criteria, paging, options, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, params Sorting[] sortings) => this.SelectAsync<T>(name, criteria, schema, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync<T>(name, criteria, schema, options, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, Paging paging, params Sorting[] sortings) => this.SelectAsync<T>(name, criteria, schema, paging, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync<T>(name, criteria, schema, paging, options, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, params Sorting[] sortings) => this.SelectAsync<T>(name, grouping, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync<T>(name, grouping, options, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, Paging paging, DataSelectOptions options = null, params Sorting[] sortings) => this.SelectAsync<T>(name, grouping, paging, options, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, params Sorting[] sortings) => this.SelectAsync<T>(name, grouping, schema, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync<T>(name, grouping, schema, options, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, Paging paging, DataSelectOptions options = null, params Sorting[] sortings) => this.SelectAsync<T>(name, grouping, schema, paging, options, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, Paging paging, params Sorting[] sortings) => this.SelectAsync<T>(name, grouping, criteria, paging, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, params Sorting[] sortings) => this.SelectAsync<T>(name, grouping, criteria, schema, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync<T>(name, grouping, criteria, schema, options, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options = null, params Sorting[] sortings) => this.SelectAsync<T>(name, grouping, criteria, schema, paging, options, sortings, CancellationToken.None);
	#endregion

	#region 取消重载
	public IAsyncEnumerable<T> SelectAsync<T>(DataSelectOptions options, CancellationToken cancellation) =>
		this.SelectAsync<T>(this.GetName<T>(), null, string.Empty, null, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, CancellationToken cancellation) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, string.Empty, null, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, DataSelectOptions options, CancellationToken cancellation) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, string.Empty, null, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, Paging paging, CancellationToken cancellation) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, string.Empty, paging, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, Paging paging, DataSelectOptions options, CancellationToken cancellation) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, string.Empty, paging, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, CancellationToken cancellation) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, schema, null, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, DataSelectOptions options, CancellationToken cancellation) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, schema, null, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, Paging paging, CancellationToken cancellation) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, schema, paging, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, Paging paging, DataSelectOptions options, CancellationToken cancellation) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, schema, paging, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, DataSelectOptions options, CancellationToken cancellation) =>
		this.SelectAsync<T>(name, null, string.Empty, null, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, CancellationToken cancellation) =>
		this.SelectAsync<T>(name, criteria, string.Empty, null, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, DataSelectOptions options, CancellationToken cancellation) =>
		this.SelectAsync<T>(name, criteria, string.Empty, null, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, Paging paging, CancellationToken cancellation) =>
		this.SelectAsync<T>(name, criteria, string.Empty, paging, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, Paging paging, DataSelectOptions options, CancellationToken cancellation) =>
		this.SelectAsync<T>(name, criteria, string.Empty, paging, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, CancellationToken cancellation) =>
		this.SelectAsync<T>(name, criteria, schema, null, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, DataSelectOptions options, CancellationToken cancellation) =>
		this.SelectAsync<T>(name, criteria, schema, null, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, Paging paging, CancellationToken cancellation) =>
		this.SelectAsync<T>(name, criteria, schema, paging, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, Paging paging, DataSelectOptions options, CancellationToken cancellation) =>
		this.SelectAsync<T>(name, criteria, schema, paging, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, CancellationToken cancellation) =>
		this.SelectAsync<T>(name, grouping, null, string.Empty, null, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, DataSelectOptions options, CancellationToken cancellation) =>
		this.SelectAsync<T>(name, grouping, null, string.Empty, null, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, Paging paging, DataSelectOptions options, CancellationToken cancellation) =>
		this.SelectAsync<T>(name, grouping, null, string.Empty, paging, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, CancellationToken cancellation) =>
		this.SelectAsync<T>(name, grouping, null, schema, null, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, DataSelectOptions options, CancellationToken cancellation) =>
		this.SelectAsync<T>(name, grouping, null, schema, null, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, Paging paging, DataSelectOptions options, CancellationToken cancellation) =>
		this.SelectAsync<T>(name, grouping, null, schema, paging, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, Paging paging, CancellationToken cancellation) =>
		this.SelectAsync<T>(name, grouping, criteria, (ISchema)null, paging, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, CancellationToken cancellation) =>
		this.SelectAsync<T>(name, grouping, criteria, schema, null, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, DataSelectOptions options, CancellationToken cancellation) =>
		this.SelectAsync<T>(name, grouping, criteria, schema, null, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options, CancellationToken cancellation) =>
		this.SelectAsync<T>(name, grouping, criteria, schema, paging, options, null, null, null, cancellation);
	#endregion

	#region 取消重载
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, Paging paging, CancellationToken cancellation) => this.SelectAsync<T>(name, grouping, criteria, schema, paging, null, null, cancellation);
	#endregion

	#region 排序重载
	public IAsyncEnumerable<T> SelectAsync<T>(Sorting[] sortings, CancellationToken cancellation) => this.SelectAsync<T>(default(DataSelectOptions), sortings, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Sorting[] sortings, CancellationToken cancellation) => this.SelectAsync<T>(name, default(DataSelectOptions), sortings, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, Paging paging, Sorting[] sortings, CancellationToken cancellation) => this.SelectAsync<T>(name, grouping, paging, default(DataSelectOptions), sortings, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation) => this.SelectAsync<T>(name, grouping, schema, paging, default(DataSelectOptions), sortings, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation) => this.SelectAsync<T>(name, grouping, criteria, schema, paging, default(DataSelectOptions), sortings, cancellation);
	#endregion
}
