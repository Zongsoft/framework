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

partial class DataServiceBase<TModel>
{
	#region 公共方法
	public ValueTask<int> InsertAsync(object data, DataInsertOptions options = null, CancellationToken cancellation = default) => this.InsertAsync(data, string.Empty, options, cancellation);
	public async ValueTask<int> InsertAsync(object data, string schema, DataInsertOptions options = null, CancellationToken cancellation = default)
	{
		//确认是否可以执行该操作
		this.EnsureInsert(options);

		if(data == null)
			return 0;

		//构建数据操作的选项对象
		if(options == null)
			options = new DataInsertOptions();

		//进行授权验证
		await this.AuthorizeAsync(DataServiceMethod.Insert(), options, cancellation);

		//解析数据模式表达式
		var schematic = this.GetSchema(schema, data.GetType());

		//将当前插入数据对象转换成数据字典
		var dictionary = DataDictionary.GetDictionary<TModel>(data);

		//验证待新增的数据
		await this.OnValidateAsync(DataServiceMethod.Insert(), schematic, dictionary, options, cancellation);

		return await this.OnInsertAsync(dictionary, schematic, options, cancellation);
	}
	#endregion

	#region 保护方法
	protected virtual ValueTask<int> OnInsertAsync(IDataDictionary<TModel> data, ISchema schema, DataInsertOptions options, CancellationToken cancellation)
	{
		if(data == null || data.Data == null || !data.HasChanges())
			return ValueTask.FromResult(0);

		//执行数据引擎的插入操作
		return this.DataAccess.InsertAsync(this.Name, data, schema, options, this.OnInsertingAsync, this.OnInsertedAsync, cancellation);
	}
	#endregion

	#region 公共方法
	public ValueTask<int> InsertManyAsync(IEnumerable items, DataInsertOptions options = null, CancellationToken cancellation = default) => this.InsertManyAsync(items, string.Empty, options, cancellation);
	public async ValueTask<int> InsertManyAsync(IEnumerable items, string schema, DataInsertOptions options = null, CancellationToken cancellation = default)
	{
		//确认是否可以执行该操作
		this.EnsureInsert(options);

		if(items == null)
			return 0;

		//构建数据操作的选项对象
		if(options == null)
			options = new DataInsertOptions();

		//进行授权验证
		await this.AuthorizeAsync(DataServiceMethod.InsertMany(), options, cancellation);

		//解析数据模式表达式
		var schematic = this.GetSchema(schema, Common.TypeExtension.GetElementType(items.GetType()));

		//将当前新增数据集合对象转换成数据字典集合
		var dictionaries = DataDictionary.GetDictionariesAsync<TModel>(items, async (dictionary, token) =>
		{
			//验证待新增的数据
			await this.OnValidateAsync(DataServiceMethod.InsertMany(), schematic, dictionary, options, token);
		}, cancellation);

		return await this.OnInsertManyAsync(dictionaries, schematic, options, cancellation);
	}

	public ValueTask<int> InsertManyAsync(string key, IEnumerable items, DataInsertOptions options = null, CancellationToken cancellation = default) => this.InsertManyAsync(key, items, null, options, cancellation);
	public async ValueTask<int> InsertManyAsync(string key, IEnumerable items, string schema, DataInsertOptions options = null, CancellationToken cancellation = default)
	{
		//确认是否可以执行该操作
		this.EnsureInsert(options);

		if(items == null)
			return 0;

		//构建数据操作的选项对象
		if(options == null)
			options = new DataInsertOptions();

		//进行授权验证
		await this.AuthorizeAsync(DataServiceMethod.InsertMany(), options, cancellation);

		//解析数据模式表达式
		var schematic = this.GetSchema(schema, Common.TypeExtension.GetElementType(items.GetType()));

		//将当前新增数据集合对象转换成数据字典集合
		var dictionaries = DataDictionary.GetDictionariesAsync<TModel>(items, async (dictionary, token) =>
		{
			//处理数据模型
			this.OnModel(key, dictionary, options);

			//验证待新增的数据
			await this.OnValidateAsync(DataServiceMethod.InsertMany(), schematic, dictionary, options, token);
		}, cancellation);

		return await this.OnInsertManyAsync(dictionaries, schematic, options, cancellation);
	}
	#endregion


