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
	#region 普通查询
	public IEnumerable<T> Select<T>(DataSelectOptions options = null, params Sorting[] sortings) =>
		this.Select<T>(this.GetName<T>(), null, string.Empty, null, options, sortings, null, null);
	public IEnumerable<T> Select<T>(ICondition criteria, params Sorting[] sortings) =>
		this.Select<T>(this.GetName<T>(), criteria, string.Empty, null, null, sortings, null, null);
	public IEnumerable<T> Select<T>(ICondition criteria, DataSelectOptions options, params Sorting[] sortings) =>
		this.Select<T>(this.GetName<T>(), criteria, string.Empty, null, options, sortings, null, null);
	public IEnumerable<T> Select<T>(ICondition criteria, Paging paging, params Sorting[] sortings) =>
		this.Select<T>(this.GetName<T>(), criteria, string.Empty, paging, null, sortings, null, null);
	public IEnumerable<T> Select<T>(ICondition criteria, Paging paging, DataSelectOptions options, params Sorting[] sortings) =>
		this.Select<T>(this.GetName<T>(), criteria, string.Empty, paging, options, sortings, null, null);
	public IEnumerable<T> Select<T>(ICondition criteria, string schema, params Sorting[] sortings) =>
		this.Select<T>(this.GetName<T>(), criteria, schema, null, null, sortings, null, null);
	public IEnumerable<T> Select<T>(ICondition criteria, string schema, DataSelectOptions options, params Sorting[] sortings) =>
		this.Select<T>(this.GetName<T>(), criteria, schema, null, options, sortings, null, null);
	public IEnumerable<T> Select<T>(ICondition criteria, string schema, Paging paging, params Sorting[] sortings) =>
		this.Select<T>(this.GetName<T>(), criteria, schema, paging, null, sortings, null, null);
	public IEnumerable<T> Select<T>(ICondition criteria, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings) =>
		this.Select<T>(this.GetName<T>(), criteria, schema, paging, options, sortings, null, null);
	public IEnumerable<T> Select<T>(ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected) =>
		this.Select<T>(this.GetName<T>(), criteria, schema, paging, options, sortings, selecting, selected);

	public IEnumerable<T> Select<T>(string name, DataSelectOptions options = null, params Sorting[] sortings) =>
		this.Select<T>(name, null, string.Empty, null, options, sortings, null, null);
	public IEnumerable<T> Select<T>(string name, ICondition criteria, params Sorting[] sortings) =>
		this.Select<T>(name, criteria, string.Empty, null, null, sortings, null, null);
	public IEnumerable<T> Select<T>(string name, ICondition criteria, DataSelectOptions options, params Sorting[] sortings) =>
		this.Select<T>(name, criteria, string.Empty, null, options, sortings, null, null);
	public IEnumerable<T> Select<T>(string name, ICondition criteria, Paging paging, params Sorting[] sortings) =>
		this.Select<T>(name, criteria, string.Empty, paging, null, sortings, null, null);
	public IEnumerable<T> Select<T>(string name, ICondition criteria, Paging paging, DataSelectOptions options, params Sorting[] sortings) =>
		this.Select<T>(name, criteria, string.Empty, paging, options, sortings, null, null);
	public IEnumerable<T> Select<T>(string name, ICondition criteria, string schema, params Sorting[] sortings) =>
		this.Select<T>(name, criteria, schema, null, null, sortings, null, null);
	public IEnumerable<T> Select<T>(string name, ICondition criteria, string schema, DataSelectOptions options, params Sorting[] sortings) =>
		this.Select<T>(name, criteria, schema, null, options, sortings, null, null);
	public IEnumerable<T> Select<T>(string name, ICondition criteria, string schema, Paging paging, params Sorting[] sortings) =>
		this.Select<T>(name, criteria, schema, paging, null, sortings, null, null);
	public IEnumerable<T> Select<T>(string name, ICondition criteria, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings) =>
		this.Select<T>(name, criteria, schema, paging, options, sortings, null, null);
	public IEnumerable<T> Select<T>(string name, ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected) =>
		this.Select<T>(name, criteria, this.Schema.Parse(name, schema, typeof(T)), paging, options, sortings, selecting, selected);
	public IEnumerable<T> Select<T>(string name, ICondition criteria, ISchema schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected)
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		//创建数据访问上下文对象
		var context = this.CreateSelectContext(name, typeof(T), criteria, null, schema, paging, sortings, options);

		//执行查询方法
		return this.Select<T>(context, selecting, selected);
	}

	public IAsyncEnumerable<T> SelectAsync<T>(CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), null, string.Empty, null, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(DataSelectOptions options, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), null, string.Empty, null, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), null, string.Empty, null, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, string.Empty, null, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, string.Empty, null, null, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, DataSelectOptions options, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, string.Empty, null, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, string.Empty, null, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, Paging paging, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, string.Empty, paging, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, string.Empty, paging, null, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, Paging paging, DataSelectOptions options, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, string.Empty, paging, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, string.Empty, paging, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, schema, null, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, schema, null, null, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, DataSelectOptions options, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, schema, null, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, schema, null, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, Paging paging, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, schema, paging, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, schema, paging, null, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, Paging paging, DataSelectOptions options, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, schema, paging, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, schema, paging, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(this.GetName<T>(), criteria, schema, paging, options, sortings, selecting, selected, cancellation);

	public IAsyncEnumerable<T> SelectAsync<T>(string name, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, null, string.Empty, null, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, DataSelectOptions options, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, null, string.Empty, null, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, null, string.Empty, null, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, criteria, string.Empty, null, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, criteria, string.Empty, null, null, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, DataSelectOptions options, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, criteria, string.Empty, null, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, criteria, string.Empty, null, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, Paging paging, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, criteria, string.Empty, paging, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, criteria, string.Empty, paging, null, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, Paging paging, DataSelectOptions options, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, criteria, string.Empty, paging, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, criteria, string.Empty, paging, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, criteria, schema, null, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, criteria, schema, null, null, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, DataSelectOptions options, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, criteria, schema, null, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, criteria, schema, null, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, Paging paging, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, criteria, schema, paging, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, criteria, schema, paging, null, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, Paging paging, DataSelectOptions options, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, criteria, schema, paging, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, criteria, schema, paging, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected, CancellationToken cancellation = default) =>
		 this.SelectAsync<T>(name, criteria, this.Schema.Parse(name, schema, typeof(T)), paging, options, sortings, selecting, selected, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, ICondition criteria, ISchema schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected, CancellationToken cancellation = default)
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
	public IEnumerable<T> Select<T>(string name, Grouping grouping, params Sorting[] sortings) =>
		this.Select<T>(name, grouping, null, string.Empty, null, null, sortings, null, null);
	public IEnumerable<T> Select<T>(string name, Grouping grouping, DataSelectOptions options, params Sorting[] sortings) =>
		this.Select<T>(name, grouping, null, string.Empty, null, options, sortings, null, null);
	public IEnumerable<T> Select<T>(string name, Grouping grouping, Paging paging, DataSelectOptions options = null, params Sorting[] sortings) =>
		this.Select<T>(name, grouping, null, string.Empty, paging, options, sortings, null, null);
	public IEnumerable<T> Select<T>(string name, Grouping grouping, string schema, params Sorting[] sortings) =>
		this.Select<T>(name, grouping, null, schema, null, null, sortings, null, null);
	public IEnumerable<T> Select<T>(string name, Grouping grouping, string schema, DataSelectOptions options, params Sorting[] sortings) =>
		this.Select<T>(name, grouping, null, schema, null, options, sortings, null, null);
	public IEnumerable<T> Select<T>(string name, Grouping grouping, string schema, Paging paging, DataSelectOptions options = null, params Sorting[] sortings) =>
		this.Select<T>(name, grouping, null, schema, paging, options, sortings, null, null);
	public IEnumerable<T> Select<T>(string name, Grouping grouping, ICondition criteria, Paging paging, params Sorting[] sortings) =>
		this.Select<T>(name, grouping, criteria, (ISchema)null, paging, null, sortings, null, null);
	public IEnumerable<T> Select<T>(string name, Grouping grouping, ICondition criteria, string schema, params Sorting[] sortings) =>
		this.Select<T>(name, grouping, criteria, schema, null, null, sortings, null, null);
	public IEnumerable<T> Select<T>(string name, Grouping grouping, ICondition criteria, string schema, DataSelectOptions options, params Sorting[] sortings) =>
		this.Select<T>(name, grouping, criteria, schema, null, options, sortings, null, null);
	public IEnumerable<T> Select<T>(string name, Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options = null, params Sorting[] sortings) =>
		this.Select<T>(name, grouping, criteria, schema, paging, options, sortings, null, null);
	public IEnumerable<T> Select<T>(string name, Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected) =>
		this.Select<T>(name, grouping, criteria, string.IsNullOrWhiteSpace(schema) ? null : this.Schema.Parse(name, schema, typeof(T)), paging, options, sortings, selecting, selected);
	public IEnumerable<T> Select<T>(string name, Grouping grouping, ICondition criteria, ISchema schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected)
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		//创建数据访问上下文对象
		var context = this.CreateSelectContext(name, typeof(T), criteria, grouping, schema, paging, sortings, options);

		//执行查询方法
		return this.Select<T>(context, selecting, selected);
	}

	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, null, string.Empty, null, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, null, string.Empty, null, null, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, DataSelectOptions options, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, null, string.Empty, null, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, null, string.Empty, null, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, Paging paging, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, null, string.Empty, paging, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, Paging paging, DataSelectOptions options, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, null, string.Empty, paging, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, null, string.Empty, paging, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, null, schema, null, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, null, schema, null, null, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, DataSelectOptions options, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, null, schema, null, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, null, schema, null, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, Paging paging, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, null, schema, paging, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, Paging paging, DataSelectOptions options, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, null, schema, paging, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		  this.SelectAsync<T>(name, grouping, null, schema, paging, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, criteria, (ISchema)null, null, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, Paging paging, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, criteria, (ISchema)null, paging, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, criteria, (ISchema)null, paging, null, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, criteria, schema, null, null, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, criteria, schema, null, null, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, DataSelectOptions options, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, criteria, schema, null, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, criteria, schema, null, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, criteria, schema, paging, options, null, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, criteria, schema, paging, options, sortings, null, null, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected, CancellationToken cancellation = default) =>
		this.SelectAsync<T>(name, grouping, criteria, string.IsNullOrWhiteSpace(schema) ? null : this.Schema.Parse(name, schema, typeof(T)), paging, options, sortings, selecting, selected, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(string name, Grouping grouping, ICondition criteria, ISchema schema, Paging paging, DataSelectOptions options, Sorting[] sortings, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected, CancellationToken cancellation = default)
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
	private IEnumerable<T> Select<T>(DataSelectContextBase context, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected)
	{
		//确实是否已处置
		this.EnsureDisposed();

		//处理数据访问操作前的回调
		if(selecting != null && selecting(context))
			return context.Result as IEnumerable<T>;

		//激发“Selecting”事件，如果被中断则返回
		if(this.OnSelecting(context))
			return context.Result as IEnumerable<T>;

		//调用数据访问过滤器前事件
		this.OnFiltering(context);

		//执行数据查询操作
		this.OnSelect(context);

		//调用数据访问过滤器后事件
		this.OnFiltered(context);

		//激发“Selected”事件
		this.OnSelected(context);

		//处理数据访问操作后的回调
		if(selected != null)
			selected(context);

		var result = ToEnumerable<T>(context.Result);

		//处置上下文资源
		context.Dispose();

		//返回最终的结果
		return result;
	}

	private IAsyncEnumerable<T> SelectAsync<T>(DataSelectContextBase context, Func<DataSelectContextBase, bool> selecting, Action<DataSelectContextBase> selected, CancellationToken cancellation)
	{
		//确实是否已处置
		this.EnsureDisposed();

		//处理数据访问操作前的回调
		if(selecting != null && selecting(context))
			return ToAsyncEnumerable<T>(context.Result, cancellation);

		//激发“Selecting”事件，如果被中断则返回
		if(this.OnSelecting(context))
			return ToAsyncEnumerable<T>(context.Result, cancellation);

		//调用数据访问过滤器前事件
		this.OnFiltering(context);

		//执行数据查询操作
		this.OnSelectAsync(context, cancellation).AsTask().GetAwaiter().GetResult();

		//调用数据访问过滤器后事件
		this.OnFiltered(context);

		//激发“Selected”事件
		this.OnSelected(context);

		//处理数据访问操作后的回调
		selected?.Invoke(context);

		var result = ToAsyncEnumerable<T>(context.Result, cancellation);

		//处置上下文资源
		context.Dispose();

		return result;
	}

	protected abstract void OnSelect(DataSelectContextBase context);
	protected abstract ValueTask OnSelectAsync(DataSelectContextBase context, CancellationToken cancellation);
	#endregion
}
