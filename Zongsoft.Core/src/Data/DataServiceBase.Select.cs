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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

using Zongsoft.Collections;

namespace Zongsoft.Data;

partial class DataServiceBase<TModel>
{
	#region 常规查询
	IAsyncEnumerable<object> IDataService.SelectAsync(CancellationToken cancellation) => this.SelectAsync(null, string.Empty, null, null, null, cancellation).Cast(cancellation);
	IAsyncEnumerable<object> IDataService.SelectAsync(DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync(null, string.Empty, null, options, sortings, CancellationToken.None).Cast();
	IAsyncEnumerable<object> IDataService.SelectAsync(DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation) => this.SelectAsync(null, string.Empty, null, options, sortings, cancellation).Cast(cancellation);

	IAsyncEnumerable<object> IDataService.SelectAsync(ICondition criteria, params Sorting[] sortings) => this.SelectAsync(criteria, string.Empty, null, null, sortings, CancellationToken.None).Cast();
	IAsyncEnumerable<object> IDataService.SelectAsync(ICondition criteria, Sorting[] sortings, CancellationToken cancellation) => this.SelectAsync(criteria, string.Empty, null, null, sortings, cancellation).Cast(cancellation);
	IAsyncEnumerable<object> IDataService.SelectAsync(ICondition criteria, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync(criteria, string.Empty, null, options, sortings, CancellationToken.None).Cast();
	IAsyncEnumerable<object> IDataService.SelectAsync(ICondition criteria, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation) => this.SelectAsync(criteria, string.Empty, null, options, sortings, cancellation).Cast(cancellation);

	IAsyncEnumerable<object> IDataService.SelectAsync(ICondition criteria, Paging paging, params Sorting[] sortings) => this.SelectAsync(criteria, string.Empty, paging, null, sortings, CancellationToken.None).Cast();
	IAsyncEnumerable<object> IDataService.SelectAsync(ICondition criteria, Paging paging, Sorting[] sortings, CancellationToken cancellation) => this.SelectAsync(criteria, string.Empty, paging, null, sortings, cancellation).Cast(cancellation);
	IAsyncEnumerable<object> IDataService.SelectAsync(ICondition criteria, Paging paging, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync(criteria, string.Empty, paging, options, sortings, CancellationToken.None).Cast();
	IAsyncEnumerable<object> IDataService.SelectAsync(ICondition criteria, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation) => this.SelectAsync(criteria, string.Empty, paging, options, sortings, cancellation).Cast(cancellation);

	IAsyncEnumerable<object> IDataService.SelectAsync(ICondition criteria, string schema, params Sorting[] sortings) => this.SelectAsync(criteria, schema, null, null, sortings, CancellationToken.None).Cast();
	IAsyncEnumerable<object> IDataService.SelectAsync(ICondition criteria, string schema, Sorting[] sortings, CancellationToken cancellation) => this.SelectAsync(criteria, schema, null, null, sortings, cancellation).Cast(cancellation);
	IAsyncEnumerable<object> IDataService.SelectAsync(ICondition criteria, string schema, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync(criteria, schema, null, options, sortings, CancellationToken.None).Cast();
	IAsyncEnumerable<object> IDataService.SelectAsync(ICondition criteria, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation) => this.SelectAsync(criteria, schema, null, options, sortings, cancellation).Cast(cancellation);

	IAsyncEnumerable<object> IDataService.SelectAsync(ICondition criteria, string schema, Paging paging, params Sorting[] sortings) => this.SelectAsync(criteria, schema, paging, null, sortings, CancellationToken.None).Cast();
	IAsyncEnumerable<object> IDataService.SelectAsync(ICondition criteria, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation) => this.SelectAsync(criteria, schema, paging, null, sortings, cancellation).Cast(cancellation);
	IAsyncEnumerable<object> IDataService.SelectAsync(ICondition criteria, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync(criteria, schema, paging, options, sortings, CancellationToken.None).Cast();
	IAsyncEnumerable<object> IDataService.SelectAsync(ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation) => this.SelectAsync(criteria, schema, paging, options, sortings, cancellation).Cast(cancellation);

	public IAsyncEnumerable<TModel> SelectAsync(CancellationToken cancellation) => this.SelectAsync(null, string.Empty, null, null, null, cancellation);
	public IAsyncEnumerable<TModel> SelectAsync(DataSelectOptions options = null, params Sorting[] sortings) => this.SelectAsync(null, string.Empty, null, options, sortings, CancellationToken.None);
	public IAsyncEnumerable<TModel> SelectAsync(DataSelectOptions options, CancellationToken cancellation) => this.SelectAsync(options, default(Sorting[]), cancellation);
	public IAsyncEnumerable<TModel> SelectAsync(Sorting[] sortings, CancellationToken cancellation) => this.SelectAsync(default(DataSelectOptions), sortings, cancellation);
	public IAsyncEnumerable<TModel> SelectAsync(DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync(null, string.Empty, null, options, sortings, cancellation);

	public IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, params Sorting[] sortings) => this.SelectAsync(criteria, string.Empty, null, null, sortings, CancellationToken.None);
	public IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, CancellationToken cancellation) => this.SelectAsync(criteria, default(Sorting[]), cancellation);
	public IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync(criteria, string.Empty, null, null, sortings, cancellation);
	public IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync(criteria, string.Empty, null, options, sortings, CancellationToken.None);
	public IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, DataSelectOptions options, CancellationToken cancellation) => this.SelectAsync(criteria, options, default(Sorting[]), cancellation);
	public IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync(criteria, string.Empty, null, options, sortings, cancellation);

	public IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, Paging paging, params Sorting[] sortings) => this.SelectAsync(criteria, string.Empty, paging, null, sortings, CancellationToken.None);
	public IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, Paging paging, CancellationToken cancellation) => this.SelectAsync(criteria, paging, default(Sorting[]), cancellation);
	public IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync(criteria, string.Empty, paging, null, sortings, cancellation);
	public IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, Paging paging, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync(criteria, string.Empty, paging, options, sortings, CancellationToken.None);
	public IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, Paging paging, DataSelectOptions options, CancellationToken cancellation) => this.SelectAsync(criteria, paging, options, default(Sorting[]), cancellation);
	public IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync(criteria, string.Empty, paging, options, sortings, cancellation);

