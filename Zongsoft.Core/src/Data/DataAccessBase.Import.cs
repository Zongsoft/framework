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
	public int Import<T>(IEnumerable<T> data, IEnumerable<string> members, DataImportOptions options = null) =>
		data == null ? 0 : this.Import(this.GetName<T>(), data, members, options);
	int IDataAccess.Import(string name, IEnumerable data, IEnumerable<string> members, DataImportOptions options) =>
		data == null ? 0 : this.Import(name, data, members, options);
	public int Import(string name, IEnumerable data, IEnumerable<string> members, DataImportOptions options, Func<DataImportContextBase, bool> importing = null, Action<DataImportContextBase> imported = null)
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		if(data == null)
			return 0;

		//创建数据访问上下文对象
		var context = this.CreateImportContext(name, data, members, options);

		//处理数据访问操作前的回调
		if(importing != null && importing(context))
			return context.Count;

		//激发“Importing”事件，如果被中断则返回
		if(this.OnImporting(context))
			return context.Count;

		//调用数据访问过滤器前事件
		this.OnFiltering(context);

		//执行数据导入操作
		this.OnImport(context);

		//调用数据访问过滤器后事件
		this.OnFiltered(context);

		//激发“Imported”事件
		this.OnImported(context);

		//处理数据访问操作后的回调
		imported?.Invoke(context);

		var result = context.Count;

		//处置上下文资源
		context.Dispose();

		//返回最终的结果
		return result;
	}

	public ValueTask<int> ImportAsync<T>(IEnumerable<T> data, IEnumerable<string> members, DataImportOptions options, CancellationToken cancellation = default) =>
		data == null ? ValueTask.FromResult(0) : this.ImportAsync(this.GetName<T>(), data, members, options, cancellation);
	ValueTask<int> IDataAccess.ImportAsync(string name, IEnumerable data, IEnumerable<string> members, DataImportOptions options, CancellationToken cancellation) =>
		data == null ? ValueTask.FromResult(0) : this.ImportAsync(name, data, members, options, cancellation, null, null);
	public async ValueTask<int> ImportAsync(string name, IEnumerable data, IEnumerable<string> members, DataImportOptions options, CancellationToken cancellation, Func<DataImportContextBase, bool> importing = null, Action<DataImportContextBase> imported = null)
	{
		//确实是否已处置
		this.EnsureDisposed();

		if(string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		if(data == null)
			return 0;

		//创建数据访问上下文对象
		var context = this.CreateImportContext(name, data, members, options);

		//处理数据访问操作前的回调
		if(importing != null && importing(context))
			return context.Count;

		//激发“Importing”事件，如果被中断则返回
		if(this.OnImporting(context))
			return context.Count;

		//调用数据访问过滤器前事件
		this.OnFiltering(context);

		//执行数据导入操作
		await this.OnImportAsync(context, cancellation);

		//调用数据访问过滤器后事件
		this.OnFiltered(context);

		//激发“Imported”事件
		this.OnImported(context);

		//处理数据访问操作后的回调
		imported?.Invoke(context);

		var result = context.Count;

		//处置上下文资源
		context.Dispose();

		//返回最终的结果
		return result;
	}

	protected abstract void OnImport(DataImportContextBase context);
	protected abstract ValueTask OnImportAsync(DataImportContextBase context, CancellationToken cancellation = default);
}
