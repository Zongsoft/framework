/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2022 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Data;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Data
{
	public partial class DataServiceBase<TModel>
	{
		#region 执行方法
		public Task<IEnumerable<T>> ExecuteAsync<T>(string name, IDictionary<string, object> inParameters, DataExecuteOptions options = null, CancellationToken cancellation = default)
		{
			//构建数据操作的选项对象
			if(options == null)
				options = new DataExecuteOptions();

			//进行授权验证
			this.Authorize(DataServiceMethod.Execute(), options);

			return this.OnExecuteAsync<T>(name, inParameters, options, cancellation);
		}

		public Task<object> ExecuteScalarAsync(string name, IDictionary<string, object> inParameters, DataExecuteOptions options = null, CancellationToken cancellation = default)
		{
			//构建数据操作的选项对象
			if(options == null)
				options = new DataExecuteOptions();

			//进行授权验证
			this.Authorize(DataServiceMethod.Execute(), options);

			return this.OnExecuteScalarAsync(name, inParameters, options, cancellation);
		}

		protected virtual Task<IEnumerable<T>> OnExecuteAsync<T>(string name, IDictionary<string, object> inParameters, DataExecuteOptions options, CancellationToken cancellation)
		{
			return this.DataAccess.ExecuteAsync<T>(name, inParameters, options, ctx => this.OnExecuting(ctx), ctx => this.OnExecuted(ctx), cancellation);
		}

		protected virtual Task<object> OnExecuteScalarAsync(string name, IDictionary<string, object> inParameters, DataExecuteOptions options, CancellationToken cancellation)
		{
			return this.DataAccess.ExecuteScalarAsync(name, inParameters, options, ctx => this.OnExecuting(ctx), ctx => this.OnExecuted(ctx), cancellation);
		}
		#endregion

		#region 存在方法
		public Task<bool> ExistsAsync(string key, DataExistsOptions options = null, CancellationToken cancellation = default)
		{
			return this.ExistsAsync(this.ConvertKey(DataServiceMethod.Exists(), key, out _), options, cancellation);
		}

		public Task<bool> ExistsAsync<TKey1>(TKey1 key1, DataExistsOptions options = null, CancellationToken cancellation = default) where TKey1 : IEquatable<TKey1>
		{
			return this.ExistsAsync(this.ConvertKey(DataServiceMethod.Exists(), key1, out _), options, cancellation);
		}

		public Task<bool> ExistsAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, DataExistsOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
		{
			return this.ExistsAsync(this.ConvertKey(DataServiceMethod.Exists(), key1, key2, out _), options, cancellation);
		}

		public Task<bool> ExistsAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, DataExistsOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
		{
			return this.ExistsAsync(this.ConvertKey(DataServiceMethod.Exists(), key1, key2, key3, out _), options, cancellation);
		}

		public Task<bool> ExistsAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataExistsOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
		{
			return this.ExistsAsync(this.ConvertKey(DataServiceMethod.Exists(), key1, key2, key3, key4, out _), options, cancellation);
		}

		public Task<bool> ExistsAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataExistsOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TKey5 : IEquatable<TKey5>
		{
			return this.ExistsAsync(this.ConvertKey(DataServiceMethod.Exists(), key1, key2, key3, key4, key5, out _), options, cancellation);
		}

		public Task<bool> ExistsAsync(ICondition criteria, DataExistsOptions options = null, CancellationToken cancellation = default)
		{
			//构建数据操作的选项对象
			if(options == null)
				options = new DataExistsOptions();

			//进行授权验证
			this.Authorize(DataServiceMethod.Exists(), options);

			//修整查询条件
			criteria = this.OnValidate(DataServiceMethod.Exists(), criteria, options.Filter, options);

			//执行存在操作
			return this.OnExistsAsync(criteria, options, cancellation);
		}

		protected virtual Task<bool> OnExistsAsync(ICondition criteria, DataExistsOptions options, CancellationToken cancellation)
		{
			return this.DataAccess.ExistsAsync(this.Name, criteria, options, ctx => this.OnExisting(ctx), ctx => this.OnExisted(ctx), cancellation);
		}
		#endregion

		#region 聚合方法
		public Task<TValue?> AggregateAsync<TValue>(DataAggregateFunction function, string member, string key, DataAggregateOptions options = null, CancellationToken cancellation = default)
			where TValue : struct, IEquatable<TValue>
		{
			return this.AggregateAsync<TValue>(function, member, this.ConvertKey(DataServiceMethod.Aggregate(function), key, out _), options, cancellation);
		}

		public Task<TValue?> AggregateAsync<TKey1, TValue>(DataAggregateFunction function, string member, TKey1 key1, DataAggregateOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TValue : struct, IEquatable<TValue>
		{
			return this.AggregateAsync<TValue>(function, member, this.ConvertKey(DataServiceMethod.Aggregate(function), key1, out _), options, cancellation);
		}

		public Task<TValue?> AggregateAsync<TKey1, TKey2, TValue>(DataAggregateFunction function, string member, TKey1 key1, TKey2 key2, DataAggregateOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TValue : struct, IEquatable<TValue>
		{
			return this.AggregateAsync<TValue>(function, member, this.ConvertKey(DataServiceMethod.Aggregate(function), key1, key2, out _), options, cancellation);
		}

		public Task<TValue?> AggregateAsync<TKey1, TKey2, TKey3, TValue>(DataAggregateFunction function, string member, TKey1 key1, TKey2 key2, TKey3 key3, DataAggregateOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TValue : struct, IEquatable<TValue>
		{
			return this.AggregateAsync<TValue>(function, member, this.ConvertKey(DataServiceMethod.Aggregate(function), key1, key2, key3, out _), options, cancellation);
		}

		public Task<TValue?> AggregateAsync<TKey1, TKey2, TKey3, TKey4, TValue>(DataAggregateFunction function, string member, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataAggregateOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TValue : struct, IEquatable<TValue>
		{
			return this.AggregateAsync<TValue>(function, member, this.ConvertKey(DataServiceMethod.Aggregate(function), key1, key2, key3, key4, out _), options, cancellation);
		}

		public Task<TValue?> AggregateAsync<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(DataAggregateFunction function, string member, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataAggregateOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TKey5 : IEquatable<TKey5>
			where TValue : struct, IEquatable<TValue>
		{
			return this.AggregateAsync<TValue>(function, member, this.ConvertKey(DataServiceMethod.Aggregate(function), key1, key2, key3, key4, key5, out _), options, cancellation);
		}

		public Task<TValue?> AggregateAsync<TValue>(DataAggregateFunction function, string member, ICondition criteria = null, DataAggregateOptions options = null, CancellationToken cancellation = default)
			where TValue : struct, IEquatable<TValue>
		{
			//构建数据操作的选项对象
			if(options == null)
				options = new DataAggregateOptions();

			//进行授权验证
			this.Authorize(DataServiceMethod.Aggregate(function), options);

			//修整查询条件
			criteria = this.OnValidate(DataServiceMethod.Aggregate(function), criteria, options.Filter, options);

			//执行聚合操作
			return this.OnAggregateAsync<TValue>(new DataAggregate(function, member), criteria, options, cancellation);
		}

		protected virtual Task<TValue?> OnAggregateAsync<TValue>(DataAggregate aggregate, ICondition criteria, DataAggregateOptions options, CancellationToken cancellation) where TValue : struct, IEquatable<TValue>
		{
			return this.DataAccess.AggregateAsync<TValue>(this.Name, aggregate, criteria, options, ctx => this.OnAggregating(ctx), ctx => this.OnAggregated(ctx), cancellation);
		}
		#endregion

		#region 递增方法
		public Task<long> IncrementAsync(string member, ICondition criteria, DataIncrementOptions options, CancellationToken cancellation = default)
		{
			return this.IncrementAsync(member, criteria, 1, options, cancellation);
		}

		public Task<long> IncrementAsync(string member, ICondition criteria, int interval = 1, DataIncrementOptions options = null, CancellationToken cancellation = default)
		{
			//构建数据操作的选项对象
			if(options == null)
				options = new DataIncrementOptions();

			//进行授权验证
			this.Authorize(DataServiceMethod.Increment(), options);

			//修整查询条件
			criteria = this.OnValidate(DataServiceMethod.Increment(), criteria, options.Filter, options);

			//执行递增操作
			return this.OnIncrementAsync(member, criteria, interval, options, cancellation);
		}

		public Task<long> DecrementAsync(string member, ICondition criteria, DataIncrementOptions options, CancellationToken cancellation = default)
		{
			return this.DecrementAsync(member, criteria, 1, options, cancellation);
		}

		public Task<long> DecrementAsync(string member, ICondition criteria, int interval = 1, DataIncrementOptions options = null, CancellationToken cancellation = default)
		{
			return this.IncrementAsync(member, criteria, -interval, options, cancellation);
		}

		protected virtual Task<long> OnIncrementAsync(string member, ICondition criteria, int interval, DataIncrementOptions options, CancellationToken cancellation)
		{
			return this.DataAccess.IncrementAsync(this.Name, member, criteria, interval, options, ctx => this.OnIncrementing(ctx), ctx => this.OnIncremented(ctx), cancellation);
		}
		#endregion

		#region 删除方法
		public Task<int> DeleteAsync(string key, DataDeleteOptions options = null, CancellationToken cancellation = default)
		{
			return this.DeleteAsync(key, null, options, cancellation);
		}

		public Task<int> DeleteAsync(string key, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default)
		{
			return this.DeleteAsync(this.ConvertKey(DataServiceMethod.Delete(), key, out _), schema, options, cancellation);
		}

		public Task<int> DeleteAsync<TKey1>(TKey1 key1, DataDeleteOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
		{
			return this.DeleteAsync(key1, null, options, cancellation);
		}

		public Task<int> DeleteAsync<TKey1>(TKey1 key1, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
		{
			return this.DeleteAsync(this.ConvertKey(DataServiceMethod.Delete(), key1, out _), schema, options, cancellation);
		}

		public Task<int> DeleteAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, DataDeleteOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
		{
			return this.DeleteAsync(key1, key2, null, options, cancellation);
		}

		public Task<int> DeleteAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
		{
			return this.DeleteAsync(this.ConvertKey(DataServiceMethod.Delete(), key1, key2, out _), schema, options, cancellation);
		}

		public Task<int> DeleteAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, DataDeleteOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
		{
			return this.DeleteAsync(key1, key2, key3, null, options, cancellation);
		}

		public Task<int> DeleteAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
		{
			return this.DeleteAsync(this.ConvertKey(DataServiceMethod.Delete(), key1, key2, key3, out _), schema, options, cancellation);
		}

		public Task<int> DeleteAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataDeleteOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
		{
			return this.DeleteAsync(key1, key2, key3, key4, null, options, cancellation);
		}

		public Task<int> DeleteAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
		{
			return this.DeleteAsync(this.ConvertKey(DataServiceMethod.Delete(), key1, key2, key3, key4, out _), schema, options, cancellation);
		}

		public Task<int> DeleteAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataDeleteOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TKey5 : IEquatable<TKey5>
		{
			return this.DeleteAsync(key1, key2, key3, key4, key5, null, options, cancellation);
		}

		public Task<int> DeleteAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TKey5 : IEquatable<TKey5>
		{
			return this.DeleteAsync(this.ConvertKey(DataServiceMethod.Delete(), key1, key2, key3, key4, key5, out _), schema, options, cancellation);
		}

		public Task<int> DeleteAsync(ICondition criteria, DataDeleteOptions options = null, CancellationToken cancellation = default)
		{
			return this.DeleteAsync(criteria, null, options, cancellation);
		}

		public Task<int> DeleteAsync(ICondition criteria, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default)
		{
			//确认是否可以执行该操作
			this.EnsureDelete(options);

			//构建数据操作的选项对象
			if(options == null)
				options = new DataDeleteOptions();

			//进行授权验证
			this.Authorize(DataServiceMethod.Delete(), options);

			//修整删除条件
			criteria = this.OnValidate(DataServiceMethod.Delete(), criteria, options.Filter, options);

			//执行删除操作
			return this.OnDeleteAsync(criteria, this.GetSchema(schema), options, cancellation);
		}

		public Task<int> DeleteAsync(Data.Condition criteria, DataDeleteOptions options = null, CancellationToken cancellation = default) => this.DeleteAsync((ICondition)criteria, options, cancellation);
		public Task<int> DeleteAsync(Data.Condition criteria, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default) => this.DeleteAsync((ICondition)criteria, schema, options, cancellation);
		public Task<int> DeleteAsync(ConditionCollection criteria, DataDeleteOptions options = null, CancellationToken cancellation = default) => this.DeleteAsync((ICondition)criteria, options, cancellation);
		public Task<int> DeleteAsync(ConditionCollection criteria, string schema, DataDeleteOptions options = null, CancellationToken cancellation = default) => this.DeleteAsync((ICondition)criteria, schema, options, cancellation);

		protected virtual Task<int> OnDeleteAsync(ICondition criteria, ISchema schema, DataDeleteOptions options, CancellationToken cancellation)
		{
			if(criteria == null)
				throw new NotSupportedException("The criteria cann't is null on delete operation.");

			return this.DataAccess.DeleteAsync(this.Name, criteria, schema, options, ctx => this.OnDeleting(ctx), ctx => this.OnDeleted(ctx), cancellation);
		}
		#endregion

		#region 插入方法
		public Task<int> InsertAsync(object data, DataInsertOptions options = null, CancellationToken cancellation = default)
		{
			return this.InsertAsync(data, string.Empty, options, cancellation);
		}

		public Task<int> InsertAsync(object data, string schema, DataInsertOptions options = null, CancellationToken cancellation = default)
		{
			//确认是否可以执行该操作
			this.EnsureInsert(options);

			if(data == null)
				return Task.FromResult(0);

			//构建数据操作的选项对象
			if(options == null)
				options = new DataInsertOptions();

			//进行授权验证
			this.Authorize(DataServiceMethod.Insert(), options);

			//将当前插入数据对象转换成数据字典
			var dictionary = DataDictionary.GetDictionary<TModel>(data);

			//解析数据模式表达式
			var schematic = this.GetSchema(schema, data.GetType());

			//验证待新增的数据
			this.OnValidate(DataServiceMethod.Insert(), schematic, dictionary, options);

			return this.OnInsertAsync(dictionary, schematic, options, cancellation);
		}

		public Task<int> InsertManyAsync(IEnumerable items, DataInsertOptions options = null, CancellationToken cancellation = default)
		{
			return this.InsertManyAsync(items, string.Empty, options, cancellation);
		}

		public Task<int> InsertManyAsync(IEnumerable items, string schema, DataInsertOptions options = null, CancellationToken cancellation = default)
		{
			//确认是否可以执行该操作
			this.EnsureInsert(options);

			if(items == null)
				return Task.FromResult(0);

			//构建数据操作的选项对象
			if(options == null)
				options = new DataInsertOptions();

			//进行授权验证
			this.Authorize(DataServiceMethod.InsertMany(), options);

			//将当前插入数据集合对象转换成数据字典集合
			var dictionares = DataDictionary.GetDictionaries<TModel>(items);

			//解析数据模式表达式
			var schematic = this.GetSchema(schema, Common.TypeExtension.GetElementType(items.GetType()));

			foreach(var dictionary in dictionares)
			{
				//验证待新增的数据
				this.OnValidate(DataServiceMethod.InsertMany(), schematic, dictionary, options);
			}

			return this.OnInsertManyAsync(dictionares, schematic, options, cancellation);
		}

		protected virtual Task<int> OnInsertAsync(IDataDictionary<TModel> data, ISchema schema, DataInsertOptions options, CancellationToken cancellation)
		{
			if(data == null || data.Data == null || !data.HasChanges())
				return Task.FromResult(0);

			//执行数据引擎的插入操作
			return this.DataAccess.InsertAsync(this.Name, data, schema, options, ctx => this.OnInserting(ctx), ctx => this.OnInserted(ctx), cancellation);
		}

		protected virtual Task<int> OnInsertManyAsync(IEnumerable<IDataDictionary<TModel>> items, ISchema schema, DataInsertOptions options, CancellationToken cancellation = default)
		{
			if(items == null)
				return Task.FromResult(0);

			//执行数据引擎的插入操作
			return this.DataAccess.InsertManyAsync(this.Name, items, schema, options, ctx => this.OnInserting(ctx), ctx => this.OnInserted(ctx), cancellation);
		}
		#endregion

		#region 增改方法
		public Task<int> UpsertAsync(object data, DataUpsertOptions options = null, CancellationToken cancellation = default) =>
			this.UpsertAsync(data, string.Empty, options, cancellation);
		public Task<int> UpsertAsync(object data, string schema, DataUpsertOptions options = null, CancellationToken cancellation = default)
		{
			//确认是否可以执行该操作
			this.EnsureUpsert(options);

			if(data == null)
				return Task.FromResult(0);

			//构建数据操作的选项对象
			if(options == null)
				options = new DataUpsertOptions();

			//进行授权验证
			this.Authorize(DataServiceMethod.Upsert(), options);

			//将当前复写数据对象转换成数据字典
			var dictionary = DataDictionary.GetDictionary<TModel>(data);

			//解析数据模式表达式
			var schematic = this.GetSchema(schema, data.GetType());

			//验证待复写的数据
			this.OnValidate(DataServiceMethod.Upsert(), schematic, dictionary, options);

			return this.OnUpsertAsync(dictionary, schematic, options, cancellation);
		}

		public Task<int> UpsertManyAsync(IEnumerable items, DataUpsertOptions options = null, CancellationToken cancellation = default) =>
			this.UpsertManyAsync(items, string.Empty, options, cancellation);
		public Task<int> UpsertManyAsync(IEnumerable items, string schema, DataUpsertOptions options = null, CancellationToken cancellation = default)
		{
			//确认是否可以执行该操作
			this.EnsureUpsert(options);

			if(items == null)
				return Task.FromResult(0);

			//构建数据操作的选项对象
			if(options == null)
				options = new DataUpsertOptions();

			//进行授权验证
			this.Authorize(DataServiceMethod.UpsertMany(), options);

			//将当前复写数据集合对象转换成数据字典集合
			var dictionares = DataDictionary.GetDictionaries<TModel>(items);

			//解析数据模式表达式
			var schematic = this.GetSchema(schema, Common.TypeExtension.GetElementType(items.GetType()));

			foreach(var dictionary in dictionares)
			{
				//验证待复写的数据
				this.OnValidate(DataServiceMethod.UpsertMany(), schematic, dictionary, options);
			}

			return this.OnUpsertManyAsync(dictionares, schematic, options, cancellation);
		}

		public Task<int> UpsertManyAsync(string key, IEnumerable items, DataUpsertOptions options = null, CancellationToken cancellation = default) =>
			this.UpsertManyAsync(key, items, null, false, options, cancellation);
		public Task<int> UpsertManyAsync(string key, IEnumerable items, bool reset, DataUpsertOptions options = null, CancellationToken cancellation = default) =>
			this.UpsertManyAsync(key, items, null, reset, options, cancellation);
		public Task<int> UpsertManyAsync(string key, IEnumerable items, string schema, DataUpsertOptions options = null, CancellationToken cancellation = default) =>
			this.UpsertManyAsync(key, items, schema, false, options, cancellation);
		public Task<int> UpsertManyAsync(string key, IEnumerable items, string schema, bool reset, DataUpsertOptions options = null, CancellationToken cancellation = default)
		{
			//确认是否可以执行该操作
			this.EnsureUpsert(options);

			if(items == null)
				return Task.FromResult(0);

			//构建数据操作的选项对象
			if(options == null)
				options = new DataUpsertOptions();

			//进行授权验证
			this.Authorize(DataServiceMethod.UpsertMany(), options);

			//定义转换后的数据字典列表
			var dictionaries = new List<IDataDictionary<TModel>>();

			//解析数据模式表达式
			var schematic = this.GetSchema(schema, Common.TypeExtension.GetElementType(items.GetType()));

			foreach(var item in items)
			{
				if(item == null)
					continue;

				//处理数据模型
				var model = this.OnModel(key, item);
				if(model == null)
					continue;

				var dictionary = DataDictionary.GetDictionary<TModel>(model);
				dictionaries.Add(dictionary);

				//验证待复写的数据
				this.OnValidate(DataServiceMethod.UpsertMany(), schematic, dictionary, options);
			}

			if(reset && this.CanDelete)
				this.Delete(key, schema);

			return dictionaries.Count > 0 ? this.OnUpsertManyAsync(dictionaries, schematic, options, cancellation) : Task.FromResult(0);
		}

		public Task<int> UpsertManyAsync<TKey1>(TKey1 key1, IEnumerable items, DataUpsertOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1> => this.UpsertManyAsync(key1, items, null, false, options, cancellation);
		public Task<int> UpsertManyAsync<TKey1>(TKey1 key1, IEnumerable items, bool reset, DataUpsertOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1> => this.UpsertManyAsync(key1, items, null, reset, options, cancellation);
		public Task<int> UpsertManyAsync<TKey1>(TKey1 key1, IEnumerable items, string schema, DataUpsertOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1> => this.UpsertManyAsync(key1, items, schema, false, options, cancellation);
		public Task<int> UpsertManyAsync<TKey1>(TKey1 key1, IEnumerable items, string schema, bool reset, DataUpsertOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1> => this.UpsertManyAsync(new object[] { key1 }, items, schema, reset, options, cancellation);

		public Task<int> UpsertManyAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, IEnumerable items, DataUpsertOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2> => this.UpsertManyAsync(key1, key2, items, null, false, options, cancellation);
		public Task<int> UpsertManyAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, IEnumerable items, bool reset, DataUpsertOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2> => this.UpsertManyAsync(key1, key2, items, null, reset, options, cancellation);
		public Task<int> UpsertManyAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, IEnumerable items, string schema, DataUpsertOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2> => this.UpsertManyAsync(key1, key2, items, schema, false, options, cancellation);
		public Task<int> UpsertManyAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, IEnumerable items, string schema, bool reset, DataUpsertOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2> => this.UpsertManyAsync(new object[] { key1, key2 }, items, schema, reset, options, cancellation);

		public Task<int> UpsertManyAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, IEnumerable items, DataUpsertOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3> => this.UpsertManyAsync(key1, key2, key3, items, null, false, options, cancellation);
		public Task<int> UpsertManyAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, IEnumerable items, bool reset, DataUpsertOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3> => this.UpsertManyAsync(key1, key2, key3, items, null, reset, options, cancellation);
		public Task<int> UpsertManyAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, IEnumerable items, string schema, DataUpsertOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3> => this.UpsertManyAsync(key1, key2, key3, items, schema, false, options, cancellation);
		public Task<int> UpsertManyAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, IEnumerable items, string schema, bool reset, DataUpsertOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3> => this.UpsertManyAsync(new object[] { key1, key2, key3 }, items, schema, reset, options, cancellation);

		public Task<int> UpsertManyAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, IEnumerable items, DataUpsertOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4> => this.UpsertManyAsync(key1, key2, key3, key4, items, null, false, options, cancellation);
		public Task<int> UpsertManyAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, IEnumerable items, bool reset, DataUpsertOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4> => this.UpsertManyAsync(key1, key2, key3, key4, items, null, reset, options, cancellation);
		public Task<int> UpsertManyAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, IEnumerable items, string schema, DataUpsertOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4> => this.UpsertManyAsync(key1, key2, key3, key4, items, schema, false, options, cancellation);
		public Task<int> UpsertManyAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, IEnumerable items, string schema, bool reset, DataUpsertOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4> => this.UpsertManyAsync(new object[] { key1, key2, key3, key4 }, items, schema, reset, options, cancellation);

		public Task<int> UpsertManyAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, IEnumerable items, DataUpsertOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TKey5 : IEquatable<TKey5> => this.UpsertManyAsync(key1, key2, key3, key4, key5, items, null, false, options, cancellation);
		public Task<int> UpsertManyAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, IEnumerable items, bool reset, DataUpsertOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TKey5 : IEquatable<TKey5> => this.UpsertManyAsync(key1, key2, key3, key4, key5, items, null, reset, options, cancellation);
		public Task<int> UpsertManyAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, IEnumerable items, string schema, DataUpsertOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TKey5 : IEquatable<TKey5> => this.UpsertManyAsync(key1, key2, key3, key4, key5, items, schema, false, options, cancellation);
		public Task<int> UpsertManyAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, IEnumerable items, string schema, bool reset, DataUpsertOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TKey5 : IEquatable<TKey5> => this.UpsertManyAsync(new object[] { key1, key2, key3, key4, key5 }, items, schema, reset, options, cancellation);

		protected virtual Task<int> OnUpsertAsync(IDataDictionary<TModel> data, ISchema schema, DataUpsertOptions options, CancellationToken cancellation)
		{
			if(data == null || data.Data == null || !data.HasChanges())
				return Task.FromResult(0);

			//执行数据引擎的复写操作
			return this.DataAccess.UpsertAsync(this.Name, data, schema, options, ctx => this.OnUpserting(ctx), ctx => this.OnUpserted(ctx), cancellation);
		}

		protected virtual Task<int> OnUpsertManyAsync(IEnumerable<IDataDictionary<TModel>> items, ISchema schema, DataUpsertOptions options, CancellationToken cancellation)
		{
			if(items == null)
				return Task.FromResult(0);

			//执行数据引擎的复写操作
			return this.DataAccess.UpsertManyAsync(this.Name, items, schema, options, ctx => this.OnUpserting(ctx), ctx => this.OnUpserted(ctx), cancellation);
		}

		protected Task<int> UpsertManyAsync(object[] keys, IEnumerable items, string schema, bool reset, DataUpsertOptions options = null, CancellationToken cancellation = default)
		{
			//确认是否可以执行该操作
			this.EnsureUpsert(options);

			if(keys == null || keys.Length == 0)
				throw new DataArgumentException(nameof(keys));

			if(items == null)
				return Task.FromResult(0);

			//构建数据操作的选项对象
			if(options == null)
				options = new DataUpsertOptions();

			//进行授权验证
			this.Authorize(DataServiceMethod.UpsertMany(), options);

			//定义转换后的数据字典列表
			var dictionaries = new List<IDataDictionary<TModel>>();

			//解析数据模式表达式
			var schematic = this.GetSchema(schema, Common.TypeExtension.GetElementType(items.GetType()));

			foreach(var item in items)
			{
				if(item == null)
					continue;

				//处理数据模型
				var model = this.OnModel(keys, item);
				if(model == null)
					continue;

				var dictionary = DataDictionary.GetDictionary<TModel>(model);
				dictionaries.Add(dictionary);

				//验证待复写的数据
				this.OnValidate(DataServiceMethod.UpsertMany(), schematic, dictionary, options);
			}

			if(reset && this.CanDelete)
				this.Delete(this.OnCondition(DataServiceMethod.Delete(), keys, out _));

			return dictionaries.Count > 0 ? this.OnUpsertManyAsync(dictionaries, schematic, options, cancellation) : Task.FromResult(0);
		}
		#endregion

		#region 更新方法
		public Task<int> UpdateAsync(string key, object data, DataUpdateOptions options = null, CancellationToken cancellation = default) =>
			this.UpdateAsync(key, data, null, options, cancellation);
		public Task<int> UpdateAsync(string key, object data, string schema, DataUpdateOptions options = null, CancellationToken cancellation = default) =>
			this.UpdateAsync(data, this.ConvertKey(DataServiceMethod.Update(), key, out _), schema, options, cancellation);

		public Task<int> UpdateAsync<TKey1>(TKey1 key1, object data, DataUpdateOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1> => this.UpdateAsync(key1, null, data, options, cancellation);
		public Task<int> UpdateAsync<TKey1>(TKey1 key1, string schema, object data, DataUpdateOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1> => this.UpdateAsync(data, this.ConvertKey(DataServiceMethod.Update(), key1, out _), schema, options, cancellation);
		public Task<int> UpdateAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, object data, DataUpdateOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2> => this.UpdateAsync(key1, key2, null, data, options, cancellation);
		public Task<int> UpdateAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, object data, DataUpdateOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2> => this.UpdateAsync(data, this.ConvertKey(DataServiceMethod.Update(), key1, key2, out _), schema, options, cancellation);
		public Task<int> UpdateAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, object data, DataUpdateOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3> => this.UpdateAsync(key1, key2, key3, null, data, options, cancellation);
		public Task<int> UpdateAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, object data, DataUpdateOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3> => this.UpdateAsync(data, this.ConvertKey(DataServiceMethod.Update(), key1, key2, key3, out _), schema, options, cancellation);
		public Task<int> UpdateAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, object data, DataUpdateOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4> => this.UpdateAsync(key1, key2, key3, key4, null, data, options, cancellation);
		public Task<int> UpdateAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, object data, DataUpdateOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4> => this.UpdateAsync(data, this.ConvertKey(DataServiceMethod.Update(), key1, key2, key3, key4, out _), schema, options, cancellation);
		public Task<int> UpdateAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, object data, DataUpdateOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TKey5 : IEquatable<TKey5> => this.UpdateAsync(key1, key2, key3, key4, key5, null, data, options, cancellation);
		public Task<int> UpdateAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, object data, DataUpdateOptions options = null, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TKey5 : IEquatable<TKey5> => this.UpdateAsync(data, this.ConvertKey(DataServiceMethod.Update(), key1, key2, key3, key4, key5, out _), schema, options, cancellation);

		public Task<int> UpdateAsync(object data, DataUpdateOptions options = null, CancellationToken cancellation = default) =>
			this.UpdateAsync(data, (ICondition)null, string.Empty, options, cancellation);
		public Task<int> UpdateAsync(object data, string schema, DataUpdateOptions options = null, CancellationToken cancellation = default) =>
			this.UpdateAsync(data, (ICondition)null, schema, options, cancellation);
		public Task<int> UpdateAsync(object data, ICondition criteria, DataUpdateOptions options = null, CancellationToken cancellation = default) =>
			this.UpdateAsync(data, criteria, string.Empty, options, cancellation);
		public Task<int> UpdateAsync(object data, ICondition criteria, string schema, DataUpdateOptions options = null, CancellationToken cancellation = default)
		{
			//确认是否可以执行该操作
			this.EnsureUpdate(options);

			if(data == null)
				return Task.FromResult(0);

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
				var keys = this.DataAccess.Metadata.Entities.Get(this.Name).Key;

				if(keys != null && keys.Length > 0)
				{
					foreach(var key in keys)
					{
						criteria.Match(key.Name, c => dictionary.TrySetValue(c.Name, c.Value));
					}
				}
			}

			//修整过滤条件
			criteria = this.OnValidate(DataServiceMethod.Update(), criteria ?? this.GetUpdateKey(dictionary), options.Filter, options);

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

		public Task<int> UpdateAsync(object data, Data.Condition criteria, DataUpdateOptions options = null, CancellationToken cancellation = default) => this.UpdateAsync(data, (ICondition)criteria, options, cancellation);
		public Task<int> UpdateAsync(object data, Data.Condition criteria, string schema, DataUpdateOptions options = null, CancellationToken cancellation = default) => this.UpdateAsync(data, (ICondition)criteria, schema, options, cancellation);
		public Task<int> UpdateAsync(object data, ConditionCollection criteria, DataUpdateOptions options = null, CancellationToken cancellation = default) => this.UpdateAsync(data, (ICondition)criteria, options, cancellation);
		public Task<int> UpdateAsync(object data, ConditionCollection criteria, string schema, DataUpdateOptions options = null, CancellationToken cancellation = default) => this.UpdateAsync(data, (ICondition)criteria, schema, options, cancellation);

		public Task<int> UpdateManyAsync(IEnumerable items, DataUpdateOptions options = null, CancellationToken cancellation = default) =>
			this.UpdateManyAsync(items, string.Empty, options, cancellation);
		public Task<int> UpdateManyAsync(IEnumerable items, string schema, DataUpdateOptions options = null, CancellationToken cancellation = default)
		{
			//确认是否可以执行该操作
			this.EnsureUpdate(options);

			if(items == null)
				return Task.FromResult(0);

			//构建数据操作的选项对象
			if(options == null)
				options = new DataUpdateOptions();

			//进行授权验证
			this.Authorize(DataServiceMethod.UpdateMany(), options);

			//将当前更新数据集合对象转换成数据字典集合
			var dictionares = DataDictionary.GetDictionaries<TModel>(items);

			//解析数据模式表达式
			var schematic = this.GetSchema(schema, Common.TypeExtension.GetElementType(items.GetType()));

			foreach(var dictionary in dictionares)
			{
				//验证待更新的数据
				this.OnValidate(DataServiceMethod.UpdateMany(), schematic, dictionary, options);
			}

			return this.OnUpdateManyAsync(dictionares, schematic, options, cancellation);
		}

		protected virtual Task<int> OnUpdateAsync(IDataDictionary<TModel> data, ICondition criteria, ISchema schema, DataUpdateOptions options, CancellationToken cancellation)
		{
			if(data == null || data.Data == null || !data.HasChanges())
				return Task.FromResult(0);

			return this.DataAccess.UpdateAsync(this.Name, data, criteria, schema, options, ctx => this.OnUpdating(ctx), ctx => this.OnUpdated(ctx), cancellation);
		}

		protected virtual Task<int> OnUpdateManyAsync(IEnumerable<IDataDictionary<TModel>> items, ISchema schema, DataUpdateOptions options, CancellationToken cancellation)
		{
			if(items == null)
				return Task.FromResult(0);

			return this.DataAccess.UpdateManyAsync(this.Name, items, schema, options, ctx => this.OnUpdating(ctx), ctx => this.OnUpdated(ctx), cancellation);
		}
		#endregion

		#region 查询方法

		#region 键值查询
		public Task<object> GetAsync(string key, params Sorting[] sortings) => this.GetAsync(key, sortings, CancellationToken.None);
		public Task<object> GetAsync(string key, Sorting[] sortings, CancellationToken cancellation = default) => this.GetAsync(key, string.Empty, Paging.Disabled, null, sortings);
		public Task<object> GetAsync(string key, DataSelectOptions options, params Sorting[] sortings) => this.GetAsync(key, string.Empty, Paging.Disabled, options, sortings, CancellationToken.None);
		public Task<object> GetAsync(string key, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) => this.GetAsync(key, string.Empty, Paging.Disabled, options, sortings, cancellation);
		public Task<object> GetAsync(string key, Paging paging, params Sorting[] sortings) => this.GetAsync(key, string.Empty, paging, null, sortings, CancellationToken.None);
		public Task<object> GetAsync(string key, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) => this.GetAsync(key, string.Empty, paging, null, sortings, cancellation);
		public Task<object> GetAsync(string key, Paging paging, DataSelectOptions options, params Sorting[] sortings) => this.GetAsync(key, string.Empty, paging, options, sortings, CancellationToken.None);
		public Task<object> GetAsync(string key, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) => this.GetAsync(key, string.Empty, paging, options, sortings, cancellation);
		public Task<object> GetAsync(string key, string schema, params Sorting[] sortings) => this.GetAsync(key, schema, Paging.Disabled, null, sortings, CancellationToken.None);
		public Task<object> GetAsync(string key, string schema, Sorting[] sortings, CancellationToken cancellation = default) => this.GetAsync(key, schema, Paging.Disabled, null, sortings, cancellation);
		public Task<object> GetAsync(string key, string schema, DataSelectOptions options, params Sorting[] sortings) => this.GetAsync(key, schema, Paging.Disabled, options, sortings, CancellationToken.None);
		public Task<object> GetAsync(string key, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) => this.GetAsync(key, schema, Paging.Disabled, options, sortings, cancellation);
		public Task<object> GetAsync(string key, string schema, Paging paging, params Sorting[] sortings) => this.GetAsync(key, schema, paging, null, sortings, CancellationToken.None);
		public Task<object> GetAsync(string key, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) => this.GetAsync(key, schema, paging, null, sortings, cancellation);
		public Task<object> GetAsync(string key, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings) => this.GetAsync(key, schema, paging, options, sortings, CancellationToken.None);
		public Task<object> GetAsync(string key, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		{
			//构建数据操作的选项对象
			if(options == null)
				options = new DataSelectOptions();

			var criteria = this.ConvertKey(DataServiceMethod.Get(), key, out var singular);

			if(singular)
			{
				//进行授权验证
				this.Authorize(DataServiceMethod.Get(), options);

				//修整查询条件
				criteria = this.OnValidate(DataServiceMethod.Get(), criteria, options.Filter, options);

				//执行单条查询方法
				return this.OnGetAsync(criteria, this.GetSchema(schema), options, cancellation).ContinueWith(task => (object)task.Result);
			}

			return this.SelectAsync(criteria, schema, paging, options, sortings, cancellation).ContinueWith(task => (object)task.Result);
		}

		protected virtual async Task<TModel> OnGetAsync(ICondition criteria, ISchema schema, DataSelectOptions options, CancellationToken cancellation)
		{
			return (await this.DataAccess.SelectAsync<TModel>(this.Name, criteria, schema, null, options, null, ctx => this.OnGetting(ctx), ctx => this.OnGetted(ctx), cancellation)).FirstOrDefault();
		}
		#endregion

		#region 单键查询
		public Task<object> GetAsync<TKey1>(TKey1 key1, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, sortings, CancellationToken.None);
		public Task<object> GetAsync<TKey1>(TKey1 key1, Sorting[] sortings, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, string.Empty, null, null, sortings, cancellation);
		public Task<object> GetAsync<TKey1>(TKey1 key1, DataSelectOptions options, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, string.Empty, null, options, sortings, CancellationToken.None);
		public Task<object> GetAsync<TKey1>(TKey1 key1, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, string.Empty, null, options, sortings, cancellation);
		public Task<object> GetAsync<TKey1>(TKey1 key1, Paging paging, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, string.Empty, paging, null, sortings, CancellationToken.None);
		public Task<object> GetAsync<TKey1>(TKey1 key1, Paging paging, Sorting[] sortings, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, string.Empty, paging, null, sortings, cancellation);
		public Task<object> GetAsync<TKey1>(TKey1 key1, Paging paging, DataSelectOptions options, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, string.Empty, paging, options, sortings, CancellationToken.None);
		public Task<object> GetAsync<TKey1>(TKey1 key1, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, string.Empty, paging, options, sortings, cancellation);
		public Task<object> GetAsync<TKey1>(TKey1 key1, string schema, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, schema, null, null, sortings, CancellationToken.None);
		public Task<object> GetAsync<TKey1>(TKey1 key1, string schema, Sorting[] sortings, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, schema, null, null, sortings, cancellation);
		public Task<object> GetAsync<TKey1>(TKey1 key1, string schema, DataSelectOptions options, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, schema, null, options, sortings, CancellationToken.None);
		public Task<object> GetAsync<TKey1>(TKey1 key1, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, schema, null, options, sortings, cancellation);
		public Task<object> GetAsync<TKey1>(TKey1 key1, string schema, Paging paging, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, schema, paging, null, sortings, CancellationToken.None);
		public Task<object> GetAsync<TKey1>(TKey1 key1, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, schema, paging, null, sortings, cancellation);
		public Task<object> GetAsync<TKey1>(TKey1 key1, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1> => this.GetAsync<TKey1>(key1, schema, paging, options, sortings, CancellationToken.None);
		public Task<object> GetAsync<TKey1>(TKey1 key1, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
		{
			//构建数据操作的选项对象
			if(options == null)
				options = new DataSelectOptions();

			var criteria = this.ConvertKey(DataServiceMethod.Get(), key1, out var singular);

			if(singular)
			{
				//进行授权验证
				this.Authorize(DataServiceMethod.Get(), options);

				//修整查询条件
				criteria = this.OnValidate(DataServiceMethod.Get(), criteria, options.Filter, options);

				//执行单条查询方法
				return this.OnGetAsync(criteria, this.GetSchema(schema), options, cancellation).ContinueWith(task => (object)task.Result);
			}

			return this.SelectAsync(criteria, schema, paging, options, sortings, cancellation).ContinueWith(task => (object)task.Result);
		}
		#endregion

		#region 双键查询
		public Task<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, sortings, CancellationToken.None);
		public Task<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, Sorting[] sortings, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, string.Empty, null, null, sortings, cancellation);
		public Task<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, DataSelectOptions options, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, string.Empty, null, options, sortings, CancellationToken.None);
		public Task<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, string.Empty, null, options, sortings, cancellation);
		public Task<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, Paging paging, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, string.Empty, paging, null, sortings, CancellationToken.None);
		public Task<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, Paging paging, Sorting[] sortings, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, string.Empty, paging, null, sortings, cancellation);
		public Task<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, Paging paging, DataSelectOptions options, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, string.Empty, paging, options, sortings, CancellationToken.None);
		public Task<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, string.Empty, paging, options, sortings, cancellation);
		public Task<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, schema, null, null, sortings, CancellationToken.None);
		public Task<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Sorting[] sortings, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, schema, null, null, sortings, cancellation);
		public Task<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, DataSelectOptions options, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, schema, null, options, sortings, CancellationToken.None);
		public Task<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, schema, null, options, sortings, cancellation);
		public Task<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Paging paging, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, schema, paging, null, sortings, CancellationToken.None);
		public Task<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, schema, paging, null, sortings, cancellation);
		public Task<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2> => this.GetAsync<TKey1, TKey2>(key1, key2, schema, paging, options, sortings, CancellationToken.None);
		public Task<object> GetAsync<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
		{
			//构建数据操作的选项对象
			if(options == null)
				options = new DataSelectOptions();

			var criteria = this.ConvertKey(DataServiceMethod.Get(), key1, key2, out var singular);

			if(singular)
			{
				//进行授权验证
				this.Authorize(DataServiceMethod.Get(), options);

				//修整查询条件
				criteria = this.OnValidate(DataServiceMethod.Get(), criteria, options.Filter, options);

				//执行单条查询方法
				return this.OnGetAsync(criteria, this.GetSchema(schema), options, cancellation).ContinueWith(task => (object)task.Result);
			}

			return this.SelectAsync(criteria, schema, paging, options, sortings, cancellation).ContinueWith(task => (object)task.Result);
		}
		#endregion

		#region 三键查询
		public Task<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, sortings, CancellationToken.None);
		public Task<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, Sorting[] sortings, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, string.Empty, null, null, sortings, cancellation);
		public Task<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, DataSelectOptions options, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, string.Empty, null, options, sortings, CancellationToken.None);
		public Task<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, string.Empty, null, options, sortings, cancellation);
		public Task<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, Paging paging, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, string.Empty, paging, null, sortings, CancellationToken.None);
		public Task<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, Paging paging, Sorting[] sortings, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, string.Empty, paging, null, sortings, cancellation);
		public Task<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, Paging paging, DataSelectOptions options, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, string.Empty, paging, options, sortings, CancellationToken.None);
		public Task<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, null, paging, options, sortings, cancellation);
		public Task<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, schema, null, null, sortings, CancellationToken.None);
		public Task<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Sorting[] sortings, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, schema, null, null, sortings, cancellation);
		public Task<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, DataSelectOptions options, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, schema, null, options, sortings, CancellationToken.None);
		public Task<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, schema, null, options, sortings, cancellation);
		public Task<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Paging paging, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, schema, paging, null, sortings, CancellationToken.None);
		public Task<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, schema, paging, null, sortings, cancellation);
		public Task<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3> => this.GetAsync<TKey1, TKey2, TKey3>(key1, key2, key3, schema, paging, options, sortings, CancellationToken.None);
		public Task<object> GetAsync<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
		{
			//构建数据操作的选项对象
			if(options == null)
				options = new DataSelectOptions();

			var criteria = this.ConvertKey(DataServiceMethod.Get(), key1, key2, key3, out var singular);

			if(singular)
			{
				//进行授权验证
				this.Authorize(DataServiceMethod.Get(), options);

				//修整查询条件
				criteria = this.OnValidate(DataServiceMethod.Get(), criteria, options.Filter, options);

				//执行单条查询方法
				return this.OnGetAsync(criteria, this.GetSchema(schema), options, cancellation).ContinueWith(task => (object)task.Result);
			}

			return this.SelectAsync(criteria, schema, paging, options, sortings, cancellation).ContinueWith(task => (object)task.Result);
		}
		#endregion

		#region 四键查询
		public Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, sortings, CancellationToken.None);
		public Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, Sorting[] sortings, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, string.Empty, null, null, sortings, cancellation);
		public Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataSelectOptions options, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, string.Empty, null, options, sortings, CancellationToken.None);
		public Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, string.Empty, null, options, sortings, cancellation);
		public Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, Paging paging, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, string.Empty, paging, null, sortings, CancellationToken.None);
		public Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, Paging paging, Sorting[] sortings, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, string.Empty, paging, null, sortings, cancellation);
		public Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, Paging paging, DataSelectOptions options, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, string.Empty, paging, options, sortings, CancellationToken.None);
		public Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, string.Empty, paging, options, sortings, cancellation);
		public Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, schema, null, null, sortings, CancellationToken.None);
		public Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, Sorting[] sortings, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, schema, null, null, sortings, cancellation);
		public Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, DataSelectOptions options, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, schema, null, options, sortings, CancellationToken.None);
		public Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, schema, null, options, sortings, cancellation);
		public Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, Paging paging, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, schema, paging, null, sortings, CancellationToken.None);
		public Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, schema, paging, null, sortings, cancellation);
		public Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4> => this.GetAsync<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, schema, paging, options, sortings, CancellationToken.None);
		public Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
		{
			//构建数据操作的选项对象
			if(options == null)
				options = new DataSelectOptions();

			var criteria = this.ConvertKey(DataServiceMethod.Get(), key1, key2, key3, key4, out var singular);

			if(singular)
			{
				//进行授权验证
				this.Authorize(DataServiceMethod.Get(), options);

				//修整查询条件
				criteria = this.OnValidate(DataServiceMethod.Get(), criteria, options.Filter, options);

				//执行单条查询方法
				return this.OnGetAsync(criteria, this.GetSchema(schema), options, cancellation).ContinueWith(task => (object)task.Result);
			}

			return this.SelectAsync(criteria, schema, paging, options, sortings, cancellation).ContinueWith(task => (object)task.Result);
		}
		#endregion

		#region 五键查询
		public Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, sortings, CancellationToken.None);
		public Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, Sorting[] sortings, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, string.Empty, null, null, sortings, cancellation);
		public Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataSelectOptions options, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, string.Empty, null, options, sortings, CancellationToken.None);
		public Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, string.Empty, null, options, sortings, cancellation);
		public Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, Paging paging, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, string.Empty, paging, null, sortings, CancellationToken.None);
		public Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, Paging paging, Sorting[] sortings, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, string.Empty, paging, null, sortings, cancellation);
		public Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, Paging paging, DataSelectOptions options, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, string.Empty, paging, options, sortings, CancellationToken.None);
		public Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, string.Empty, paging, options, sortings, cancellation);
		public Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, schema, null, null, sortings, CancellationToken.None);
		public Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, Sorting[] sortings, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, schema, null, null, sortings, cancellation);
		public Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, DataSelectOptions options, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, schema, null, options, sortings, CancellationToken.None);
		public Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, schema, null, options, sortings, cancellation);
		public Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, Paging paging, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, schema, paging, null, sortings, CancellationToken.None);
		public Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, schema, paging, null, sortings, cancellation);
		public Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TKey5 : IEquatable<TKey5> => this.GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, schema, paging, options, sortings, CancellationToken.None);
		public Task<object> GetAsync<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TKey5 : IEquatable<TKey5>
		{
			//构建数据操作的选项对象
			if(options == null)
				options = new DataSelectOptions();

			var criteria = this.ConvertKey(DataServiceMethod.Get(), key1, key2, key3, key4, key5, out var singular);

			if(singular)
			{
				//进行授权验证
				this.Authorize(DataServiceMethod.Get(), options);

				//修整查询条件
				criteria = this.OnValidate(DataServiceMethod.Get(), criteria, options.Filter, options);

				//执行单条查询方法
				return this.OnGetAsync(criteria, this.GetSchema(schema), options, cancellation).ContinueWith(task => (object)task.Result);
			}

			return this.SelectAsync(criteria, schema, paging, options, sortings, cancellation).ContinueWith(task => (object)task.Result);
		}
		#endregion

		#region 普通查询
		Task<IEnumerable> IDataService.SelectAsync(CancellationToken cancellation) => this.SelectAsync(null, string.Empty, null, null, null, cancellation).ContinueWith(task => (IEnumerable)task.Result);
		Task<IEnumerable> IDataService.SelectAsync(DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync(null, string.Empty, null, options, sortings, CancellationToken.None).ContinueWith(task => (IEnumerable)task.Result);
		Task<IEnumerable> IDataService.SelectAsync(DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation) => this.SelectAsync(null, string.Empty, null, options, sortings, cancellation).ContinueWith(task => (IEnumerable)task.Result);
		Task<IEnumerable> IDataService.SelectAsync(ICondition criteria, params Sorting[] sortings) => this.SelectAsync(criteria, string.Empty, null, null, sortings, CancellationToken.None).ContinueWith(task => (IEnumerable)task.Result);
		Task<IEnumerable> IDataService.SelectAsync(ICondition criteria, Sorting[] sortings, CancellationToken cancellation) => this.SelectAsync(criteria, string.Empty, null, null, sortings, cancellation).ContinueWith(task => (IEnumerable)task.Result);
		Task<IEnumerable> IDataService.SelectAsync(ICondition criteria, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync(criteria, string.Empty, null, options, sortings, CancellationToken.None).ContinueWith(task => (IEnumerable)task.Result);
		Task<IEnumerable> IDataService.SelectAsync(ICondition criteria, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation) => this.SelectAsync(criteria, string.Empty, null, options, sortings, cancellation).ContinueWith(task => (IEnumerable)task.Result);
		Task<IEnumerable> IDataService.SelectAsync(ICondition criteria, Paging paging, params Sorting[] sortings) => this.SelectAsync(criteria, string.Empty, paging, null, sortings, CancellationToken.None).ContinueWith(task => (IEnumerable)task.Result);
		Task<IEnumerable> IDataService.SelectAsync(ICondition criteria, Paging paging, Sorting[] sortings, CancellationToken cancellation) => this.SelectAsync(criteria, string.Empty, paging, null, sortings, cancellation).ContinueWith(task => (IEnumerable)task.Result);
		Task<IEnumerable> IDataService.SelectAsync(ICondition criteria, Paging paging, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync(criteria, string.Empty, paging, options, sortings, CancellationToken.None).ContinueWith(task => (IEnumerable)task.Result);
		Task<IEnumerable> IDataService.SelectAsync(ICondition criteria, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation) => this.SelectAsync(criteria, string.Empty, paging, options, sortings, cancellation).ContinueWith(task => (IEnumerable)task.Result);
		Task<IEnumerable> IDataService.SelectAsync(ICondition criteria, string schema, params Sorting[] sortings) => this.SelectAsync(criteria, schema, null, null, sortings, CancellationToken.None).ContinueWith(task => (IEnumerable)task.Result);
		Task<IEnumerable> IDataService.SelectAsync(ICondition criteria, string schema, Sorting[] sortings, CancellationToken cancellation) => this.SelectAsync(criteria, schema, null, null, sortings, cancellation).ContinueWith(task => (IEnumerable)task.Result);
		Task<IEnumerable> IDataService.SelectAsync(ICondition criteria, string schema, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync(criteria, schema, null, options, sortings, CancellationToken.None).ContinueWith(task => (IEnumerable)task.Result);
		Task<IEnumerable> IDataService.SelectAsync(ICondition criteria, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation) => this.SelectAsync(criteria, schema, null, options, sortings, cancellation).ContinueWith(task => (IEnumerable)task.Result);
		Task<IEnumerable> IDataService.SelectAsync(ICondition criteria, string schema, Paging paging, params Sorting[] sortings) => this.SelectAsync(criteria, schema, paging, null, sortings, CancellationToken.None).ContinueWith(task => (IEnumerable)task.Result);
		Task<IEnumerable> IDataService.SelectAsync(ICondition criteria, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation) => this.SelectAsync(criteria, schema, paging, null, sortings, cancellation).ContinueWith(task => (IEnumerable)task.Result);
		Task<IEnumerable> IDataService.SelectAsync(ICondition criteria, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync(criteria, schema, paging, options, sortings, CancellationToken.None).ContinueWith(task => (IEnumerable)task.Result);
		Task<IEnumerable> IDataService.SelectAsync(ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation) => this.SelectAsync(criteria, schema, paging, options, sortings, cancellation).ContinueWith(task => (IEnumerable)task.Result);

		public Task<IEnumerable<TModel>> SelectAsync(CancellationToken cancellation = default) => this.SelectAsync(null, string.Empty, null, null, null, cancellation);
		public Task<IEnumerable<TModel>> SelectAsync(DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync(null, string.Empty, null, options, sortings, CancellationToken.None);
		public Task<IEnumerable<TModel>> SelectAsync(DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync(null, string.Empty, null, options, sortings, cancellation);
		public Task<IEnumerable<TModel>> SelectAsync(ICondition criteria, params Sorting[] sortings) => this.SelectAsync(criteria, string.Empty, null, null, sortings, CancellationToken.None);
		public Task<IEnumerable<TModel>> SelectAsync(ICondition criteria, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync(criteria, string.Empty, null, null, sortings, cancellation);
		public Task<IEnumerable<TModel>> SelectAsync(ICondition criteria, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync(criteria, string.Empty, null, options, sortings, CancellationToken.None);
		public Task<IEnumerable<TModel>> SelectAsync(ICondition criteria, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync(criteria, string.Empty, null, options, sortings, cancellation);
		public Task<IEnumerable<TModel>> SelectAsync(ICondition criteria, Paging paging, params Sorting[] sortings) => this.SelectAsync(criteria, string.Empty, paging, null, sortings, CancellationToken.None);
		public Task<IEnumerable<TModel>> SelectAsync(ICondition criteria, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync(criteria, string.Empty, paging, null, sortings, cancellation);
		public Task<IEnumerable<TModel>> SelectAsync(ICondition criteria, Paging paging, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync(criteria, string.Empty, paging, options, sortings, CancellationToken.None);
		public Task<IEnumerable<TModel>> SelectAsync(ICondition criteria, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync(criteria, string.Empty, paging, options, sortings, cancellation);
		public Task<IEnumerable<TModel>> SelectAsync(ICondition criteria, string schema, params Sorting[] sortings) => this.SelectAsync(criteria, schema, null, null, sortings, CancellationToken.None);
		public Task<IEnumerable<TModel>> SelectAsync(ICondition criteria, string schema, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync(criteria, schema, null, null, sortings, cancellation);
		public Task<IEnumerable<TModel>> SelectAsync(ICondition criteria, string schema, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync(criteria, schema, null, options, sortings, CancellationToken.None);
		public Task<IEnumerable<TModel>> SelectAsync(ICondition criteria, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync(criteria, schema, null, options, sortings, cancellation);
		public Task<IEnumerable<TModel>> SelectAsync(ICondition criteria, string schema, Paging paging, params Sorting[] sortings) => this.SelectAsync(criteria, schema, paging, null, sortings, CancellationToken.None);
		public Task<IEnumerable<TModel>> SelectAsync(ICondition criteria, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync(criteria, schema, paging, null, sortings, cancellation);
		public Task<IEnumerable<TModel>> SelectAsync(ICondition criteria, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync(criteria, schema, paging, options, sortings, CancellationToken.None);
		public Task<IEnumerable<TModel>> SelectAsync(ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		{
			//构建数据操作的选项对象
			if(options == null)
				options = new DataSelectOptions();

			//进行授权验证
			this.Authorize(DataServiceMethod.Select(), options);

			//修整查询条件
			criteria = this.OnValidate(DataServiceMethod.Select(), criteria, options.Filter, options);

			//执行查询方法
			return this.OnSelectAsync(criteria, this.GetSchema(schema, typeof(TModel)), paging, sortings, options, cancellation);
		}

		protected virtual Task<IEnumerable<TModel>> OnSelectAsync(ICondition criteria, ISchema schema, Paging paging, Sorting[] sortings, DataSelectOptions options, CancellationToken cancellation)
		{
			//如果没有指定排序设置则应用默认排序规则
			if(sortings == null || sortings.Length == 0)
				sortings = this.GetDefaultSortings();

			return this.DataAccess.SelectAsync<TModel>(this.Name, criteria, schema, paging, options, sortings, ctx => this.OnSelecting(ctx), ctx => this.OnSelected(ctx), cancellation);
		}
		#endregion

		#region 分组查询
		public Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, params Sorting[] sortings) => this.SelectAsync<T>(grouping, null, string.Empty, null, null, sortings, CancellationToken.None);
		public Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync<T>(grouping, null, string.Empty, null, null, sortings, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync<T>(grouping, null, string.Empty, null, options, sortings, CancellationToken.None);
		public Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync<T>(grouping, null, string.Empty, null, options, sortings, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, Paging paging, params Sorting[] sortings) => this.SelectAsync<T>(grouping, null, string.Empty, paging, null, sortings, CancellationToken.None);
		public Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync<T>(grouping, null, string.Empty, paging, null, sortings, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, Paging paging, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync<T>(grouping, null, string.Empty, paging, options, sortings, CancellationToken.None);
		public Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync<T>(grouping, null, string.Empty, paging, options, sortings, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, string schema, params Sorting[] sortings) => this.SelectAsync<T>(grouping, null, schema, null, null, sortings, CancellationToken.None);
		public Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, string schema, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync<T>(grouping, null, schema, null, null, sortings, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, string schema, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync<T>(grouping, null, schema, null, options, sortings, CancellationToken.None);
		public Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync<T>(grouping, null, schema, null, options, sortings, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, string schema, Paging paging, params Sorting[] sortings) => this.SelectAsync<T>(grouping, null, schema, paging, null, sortings, CancellationToken.None);
		public Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync<T>(grouping, null, schema, paging, null, sortings, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync<T>(grouping, null, schema, paging, options, sortings, CancellationToken.None);
		public Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync<T>(grouping, null, schema, paging, options, sortings, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, ICondition criteria, Paging paging, params Sorting[] sortings) => this.SelectAsync<T>(grouping, criteria, string.Empty, paging, null, sortings, CancellationToken.None);
		public Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, ICondition criteria, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync<T>(grouping, criteria, string.Empty, paging, null, sortings, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema = null, params Sorting[] sortings) => this.SelectAsync<T>(grouping, criteria, schema, null, null, sortings, CancellationToken.None);
		public Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync<T>(grouping, criteria, schema, null, null, sortings, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync<T>(grouping, criteria, schema, null, options, sortings, CancellationToken.None);
		public Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync<T>(grouping, criteria, schema, null, options, sortings, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema, Paging paging, params Sorting[] sortings) => this.SelectAsync<T>(grouping, criteria, schema, paging, null, sortings, CancellationToken.None);
		public Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema, Paging paging, Sorting[] sortings, CancellationToken cancellation = default) => this.SelectAsync<T>(grouping, criteria, schema, paging, null, sortings, cancellation);
		public Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings) => this.SelectAsync<T>(grouping, criteria, schema, paging, options, sortings, CancellationToken.None);
		public Task<IEnumerable<T>> SelectAsync<T>(Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options, Sorting[] sortings, CancellationToken cancellation = default)
		{
			//构建数据操作的选项对象
			if(options == null)
				options = new DataSelectOptions();

			//进行授权验证
			this.Authorize(DataServiceMethod.Select(), options);

			//修整查询条件
			criteria = this.OnValidate(DataServiceMethod.Select(), criteria, options.Filter, options);

			//执行查询方法
			return this.OnSelectAsync<T>(grouping, criteria, string.IsNullOrWhiteSpace(schema) ? null : this.GetSchema(schema, typeof(TModel)), paging, sortings, options, cancellation);
		}

		protected virtual Task<IEnumerable<T>> OnSelectAsync<T>(Grouping grouping, ICondition criteria, ISchema schema, Paging paging, Sorting[] sortings, DataSelectOptions options, CancellationToken cancellation)
		{
			return this.DataAccess.SelectAsync<T>(this.Name, grouping, criteria, schema, paging, options, sortings, ctx => this.OnSelecting(ctx), ctx => this.OnSelected(ctx), cancellation);
		}
		#endregion

		#endregion
	}
}
