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
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Collections;

namespace Zongsoft.Data;

partial class DataServiceBase<TModel>
{
	#region 存在方法
	public ValueTask<bool> ExistsAsync(string key, DataExistsOptions options = null, CancellationToken cancellation = default) => this.ExistsAsync(this.ConvertKey(DataServiceMethod.Exists(), key, options, out _), options, cancellation);
	public ValueTask<bool> ExistsAsync<TKey1>(TKey1 key1, DataExistsOptions options = null, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1> => this.ExistsAsync(this.ConvertKey(DataServiceMethod.Exists(), key1, options, out _), options, cancellation);
	public ValueTask<bool> ExistsAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, DataExistsOptions options = null, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.ExistsAsync(this.ConvertKey(DataServiceMethod.Exists(), key1, key2, options, out _), options, cancellation);
	public ValueTask<bool> ExistsAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, DataExistsOptions options = null, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.ExistsAsync(this.ConvertKey(DataServiceMethod.Exists(), key1, key2, key3, options, out _), options, cancellation);
	public ValueTask<bool> ExistsAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataExistsOptions options = null, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.ExistsAsync(this.ConvertKey(DataServiceMethod.Exists(), key1, key2, key3, key4, options, out _), options, cancellation);
	public ValueTask<bool> ExistsAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataExistsOptions options = null, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.ExistsAsync(this.ConvertKey(DataServiceMethod.Exists(), key1, key2, key3, key4, key5, options, out _), options, cancellation);

	public ValueTask<bool> ExistsAsync(ICondition criteria, DataExistsOptions options = null, CancellationToken cancellation = default)
	{
		//构建数据操作的选项对象
		if(options == null)
			options = new DataExistsOptions();

		//进行授权验证
		this.Authorize(DataServiceMethod.Exists(), options);

		//修整查询条件
		criteria = this.OnValidate(DataServiceMethod.Exists(), criteria, options);

		//执行存在操作
		return this.OnExistsAsync(criteria, options, cancellation);
	}

	protected virtual ValueTask<bool> OnExistsAsync(ICondition criteria, DataExistsOptions options, CancellationToken cancellation)
	{
		return this.DataAccess.ExistsAsync(this.Name, criteria, options, ctx => this.OnExisting(ctx), ctx => this.OnExisted(ctx), cancellation);
	}
	#endregion

	#region 聚合方法
	public ValueTask<TValue?> AggregateAsync<TValue>(DataAggregate aggregate, string key, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, this.ConvertKey(DataServiceMethod.Aggregate(aggregate), key, null, out _), null, cancellation);
	public ValueTask<TValue?> AggregateAsync<TValue>(DataAggregate aggregate, string key, DataAggregateOptions options, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, this.ConvertKey(DataServiceMethod.Aggregate(aggregate), key, options, out _), options, cancellation);
	public ValueTask<TValue?> AggregateAsync<TKey1, TValue>(DataAggregate aggregate, TKey1 key1, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, this.ConvertKey(DataServiceMethod.Aggregate(aggregate), key1, null, out _), null, cancellation);
	public ValueTask<TValue?> AggregateAsync<TKey1, TValue>(DataAggregate aggregate, TKey1 key1, DataAggregateOptions options, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, this.ConvertKey(DataServiceMethod.Aggregate(aggregate), key1, options, out _), options, cancellation);
	public ValueTask<TValue?> AggregateAsync<TKey1, TKey2, TValue>(DataAggregate aggregate, TKey1 key1, TKey2 key2, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, this.ConvertKey(DataServiceMethod.Aggregate(aggregate), key1, key2, null, out _), null, cancellation);
	public ValueTask<TValue?> AggregateAsync<TKey1, TKey2, TValue>(DataAggregate aggregate, TKey1 key1, TKey2 key2, DataAggregateOptions options, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, this.ConvertKey(DataServiceMethod.Aggregate(aggregate), key1, key2, options, out _), options, cancellation);
	public ValueTask<TValue?> AggregateAsync<TKey1, TKey2, TKey3, TValue>(DataAggregate aggregate, TKey1 key1, TKey2 key2, TKey3 key3, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, this.ConvertKey(DataServiceMethod.Aggregate(aggregate), key1, key2, key3, null, out _), null, cancellation);
	public ValueTask<TValue?> AggregateAsync<TKey1, TKey2, TKey3, TValue>(DataAggregate aggregate, TKey1 key1, TKey2 key2, TKey3 key3, DataAggregateOptions options, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, this.ConvertKey(DataServiceMethod.Aggregate(aggregate), key1, key2, key3, options, out _), options, cancellation);
	public ValueTask<TValue?> AggregateAsync<TKey1, TKey2, TKey3, TKey4, TValue>(DataAggregate aggregate, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, this.ConvertKey(DataServiceMethod.Aggregate(aggregate), key1, key2, key3, key4, null, out _), null, cancellation);
	public ValueTask<TValue?> AggregateAsync<TKey1, TKey2, TKey3, TKey4, TValue>(DataAggregate aggregate, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataAggregateOptions options, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, this.ConvertKey(DataServiceMethod.Aggregate(aggregate), key1, key2, key3, key4, options, out _), options, cancellation);
	public ValueTask<TValue?> AggregateAsync<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(DataAggregate aggregate, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5> where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, this.ConvertKey(DataServiceMethod.Aggregate(aggregate), key1, key2, key3, key4, key5, null, out _), null, cancellation);
	public ValueTask<TValue?> AggregateAsync<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(DataAggregate aggregate, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataAggregateOptions options, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1> where TKey2 : IEquatable<TKey2> where TKey3 : IEquatable<TKey3> where TKey4 : IEquatable<TKey4> where TKey5 : IEquatable<TKey5> where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, this.ConvertKey(DataServiceMethod.Aggregate(aggregate), key1, key2, key3, key4, key5, options, out _), options, cancellation);

	public ValueTask<TValue?> AggregateAsync<TValue>(DataAggregate aggregate, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, (ICondition)null, null, cancellation);
	public ValueTask<TValue?> AggregateAsync<TValue>(DataAggregate aggregate, DataAggregateOptions options, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, (ICondition)null, options, cancellation);
	public ValueTask<TValue?> AggregateAsync<TValue>(DataAggregate aggregate, ICondition criteria, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, criteria, null, cancellation);
	public ValueTask<TValue?> AggregateAsync<TValue>(DataAggregate aggregate, ICondition criteria, DataAggregateOptions options, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue>
	{
		//构建数据操作的选项对象
		if(options == null)
			options = new DataAggregateOptions();

		//进行授权验证
		this.Authorize(DataServiceMethod.Aggregate(aggregate.Function), options);

		//修整查询条件
		criteria = this.OnValidate(DataServiceMethod.Aggregate(aggregate.Function), criteria, options);

		//执行聚合操作
		return this.OnAggregateAsync<TValue>(aggregate, criteria, options, cancellation);
	}

	public ValueTask<TValue?> AggregateAsync<TValue>(DataAggregate aggregate, Data.Condition criteria, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, (ICondition)criteria, null, cancellation);
	public ValueTask<TValue?> AggregateAsync<TValue>(DataAggregate aggregate, Data.Condition criteria, DataAggregateOptions options, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, (ICondition)criteria, options, cancellation);
	public ValueTask<TValue?> AggregateAsync<TValue>(DataAggregate aggregate, ConditionCollection criteria, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, (ICondition)criteria, null, cancellation);
	public ValueTask<TValue?> AggregateAsync<TValue>(DataAggregate aggregate, ConditionCollection criteria, DataAggregateOptions options, CancellationToken cancellation = default) where TValue : struct, IEquatable<TValue> => this.AggregateAsync<TValue>(aggregate, (ICondition)criteria, options, cancellation);

	protected virtual ValueTask<TValue?> OnAggregateAsync<TValue>(DataAggregate aggregate, ICondition criteria, DataAggregateOptions options, CancellationToken cancellation) where TValue : struct, IEquatable<TValue>
	{
		return this.DataAccess.AggregateAsync<TValue>(this.Name, aggregate, criteria, options, ctx => this.OnAggregating(ctx), ctx => this.OnAggregated(ctx), cancellation);
	}
	#endregion

	#region 删除方法
	public ValueTask<int> DeleteAsync(string key, DataDeleteOptions options = null, CancellationToken cancellation = default) => this.DeleteAsync(key, null, options, cancellation);
	public ValueTask<int> DeleteAsync(string key, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default) => this.DeleteAsync(this.ConvertKey(DataServiceMethod.Delete(), key, options, out _), schema, options, cancellation);

	public ValueTask<int> DeleteAsync<TKey1>(TKey1 key1, DataDeleteOptions options = null, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1> => this.DeleteAsync(key1, null, options, cancellation);
	public ValueTask<int> DeleteAsync<TKey1>(TKey1 key1, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1> => this.DeleteAsync(this.ConvertKey(DataServiceMethod.Delete(), key1, options, out _), schema, options, cancellation);

	public ValueTask<int> DeleteAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, DataDeleteOptions options = null, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.DeleteAsync(key1, key2, null, options, cancellation);
	public ValueTask<int> DeleteAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.DeleteAsync(this.ConvertKey(DataServiceMethod.Delete(), key1, key2, options, out _), schema, options, cancellation);

	public ValueTask<int> DeleteAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, DataDeleteOptions options = null, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.DeleteAsync(key1, key2, key3, null, options, cancellation);
	public ValueTask<int> DeleteAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.DeleteAsync(this.ConvertKey(DataServiceMethod.Delete(), key1, key2, key3, options, out _), schema, options, cancellation);

	public ValueTask<int> DeleteAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataDeleteOptions options = null, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.DeleteAsync(key1, key2, key3, key4, null, options, cancellation);
	public ValueTask<int> DeleteAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.DeleteAsync(this.ConvertKey(DataServiceMethod.Delete(), key1, key2, key3, key4, options, out _), schema, options, cancellation);

	public ValueTask<int> DeleteAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataDeleteOptions options = null, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.DeleteAsync(key1, key2, key3, key4, key5, null, options, cancellation);
	public ValueTask<int> DeleteAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.DeleteAsync(this.ConvertKey(DataServiceMethod.Delete(), key1, key2, key3, key4, key5, options, out _), schema, options, cancellation);

	public ValueTask<int> DeleteAsync(ICondition criteria, DataDeleteOptions options = null, CancellationToken cancellation = default) => this.DeleteAsync(criteria, null, options, cancellation);
	public ValueTask<int> DeleteAsync(ICondition criteria, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default)
	{
		//确认是否可以执行该操作
		this.EnsureDelete(options);

		//构建数据操作的选项对象
		if(options == null)
			options = new DataDeleteOptions();

		//进行授权验证
		this.Authorize(DataServiceMethod.Delete(), options);

		//修整删除条件
		criteria = this.OnValidate(DataServiceMethod.Delete(), criteria, options);

		//执行删除操作
		return this.OnDeleteAsync(criteria, this.GetSchema(schema), options, cancellation);
	}

	public ValueTask<int> DeleteAsync(Data.Condition criteria, DataDeleteOptions options = null, CancellationToken cancellation = default) => this.DeleteAsync((ICondition)criteria, options, cancellation);
	public ValueTask<int> DeleteAsync(Data.Condition criteria, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default) => this.DeleteAsync((ICondition)criteria, schema, options, cancellation);
	public ValueTask<int> DeleteAsync(ConditionCollection criteria, DataDeleteOptions options = null, CancellationToken cancellation = default) => this.DeleteAsync((ICondition)criteria, options, cancellation);
	public ValueTask<int> DeleteAsync(ConditionCollection criteria, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default) => this.DeleteAsync((ICondition)criteria, schema, options, cancellation);

	protected virtual ValueTask<int> OnDeleteAsync(ICondition criteria, ISchema schema, DataDeleteOptions options, CancellationToken cancellation)
	{
		if(criteria == null)
			throw new NotSupportedException("The criteria cann't is null on delete operation.");

		return this.DataAccess.DeleteAsync(this.Name, criteria, schema, options, ctx => this.OnDeleting(ctx), ctx => this.OnDeleted(ctx), cancellation);
	}
	#endregion

	#region 插入方法
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
	#endregion

	#region 增改方法
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
	#endregion

	#region 更新方法
	public ValueTask<int> UpdateAsync(string key, object data, DataUpdateOptions options = null, CancellationToken cancellation = default) => this.UpdateAsync(key, data, null, options, cancellation);
	public ValueTask<int> UpdateAsync(string key, object data, string schema, DataUpdateOptions options = null, CancellationToken cancellation = default) => this.UpdateAsync(data, this.ConvertKey(DataServiceMethod.Update(), key, options, out _), schema, options, cancellation);

	public ValueTask<int> UpdateAsync<TKey1>(TKey1 key1, object data, DataUpdateOptions options = null, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1> => this.UpdateAsync(key1, null, data, options, cancellation);
	public ValueTask<int> UpdateAsync<TKey1>(TKey1 key1, string schema, object data, DataUpdateOptions options = null, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1> => this.UpdateAsync(data, this.ConvertKey(DataServiceMethod.Update(), key1, options, out _), schema, options, cancellation);
	public ValueTask<int> UpdateAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, object data, DataUpdateOptions options = null, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.UpdateAsync(key1, key2, null, data, options, cancellation);
	public ValueTask<int> UpdateAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, object data, DataUpdateOptions options = null, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.UpdateAsync(data, this.ConvertKey(DataServiceMethod.Update(), key1, key2, options, out _), schema, options, cancellation);
	public ValueTask<int> UpdateAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, object data, DataUpdateOptions options = null, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.UpdateAsync(key1, key2, key3, null, data, options, cancellation);
	public ValueTask<int> UpdateAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, object data, DataUpdateOptions options = null, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.UpdateAsync(data, this.ConvertKey(DataServiceMethod.Update(), key1, key2, key3, options, out _), schema, options, cancellation);
	public ValueTask<int> UpdateAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, object data, DataUpdateOptions options = null, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.UpdateAsync(key1, key2, key3, key4, null, data, options, cancellation);
	public ValueTask<int> UpdateAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, object data, DataUpdateOptions options = null, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.UpdateAsync(data, this.ConvertKey(DataServiceMethod.Update(), key1, key2, key3, key4, options, out _), schema, options, cancellation);
	public ValueTask<int> UpdateAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, object data, DataUpdateOptions options = null, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.UpdateAsync(key1, key2, key3, key4, key5, null, data, options, cancellation);
	public ValueTask<int> UpdateAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, object data, DataUpdateOptions options = null, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.UpdateAsync(data, this.ConvertKey(DataServiceMethod.Update(), key1, key2, key3, key4, key5, options, out _), schema, options, cancellation);

	public ValueTask<int> UpdateAsync(object data, DataUpdateOptions options = null, CancellationToken cancellation = default) => this.UpdateAsync(data, (ICondition)null, string.Empty, options, cancellation);
	public ValueTask<int> UpdateAsync(object data, string schema, DataUpdateOptions options = null, CancellationToken cancellation = default) => this.UpdateAsync(data, (ICondition)null, schema, options, cancellation);
	public ValueTask<int> UpdateAsync(object data, ICondition criteria, DataUpdateOptions options = null, CancellationToken cancellation = default) => this.UpdateAsync(data, criteria, string.Empty, options, cancellation);
	public ValueTask<int> UpdateAsync(object data, ICondition criteria, string schema, DataUpdateOptions options = null, CancellationToken cancellation = default)
	{
		//确认是否可以执行该操作
		this.EnsureUpdate(options);

		if(data == null)
			return ValueTask.FromResult(0);

		//构建数据操作的选项对象
		if(options == null)
			options = new DataUpdateOptions();

		//进行授权验证
		this.Authorize(DataServiceMethod.Update(), options);

		//将当前更新数据对象转换成数据字典
		var dictionary = DataDictionary.GetDictionary<TModel>(data);

		//如果指定了更新条件，则尝试将条件中的主键值同步设置到数据字典中
		if(criteria != null)
		{
			//获取当前数据服务的实体主键集
			var keys = Mapping.Entities[this.Name].Key;

			if(keys != null && keys.Length > 0)
			{
				foreach(var key in keys)
				{
					criteria.Match(key.Name, c => dictionary.TrySetValue(c.Name, c.Value));
				}
			}
		}

		//修整过滤条件
		criteria = this.OnValidate(DataServiceMethod.Update(), criteria ?? this.GetUpdateKey(dictionary), options);

		//解析数据模式表达式
		var schematic = this.GetSchema(schema, data.GetType());

		//验证待更新的数据
		this.OnValidate(DataServiceMethod.Update(), schematic, dictionary, options);

		//如果缺少必须的更新条件则抛出异常
		if(criteria == null)
		{
			//再次从数据中获取主键条件
			criteria = this.GetUpdateKey(dictionary);

			if(criteria == null)
				throw new DataOperationException($"The update operation of the specified ‘{this.Name}’ entity missing required conditions.");
		}

		//执行更新操作
		return this.OnUpdateAsync(dictionary, criteria, schematic, options, cancellation);
	}

	public ValueTask<int> UpdateAsync(object data, Data.Condition criteria, DataUpdateOptions options = null, CancellationToken cancellation = default) => this.UpdateAsync(data, (ICondition)criteria, options, cancellation);
	public ValueTask<int> UpdateAsync(object data, Data.Condition criteria, string schema, DataUpdateOptions options = null, CancellationToken cancellation = default) => this.UpdateAsync(data, (ICondition)criteria, schema, options, cancellation);
	public ValueTask<int> UpdateAsync(object data, ConditionCollection criteria, DataUpdateOptions options = null, CancellationToken cancellation = default) => this.UpdateAsync(data, (ICondition)criteria, options, cancellation);
	public ValueTask<int> UpdateAsync(object data, ConditionCollection criteria, string schema, DataUpdateOptions options = null, CancellationToken cancellation = default) => this.UpdateAsync(data, (ICondition)criteria, schema, options, cancellation);

	protected virtual ValueTask<int> OnUpdateAsync(IDataDictionary<TModel> data, ICondition criteria, ISchema schema, DataUpdateOptions options, CancellationToken cancellation)
	{
		if(data == null || data.Data == null || !data.HasChanges())
			return ValueTask.FromResult(0);

		return this.DataAccess.UpdateAsync(this.Name, data, criteria, schema, options, ctx => this.OnUpdating(ctx), ctx => this.OnUpdated(ctx), cancellation);
	}
	#endregion

	#region 查询方法

	#region 键值查询
	public ValueTask<object> GetAsync(string key, params Sorting[] sortings) => this.GetAsync(key, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync(string key, Sorting[] sortings, CancellationToken cancellation = default) => this.GetAsync(key, string.Empty, Paging.Disabled, null, sortings);
	public ValueTask<object> GetAsync(string key, DataSelectOptions options, params Sorting[] sortings) => this.GetAsync(key, string.Empty, Paging.Disabled, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync(string key, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) => this.GetAsync(key, string.Empty, Paging.Disabled, options, sortings, cancellation);
	public ValueTask<object> GetAsync(string key, Paging paging, params Sorting[] sortings) => this.GetAsync(key, string.Empty, paging, null, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync(string key, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) => this.GetAsync(key, string.Empty, paging, null, sortings, cancellation);
	public ValueTask<object> GetAsync(string key, Paging paging, DataSelectOptions options, params Sorting[] sortings) => this.GetAsync(key, string.Empty, paging, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync(string key, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) => this.GetAsync(key, string.Empty, paging, options, sortings, cancellation);
	public ValueTask<object> GetAsync(string key, string schema, params Sorting[] sortings) => this.GetAsync(key, schema, Paging.Disabled, null, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync(string key, string schema, Sorting[] sortings, CancellationToken cancellation = default) => this.GetAsync(key, schema, Paging.Disabled, null, sortings, cancellation);
	public ValueTask<object> GetAsync(string key, string schema, DataSelectOptions options, params Sorting[] sortings) => this.GetAsync(key, schema, Paging.Disabled, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync(string key, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) => this.GetAsync(key, schema, Paging.Disabled, options, sortings, cancellation);
	public ValueTask<object> GetAsync(string key, string schema, Paging paging, params Sorting[] sortings) => this.GetAsync(key, schema, paging, null, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync(string key, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) => this.GetAsync(key, schema, paging, null, sortings, cancellation);
	public ValueTask<object> GetAsync(string key, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings) => this.GetAsync(key, schema, paging, options, sortings, CancellationToken.None);
	public async ValueTask<object> GetAsync(string key, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
	{
		//构建数据操作的选项对象
		if(options == null)
			options = new DataSelectOptions();

		var criteria = this.ConvertKey(DataServiceMethod.Get(), key, options, out var singular);

		if(singular)
		{
			//进行授权验证
			this.Authorize(DataServiceMethod.Get(), options);

			//修整查询条件
			criteria = this.OnValidate(DataServiceMethod.Get(), criteria, options);

			//执行单条查询方法
			return await this.OnGetAsync(criteria, this.GetSchema(schema), options, cancellation);
		}

		return this.SelectAsync(criteria, schema, paging, options, sortings, cancellation);
	}

	protected virtual async ValueTask<TModel> OnGetAsync(ICondition criteria, ISchema schema, DataSelectOptions options, CancellationToken cancellation)
	{
		var result = this.DataAccess.SelectAsync<TModel>(this.Name, criteria, schema, null, options, null, ctx => this.OnGetting(ctx), ctx => this.OnGetted(ctx), cancellation);
		await using var iterator = result.GetAsyncEnumerator(cancellation);
		return (await iterator.MoveNextAsync()) ? iterator.Current : default;
	}
	#endregion

	#region 单键查询
	public ValueTask<object> GetAsync<TKey1>(TKey1 key1, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1>(TKey1 key1, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, string.Empty, null, null, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1>(TKey1 key1, DataSelectOptions options, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, string.Empty, null, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1>(TKey1 key1, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, string.Empty, null, options, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1>(TKey1 key1, Paging paging, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, string.Empty, paging, null, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1>(TKey1 key1, Paging paging, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, string.Empty, paging, null, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1>(TKey1 key1, Paging paging, DataSelectOptions options, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, string.Empty, paging, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1>(TKey1 key1, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, string.Empty, paging, options, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1>(TKey1 key1, string schema, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, schema, null, null, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1>(TKey1 key1, string schema, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, schema, null, null, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1>(TKey1 key1, string schema, DataSelectOptions options, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, schema, null, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1>(TKey1 key1, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, schema, null, options, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1>(TKey1 key1, string schema, Paging paging, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, schema, paging, null, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1>(TKey1 key1, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, schema, paging, null, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1>(TKey1 key1, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, schema, paging, options, sortings, CancellationToken.None);
	public async ValueTask<object> GetAsync<TKey1>(TKey1 key1, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
	{
		//构建数据操作的选项对象
		if(options == null)
			options = new DataSelectOptions();

		var criteria = this.ConvertKey(DataServiceMethod.Get(), key1, options, out var singular);

		if(singular)
		{
			//进行授权验证
			this.Authorize(DataServiceMethod.Get(), options);

			//修整查询条件
			criteria = this.OnValidate(DataServiceMethod.Get(), criteria, options);

			//执行单条查询方法
			return await this.OnGetAsync(criteria, this.GetSchema(schema), options, cancellation);
		}

		return this.SelectAsync(criteria, schema, paging, options, sortings, cancellation);
	}
	#endregion

	#region 双键查询
	public ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, string.Empty, null, null, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, DataSelectOptions options, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, string.Empty, null, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, string.Empty, null, options, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, Paging paging, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, string.Empty, paging, null, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, Paging paging, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, string.Empty, paging, null, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, Paging paging, DataSelectOptions options, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, string.Empty, paging, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, string.Empty, paging, options, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, schema, null, null, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, schema, null, null, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, DataSelectOptions options, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, schema, null, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, schema, null, options, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Paging paging, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, schema, paging, null, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, schema, paging, null, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, schema, paging, options, sortings, CancellationToken.None);
	public async ValueTask<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
	{
		//构建数据操作的选项对象
		if(options == null)
			options = new DataSelectOptions();

		var criteria = this.ConvertKey(DataServiceMethod.Get(), key1, key2, options, out var singular);

		if(singular)
		{
			//进行授权验证
			this.Authorize(DataServiceMethod.Get(), options);

			//修整查询条件
			criteria = this.OnValidate(DataServiceMethod.Get(), criteria, options);

			//执行单条查询方法
			return await this.OnGetAsync(criteria, this.GetSchema(schema), options, cancellation);
		}

		return this.SelectAsync(criteria, schema, paging, options, sortings, cancellation);
	}
	#endregion

	#region 三键查询
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, string.Empty, null, null, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, DataSelectOptions options, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, string.Empty, null, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, string.Empty, null, options, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, Paging paging, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, string.Empty, paging, null, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, Paging paging, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, string.Empty, paging, null, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, Paging paging, DataSelectOptions options, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, string.Empty, paging, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, null, paging, options, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, schema, null, null, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, schema, null, null, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, DataSelectOptions options, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, schema, null, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, schema, null, options, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Paging paging, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, schema, paging, null, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, schema, paging, null, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, schema, paging, options, sortings, CancellationToken.None);
	public async ValueTask<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
	{
		//构建数据操作的选项对象
		if(options == null)
			options = new DataSelectOptions();

		var criteria = this.ConvertKey(DataServiceMethod.Get(), key1, key2, key3, options, out var singular);

		if(singular)
		{
			//进行授权验证
			this.Authorize(DataServiceMethod.Get(), options);

			//修整查询条件
			criteria = this.OnValidate(DataServiceMethod.Get(), criteria, options);

			//执行单条查询方法
			return await this.OnGetAsync(criteria, this.GetSchema(schema), options, cancellation);
		}

		return this.SelectAsync(criteria, schema, paging, options, sortings, cancellation);
	}
	#endregion

	#region 四键查询
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, string.Empty, null, null, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataSelectOptions options, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, string.Empty, null, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, string.Empty, null, options, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, Paging paging, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, string.Empty, paging, null, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, Paging paging, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, string.Empty, paging, null, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, Paging paging, DataSelectOptions options, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, string.Empty, paging, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, string.Empty, paging, options, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, schema, null, null, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, schema, null, null, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, DataSelectOptions options, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, schema, null, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, schema, null, options, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, Paging paging, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, schema, paging, null, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, schema, paging, null, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, schema, paging, options, sortings, CancellationToken.None);
	public async ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
	{
		//构建数据操作的选项对象
		if(options == null)
			options = new DataSelectOptions();

		var criteria = this.ConvertKey(DataServiceMethod.Get(), key1, key2, key3, key4, options, out var singular);

		if(singular)
		{
			//进行授权验证
			this.Authorize(DataServiceMethod.Get(), options);

			//修整查询条件
			criteria = this.OnValidate(DataServiceMethod.Get(), criteria, options);

			//执行单条查询方法
			return await this.OnGetAsync(criteria, this.GetSchema(schema), options, cancellation);
		}

		return this.SelectAsync(criteria, schema, paging, options, sortings, cancellation);
	}
	#endregion

	#region 五键查询
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, string.Empty, null, null, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataSelectOptions options, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, string.Empty, null, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, string.Empty, null, options, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, Paging paging, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, string.Empty, paging, null, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, Paging paging, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, string.Empty, paging, null, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, Paging paging, DataSelectOptions options, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, string.Empty, paging, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, string.Empty, paging, options, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, schema, null, null, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, schema, null, null, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, DataSelectOptions options, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, schema, null, options, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, schema, null, options, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, Paging paging, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, schema, paging, null, sortings, CancellationToken.None);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, schema, paging, null, sortings, cancellation);
	public ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, schema, paging, options, sortings, CancellationToken.None);
	public async ValueTask<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5>
	{
		//构建数据操作的选项对象
		if(options == null)
			options = new DataSelectOptions();

		var criteria = this.ConvertKey(DataServiceMethod.Get(), key1, key2, key3, key4, key5, options, out var singular);

		if(singular)
		{
			//进行授权验证
			this.Authorize(DataServiceMethod.Get(), options);

			//修整查询条件
			criteria = this.OnValidate(DataServiceMethod.Get(), criteria, options);

			//执行单条查询方法
			return await this.OnGetAsync(criteria, this.GetSchema(schema), options, cancellation);
		}

		return this.SelectAsync(criteria, schema, paging, options, sortings, cancellation);
	}
	#endregion

	#region 普通查询
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

	public IAsyncEnumerable<TModel> SelectAsync(CancellationToken cancellation = default) => this.SelectAsync(null, string.Empty, null, null, null, cancellation);
	public IAsyncEnumerable<TModel> SelectAsync(DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync(null, string.Empty, null, options, sortings, CancellationToken.None);
	public IAsyncEnumerable<TModel> SelectAsync(DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync(null, string.Empty, null, options, sortings, cancellation);
	public IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, params Sorting[] sortings) => this.SelectAsync(criteria, string.Empty, null, null, sortings, CancellationToken.None);
	public IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync(criteria, string.Empty, null, null, sortings, cancellation);
	public IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync(criteria, string.Empty, null, options, sortings, CancellationToken.None);
	public IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync(criteria, string.Empty, null, options, sortings, cancellation);
	public IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, Paging paging, params Sorting[] sortings) => this.SelectAsync(criteria, string.Empty, paging, null, sortings, CancellationToken.None);
	public IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync(criteria, string.Empty, paging, null, sortings, cancellation);
	public IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, Paging paging, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync(criteria, string.Empty, paging, options, sortings, CancellationToken.None);
	public IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync(criteria, string.Empty, paging, options, sortings, cancellation);
	public IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, string schema, params Sorting[] sortings) => this.SelectAsync(criteria, schema, null, null, sortings, CancellationToken.None);
	public IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, string schema, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync(criteria, schema, null, null, sortings, cancellation);
	public IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, string schema, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync(criteria, schema, null, options, sortings, CancellationToken.None);
	public IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync(criteria, schema, null, options, sortings, cancellation);
	public IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, string schema, Paging paging, params Sorting[] sortings) => this.SelectAsync(criteria, schema, paging, null, sortings, CancellationToken.None);
	public IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync(criteria, schema, paging, null, sortings, cancellation);
	public IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync(criteria, schema, paging, options, sortings, CancellationToken.None);
	public IAsyncEnumerable<TModel> SelectAsync(ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
	{
		//构建数据操作的选项对象
		if(options == null)
			options = new DataSelectOptions();

		//进行授权验证
		this.Authorize(DataServiceMethod.Select(), options);

		//修整查询条件
		criteria = this.OnValidate(DataServiceMethod.Select(), criteria, options);

		//执行查询方法
		return this.OnSelectAsync(criteria, this.GetSchema(schema, typeof(TModel)), paging, sortings, options, cancellation);
	}

	protected virtual IAsyncEnumerable<TModel> OnSelectAsync(ICondition criteria, ISchema schema, Paging paging, Sorting[] sortings, DataSelectOptions options, CancellationToken cancellation)
	{
		//如果没有指定排序设置则应用默认排序规则
		if(sortings == null || sortings.Length == 0)
			sortings = this.GetDefaultSortings();

		return this.DataAccess.SelectAsync<TModel>(this.Name, criteria, schema, paging, options, sortings, ctx => this.OnSelecting(ctx), ctx => this.OnSelected(ctx), cancellation);
	}
	#endregion

	#region 分组查询
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, params Sorting[] sortings) => this.SelectAsync<T>(grouping, null, string.Empty, null, null, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync<T>(grouping, null, string.Empty, null, null, sortings, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync<T>(grouping, null, string.Empty, null, options, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync<T>(grouping, null, string.Empty, null, options, sortings, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, Paging paging, params Sorting[] sortings) => this.SelectAsync<T>(grouping, null, string.Empty, paging, null, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync<T>(grouping, null, string.Empty, paging, null, sortings, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, Paging paging, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync<T>(grouping, null, string.Empty, paging, options, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync<T>(grouping, null, string.Empty, paging, options, sortings, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, string schema, params Sorting[] sortings) => this.SelectAsync<T>(grouping, null, schema, null, null, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, string schema, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync<T>(grouping, null, schema, null, null, sortings, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, string schema, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync<T>(grouping, null, schema, null, options, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync<T>(grouping, null, schema, null, options, sortings, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, string schema, Paging paging, params Sorting[] sortings) => this.SelectAsync<T>(grouping, null, schema, paging, null, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync<T>(grouping, null, schema, paging, null, sortings, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync<T>(grouping, null, schema, paging, options, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync<T>(grouping, null, schema, paging, options, sortings, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, ICondition criteria, Paging paging, params Sorting[] sortings) => this.SelectAsync<T>(grouping, criteria, string.Empty, paging, null, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, ICondition criteria, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync<T>(grouping, criteria, string.Empty, paging, null, sortings, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema = null, params Sorting[] sortings) => this.SelectAsync<T>(grouping, criteria, schema, null, null, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync<T>(grouping, criteria, schema, null, null, sortings, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync<T>(grouping, criteria, schema, null, options, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync<T>(grouping, criteria, schema, null, options, sortings, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema, Paging paging, params Sorting[] sortings) => this.SelectAsync<T>(grouping, criteria, schema, paging, null, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync<T>(grouping, criteria, schema, paging, null, sortings, cancellation);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync<T>(grouping, criteria, schema, paging, options, sortings, CancellationToken.None);
	public IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
	{
		//构建数据操作的选项对象
		if(options == null)
			options = new DataSelectOptions();

		//进行授权验证
		this.Authorize(DataServiceMethod.Select(), options);

		//修整查询条件
		criteria = this.OnValidate(DataServiceMethod.Select(), criteria, options);

		//执行查询方法
		return this.OnSelectAsync<T>(grouping, criteria, string.IsNullOrWhiteSpace(schema) ? null : this.GetSchema(schema, typeof(TModel)), paging, sortings, options, cancellation);
	}

	protected virtual IAsyncEnumerable<T> OnSelectAsync<T>(Grouping grouping, ICondition criteria, ISchema schema, Paging paging, Sorting[] sortings, DataSelectOptions options, CancellationToken cancellation)
	{
		return this.DataAccess.SelectAsync<T>(this.Name, grouping, criteria, schema, paging, options, sortings, ctx => this.OnSelecting(ctx), ctx => this.OnSelected(ctx), cancellation);
	}
	#endregion

	#endregion
}
