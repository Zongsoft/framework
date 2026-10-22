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
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Data;

partial class DataServiceBase<TModel>
{
	#region 公共方法
	public ValueTask<int> UpsertAsync(object data, DataUpsertOptions options = null, CancellationToken cancellation = default) => this.UpsertAsync(data, string.Empty, options, cancellation);
	public async ValueTask<int> UpsertAsync(object data, string schema, DataUpsertOptions options = null, CancellationToken cancellation = default)
	{
		//确认是否可以执行该操作
		this.EnsureUpsert(options);

		if(data == null)
			return 0;

		//构建数据操作的选项对象
		if(options == null)
			options = new DataUpsertOptions();

		//进行授权验证
		await this.AuthorizeAsync(DataServiceMethod.Upsert(), options, cancellation);

		//解析数据模式表达式
		var schematic = this.GetSchema(schema, data.GetType());

		//将当前增改数据对象转换成数据字典
		var dictionary = DataDictionary.GetDictionary<TModel>(data);

		//验证待增改的数据
		await this.OnValidateAsync(DataServiceMethod.Upsert(), schematic, dictionary, options, cancellation);

		return await this.OnUpsertAsync(dictionary, schematic, options, cancellation);
	}
	#endregion

	#region 保护方法
	protected virtual ValueTask<int> OnUpsertAsync(IDataDictionary<TModel> data, ISchema schema, DataUpsertOptions options, CancellationToken cancellation)
	{
		if(data == null || data.Data == null || !data.HasChanges())
			return ValueTask.FromResult(0);

		//执行数据引擎的增改操作
		return this.DataAccess.UpsertAsync(this.Name, data, schema, options, this.OnUpsertingAsync, this.OnUpsertedAsync, cancellation);
	}
	#endregion

	#region 公共方法
	public ValueTask<int> UpsertManyAsync(IEnumerable items, DataUpsertOptions options = null, CancellationToken cancellation = default) => this.UpsertManyAsync(items, string.Empty, options, cancellation);
	public async ValueTask<int> UpsertManyAsync(IEnumerable items, string schema, DataUpsertOptions options = null, CancellationToken cancellation = default)
	{
		//确认是否可以执行该操作
		this.EnsureUpsert(options);

		if(items == null)
			return 0;

		//构建数据操作的选项对象
		if(options == null)
			options = new DataUpsertOptions();

		//进行授权验证
		await this.AuthorizeAsync(DataServiceMethod.UpsertMany(), options, cancellation);

		//解析数据模式表达式
		var schematic = this.GetSchema(schema, Common.TypeExtension.GetElementType(items.GetType()));

		//将当前增改数据集合对象转换成数据字典集合
		var dictionaries = DataDictionary.GetDictionariesAsync<TModel>(items, async (dictionary, token) =>
		{
			//验证待增改的数据
			await this.OnValidateAsync(DataServiceMethod.UpsertMany(), schematic, dictionary, options, token);
		}, cancellation);

		return await this.OnUpsertManyAsync(dictionaries, schematic, options, cancellation);
	}

	public ValueTask<int> UpsertManyAsync(string key, IEnumerable items, DataUpsertOptions options = null, CancellationToken cancellation = default) => this.UpsertManyAsync(key, items, null, options, cancellation);
	public async ValueTask<int> UpsertManyAsync(string key, IEnumerable items, string schema, DataUpsertOptions options = null, CancellationToken cancellation = default)
	{
		//确认是否可以执行该操作
		this.EnsureUpsert(options);

		if(items == null)
			return 0;

		//构建数据操作的选项对象
		if(options == null)
			options = new DataUpsertOptions();

		//进行授权验证
		await this.AuthorizeAsync(DataServiceMethod.UpsertMany(), options, cancellation);

		//解析数据模式表达式
		var schematic = this.GetSchema(schema, Common.TypeExtension.GetElementType(items.GetType()));

		//将当前增改数据集合对象转换成数据字典集合
		var dictionaries = DataDictionary.GetDictionariesAsync<TModel>(items, async (dictionary, token) =>
		{
			//处理数据模型
			this.OnModel(key, dictionary, options);

			//验证待增改的数据
			await this.OnValidateAsync(DataServiceMethod.UpsertMany(), schematic, dictionary, options, token);
		}, cancellation);

		return await this.OnUpsertManyAsync(dictionaries, schematic, options, cancellation);
	}
	#endregion


