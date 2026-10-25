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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Data;

partial class DataServiceBase<TModel>
{
	#region 插入方法
	protected ValueTask<int> InsertAsync(object data, ISchema schema, CancellationToken cancellation) => this.InsertAsync(data, schema, default(DataInsertOptions), cancellation);
	protected async ValueTask<int> InsertAsync(object data, ISchema schema, DataInsertOptions options = null, CancellationToken cancellation = default)
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

		//将当前插入数据对象转换成数据字典
		var dictionary = DataDictionary.GetDictionary<TModel>(data);

		//修正数据模式对象
		schema = this.Schematic(schema, data.GetType());

		//验证待新增的数据
		await this.OnValidateAsync(DataServiceMethod.Insert(), schema, dictionary, options, cancellation);

		return await this.OnInsertAsync(dictionary, schema, options, cancellation);
	}

	protected ValueTask<int> InsertManyAsync(IEnumerable items, ISchema schema, CancellationToken cancellation) => this.InsertManyAsync(items, schema, default(DataInsertOptions), cancellation);
	protected async ValueTask<int> InsertManyAsync(IEnumerable items, ISchema schema, DataInsertOptions options = null, CancellationToken cancellation = default)
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

		//修正数据模式对象
		schema = this.Schematic(schema, Common.TypeExtension.GetElementType(items.GetType()));

		var dictionaries = DataDictionary.GetDictionariesAsync<TModel>(items, (dictionary, token) => this.OnValidateAsync(DataServiceMethod.InsertMany(), schema, dictionary, options, token), cancellation);
		return await this.OnInsertManyAsync(dictionaries, schema, options, cancellation);
	}
	#endregion

	#region 增改方法
	protected ValueTask<int> UpsertAsync(object data, ISchema schema, CancellationToken cancellation) => this.UpsertAsync(data, schema, default(DataUpsertOptions), cancellation);
	protected async ValueTask<int> UpsertAsync(object data, ISchema schema, DataUpsertOptions options = null, CancellationToken cancellation = default)
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

		//将当前复写数据对象转换成数据字典
		var dictionary = DataDictionary.GetDictionary<TModel>(data);

		//修正数据模式对象
		schema = this.Schematic(schema, data.GetType());

		//验证待复写的数据
		await this.OnValidateAsync(DataServiceMethod.Upsert(), schema, dictionary, options, cancellation);

		return await this.OnUpsertAsync(dictionary, schema, options, cancellation);
	}

	protected ValueTask<int> UpsertManyAsync(IEnumerable items, ISchema schema, CancellationToken cancellation) => this.UpsertManyAsync(items, schema, default(DataUpsertOptions), cancellation);
	protected async ValueTask<int> UpsertManyAsync(IEnumerable items, ISchema schema, DataUpsertOptions options = null, CancellationToken cancellation = default)
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

		//修正数据模式对象
		schema = this.Schematic(schema, Common.TypeExtension.GetElementType(items.GetType()));

		var dictionaries = DataDictionary.GetDictionariesAsync<TModel>(items, (dictionary, token) => this.OnValidateAsync(DataServiceMethod.UpsertMany(), schema, dictionary, options, token), cancellation);
		return await this.OnUpsertManyAsync(dictionaries, schema, options, cancellation);
	}
	#endregion

	#region 更新方法
	protected ValueTask<int> UpdateAsync(object data, ICondition criteria, ISchema schema, CancellationToken cancellation) => this.UpdateAsync(data, criteria, schema, default(DataUpdateOptions), cancellation);
	protected async ValueTask<int> UpdateAsync(object data, ICondition criteria, ISchema schema, DataUpdateOptions options = null, CancellationToken cancellation = default)
	{
		//确认是否可以执行该操作
		this.EnsureUpdate(options);

		if(data == null)
			return 0;

		//构建数据操作的选项对象
		if(options == null)
			options = new DataUpdateOptions();

		//进行授权验证
		await this.AuthorizeAsync(DataServiceMethod.Update(), options, cancellation);

		//将当前更新数据对象转换成数据字典
		var dictionary = DataDictionary.GetDictionary<TModel>(data);

		//只从条件中可以确定的等值常量回填主键
		if(criteria != null)
		{
			//获取当前数据服务的实体主键集
			var keys = Mapping.Entities[this.Name].Key;

			if(keys != null && keys.Length > 0)
			{
				foreach(var key in keys)
				{
					if(criteria.TryGetValue(key.Name, out var value))
						dictionary.TrySetValue(key.Name, value);
				}
			}
		}

		//修整过滤条件
		criteria = await this.OnValidateAsync(DataServiceMethod.Update(), criteria ?? this.GetUpdateKey(dictionary), options, cancellation);

		//修正数据模式对象
		schema = this.Schematic(schema, data.GetType());

		//验证待更新的数据
		await this.OnValidateAsync(DataServiceMethod.Update(), schema, dictionary, options, cancellation);

		//如果缺少必须的更新条件则抛出异常
		if(criteria == null)
		{
			//再次从数据中获取主键条件
			criteria = this.GetUpdateKey(dictionary);

			if(criteria == null)
				throw new DataOperationException(string.Format(Properties.Resources.DataService_UpdateRequiresCriteria_Message, this.Name));
		}

		//执行更新操作
		return await this.OnUpdateAsync(dictionary, criteria, schema, options, cancellation);
	}
	#endregion

	#region 私有方法
	private ISchema Schematic(ISchema schema, Type modelType)
	{
		if(schema == null)
			return this.GetSchema(null, modelType);
		else if(schema is Schema.ReadOnlySchema invariant)
			return this.GetSchema(invariant.Text, modelType, true);

		return schema;
	}
	#endregion

	#region 嵌套子类
	public static class Schema
	{
		public static ISchema Invariant(string schema = null) => new ReadOnlySchema(schema);

		internal class ReadOnlySchema : ISchema
		{
			private string _expression;
			internal ReadOnlySchema(string schema) => _expression = schema;

			public string Name => string.Empty;
			public string Text => _expression;
			public Type ModelType => null;
			public bool IsEmpty => true;
			public bool IsReadOnly { get => true; set => throw new InvalidOperationException(); }

			public void Clear() { }
			public bool Contains(string path) => false;
			public SchemaMemberBase Find(string path) => null;
			public ISchema Include(string path) => this;
			public ISchema Exclude(string path) => this;
			public bool Exclude(string path, out SchemaMemberBase member) { member = null; return false; }
		}
	}
	#endregion
}