	#region 便利取消
	public ValueTask<int> InsertAsync(object data, CancellationToken cancellation) => this.InsertAsync(data, default(DataInsertOptions), cancellation);
	public ValueTask<int> InsertAsync(object data, string schema, CancellationToken cancellation) => this.InsertAsync(data, schema, default(DataInsertOptions), cancellation);
	public ValueTask<int> InsertManyAsync(IEnumerable items, CancellationToken cancellation) => this.InsertManyAsync(items, default(DataInsertOptions), cancellation);
	public ValueTask<int> InsertManyAsync(IEnumerable items, string schema, CancellationToken cancellation) => this.InsertManyAsync(items, schema, default(DataInsertOptions), cancellation);
	public ValueTask<int> InsertManyAsync(string key, IEnumerable items, CancellationToken cancellation) => this.InsertManyAsync(key, items, default(DataInsertOptions), cancellation);
	public ValueTask<int> InsertManyAsync(string key, IEnumerable items, string schema, CancellationToken cancellation) => this.InsertManyAsync(key, items, schema, default(DataInsertOptions), cancellation);
	#endregion

	#region 异步数据源
	public ValueTask<int> InsertManyAsync<T>(IAsyncEnumerable<T> items, DataInsertOptions options = null, CancellationToken cancellation = default) => this.InsertManyAsync<T>(items, string.Empty, options, cancellation);
	public async ValueTask<int> InsertManyAsync<T>(IAsyncEnumerable<T> items, string schema, DataInsertOptions options = null, CancellationToken cancellation = default)
	{
		//确认是否可以执行该操作
		this.EnsureInsert(options);

		if(items == null)
			return 0;

		//构建数据操作的选项对象
		if(options == null)
			options = new DataInsertOptions();

		//进行授权验证
		await this.AuthorizeAsync(DataServiceMethod.InsertMany(), options, cancellation);

		//解析数据模式表达式
		var schematic = this.GetSchema(schema, typeof(T));

		//将当前新增数据集合对象转换成数据字典集合
		var dictionaries = DataDictionary.GetDictionariesAsync<TModel, T>(items, async (dictionary, token) =>
		{
			//验证待新增的数据
			await this.OnValidateAsync(DataServiceMethod.InsertMany(), schematic, dictionary, options, token);
		}, cancellation);

		return await this.OnInsertManyAsync(dictionaries, schematic, options, cancellation);
	}
	public ValueTask<int> InsertManyAsync<T>(string key, IAsyncEnumerable<T> items, DataInsertOptions options = null, CancellationToken cancellation = default) => this.InsertManyAsync<T>(key, items, null, options, cancellation);
	public async ValueTask<int> InsertManyAsync<T>(string key, IAsyncEnumerable<T> items, string schema, DataInsertOptions options = null, CancellationToken cancellation = default)
	{
		//确认是否可以执行该操作
		this.EnsureInsert(options);

		if(items == null)
			return 0;

		//构建数据操作的选项对象
		if(options == null)
			options = new DataInsertOptions();

		//进行授权验证
		await this.AuthorizeAsync(DataServiceMethod.InsertMany(), options, cancellation);

		//解析数据模式表达式
		var schematic = this.GetSchema(schema, typeof(T));

		//将当前新增数据集合对象转换成数据字典集合
		var dictionaries = DataDictionary.GetDictionariesAsync<TModel, T>(items, async (dictionary, token) =>
		{
			//处理数据模型
			this.OnModel(key, dictionary, options);

			//验证待新增的数据
			await this.OnValidateAsync(DataServiceMethod.InsertMany(), schematic, dictionary, options, token);
		}, cancellation);

		return await this.OnInsertManyAsync(dictionaries, schematic, options, cancellation);
	}
	protected virtual ValueTask<int> OnInsertManyAsync(IAsyncEnumerable<IDataDictionary<TModel>> items, ISchema schema, DataInsertOptions options, CancellationToken cancellation = default)
	{
		if(items == null)
			return ValueTask.FromResult(0);

		//执行数据引擎的插入操作
		return this.DataAccess.InsertManyAsync(this.Name, items, schema, options, this.OnInsertingAsync, this.OnInsertedAsync, cancellation);
	}
	public ValueTask<int> InsertManyAsync<T>(IAsyncEnumerable<T> items, CancellationToken cancellation) => this.InsertManyAsync<T>(items, default(DataInsertOptions), cancellation);
	public ValueTask<int> InsertManyAsync<T>(IAsyncEnumerable<T> items, string schema, CancellationToken cancellation) => this.InsertManyAsync<T>(items, schema, default(DataInsertOptions), cancellation);
	public ValueTask<int> InsertManyAsync<T>(string key, IAsyncEnumerable<T> items, CancellationToken cancellation) => this.InsertManyAsync<T>(key, items, default(DataInsertOptions), cancellation);
	public ValueTask<int> InsertManyAsync<T>(string key, IAsyncEnumerable<T> items, string schema, CancellationToken cancellation) => this.InsertManyAsync<T>(key, items, schema, default(DataInsertOptions), cancellation);
	#endregion
}