	#region 便利取消
	public ValueTask<int> UpsertAsync(object data, CancellationToken cancellation) => this.UpsertAsync(data, default(DataUpsertOptions), cancellation);
	public ValueTask<int> UpsertAsync(object data, string schema, CancellationToken cancellation) => this.UpsertAsync(data, schema, default(DataUpsertOptions), cancellation);
	public ValueTask<int> UpsertManyAsync(IEnumerable items, CancellationToken cancellation) => this.UpsertManyAsync(items, default(DataUpsertOptions), cancellation);
	public ValueTask<int> UpsertManyAsync(IEnumerable items, string schema, CancellationToken cancellation) => this.UpsertManyAsync(items, schema, default(DataUpsertOptions), cancellation);
	public ValueTask<int> UpsertManyAsync(string key, IEnumerable items, CancellationToken cancellation) => this.UpsertManyAsync(key, items, default(DataUpsertOptions), cancellation);
	public ValueTask<int> UpsertManyAsync(string key, IEnumerable items, string schema, CancellationToken cancellation) => this.UpsertManyAsync(key, items, schema, default(DataUpsertOptions), cancellation);
	#endregion

	#region 异步数据源
	public ValueTask<int> UpsertManyAsync<T>(IAsyncEnumerable<T> items, DataUpsertOptions options = null, CancellationToken cancellation = default) => this.UpsertManyAsync<T>(items, string.Empty, options, cancellation);
	public async ValueTask<int> UpsertManyAsync<T>(IAsyncEnumerable<T> items, string schema, DataUpsertOptions options = null, CancellationToken cancellation = default)
	{
		//确认是否可以执行该操作
		this.EnsureUpsert(options);

		if(items == null)
			return 0;

		//构建数据操作的选项对象
		if(options == null)
			options = new DataUpsertOptions();

		//进行授权验证
		await this.AuthorizeAsync(DataServiceMethod.UpsertMany(), options, cancellation);

		//解析数据模式表达式
		var schematic = this.GetSchema(schema, typeof(T));

		//将当前增改数据集合对象转换成数据字典集合
		var dictionaries = DataDictionary.GetDictionariesAsync<TModel, T>(items, async (dictionary, token) =>
		{
			//验证待增改的数据
			await this.OnValidateAsync(DataServiceMethod.UpsertMany(), schematic, dictionary, options, token);
		}, cancellation);

		return await this.OnUpsertManyAsync(dictionaries, schematic, options, cancellation);
	}
	public ValueTask<int> UpsertManyAsync<T>(string key, IAsyncEnumerable<T> items, DataUpsertOptions options = null, CancellationToken cancellation = default) => this.UpsertManyAsync<T>(key, items, null, options, cancellation);
	public async ValueTask<int> UpsertManyAsync<T>(string key, IAsyncEnumerable<T> items, string schema, DataUpsertOptions options = null, CancellationToken cancellation = default)
	{
		//确认是否可以执行该操作
		this.EnsureUpsert(options);

		if(items == null)
			return 0;

		//构建数据操作的选项对象
		if(options == null)
			options = new DataUpsertOptions();

		//进行授权验证
		await this.AuthorizeAsync(DataServiceMethod.UpsertMany(), options, cancellation);

		//解析数据模式表达式
		var schematic = this.GetSchema(schema, typeof(T));

		//将当前增改数据集合对象转换成数据字典集合
		var dictionaries = DataDictionary.GetDictionariesAsync<TModel, T>(items, async (dictionary, token) =>
		{
			//处理数据模型
			this.OnModel(key, dictionary, options);

			//验证待增改的数据
			await this.OnValidateAsync(DataServiceMethod.UpsertMany(), schematic, dictionary, options, token);
		}, cancellation);

		return await this.OnUpsertManyAsync(dictionaries, schematic, options, cancellation);
	}
	protected virtual ValueTask<int> OnUpsertManyAsync(IAsyncEnumerable<IDataDictionary<TModel>> items, ISchema schema, DataUpsertOptions options, CancellationToken cancellation)
	{
		if(items == null)
			return ValueTask.FromResult(0);

		//执行数据引擎的增改操作
		return this.DataAccess.UpsertManyAsync(this.Name, items, schema, options, this.OnUpsertingAsync, this.OnUpsertedAsync, cancellation);
	}
	public ValueTask<int> UpsertManyAsync<T>(IAsyncEnumerable<T> items, CancellationToken cancellation) => this.UpsertManyAsync<T>(items, default(DataUpsertOptions), cancellation);
	public ValueTask<int> UpsertManyAsync<T>(IAsyncEnumerable<T> items, string schema, CancellationToken cancellation) => this.UpsertManyAsync<T>(items, schema, default(DataUpsertOptions), cancellation);
	public ValueTask<int> UpsertManyAsync<T>(string key, IAsyncEnumerable<T> items, CancellationToken cancellation) => this.UpsertManyAsync<T>(key, items, default(DataUpsertOptions), cancellation);
	public ValueTask<int> UpsertManyAsync<T>(string key, IAsyncEnumerable<T> items, string schema, CancellationToken cancellation) => this.UpsertManyAsync<T>(key, items, schema, default(DataUpsertOptions), cancellation);
	#endregion
}
