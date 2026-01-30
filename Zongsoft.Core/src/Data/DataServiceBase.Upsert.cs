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
	public int Upsert(object data, DataUpsertOptions options = null) => this.Upsert(data, string.Empty, options);
	public int Upsert(object data, string schema, DataUpsertOptions options = null)
	{
		//确认是否可以执行该操作
		this.EnsureUpsert(options);

		if(data == null)
			return 0;

		//构建数据操作的选项对象
		if(options == null)
			options = new DataUpsertOptions();

		//进行授权验证
		this.Authorize(DataServiceMethod.Upsert(), options);

		//解析数据模式表达式
		var schematic = this.GetSchema(schema, data.GetType());

		//将当前增改数据对象转换成数据字典
		var dictionary = DataDictionary.GetDictionary<TModel>(data);

		//验证待增改的数据
		this.OnValidate(DataServiceMethod.Upsert(), schematic, dictionary, options);

		return this.OnUpsert(dictionary, schematic, options);
	}

	protected virtual int OnUpsert(IDataDictionary<TModel> data, ISchema schema, DataUpsertOptions options)
	{
		if(data == null || data.Data == null || !data.HasChanges())
			return 0;

		//执行数据引擎的增改操作
		return this.DataAccess.Upsert(this.Name, data, schema, options, ctx => this.OnUpserting(ctx), ctx => this.OnUpserted(ctx));
	}

	public int UpsertMany(IEnumerable items, DataUpsertOptions options = null) => this.UpsertMany(items, string.Empty, options);
	public int UpsertMany(IEnumerable items, string schema, DataUpsertOptions options = null)
	{
		//确认是否可以执行该操作
		this.EnsureUpsert(options);

		if(items == null)
			return 0;

		//构建数据操作的选项对象
		if(options == null)
			options = new DataUpsertOptions();

		//进行授权验证
		this.Authorize(DataServiceMethod.UpsertMany(), options);

		//解析数据模式表达式
		var schematic = this.GetSchema(schema, Common.TypeExtension.GetElementType(items.GetType()));

		//将当前增改数据集合对象转换成数据字典集合
		var dictionaries = DataDictionary.GetDictionaries<TModel>(items, dictionary =>
		{
			//验证待增改的数据
			this.OnValidate(DataServiceMethod.UpsertMany(), schematic, dictionary, options);
		});

		return this.OnUpsertMany(dictionaries, schematic, options);
	}

	public int UpsertMany(string key, IEnumerable items, DataUpsertOptions options = null) => this.UpsertMany(key, items, null, options);
	public int UpsertMany(string key, IEnumerable items, string schema, DataUpsertOptions options = null)
	{
		//确认是否可以执行该操作
		this.EnsureUpsert(options);

		if(items == null)
			return 0;

		//构建数据操作的选项对象
		if(options == null)
			options = new DataUpsertOptions();

		//进行授权验证
		this.Authorize(DataServiceMethod.UpsertMany(), options);

		//解析数据模式表达式
		var schematic = this.GetSchema(schema, Common.TypeExtension.GetElementType(items.GetType()));

		//将当前增改数据集合对象转换成数据字典集合
		var dictionaries = DataDictionary.GetDictionaries<TModel>(items, dictionary =>
		{
			//处理数据模型
			this.OnModel(key, dictionary, options);

			//验证待增改的数据
			this.OnValidate(DataServiceMethod.UpsertMany(), schematic, dictionary, options);
		});

		return this.OnUpsertMany(dictionaries, schematic, options);
	}

	protected virtual int OnUpsertMany(IEnumerable<IDataDictionary<TModel>> items, ISchema schema, DataUpsertOptions options)
	{
		if(items == null)
			return 0;

		//执行数据引擎的增改操作
		return this.DataAccess.UpsertMany(this.Name, items, schema, options, ctx => this.OnUpserting(ctx), ctx => this.OnUpserted(ctx));
	}

	public ValueTask<int> UpsertAsync(object data, DataUpsertOptions options = null, CancellationToken cancellation = default) => this.UpsertAsync(data, string.Empty, options, cancellation);
	public ValueTask<int> UpsertAsync(object data, string schema, DataUpsertOptions options = null, CancellationToken cancellation = default)
	{
		//确认是否可以执行该操作
		this.EnsureUpsert(options);

		if(data == null)
			return ValueTask.FromResult(0);

		//构建数据操作的选项对象
		if(options == null)
			options = new DataUpsertOptions();

		//进行授权验证
		this.Authorize(DataServiceMethod.Upsert(), options);

		//解析数据模式表达式
		var schematic = this.GetSchema(schema, data.GetType());

		//将当前增改数据对象转换成数据字典
		var dictionary = DataDictionary.GetDictionary<TModel>(data);

		//验证待增改的数据
		this.OnValidate(DataServiceMethod.Upsert(), schematic, dictionary, options);

		return this.OnUpsertAsync(dictionary, schematic, options, cancellation);
	}

	protected virtual ValueTask<int> OnUpsertAsync(IDataDictionary<TModel> data, ISchema schema, DataUpsertOptions options, CancellationToken cancellation)
	{
		if(data == null || data.Data == null || !data.HasChanges())
			return ValueTask.FromResult(0);

		//执行数据引擎的增改操作
		return this.DataAccess.UpsertAsync(this.Name, data, schema, options, ctx => this.OnUpserting(ctx), ctx => this.OnUpserted(ctx), cancellation);
	}

	public ValueTask<int> UpsertManyAsync(IEnumerable items, DataUpsertOptions options = null, CancellationToken cancellation = default) => this.UpsertManyAsync(items, string.Empty, options, cancellation);
	public ValueTask<int> UpsertManyAsync(IEnumerable items, string schema, DataUpsertOptions options = null, CancellationToken cancellation = default)
	{
		//确认是否可以执行该操作
		this.EnsureUpsert(options);

		if(items == null)
			return ValueTask.FromResult(0);

		//构建数据操作的选项对象
		if(options == null)
			options = new DataUpsertOptions();

		//进行授权验证
		this.Authorize(DataServiceMethod.UpsertMany(), options);

		//解析数据模式表达式
		var schematic = this.GetSchema(schema, Common.TypeExtension.GetElementType(items.GetType()));

		//将当前增改数据集合对象转换成数据字典集合
		var dictionaries = DataDictionary.GetDictionaries<TModel>(items, dictionary =>
		{
			//验证待增改的数据
			this.OnValidate(DataServiceMethod.UpsertMany(), schematic, dictionary, options);
		});

		return this.OnUpsertManyAsync(dictionaries, schematic, options, cancellation);
	}

	public ValueTask<int> UpsertManyAsync(string key, IEnumerable items, DataUpsertOptions options = null, CancellationToken cancellation = default) => this.UpsertManyAsync(key, items, null, options, cancellation);
	public ValueTask<int> UpsertManyAsync(string key, IEnumerable items, string schema, DataUpsertOptions options = null, CancellationToken cancellation = default)
	{
		//确认是否可以执行该操作
		this.EnsureUpsert(options);

		if(items == null)
			return ValueTask.FromResult(0);

		//构建数据操作的选项对象
		if(options == null)
			options = new DataUpsertOptions();

		//进行授权验证
		this.Authorize(DataServiceMethod.UpsertMany(), options);

		//解析数据模式表达式
		var schematic = this.GetSchema(schema, Common.TypeExtension.GetElementType(items.GetType()));

		//将当前增改数据集合对象转换成数据字典集合
		var dictionaries = DataDictionary.GetDictionaries<TModel>(items, dictionary =>
		{
			//处理数据模型
			this.OnModel(key, dictionary, options);

			//验证待增改的数据
			this.OnValidate(DataServiceMethod.UpsertMany(), schematic, dictionary, options);
		});

		return this.OnUpsertManyAsync(dictionaries, schematic, options, cancellation);
	}

	protected virtual ValueTask<int> OnUpsertManyAsync(IEnumerable<IDataDictionary<TModel>> items, ISchema schema, DataUpsertOptions options, CancellationToken cancellation)
	{
		if(items == null)
			return ValueTask.FromResult(0);

		//执行数据引擎的增改操作
		return this.DataAccess.UpsertManyAsync(this.Name, items, schema, options, ctx => this.OnUpserting(ctx), ctx => this.OnUpserted(ctx), cancellation);
	}
}
