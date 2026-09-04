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
		var ownsContext = true;

		try
		{
			//在执行任何查询准备操作前响应取消
			cancellation.ThrowIfCancellationRequested();

			//处理数据访问操作前的回调
			if(selecting != null && selecting(context))
				return ToAsyncEnumerable<T>(context.Result, cancellation);

			//激发“Selecting”事件，如果被中断则返回
			if(this.OnSelecting(context))
				return ToAsyncEnumerable<T>(context.Result, cancellation);

			//调用数据访问过滤器前事件
			this.OnFiltering(context);

			//启动异步查询准备并将上下文的所有权移交给异步结果集
			var result = new AsyncSelectResult<T>(this, context, this.OnSelectAsync(context, cancellation), selected, cancellation);

			//将上下文的所有权移交给异步结果集
			ownsContext = false;
			return result;
		}
		finally
		{
			if(ownsContext)
				context.Dispose();
		}
	}

	protected abstract void OnSelect(DataSelectContextBase context);
	protected abstract ValueTask OnSelectAsync(DataSelectContextBase context, CancellationToken cancellation);
	#endregion

	#region 异步结果
	private sealed class AsyncSelectResult<T> : IAsyncEnumerable<T>, IPageable
	{
		#region 事件声明
		public event EventHandler<PagingEventArgs> Paginated;
		#endregion

		#region 成员字段
		private int _prepared;
		private IPageable _pageable;
		private readonly bool _suppressed;
		private readonly CancellationToken _cancellation;
		private readonly Task<IAsyncEnumerable<T>> _preparation;
		#endregion

		#region 构造函数
		public AsyncSelectResult(DataAccessBase accessor, DataSelectContextBase context, ValueTask operation, Action<DataSelectContextBase> selected, CancellationToken cancellation)
		{
			_cancellation = cancellation;
			_suppressed = context.Paging == null || !context.Paging.IsPaged();
			_preparation = this.PrepareAsync(accessor, context, operation, selected);

			//异步结果集可能永远不会被枚举，仍需观察准备任务的异常
			_ = _preparation.ContinueWith(
				static task => _ = task.Exception,
				CancellationToken.None,
				TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnFaulted,
				TaskScheduler.Default);
		}
		#endregion

		#region 公共属性
		public bool Suppressed
		{
			get
			{
				var pageable = Volatile.Read(ref _pageable);
				return pageable?.Suppressed ?? (Volatile.Read(ref _prepared) != 0 || _suppressed);
			}
		}
		#endregion

		#region 公共方法
		public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellation = default) => this.EnumerateAsync(cancellation).GetAsyncEnumerator();
		#endregion

		#region 私有方法
		private void OnPaginated(object sender, PagingEventArgs args) => this.Paginated?.Invoke(this, args);
		private async Task<IAsyncEnumerable<T>> PrepareAsync(DataAccessBase accessor, DataSelectContextBase context, ValueTask operation, Action<DataSelectContextBase> selected)
		{
			try
			{
				//执行数据查询操作
				await operation.ConfigureAwait(false);

				//调用数据访问过滤器后事件
				accessor.OnFiltered(context);

				//激发“Selected”事件
				accessor.OnSelected(context);

				//处理数据访问操作后的回调
				selected?.Invoke(context);

				var result = ToAsyncEnumerable<T>(context.Result, _cancellation);

				if(result is IPageable pageable)
				{
					pageable.Paginated += this.OnPaginated;
					Volatile.Write(ref _pageable, pageable);
				}

				Volatile.Write(ref _prepared, 1);
				return result;
			}
			finally
			{
				context.Dispose();
			}
		}

		private async IAsyncEnumerable<T> EnumerateAsync([EnumeratorCancellation] CancellationToken cancellation)
		{
			var token = GetCancellation(_cancellation, cancellation, out var source);

			using(source)
			{
				token.ThrowIfCancellationRequested();
				var result = await _preparation.WaitAsync(token).ConfigureAwait(false);

				await foreach(var item in result.WithCancellation(token).ConfigureAwait(false))
					yield return item;
			}

			static CancellationToken GetCancellation(CancellationToken first, CancellationToken second, out CancellationTokenSource source)
			{
				source = null;

				if(!first.CanBeCanceled)
					return second;
				if(!second.CanBeCanceled || first == second)
					return first;

				return (source = CancellationTokenSource.CreateLinkedTokenSource(first, second)).Token;
			}
		}
		#endregion
	}
	#endregion
}
