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
	public int Insert(object data, DataInsertOptions options = null) => this.Insert(data, string.Empty, options);
	public int Insert(object data, string schema, DataInsertOptions options = null)
	{
		//确认是否可以执行该操作
		this.EnsureInsert(options);

		if(data == null)
			return 0;

		//构建数据操作的选项对象
		if(options == null)
			options = new DataInsertOptions();

		//进行授权验证
		this.Authorize(DataServiceMethod.Insert(), options);

		//解析数据模式表达式
		var schematic = this.GetSchema(schema, data.GetType());

		//将当前插入数据对象转换成数据字典
		var dictionary = DataDictionary.GetDictionary<TModel>(data);

		//验证待新增的数据
		this.OnValidate(DataServiceMethod.Insert(), schematic, dictionary, options);

		return this.OnInsert(dictionary, schematic, options);
	}

	protected virtual int OnInsert(IDataDictionary<TModel> data, ISchema schema, DataInsertOptions options)
	{
		if(data == null || data.Data == null || !data.HasChanges())
			return 0;

		//执行数据引擎的插入操作
		return this.DataAccess.Insert(this.Name, data, schema, options, ctx => this.OnInserting(ctx), ctx => this.OnInserted(ctx));
	}

	public int InsertMany(IEnumerable items, DataInsertOptions options = null) => this.InsertMany(items, string.Empty, options);
	public int InsertMany(IEnumerable items, string schema, DataInsertOptions options = null)
	{
		//确认是否可以执行该操作
		this.EnsureInsert(options);

		if(items == null)
			return 0;

		//构建数据操作的选项对象
		if(options == null)
			options = new DataInsertOptions();

		//进行授权验证
		this.Authorize(DataServiceMethod.InsertMany(), options);

		//解析数据模式表达式
		var schematic = this.GetSchema(schema, Common.TypeExtension.GetElementType(items.GetType()));

		//将当前新增数据集合对象转换成数据字典集合
		var dictionaries = DataDictionary.GetDictionaries<TModel>(items, dictionary =>
		{
			//验证待新增的数据
			this.OnValidate(DataServiceMethod.InsertMany(), schematic, dictionary, options);
		});

		return this.OnInsertMany(dictionaries, schematic, options);
	}

	public int InsertMany(string key, IEnumerable items, DataInsertOptions options = null) => this.InsertMany(key, items, null, options);
	public int InsertMany(string key, IEnumerable items, string schema, DataInsertOptions options = null)
	{
		//确认是否可以执行该操作
		this.EnsureInsert(options);

		if(items == null)
			return 0;

		//构建数据操作的选项对象
		if(options == null)
			options = new DataInsertOptions();

		//进行授权验证
		this.Authorize(DataServiceMethod.InsertMany(), options);

		//解析数据模式表达式
		var schematic = this.GetSchema(schema, Common.TypeExtension.GetElementType(items.GetType()));

		//将当前新增数据集合对象转换成数据字典集合
		var dictionaries = DataDictionary.GetDictionaries<TModel>(items, dictionary =>
		{
			//处理数据模型
			this.OnModel(key, dictionary, options);

			//验证待新增的数据
			this.OnValidate(DataServiceMethod.InsertMany(), schematic, dictionary, options);
		});

		return this.OnInsertMany(dictionaries, schematic, options);
	}

	protected virtual int OnInsertMany(IEnumerable<IDataDictionary<TModel>> items, ISchema schema, DataInsertOptions options)
	{
		if(items == null)
			return 0;

		//执行数据引擎的插入操作
		return this.DataAccess.InsertMany(this.Name, items, schema, options, ctx => this.OnInserting(ctx), ctx => this.OnInserted(ctx));
	}

	public ValueTask<int> InsertAsync(object data, DataInsertOptions options = null, CancellationToken cancellation = default) => this.InsertAsync(data, string.Empty, options, cancellation);
	public ValueTask<int> InsertAsync(object data, string schema, DataInsertOptions options = null, CancellationToken cancellation = default)
	{
		//确认是否可以执行该操作
		this.EnsureInsert(options);

		if(data == null)
			return ValueTask.FromResult(0);

		//构建数据操作的选项对象
		if(options == null)
			options = new DataInsertOptions();

		//进行授权验证
		this.Authorize(DataServiceMethod.Insert(), options);

		//解析数据模式表达式
		var schematic = this.GetSchema(schema, data.GetType());

		//将当前插入数据对象转换成数据字典
		var dictionary = DataDictionary.GetDictionary<TModel>(data);

		//验证待新增的数据
		this.OnValidate(DataServiceMethod.Insert(), schematic, dictionary, options);

		return this.OnInsertAsync(dictionary, schematic, options, cancellation);
	}

	protected virtual ValueTask<int> OnInsertAsync(IDataDictionary<TModel> data, ISchema schema, DataInsertOptions options, CancellationToken cancellation)
	{
		if(data == null || data.Data == null || !data.HasChanges())
			return ValueTask.FromResult(0);

		//执行数据引擎的插入操作
		return this.DataAccess.InsertAsync(this.Name, data, schema, options, ctx => this.OnInserting(ctx), ctx => this.OnInserted(ctx), cancellation);
	}

	public ValueTask<int> InsertManyAsync(IEnumerable items, DataInsertOptions options = null, CancellationToken cancellation = default) => this.InsertManyAsync(items, string.Empty, options, cancellation);
	public ValueTask<int> InsertManyAsync(IEnumerable items, string schema, DataInsertOptions options = null, CancellationToken cancellation = default)
	{
		//确认是否可以执行该操作
		this.EnsureInsert(options);

		if(items == null)
			return ValueTask.FromResult(0);

		//构建数据操作的选项对象
		if(options == null)
			options = new DataInsertOptions();

		//进行授权验证
		this.Authorize(DataServiceMethod.InsertMany(), options);

		//解析数据模式表达式
		var schematic = this.GetSchema(schema, Common.TypeExtension.GetElementType(items.GetType()));

		//将当前新增数据集合对象转换成数据字典集合
		var dictionaries = DataDictionary.GetDictionaries<TModel>(items, dictionary =>
		{
			//验证待新增的数据
			this.OnValidate(DataServiceMethod.InsertMany(), schematic, dictionary, options);
		});

		return this.OnInsertManyAsync(dictionaries, schematic, options, cancellation);
	}

	public ValueTask<int> InsertManyAsync(string key, IEnumerable items, DataInsertOptions options = null, CancellationToken cancellation = default) => this.InsertManyAsync(key, items, null, options, cancellation);
	public ValueTask<int> InsertManyAsync(string key, IEnumerable items, string schema, DataInsertOptions options = null, CancellationToken cancellation = default)
	{
		//确认是否可以执行该操作
		this.EnsureInsert(options);

		if(items == null)
			return ValueTask.FromResult(0);

		//构建数据操作的选项对象
		if(options == null)
			options = new DataInsertOptions();

		//进行授权验证
		this.Authorize(DataServiceMethod.InsertMany(), options);

		//解析数据模式表达式
		var schematic = this.GetSchema(schema, Common.TypeExtension.GetElementType(items.GetType()));

		//将当前新增数据集合对象转换成数据字典集合
		var dictionaries = DataDictionary.GetDictionaries<TModel>(items, dictionary =>
		{
			//处理数据模型
			this.OnModel(key, dictionary, options);

			//验证待新增的数据
			this.OnValidate(DataServiceMethod.InsertMany(), schematic, dictionary, options);
		});

		return this.OnInsertManyAsync(dictionaries, schematic, options, cancellation);
	}

	protected virtual ValueTask<int> OnInsertManyAsync(IEnumerable<IDataDictionary<TModel>> items, ISchema schema, DataInsertOptions options, CancellationToken cancellation = default)
	{
		if(items == null)
			return ValueTask.FromResult(0);

		//执行数据引擎的插入操作
		return this.DataAccess.InsertManyAsync(this.Name, items, schema, options, ctx => this.OnInserting(ctx), ctx => this.OnInserted(ctx), cancellation);
	}
}
