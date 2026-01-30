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
	public int Update(string key, object data, DataUpdateOptions options = null) => this.Update(key, data, null, options);
	public int Update(string key, object data, string schema, DataUpdateOptions options = null) => this.Update(data, this.ConvertKey(DataServiceMethod.Update(), key, options, out _), schema, options);

	public int UpdateMany(string key, IEnumerable items, DataUpdateOptions options = null) => this.UpdateMany(key, items, null, options);
	public int UpdateMany(string key, IEnumerable items, string schema, DataUpdateOptions options = null)
	{
		if(items == null)
			return 0;

		//确认是否可以执行该操作
		this.EnsureUpdate(options);

		//创建事务
		var transaction = new Zongsoft.Transactions.Transaction();

		try
		{
			var count = 0;

			foreach(var item in items)
			{
				if(item == null)
					continue;

				var dictionary = DataDictionary.GetDictionary<TModel>(item);

				//处理数据模型
				this.OnModel(key, dictionary, options);

				//执行数据更新
				count += this.Update(dictionary.Data, schema, options);
			}

			//提交事务
			transaction.Commit();

			//返回更新数
			return count;
		}
		catch
		{
			//回滚事务
			transaction.Rollback();
			//重抛异常
			throw;
		}
	}

	public int Update<TKey1>(TKey1 key1, object data, DataUpdateOptions options = null)
		where TKey1 : IEquatable<TKey1> => this.Update(key1, null, data, options);
	public int Update<TKey1>(TKey1 key1, string schema, object data, DataUpdateOptions options = null)
		where TKey1 : IEquatable<TKey1> => this.Update(data, this.ConvertKey(DataServiceMethod.Update(), key1, options, out _), schema, options);

	public int Update<TKey1, TKey2>(TKey1 key1, TKey2 key2, object data, DataUpdateOptions options = null)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.Update(key1, key2, null, data, options);
	public int Update<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, object data, DataUpdateOptions options = null)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.Update(data, this.ConvertKey(DataServiceMethod.Update(), key1, key2, options, out _), schema, options);

	public int Update<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, object data, DataUpdateOptions options = null)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.Update(key1, key2, key3, null, data, options);
	public int Update<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, object data, DataUpdateOptions options = null)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.Update(data, this.ConvertKey(DataServiceMethod.Update(), key1, key2, key3, options, out _), schema, options);

	public int Update<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, object data, DataUpdateOptions options = null)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.Update(key1, key2, key3, key4, null, data, options);
	public int Update<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, object data, DataUpdateOptions options = null)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.Update(data, this.ConvertKey(DataServiceMethod.Update(), key1, key2, key3, key4, options, out _), schema, options);

	public int Update<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, object data, DataUpdateOptions options = null)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.Update(key1, key2, key3, key4, key5, null, data, options);
	public int Update<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, object data, DataUpdateOptions options = null)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.Update(data, this.ConvertKey(DataServiceMethod.Update(), key1, key2, key3, key4, key5, options, out _), schema, options);

	public int Update(object data, DataUpdateOptions options = null) => this.Update(data, (ICondition)null, string.Empty, options);
	public int Update(object data, string schema, DataUpdateOptions options = null) => this.Update(data, (ICondition)null, schema, options);
	public int Update(object data, ICondition criteria, DataUpdateOptions options = null) => this.Update(data, criteria, string.Empty, options);
	public int Update(object data, ICondition criteria, string schema, DataUpdateOptions options = null)
	{
		//确认是否可以执行该操作
		this.EnsureUpdate(options);

		if(data == null)
			return 0;

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
		return this.OnUpdate(dictionary, criteria, schematic, options);
	}

	public int Update(object data, Data.Condition criteria, DataUpdateOptions options = null) => this.Update(data, (ICondition)criteria, options);
	public int Update(object data, Data.Condition criteria, string schema, DataUpdateOptions options = null) => this.Update(data, (ICondition)criteria, schema, options);
	public int Update(object data, ConditionCollection criteria, DataUpdateOptions options = null) => this.Update(data, (ICondition)criteria, options);
	public int Update(object data, ConditionCollection criteria, string schema, DataUpdateOptions options = null) => this.Update(data, (ICondition)criteria, schema, options);

	protected virtual int OnUpdate(IDataDictionary<TModel> data, ICondition criteria, ISchema schema, DataUpdateOptions options)
	{
		if(data == null || data.Data == null || !data.HasChanges())
			return 0;

		return this.DataAccess.Update(this.Name, data, criteria, schema, options, ctx => this.OnUpdating(ctx), ctx => this.OnUpdated(ctx));
	}

	public ValueTask<int> UpdateAsync(string key, object data, DataUpdateOptions options = null, CancellationToken cancellation = default) => this.UpdateAsync(key, data, null, options, cancellation);
	public ValueTask<int> UpdateAsync(string key, object data, string schema, DataUpdateOptions options = null, CancellationToken cancellation = default) => this.UpdateAsync(data, this.ConvertKey(DataServiceMethod.Update(), key, options, out _), schema, options, cancellation);

	public ValueTask<int> UpdateManyAsync(string key, IEnumerable items, DataUpdateOptions options, CancellationToken cancellation = default) => this.UpdateManyAsync(key, items, null, options, cancellation);
	public async ValueTask<int> UpdateManyAsync(string key, IEnumerable items, string schema, DataUpdateOptions options, CancellationToken cancellation = default)
	{
		if(items == null)
			return 0;

		//确认是否可以执行该操作
		this.EnsureUpdate(options);

		//创建事务
		var transaction = new Zongsoft.Transactions.Transaction();

		try
		{
			var count = 0;

			foreach(var item in items)
			{
				if(item == null)
					continue;

				var dictionary = DataDictionary.GetDictionary<TModel>(item);

				//处理数据模型
				this.OnModel(key, dictionary, options);

				//执行数据更新
				count += await this.UpdateAsync(dictionary.Data, schema, options, cancellation);
			}

			//提交事务
			transaction.Commit();

			//返回更新数
			return count;
		}
		catch
		{
			//回滚事务
			transaction.Rollback();
			//重抛异常
			throw;
		}
	}

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
}