	public IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, string schema, params Sorting[] sortings) => this.SelectAsync(criteria, schema, null, null, sortings, CancellationToken.None);
	public IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, string schema, CancellationToken cancellation) => this.SelectAsync(criteria, schema, default(Sorting[]), cancellation);
	public IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, string schema, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync(criteria, schema, null, null, sortings, cancellation);
	public IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, string schema, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync(criteria, schema, null, options, sortings, CancellationToken.None);
	public IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, string schema, DataSelectOptions options, CancellationToken cancellation) => this.SelectAsync(criteria, schema, options, default(Sorting[]), cancellation);
	public IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync(criteria, schema, null, options, sortings, cancellation);

	public IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, string schema, Paging paging, params Sorting[] sortings) => this.SelectAsync(criteria, schema, paging, null, sortings, CancellationToken.None);
	public IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, string schema, Paging paging, CancellationToken cancellation) => this.SelectAsync(criteria, schema, paging, default(Sorting[]), cancellation);
	public IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync(criteria, schema, paging, null, sortings, cancellation);
	public IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync(criteria, schema, paging, options, sortings, CancellationToken.None);
	public IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, string schema, Paging paging, DataSelectOptions options, CancellationToken cancellation) => this.SelectAsync(criteria, schema, paging, options, default(Sorting[]), cancellation);
	public IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
	{
		return DataAsyncEnumerable<TModel>.From(PrepareAsync(), cancellation, paging);

		async ValueTask<IAsyncEnumerable<TModel>> PrepareAsync()
		{
			//构建数据操作的选项对象
			if(options == null)
				options = new DataSelectOptions();

			//进行授权验证
			await this.AuthorizeAsync(DataServiceMethod.Select(), options, cancellation);

			//修整查询条件
			criteria = await this.OnValidateAsync(DataServiceMethod.Select(), criteria, options, cancellation);

			//执行查询方法
			return this.OnSelectAsync(criteria, this.GetSchema(schema, typeof(TModel)), paging, sortings, options, cancellation);
		}
	}
	#endregion

	#region 分组查询
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, params Sorting[] sortings) => this.SelectAsync<T>(grouping, null, string.Empty, null, null, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, CancellationToken cancellation) => this.SelectAsync<T>(grouping, default(Sorting[]), cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync<T>(grouping, null, string.Empty, null, null, sortings, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync<T>(grouping, null, string.Empty, null, options, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, DataSelectOptions options, CancellationToken cancellation) => this.SelectAsync<T>(grouping, options, default(Sorting[]), cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync<T>(grouping, null, string.Empty, null, options, sortings, cancellation);

	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, Paging paging, params Sorting[] sortings) => this.SelectAsync<T>(grouping, null, string.Empty, paging, null, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, Paging paging, CancellationToken cancellation) => this.SelectAsync<T>(grouping, paging, default(Sorting[]), cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync<T>(grouping, null, string.Empty, paging, null, sortings, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, Paging paging, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync<T>(grouping, null, string.Empty, paging, options, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, Paging paging, DataSelectOptions options, CancellationToken cancellation) => this.SelectAsync<T>(grouping, paging, options, default(Sorting[]), cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync<T>(grouping, null, string.Empty, paging, options, sortings, cancellation);

	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, string schema, params Sorting[] sortings) => this.SelectAsync<T>(grouping, null, schema, null, null, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, string schema, CancellationToken cancellation) => this.SelectAsync<T>(grouping, schema, default(Sorting[]), cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, string schema, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync<T>(grouping, null, schema, null, null, sortings, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, string schema, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync<T>(grouping, null, schema, null, options, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, string schema, DataSelectOptions options, CancellationToken cancellation) => this.SelectAsync<T>(grouping, schema, options, default(Sorting[]), cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync<T>(grouping, null, schema, null, options, sortings, cancellation);

	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, string schema, Paging paging, params Sorting[] sortings) => this.SelectAsync<T>(grouping, null, schema, paging, null, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, string schema, Paging paging, CancellationToken cancellation) => this.SelectAsync<T>(grouping, schema, paging, default(Sorting[]), cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync<T>(grouping, null, schema, paging, null, sortings, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync<T>(grouping, null, schema, paging, options, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, string schema, Paging paging, DataSelectOptions options, CancellationToken cancellation) => this.SelectAsync<T>(grouping, schema, paging, options, default(Sorting[]), cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync<T>(grouping, null, schema, paging, options, sortings, cancellation);

	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, ICondition criteria, Paging paging, params Sorting[] sortings) => this.SelectAsync<T>(grouping, criteria, string.Empty, paging, null, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, ICondition criteria, Paging paging, CancellationToken cancellation) => this.SelectAsync<T>(grouping, criteria, paging, default(Sorting[]), cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, ICondition criteria, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync<T>(grouping, criteria, string.Empty, paging, null, sortings, cancellation);

	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema, params Sorting[] sortings) => this.SelectAsync<T>(grouping, criteria, schema, null, null, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema, CancellationToken cancellation) => this.SelectAsync<T>(grouping, criteria, schema, default(Sorting[]), cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync<T>(grouping, criteria, schema, null, null, sortings, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync<T>(grouping, criteria, schema, null, options, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema, DataSelectOptions options, CancellationToken cancellation) => this.SelectAsync<T>(grouping, criteria, schema, options, default(Sorting[]), cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync<T>(grouping, criteria, schema, null, options, sortings, cancellation);

	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema, Paging paging, params Sorting[] sortings) => this.SelectAsync<T>(grouping, criteria, schema, paging, null, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema, Paging paging, CancellationToken cancellation) => this.SelectAsync<T>(grouping, criteria, schema, paging, default(Sorting[]), cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync<T>(grouping, criteria, schema, paging, null, sortings, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync<T>(grouping, criteria, schema, paging, options, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options, CancellationToken cancellation) => this.SelectAsync<T>(grouping, criteria, schema, paging, options, default(Sorting[]), cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
	{
		return DataAsyncEnumerable<T>.From(PrepareAsync(), cancellation, paging);

		async ValueTask<IAsyncEnumerable<T>> PrepareAsync()
		{
			//构建数据操作的选项对象
			if(options == null)
				options = new DataSelectOptions();

			//进行授权验证
			await this.AuthorizeAsync(DataServiceMethod.Select(), options, cancellation);

			//修整查询条件
			criteria = await this.OnValidateAsync(DataServiceMethod.Select(), criteria, options, cancellation);

			//执行查询方法
			return this.OnSelectAsync<T>(grouping, criteria, string.IsNullOrWhiteSpace(schema) ? null : this.GetSchema(schema, typeof(TModel)), paging, sortings, options, cancellation);
		}
	}
	#endregion

	#region 保护方法
	protected virtual IAsyncEnumerable<TModel> OnSelectAsync(ICondition criteria, ISchema schema, Paging paging, Sorting[] sortings, DataSelectOptions options, CancellationToken cancellation)
	{
		//如果没有指定排序设置则应用默认排序规则
		if(sortings == null || sortings.Length == 0)
			sortings = this.GetDefaultSortings();

		return this.DataAccess.SelectAsync<TModel>(this.Name, criteria, schema, paging, options, sortings, this.OnSelectingAsync, this.OnSelectedAsync, cancellation);
	}

	protected virtual IAsyncEnumerable<T> OnSelectAsync<T>(Grouping grouping, ICondition criteria, ISchema schema, Paging paging, Sorting[] sortings, DataSelectOptions options, CancellationToken cancellation)
	{
		return this.DataAccess.SelectAsync<T>(this.Name, grouping, criteria, schema, paging, options, sortings, this.OnSelectingAsync, this.OnSelectedAsync, cancellation);
	}
	#endregion
}
