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
using System.Data;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace Zongsoft.Data
{
	public class DataServiceBase<TModel> : IDataService<TModel>
	{
		#region 事件定义
		public event EventHandler<DataGettedEventArgs<TModel>> Getted;
		public event EventHandler<DataGettingEventArgs<TModel>> Getting;
		public event EventHandler<DataExecutedEventArgs> Executed;
		public event EventHandler<DataExecutingEventArgs> Executing;
		public event EventHandler<DataExistedEventArgs> Existed;
		public event EventHandler<DataExistingEventArgs> Existing;
		public event EventHandler<DataAggregatedEventArgs> Aggregated;
		public event EventHandler<DataAggregatingEventArgs> Aggregating;
		public event EventHandler<DataIncrementedEventArgs> Incremented;
		public event EventHandler<DataIncrementingEventArgs> Incrementing;
		public event EventHandler<DataDeletedEventArgs> Deleted;
		public event EventHandler<DataDeletingEventArgs> Deleting;
		public event EventHandler<DataInsertedEventArgs> Inserted;
		public event EventHandler<DataInsertingEventArgs> Inserting;
		public event EventHandler<DataUpsertedEventArgs> Upserted;
		public event EventHandler<DataUpsertingEventArgs> Upserting;
		public event EventHandler<DataUpdatedEventArgs> Updated;
		public event EventHandler<DataUpdatingEventArgs> Updating;
		public event EventHandler<DataSelectedEventArgs> Selected;
		public event EventHandler<DataSelectingEventArgs> Selecting;
		#endregion

		#region 成员字段
		private string _name;
		private IDataAccess _dataAccess;
		private IDataSearcher<TModel> _searcher;
		private IServiceProvider _serviceProvider;
		#endregion

		#region 构造函数
		protected DataServiceBase(IServiceProvider serviceProvider, IDataSearcher<TModel> searcher = null)
		{
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
			_dataAccess = (IDataAccess)serviceProvider.GetService(typeof(IDataAccess)) ?? ((IDataAccessProvider)serviceProvider.GetService(typeof(IDataAccessProvider)))?.GetAccessor(null);

			//创建数据搜索器
			_searcher = searcher ?? new DataSearcher<TModel>(this);
		}

		protected DataServiceBase(string name, IServiceProvider serviceProvider, IDataSearcher<TModel> searcher = null)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			_name = name.Trim();
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
			_dataAccess = (IDataAccess)serviceProvider.GetService(typeof(IDataAccess)) ?? ((IDataAccessProvider)serviceProvider.GetService(typeof(IDataAccessProvider)))?.GetAccessor(null);

			//创建数据搜索器
			_searcher = searcher ?? new DataSearcher<TModel>(this);
		}
		#endregion

		#region 公共属性
		public string Name
		{
			get
			{
				if(string.IsNullOrEmpty(_name))
				{
					var dataAccess = this.DataAccess;

					if(dataAccess != null && dataAccess.Naming != null)
						_name = dataAccess.Naming.Get<TModel>();
				}

				return _name;
			}
			protected set
			{
				if(string.IsNullOrWhiteSpace(value))
					throw new ArgumentNullException();

				_name = value.Trim();
			}
		}

		public virtual bool CanDelete { get => true; }

		public virtual bool CanInsert { get => true; }

		public virtual bool CanUpdate { get => true; }

		public virtual bool CanUpsert { get => true; }

		public IDataAccess DataAccess
		{
			get => _dataAccess;
			set => _dataAccess = value ?? throw new ArgumentNullException();
		}

		public IDataSearcher<TModel> Searcher
		{
			get => _searcher;
			set => _searcher = value ?? throw new ArgumentNullException();
		}

		public IServiceProvider ServiceProvider
		{
			get => _serviceProvider;
			set => _serviceProvider = value ?? throw new ArgumentNullException();
		}
		#endregion

		#region 保护属性
		protected virtual System.Security.Claims.ClaimsPrincipal Principal
		{
			get => Services.ApplicationContext.Current?.Principal;
		}
		#endregion

		#region 授权验证
		protected virtual void Authorize(Method method, IDataOptions options)
		{
			if(Security.ClaimsPrincipalExtension.IsAnonymous(this.Principal))
				throw new Security.Membership.AuthorizationException();
		}
		#endregion

		#region 执行方法
		public IEnumerable<T> Execute<T>(string name, IDictionary<string, object> inParameters, IDataExecuteOptions options = null)
		{
			return this.Execute<T>(name, inParameters, out _, options);
		}

		public IEnumerable<T> Execute<T>(string name, IDictionary<string, object> inParameters, out IDictionary<string, object> outParameters, IDataExecuteOptions options = null)
		{
			//构建数据操作的选项对象
			if(options == null)
				options = new DataExecuteOptions();

			//进行授权验证
			this.Authorize(Method.Execute(), options);

			return this.OnExecute<T>(name, inParameters, out outParameters, options);
		}

		protected virtual IEnumerable<T> OnExecute<T>(string name, IDictionary<string, object> inParameters, out IDictionary<string, object> outParameters, IDataExecuteOptions options)
		{
			return this.DataAccess.Execute<T>(name, inParameters, out outParameters, options, ctx => this.OnExecuting(ctx), ctx => this.OnExecuted(ctx));
		}

		public object ExecuteScalar(string name, IDictionary<string, object> inParameters, IDataExecuteOptions options = null)
		{
			return this.ExecuteScalar(name, inParameters, out _, options);
		}

		public object ExecuteScalar(string name, IDictionary<string, object> inParameters, out IDictionary<string, object> outParameters, IDataExecuteOptions options = null)
		{
			//构建数据操作的选项对象
			if(options == null)
				options = new DataExecuteOptions();

			//进行授权验证
			this.Authorize(Method.Execute(), options);

			return this.OnExecuteScalar(name, inParameters, out outParameters, options);
		}

		protected virtual object OnExecuteScalar(string name, IDictionary<string, object> inParameters, out IDictionary<string, object> outParameters, IDataExecuteOptions options)
		{
			return this.DataAccess.ExecuteScalar(name, inParameters, out outParameters, options, ctx => this.OnExecuting(ctx), ctx => this.OnExecuted(ctx));
		}
		#endregion

		#region 存在方法
		public bool Exists<TKey>(TKey key, string filter = null, IDataExistsOptions options = null)
		{
			return this.Exists(this.ConvertKey(Method.Exists(), key, filter, out _), options);
		}

		public bool Exists<TKey1, TKey2>(TKey1 key1, TKey2 key2, string filter = null, IDataExistsOptions options = null)
		{
			return this.Exists(this.ConvertKey(Method.Exists(), key1, key2, filter, out _), options);
		}

		public bool Exists<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string filter = null, IDataExistsOptions options = null)
		{
			return this.Exists(this.ConvertKey(Method.Exists(), key1, key2, key3, filter, out _), options);
		}

		public bool Exists(ICondition criteria, IDataExistsOptions options = null)
		{
			//构建数据操作的选项对象
			if(options == null)
				options = new DataExistsOptions();

			//进行授权验证
			this.Authorize(Method.Exists(), options);

			//修整查询条件
			criteria = this.OnValidate(Method.Exists(), criteria);

			//执行存在操作
			return this.OnExists(criteria, options);
		}

		protected virtual bool OnExists(ICondition criteria, IDataExistsOptions options)
		{
			return this.DataAccess.Exists(this.Name, criteria, options, ctx => this.OnExisting(ctx), ctx => this.OnExisted(ctx));
		}
		#endregion

		#region 聚合方法
		public int Count(ICondition criteria = null, string member = null, IDataAggregateOptions options = null)
		{
			//构建数据操作的选项对象
			if(options == null)
				options = new DataAggregateOptions();

			//进行授权验证
			this.Authorize(Method.Count(), options);

			//修整查询条件
			criteria = this.OnValidate(Method.Count(), criteria);

			//执行聚合操作
			return (int)(this.OnAggregate(new DataAggregate(DataAggregateFunction.Count, member), criteria, options) ?? 0d);
		}

		public int Count<TKey>(TKey key, string member = null, string filter = null, IDataAggregateOptions options = null)
		{
			return this.Count(this.ConvertKey(Method.Count(), key, filter, out _), member, options);
		}

		public int Count<TKey1, TKey2>(TKey1 key1, TKey2 key2, string member = null, string filter = null, IDataAggregateOptions options = null)
		{
			return this.Count(this.ConvertKey(Method.Count(), key1, key2, filter, out _), member, options);
		}

		public int Count<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string member = null, string filter = null, IDataAggregateOptions options = null)
		{
			return this.Count(this.ConvertKey(Method.Count(), key1, key2, key3, filter, out _), member, options);
		}

		public double? Aggregate(DataAggregateFunction function, string member, ICondition criteria = null, IDataAggregateOptions options = null)
		{
			//构建数据操作的选项对象
			if(options == null)
				options = new DataAggregateOptions();

			//进行授权验证
			this.Authorize(Method.Aggregate(function), options);

			//修整查询条件
			criteria = this.OnValidate(Method.Aggregate(function), criteria);

			//执行聚合操作
			return this.OnAggregate(new DataAggregate(function, member), criteria, options);
		}

		public double? Aggregate<TKey>(TKey key, DataAggregateFunction function, string member, string filter = null, IDataAggregateOptions options = null)
		{
			return this.Aggregate(function, member, this.ConvertKey(Method.Aggregate(function), key, filter, out _), options);
		}

		public double? Aggregate<TKey1, TKey2>(TKey1 key1, TKey2 key2, DataAggregateFunction function, string member, string filter = null, IDataAggregateOptions options = null)
		{
			return this.Aggregate(function, member, this.ConvertKey(Method.Aggregate(function), key1, key2, filter, out _), options);
		}

		public double? Aggregate<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, DataAggregateFunction function, string member, string filter = null, IDataAggregateOptions options = null)
		{
			return this.Aggregate(function, member, this.ConvertKey(Method.Aggregate(function), key1, key2, key3, filter, out _), options);
		}

		protected virtual double? OnAggregate(DataAggregate aggregate, ICondition criteria, IDataAggregateOptions options)
		{
			return this.DataAccess.Aggregate(this.Name, aggregate, criteria, options, ctx => this.OnAggregating(ctx), ctx => this.OnAggregated(ctx));
		}
		#endregion

		#region 递增方法
		public long Decrement(string member, ICondition criteria, IDataIncrementOptions options)
		{
			return this.Decrement(member, criteria, 1, options);
		}

		public long Decrement(string member, ICondition criteria, int interval = 1, IDataIncrementOptions options = null)
		{
			return this.Increment(member, criteria, -interval, options);
		}

		public long Increment(string member, ICondition criteria, IDataIncrementOptions options)
		{
			return this.Increment(member, criteria, 1, options);
		}

		public long Increment(string member, ICondition criteria, int interval = 1, IDataIncrementOptions options = null)
		{
			//构建数据操作的选项对象
			if(options == null)
				options = new DataIncrementOptions();

			//进行授权验证
			this.Authorize(Method.Increment(), options);

			//修整查询条件
			criteria = this.OnValidate(Method.Increment(), criteria);

			//执行递增操作
			return this.OnIncrement(member, criteria, interval, options);
		}

		protected virtual long OnIncrement(string member, ICondition criteria, int interval, IDataIncrementOptions options)
		{
			return this.DataAccess.Increment(this.Name, member, criteria, interval, options, ctx => this.OnIncrementing(ctx), ctx => this.OnIncremented(ctx));
		}
		#endregion

		#region 删除方法
		public int Delete<TKey>(TKey key, IDataDeleteOptions options = null)
		{
			return this.Delete<TKey>(key, null, options);
		}

		public int Delete<TKey>(TKey key, string schema, IDataDeleteOptions options = null)
		{
			//确认是否可以执行该操作
			this.EnsureDelete();

			//构建数据操作的选项对象
			if(options == null)
				options = new DataDeleteOptions();

			//进行授权验证
			this.Authorize(Method.Delete(), options);

			//将删除键转换成条件对象，并进行修整
			var criteria = this.OnValidate(Method.Delete(), this.ConvertKey(Method.Delete(), key, null, out _));

			//执行删除操作
			return this.OnDelete(criteria, this.GetSchema(schema), options);
		}

		public int Delete<TKey1, TKey2>(TKey1 key1, TKey2 key2, IDataDeleteOptions options = null)
		{
			return this.Delete<TKey1, TKey2>(key1, key2, null, options);
		}

		public int Delete<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, IDataDeleteOptions options = null)
		{
			//确认是否可以执行该操作
			this.EnsureDelete();

			//构建数据操作的选项对象
			if(options == null)
				options = new DataDeleteOptions();

			//进行授权验证
			this.Authorize(Method.Delete(), options);

			//将删除键转换成条件对象，并进行修整
			var criteria = this.OnValidate(Method.Delete(), this.ConvertKey(Method.Delete(), key1, key2, null, out _));

			//执行删除操作
			return this.OnDelete(criteria, this.GetSchema(schema), options);
		}

		public int Delete<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, IDataDeleteOptions options = null)
		{
			return this.Delete<TKey1, TKey2, TKey3>(key1, key2, key3, null, options);
		}

		public int Delete<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, IDataDeleteOptions options = null)
		{
			//确认是否可以执行该操作
			this.EnsureDelete();

			//构建数据操作的选项对象
			if(options == null)
				options = new DataDeleteOptions();

			//进行授权验证
			this.Authorize(Method.Delete(), options);

			//将删除键转换成条件对象，并进行修整
			var criteria = this.OnValidate(Method.Delete(), this.ConvertKey(Method.Delete(), key1, key2, key3, null, out _));

			//执行删除操作
			return this.OnDelete(criteria, this.GetSchema(schema), options);
		}

		public int Delete(ICondition criteria, IDataDeleteOptions options = null)
		{
			return this.Delete(criteria, null, options);
		}

		public int Delete(ICondition criteria, string schema, IDataDeleteOptions options = null)
		{
			//确认是否可以执行该操作
			this.EnsureDelete();

			//构建数据操作的选项对象
			if(options == null)
				options = new DataDeleteOptions();

			//进行授权验证
			this.Authorize(Method.Delete(), options);

			//修整删除条件
			criteria = this.OnValidate(Method.Delete(), criteria);

			//执行删除操作
			return this.OnDelete(criteria, this.GetSchema(schema), options);
		}

		protected virtual int OnDelete(ICondition criteria, ISchema schema, IDataDeleteOptions options)
		{
			if(criteria == null)
				throw new NotSupportedException("The criteria cann't is null on delete operation.");

			return this.DataAccess.Delete(this.Name, criteria, schema, options, ctx => this.OnDeleting(ctx), ctx => this.OnDeleted(ctx));
		}
		#endregion

		#region 插入方法
		public int Insert(object data, IDataInsertOptions options = null)
		{
			return this.Insert(data, null, options);
		}

		public int Insert(object data, string schema, IDataInsertOptions options = null)
		{
			//确认是否可以执行该操作
			this.EnsureInsert();

			if(data == null)
				return 0;

			//构建数据操作的选项对象
			if(options == null)
				options = new DataInsertOptions();

			//进行授权验证
			this.Authorize(Method.Insert(), options);

			//将当前插入数据对象转换成数据字典
			var dictionary = DataDictionary.GetDictionary<TModel>(data);

			//解析数据模式表达式
			var schematic = this.GetSchema(schema, data.GetType());

			//验证待新增的数据
			this.OnValidate(Method.Insert(), schematic, dictionary);

			return this.OnInsert(dictionary, schematic, options);
		}

		protected virtual int OnInsert(IDataDictionary<TModel> data, ISchema schema, IDataInsertOptions options)
		{
			if(data == null || data.Data == null)
				return 0;

			//执行数据引擎的插入操作
			return this.DataAccess.Insert(this.Name, data, schema, options, ctx => this.OnInserting(ctx), ctx => this.OnInserted(ctx));
		}

		public int InsertMany(IEnumerable items, IDataInsertOptions options = null)
		{
			return this.InsertMany(items, null, options);
		}

		public int InsertMany(IEnumerable items, string schema, IDataInsertOptions options = null)
		{
			//确认是否可以执行该操作
			this.EnsureInsert();

			if(items == null)
				return 0;

			//构建数据操作的选项对象
			if(options == null)
				options = new DataInsertOptions();

			//进行授权验证
			this.Authorize(Method.InsertMany(), options);

			//将当前插入数据集合对象转换成数据字典集合
			var dictionares = DataDictionary.GetDictionaries<TModel>(items);

			//解析数据模式表达式
			var schematic = this.GetSchema(schema, Common.TypeExtension.GetElementType(items.GetType()));

			foreach(var dictionary in dictionares)
			{
				//验证待新增的数据
				this.OnValidate(Method.InsertMany(), schematic, dictionary);
			}

			return this.OnInsertMany(dictionares, schematic, options);
		}

		protected virtual int OnInsertMany(IEnumerable<IDataDictionary<TModel>> items, ISchema schema, IDataInsertOptions options)
		{
			if(items == null)
				return 0;

			//执行数据引擎的插入操作
			return this.DataAccess.InsertMany(this.Name, items, schema, options, ctx => this.OnInserting(ctx), ctx => this.OnInserted(ctx));
		}
		#endregion

		#region 增改方法
		public int Upsert(object data, IDataUpsertOptions options = null)
		{
			return this.Upsert(data, null, options);
		}

		public int Upsert(object data, string schema, IDataUpsertOptions options = null)
		{
			//确认是否可以执行该操作
			this.EnsureUpsert();

			if(data == null)
				return 0;

			//构建数据操作的选项对象
			if(options == null)
				options = new DataUpsertOptions();

			//进行授权验证
			this.Authorize(Method.Upsert(), options);

			//将当前复写数据对象转换成数据字典
			var dictionary = DataDictionary.GetDictionary<TModel>(data);

			//解析数据模式表达式
			var schematic = this.GetSchema(schema, data.GetType());

			//验证待复写的数据
			this.OnValidate(Method.Upsert(), schematic, dictionary);

			return this.OnUpsert(dictionary, schematic, options);
		}

		protected virtual int OnUpsert(IDataDictionary<TModel> data, ISchema schema, IDataUpsertOptions options)
		{
			if(data == null || data.Data == null)
				return 0;

			//执行数据引擎的复写操作
			return this.DataAccess.Upsert(this.Name, data, schema, options, ctx => this.OnUpserting(ctx), ctx => this.OnUpserted(ctx));
		}

		public int UpsertMany(IEnumerable items, IDataUpsertOptions options = null)
		{
			return this.UpsertMany(items, null, options);
		}

		public int UpsertMany(IEnumerable items, string schema, IDataUpsertOptions options = null)
		{
			//确认是否可以执行该操作
			this.EnsureUpsert();

			if(items == null)
				return 0;

			//构建数据操作的选项对象
			if(options == null)
				options = new DataUpsertOptions();

			//进行授权验证
			this.Authorize(Method.UpsertMany(), options);

			//将当前复写数据集合对象转换成数据字典集合
			var dictionares = DataDictionary.GetDictionaries<TModel>(items);

			//解析数据模式表达式
			var schematic = this.GetSchema(schema, Common.TypeExtension.GetElementType(items.GetType()));

			foreach(var dictionary in dictionares)
			{
				//验证待复写的数据
				this.OnValidate(Method.UpsertMany(), schematic, dictionary);
			}

			return this.OnUpsertMany(dictionares, schematic, options);
		}

		protected virtual int OnUpsertMany(IEnumerable<IDataDictionary<TModel>> items, ISchema schema, IDataUpsertOptions options)
		{
			if(items == null)
				return 0;

			//执行数据引擎的复写操作
			return this.DataAccess.UpsertMany(this.Name, items, schema, options, ctx => this.OnUpserting(ctx), ctx => this.OnUpserted(ctx));
		}
		#endregion

		#region 更新方法
		public int Update<TKey>(object data, TKey key, IDataUpdateOptions options = null)
		{
			return this.Update<TKey>(data, key, null, options);
		}

		public int Update<TKey>(object data, TKey key, string schema, IDataUpdateOptions options = null)
		{
			return this.Update(data, this.ConvertKey(Method.Update(), key, null, out _), schema, options);
		}

		public int Update<TKey1, TKey2>(object data, TKey1 key1, TKey2 key2, IDataUpdateOptions options = null)
		{
			return this.Update<TKey1, TKey2>(data, key1, key2, null, options);
		}

		public int Update<TKey1, TKey2>(object data, TKey1 key1, TKey2 key2, string schema, IDataUpdateOptions options = null)
		{
			return this.Update(data, this.ConvertKey(Method.Update(), key1, key2, null, out _), schema, options);
		}

		public int Update<TKey1, TKey2, TKey3>(object data, TKey1 key1, TKey2 key2, TKey3 key3, IDataUpdateOptions options = null)
		{
			return this.Update<TKey1, TKey2, TKey3>(data, key1, key2, key3, null, options);
		}

		public int Update<TKey1, TKey2, TKey3>(object data, TKey1 key1, TKey2 key2, TKey3 key3, string schema, IDataUpdateOptions options = null)
		{
			return this.Update(data, this.ConvertKey(Method.Update(), key1, key2, key3, null, out _), schema, options);
		}

		public int Update(object data, IDataUpdateOptions options = null)
		{
			return this.Update(data, null, null, options);
		}

		public int Update(object data, string schema, IDataUpdateOptions options = null)
		{
			return this.Update(data, null, schema, options);
		}

		public int Update(object data, ICondition criteria, IDataUpdateOptions options = null)
		{
			return this.Update(data, criteria, null, options);
		}

		public int Update(object data, ICondition criteria, string schema, IDataUpdateOptions options = null)
		{
			//确认是否可以执行该操作
			this.EnsureUpdate();

			if(data == null)
				return 0;

			//构建数据操作的选项对象
			if(options == null)
				options = new DataUpdateOptions();

			//进行授权验证
			this.Authorize(Method.Update(), options);

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
			criteria = this.OnValidate(Method.Update(), criteria ?? this.EnsureUpdateCondition(dictionary));

			//解析数据模式表达式
			var schematic = this.GetSchema(schema, data.GetType());

			//验证待更新的数据
			this.OnValidate(Method.Update(), schematic, dictionary);

			//执行更新操作
			return this.OnUpdate(dictionary, criteria, schematic, options);
		}

		protected virtual int OnUpdate(IDataDictionary<TModel> data, ICondition criteria, ISchema schema, IDataUpdateOptions options)
		{
			if(data == null || data.Data == null)
				return 0;

			return this.DataAccess.Update(this.Name, data, criteria, schema, options, ctx => this.OnUpdating(ctx), ctx => this.OnUpdated(ctx));
		}

		public int UpdateMany(IEnumerable items, IDataUpdateOptions options = null)
		{
			return this.UpdateMany(items, null, options);
		}

		public int UpdateMany(IEnumerable items, string schema, IDataUpdateOptions options = null)
		{
			//确认是否可以执行该操作
			this.EnsureUpdate();

			if(items == null)
				return 0;

			//构建数据操作的选项对象
			if(options == null)
				options = new DataUpdateOptions();

			//进行授权验证
			this.Authorize(Method.UpdateMany(), options);

			//将当前更新数据集合对象转换成数据字典集合
			var dictionares = DataDictionary.GetDictionaries<TModel>(items);

			//解析数据模式表达式
			var schematic = this.GetSchema(schema, Common.TypeExtension.GetElementType(items.GetType()));

			foreach(var dictionary in dictionares)
			{
				//验证待更新的数据
				this.OnValidate(Method.UpdateMany(), schematic, dictionary);
			}

			return this.OnUpdateMany(dictionares, schematic, options);
		}

		protected virtual int OnUpdateMany(IEnumerable<IDataDictionary<TModel>> items, ISchema schema, IDataUpdateOptions options)
		{
			if(items == null)
				return 0;

			return this.DataAccess.UpdateMany(this.Name, items, schema, options, ctx => this.OnUpdating(ctx), ctx => this.OnUpdated(ctx));
		}
		#endregion

		#region 查询方法

		#region 单键查询
		public object Get<TKey>(TKey key, string filter = null, params Sorting[] sortings)
		{
			return this.Get<TKey>(key, string.Empty, null, null, filter, sortings);
		}

		public object Get<TKey>(TKey key, IDataSelectOptions options, string filter = null, params Sorting[] sortings)
		{
			return this.Get<TKey>(key, string.Empty, null, options, filter, sortings);
		}

		public object Get<TKey>(TKey key, Paging paging, string filter = null, params Sorting[] sortings)
		{
			return this.Get<TKey>(key, string.Empty, paging, null, filter, sortings);
		}

		public object Get<TKey>(TKey key, string schema, string filter = null, params Sorting[] sortings)
		{
			return this.Get<TKey>(key, schema, null, null, filter, sortings);
		}

		public object Get<TKey>(TKey key, string schema, IDataSelectOptions options, string filter = null, params Sorting[] sortings)
		{
			return this.Get<TKey>(key, schema, null, options, filter, sortings);
		}

		public object Get<TKey>(TKey key, string schema, Paging paging, string filter = null, params Sorting[] sortings)
		{
			return this.Get<TKey>(key, schema, paging, null, filter, sortings);
		}

		public object Get<TKey>(TKey key, string schema, Paging paging, IDataSelectOptions options, string filter = null, params Sorting[] sortings)
		{
			//构建数据操作的选项对象
			if(options == null)
				options = new DataSelectOptions();

			var criteria = this.ConvertKey(Method.Get(), key, filter, out var singular);

			if(singular)
			{
				//进行授权验证
				this.Authorize(Method.Get(), options);

				//修整查询条件
				criteria = this.OnValidate(Method.Get(), criteria);

				//执行单条查询方法
				return this.OnGet(criteria, this.GetSchema(schema), options);
			}

			return this.Select(criteria, schema, paging, options, sortings);
		}
		#endregion

		#region 双键查询
		public object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, string filter = null, params Sorting[] sortings)
		{
			return this.Get<TKey1, TKey2>(key1, key2, string.Empty, null, null, filter, sortings);
		}

		public object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, IDataSelectOptions options, string filter = null, params Sorting[] sortings)
		{
			return this.Get<TKey1, TKey2>(key1, key2, string.Empty, null, options, filter, sortings);
		}

		public object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, Paging paging, string filter = null, params Sorting[] sortings)
		{
			return this.Get<TKey1, TKey2>(key1, key2, string.Empty, paging, null, filter, sortings);
		}

		public object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, string filter = null, params Sorting[] sortings)
		{
			return this.Get<TKey1, TKey2>(key1, key2, schema, null, null, filter, sortings);
		}

		public object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, IDataSelectOptions options, string filter = null, params Sorting[] sortings)
		{
			return this.Get<TKey1, TKey2>(key1, key2, schema, null, options, filter, sortings);
		}

		public object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Paging paging, string filter = null, params Sorting[] sortings)
		{
			return this.Get<TKey1, TKey2>(key1, key2, schema, paging, null, filter, sortings);
		}

		public object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Paging paging, IDataSelectOptions options, string filter = null, params Sorting[] sortings)
		{
			//构建数据操作的选项对象
			if(options == null)
				options = new DataSelectOptions();

			var criteria = this.ConvertKey(Method.Get(), key1, key2, filter, out var singular);

			if(singular)
			{
				//进行授权验证
				this.Authorize(Method.Get(), options);

				//修整查询条件
				criteria = this.OnValidate(Method.Get(), criteria);

				//执行单条查询方法
				return this.OnGet(criteria, this.GetSchema(schema), options);
			}

			return this.Select(criteria, schema, paging, options, sortings);
		}
		#endregion

		#region 三键查询
		public object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string filter = null, params Sorting[] sortings)
		{
			return this.Get(key1, key2, key3, string.Empty, null, null, filter, sortings);
		}

		public object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, IDataSelectOptions options, string filter = null, params Sorting[] sortings)
		{
			return this.Get(key1, key2, key3, string.Empty, null, options, filter, sortings);
		}

		public object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, Paging paging, string filter = null, params Sorting[] sortings)
		{
			return this.Get(key1, key2, key3, string.Empty, paging, null, filter, sortings);
		}

		public object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, string filter = null, params Sorting[] sortings)
		{
			return this.Get(key1, key2, key3, schema, null, null, filter, sortings);
		}

		public object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, IDataSelectOptions options, string filter = null, params Sorting[] sortings)
		{
			return this.Get(key1, key2, key3, schema, null, options, filter, sortings);
		}

		public object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Paging paging, string filter = null, params Sorting[] sortings)
		{
			return this.Get(key1, key2, key3, schema, paging, null, filter, sortings);
		}

		public object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Paging paging, IDataSelectOptions options, string filter = null, params Sorting[] sortings)
		{
			//构建数据操作的选项对象
			if(options == null)
				options = new DataSelectOptions();

			var criteria = this.ConvertKey(Method.Get(), key1, key2, key3, filter, out var singular);

			if(singular)
			{
				//进行授权验证
				this.Authorize(Method.Get(), options);

				//修整查询条件
				criteria = this.OnValidate(Method.Get(), criteria);

				//执行单条查询方法
				return this.OnGet(criteria, this.GetSchema(schema), options);
			}

			return this.Select(criteria, schema, paging, options, sortings);
		}

		protected virtual TModel OnGet(ICondition criteria, ISchema schema, IDataSelectOptions options)
		{
			return this.DataAccess.Select<TModel>(this.Name, criteria, schema, null, options, null, ctx => this.OnGetting(ctx), ctx => this.OnGetted(ctx)).FirstOrDefault();
		}
		#endregion

		#region 常规查询
		public IEnumerable<TModel> Select(IDataSelectOptions options = null, params Sorting[] sortings)
		{
			return this.Select(null, string.Empty, null, options, sortings);
		}

		public IEnumerable<TModel> Select(ICondition criteria, params Sorting[] sortings)
		{
			return this.Select(criteria, string.Empty, null, null, sortings);
		}

		public IEnumerable<TModel> Select(ICondition criteria, IDataSelectOptions options, params Sorting[] sortings)
		{
			return this.Select(criteria, string.Empty, null, options, sortings);
		}

		public IEnumerable<TModel> Select(ICondition criteria, Paging paging, params Sorting[] sortings)
		{
			return this.Select(criteria, string.Empty, paging, null, sortings);
		}

		public IEnumerable<TModel> Select(ICondition criteria, Paging paging, IDataSelectOptions options, params Sorting[] sortings)
		{
			return this.Select(criteria, string.Empty, paging, options, sortings);
		}

		public IEnumerable<TModel> Select(ICondition criteria, string schema, params Sorting[] sortings)
		{
			return this.Select(criteria, schema, null, null, sortings);
		}

		public IEnumerable<TModel> Select(ICondition criteria, string schema, IDataSelectOptions options, params Sorting[] sortings)
		{
			return this.Select(criteria, schema, null, options, sortings);
		}

		public IEnumerable<TModel> Select(ICondition criteria, string schema, Paging paging, params Sorting[] sortings)
		{
			return this.Select(criteria, schema, paging, null, sortings);
		}

		public IEnumerable<TModel> Select(ICondition criteria, string schema, Paging paging, IDataSelectOptions options, params Sorting[] sortings)
		{
			//构建数据操作的选项对象
			if(options == null)
				options = new DataSelectOptions();

			//进行授权验证
			this.Authorize(Method.Select(), options);

			//修整查询条件
			criteria = this.OnValidate(Method.Select(), criteria);

			//执行查询方法
			return this.OnSelect(criteria, this.GetSchema(schema, typeof(TModel)), paging, sortings, options);
		}

		protected virtual IEnumerable<TModel> OnSelect(ICondition criteria, ISchema schema, Paging paging, Sorting[] sortings, IDataSelectOptions options)
		{
			//如果没有指定排序设置则应用默认排序规则
			if(sortings == null || sortings.Length == 0)
				sortings = this.GetDefaultSortings();

			return this.DataAccess.Select<TModel>(this.Name, criteria, schema, paging, options, sortings, ctx => this.OnSelecting(ctx), ctx => this.OnSelected(ctx));
		}
		#endregion

		#region 分组查询
		public IEnumerable<T> Select<T>(Grouping grouping, params Sorting[] sortings)
		{
			return this.Select<T>(grouping, null, string.Empty, null, null, sortings);
		}

		public IEnumerable<T> Select<T>(Grouping grouping, IDataSelectOptions options, params Sorting[] sortings)
		{
			return this.Select<T>(grouping, null, string.Empty, null, options, sortings);
		}

		public IEnumerable<T> Select<T>(Grouping grouping, Paging paging, IDataSelectOptions options = null, params Sorting[] sortings)
		{
			return this.Select<T>(grouping, null, string.Empty, paging, options, sortings);
		}

		public IEnumerable<T> Select<T>(Grouping grouping, string schema, params Sorting[] sortings)
		{
			return this.Select<T>(grouping, null, schema, null, null, sortings);
		}

		public IEnumerable<T> Select<T>(Grouping grouping, string schema, IDataSelectOptions options, params Sorting[] sortings)
		{
			return this.Select<T>(grouping, null, schema, null, options, sortings);
		}

		public IEnumerable<T> Select<T>(Grouping grouping, string schema, Paging paging, IDataSelectOptions options = null, params Sorting[] sortings)
		{
			return this.Select<T>(grouping, null, schema, paging, options, sortings);
		}

		public IEnumerable<T> Select<T>(Grouping grouping, ICondition criteria, Paging paging, params Sorting[] sortings)
		{
			return this.Select<T>(grouping, criteria, null, paging, null, sortings);
		}

		public IEnumerable<T> Select<T>(Grouping grouping, ICondition criteria, string schema, params Sorting[] sortings)
		{
			return this.Select<T>(grouping, criteria, schema, null, null, sortings);
		}

		public IEnumerable<T> Select<T>(Grouping grouping, ICondition criteria, string schema, IDataSelectOptions options, params Sorting[] sortings)
		{
			return this.Select<T>(grouping, criteria, schema, null, options, sortings);
		}

		public IEnumerable<T> Select<T>(Grouping grouping, ICondition criteria, string schema, Paging paging, IDataSelectOptions options = null, params Sorting[] sortings)
		{
			//构建数据操作的选项对象
			if(options == null)
				options = new DataSelectOptions();

			//进行授权验证
			this.Authorize(Method.Select(), options);

			//修整查询条件
			criteria = this.OnValidate(Method.Select(), criteria);

			//执行查询方法
			return this.OnSelect<T>(grouping, criteria, string.IsNullOrWhiteSpace(schema) ? null : this.GetSchema(schema, typeof(TModel)), paging, sortings, options);
		}

		protected virtual IEnumerable<T> OnSelect<T>(Grouping grouping, ICondition criteria, ISchema schema, Paging paging, Sorting[] sortings, IDataSelectOptions options)
		{
			return this.DataAccess.Select<T>(this.Name, grouping, criteria, schema, paging, options, sortings, ctx => this.OnSelecting(ctx), ctx => this.OnSelected(ctx));
		}
		#endregion

		#region 显式实现
		IEnumerable IDataService.Select(IDataSelectOptions options, params Sorting[] sortings)
		{
			return this.Select(options, sortings);
		}

		IEnumerable IDataService.Select(ICondition criteria, params Sorting[] sortings)
		{
			return this.Select(criteria, sortings);
		}

		IEnumerable IDataService.Select(ICondition criteria, IDataSelectOptions options, params Sorting[] sortings)
		{
			return this.Select(criteria, options, sortings);
		}

		IEnumerable IDataService.Select(ICondition criteria, string schema, params Sorting[] sortings)
		{
			return this.Select(criteria, schema, sortings);
		}

		IEnumerable IDataService.Select(ICondition criteria, string schema, IDataSelectOptions options, params Sorting[] sortings)
		{
			return this.Select(criteria, schema, options, sortings);
		}

		IEnumerable IDataService.Select(ICondition criteria, string schema, Paging paging, params Sorting[] sortings)
		{
			return this.Select(criteria, schema, paging, sortings);
		}

		IEnumerable IDataService.Select(ICondition criteria, string schema, Paging paging, IDataSelectOptions options, params Sorting[] sortings)
		{
			return this.Select(criteria, schema, paging, options, sortings);
		}

		IEnumerable IDataService.Select(ICondition criteria, Paging paging, params Sorting[] sortings)
		{
			return this.Select(criteria, paging, sortings);
		}

		IEnumerable IDataService.Select(ICondition criteria, Paging paging, IDataSelectOptions options, params Sorting[] sortings)
		{
			return this.Select(criteria, paging, options, sortings);
		}
		#endregion

		#endregion

		#region 默认排序
		private Sorting[] _defaultSortings;

		private Sorting[] GetDefaultSortings()
		{
			if(_defaultSortings == null)
				_defaultSortings = GetSortings() ?? Array.Empty<Sorting>();

			return _defaultSortings;
		}

		protected virtual Sorting[] GetSortings()
		{
			var sortings = SortingAttribute.GetSortings(this.GetType());

			if(sortings == null || sortings.Length == 0)
			{
				var keys = this.DataAccess.Metadata.Entities.Get(this.Name).Key;

				if(keys != null && keys.Length > 0)
				{
					sortings = new Sorting[keys.Length];

					for(int i = 0; i < keys.Length; i++)
						sortings[i] = Sorting.Descending(keys[i].Name);
				}
			}

			return sortings;
		}
		#endregion

		#region 校验方法
		protected virtual ICondition OnValidate(Method method, ICondition criteria)
		{
			return criteria;
		}

		protected virtual void OnValidate(Method method, ISchema schema, IDataDictionary<TModel> data)
		{
		}
		#endregion

		#region 激发事件
		protected virtual void OnGetted(DataSelectContextBase context)
		{
			this.Getted?.Invoke(this, new DataGettedEventArgs<TModel>(context));
		}

		protected virtual bool OnGetting(DataSelectContextBase context)
		{
			var e = this.Getting;

			if(e == null)
				return false;

			var args = new DataGettingEventArgs<TModel>(context);
			e(this, args);
			return args.Cancel;
		}

		protected virtual void OnExecuted(DataExecuteContextBase context)
		{
			this.Executed?.Invoke(this, new DataExecutedEventArgs(context));
		}

		protected virtual bool OnExecuting(DataExecuteContextBase context)
		{
			var e = this.Executing;

			if(e == null)
				return false;

			var args = new DataExecutingEventArgs(context);
			e(this, args);
			return args.Cancel;
		}

		protected virtual void OnExisted(DataExistContextBase context)
		{
			this.Existed?.Invoke(this, new DataExistedEventArgs(context));
		}

		protected virtual bool OnExisting(DataExistContextBase context)
		{
			var e = this.Existing;

			if(e == null)
				return false;

			var args = new DataExistingEventArgs(context);
			e(this, args);
			return args.Cancel;
		}

		protected virtual void OnAggregated(DataAggregateContextBase context)
		{
			this.Aggregated?.Invoke(this, new DataAggregatedEventArgs(context));
		}

		protected virtual bool OnAggregating(DataAggregateContextBase context)
		{
			var e = this.Aggregating;

			if(e == null)
				return false;

			var args = new DataAggregatingEventArgs(context);
			e(this, args);
			return args.Cancel;
		}

		protected virtual void OnIncremented(DataIncrementContextBase context)
		{
			this.Incremented?.Invoke(this, new DataIncrementedEventArgs(context));
		}

		protected virtual bool OnIncrementing(DataIncrementContextBase context)
		{
			var e = this.Incrementing;

			if(e == null)
				return false;

			var args = new DataIncrementingEventArgs(context);
			e(this, args);
			return args.Cancel;
		}

		protected virtual void OnDeleted(DataDeleteContextBase context)
		{
			this.Deleted?.Invoke(this, new DataDeletedEventArgs(context));
		}

		protected virtual bool OnDeleting(DataDeleteContextBase context)
		{
			var e = this.Deleting;

			if(e == null)
				return false;

			var args = new DataDeletingEventArgs(context);
			e(this, args);
			return args.Cancel;
		}

		protected virtual void OnInserted(DataInsertContextBase context)
		{
			this.Inserted?.Invoke(this, new DataInsertedEventArgs(context));
		}

		protected virtual bool OnInserting(DataInsertContextBase context)
		{
			var e = this.Inserting;

			if(e == null)
				return false;

			var args = new DataInsertingEventArgs(context);
			e(this, args);
			return args.Cancel;
		}

		protected virtual void OnUpserted(DataUpsertContextBase context)
		{
			this.Upserted?.Invoke(this, new DataUpsertedEventArgs(context));
		}

		protected virtual bool OnUpserting(DataUpsertContextBase context)
		{
			var e = this.Upserting;

			if(e == null)
				return false;

			var args = new DataUpsertingEventArgs(context);
			e(this, args);
			return args.Cancel;
		}

		protected virtual void OnUpdated(DataUpdateContextBase context)
		{
			this.Updated?.Invoke(this, new DataUpdatedEventArgs(context));
		}

		protected virtual bool OnUpdating(DataUpdateContextBase context)
		{
			var e = this.Updating;

			if(e == null)
				return false;

			var args = new DataUpdatingEventArgs(context);
			e(this, args);
			return args.Cancel;
		}

		protected virtual void OnSelected(DataSelectContextBase context)
		{
			this.Selected?.Invoke(this, new DataSelectedEventArgs(context));
		}

		protected virtual bool OnSelecting(DataSelectContextBase context)
		{
			var e = this.Selecting;

			if(e == null)
				return false;

			var args = new DataSelectingEventArgs(context);
			e(this, args);
			return args.Cancel;
		}
		#endregion

		#region 条件转换
		/// <summary>
		/// 根据指定的参数值获取对应的操作<see cref="ICondition"/>条件。
		/// </summary>
		/// <param name="method">指定的操作方法。</param>
		/// <param name="values">指定的参数值数组。</param>
		/// <param name="singular">输出一个值，指示转换后的操作条件作用结果是否为必定为单个对象。</param>
		/// <returns>返回对应的操作<see cref="ICondition"/>条件。</returns>
		protected virtual ICondition OnCondition(Method method, object[] values, out bool singular)
		{
			//设置输出参数默认值
			singular = false;

			if(values == null || values.Length == 0)
				return null;

			//获取当前数据服务对应的主键
			var primaryKey = this.DataAccess.Metadata.Entities.Get(this.Name).Key;

			//如果主键获取失败或主键未定义或主键项数量小于传入的数组元素个数则返回空
			if(primaryKey == null || primaryKey.Length == 0 || primaryKey.Length < values.Length)
				return null;

			//只有传入参数值数量与主键数匹配其结果才为单个项
			singular = primaryKey.Length == values.Length;

			//如果主键成员只有一个则返回单个条件
			if(primaryKey.Length == 1)
			{
				if(values[0] is string text && text != null && text.Length > 0)
				{
					var parts = text.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0).ToArray();

					if(parts.Length > 1)
					{
						singular = false;
						return Condition.In(primaryKey[0].Name, parts);
					}
				}

				return Condition.Equal(primaryKey[0].Name, ConvertValue(values[0], primaryKey[0].Type));
			}

			//创建返回的条件集（AND组合）
			var conditions = ConditionCollection.And();

			for(int i = 0; i < values.Length; i++)
			{
				conditions.Add(Data.Condition.Equal(primaryKey[i].Name, ConvertValue(values[i], primaryKey[i].Type)));
			}

			return conditions;
		}

		/// <summary>
		/// 将指定的过滤表达式文本转换为条件。
		/// </summary>
		/// <param name="method">指定的操作方法。</param>
		/// <param name="filter">指定的过滤表达式文本。</param>
		/// <returns>返回对应的过滤条件。</returns>
		protected virtual ICondition OnCondition(Method method, string filter)
		{
			return null;
		}
		#endregion

		#region 私有方法
		private ICondition ConvertKey(Method method, object[] values, string filter, out bool singular)
		{
			if(values != null && values.Length > 3)
				throw new NotSupportedException("Too many key values specified.");

			//获取查询键值对数组，如果没有映射到条件则抛出异常
			var criteria = this.OnCondition(method, values ?? Array.Empty<object>(), out singular);

			//如果不是单值结果则生成对应的过滤条件
			var filtering = singular ? null : this.OnCondition(method, filter);

			if(criteria is ConditionCollection conditions)
			{
				if(filtering == null)
					return conditions.Count == 1 ? conditions[0] : conditions;

				if(conditions.Combination == ConditionCombination.And)
				{
					conditions.Add(filtering);
					return conditions;
				}

				return ConditionCollection.And(conditions, filtering);
			}

			return filtering == null ? criteria : (criteria == null ? filtering : ConditionCollection.And(criteria, filtering));
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private ICondition ConvertKey<TKey>(Method method, TKey key, string filter, out bool singular)
		{
			return this.ConvertKey(method, key == null ? Array.Empty<object>() : new object[] { key }, filter, out singular);
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private ICondition ConvertKey<TKey1, TKey2>(Method method, TKey1 key1, TKey2 key2, string filter, out bool singular)
		{
			return this.ConvertKey(method, new object[] { key1, key2 }, filter, out singular);
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private ICondition ConvertKey<TKey1, TKey2, TKey3>(Method method, TKey1 key1, TKey2 key2, TKey3 key3, string filter, out bool singular)
		{
			return this.ConvertKey(method, new object[] { key1, key2, key3 }, filter, out singular);
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private ISchema GetSchema(string expression, Type type = null)
		{
			return this.DataAccess.Schema.Parse(this.Name, expression, type ?? typeof(TModel));
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private ICondition EnsureUpdateCondition(IDataDictionary dictionary)
		{
			var keys = this.DataAccess.Metadata.Entities.Get(this.Name).Key;

			if(keys == null || keys.Length == 0)
				throw new DataException($"The specified '{this.Name}' data entity does not define a primary key and does not support update operation.");

			var requisite = new ICondition[keys.Length];

			for(int i = 0; i < keys.Length; i++)
			{
				if(dictionary.TryGetValue(keys[i].Name, out var value) && value != null)
					requisite[i] = Condition.Equal(keys[i].Name, value);
				else
					throw new DataException($"No required primary key field value is specified for the update '{this.Name}' entity data.");
			}

			return requisite.Length > 1 ? ConditionCollection.And(requisite) : requisite[0];
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private void EnsureDelete()
		{
			if(!this.CanDelete)
				throw new InvalidOperationException("The delete operation is not allowed.");
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private void EnsureInsert()
		{
			if(!this.CanInsert)
				throw new InvalidOperationException("The insert operation is not allowed.");
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private void EnsureUpdate()
		{
			if(!this.CanUpdate)
				throw new InvalidOperationException("The update operation is not allowed.");
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private void EnsureUpsert()
		{
			if(!(this.CanInsert && this.CanUpdate && this.CanUpsert))
				throw new InvalidOperationException("The upsert operation is not allowed.");
		}

		private static object ConvertValue(object value, DbType dbType)
		{
			if(value == null)
				return null;

			switch(dbType)
			{
				case DbType.Byte:
					return Common.Convert.ConvertValue<byte>(value);
				case DbType.SByte:
					return Common.Convert.ConvertValue<sbyte>(value);
				case DbType.Boolean:
					return Common.Convert.ConvertValue<bool>(value);
				case DbType.Int16:
					return Common.Convert.ConvertValue<short>(value);
				case DbType.Int32:
					return Common.Convert.ConvertValue<int>(value);
				case DbType.Int64:
					return Common.Convert.ConvertValue<long>(value);
				case DbType.UInt16:
					return Common.Convert.ConvertValue<ushort>(value);
				case DbType.UInt32:
					return Common.Convert.ConvertValue<uint>(value);
				case DbType.UInt64:
					return Common.Convert.ConvertValue<ulong>(value);
				case DbType.Single:
					return Common.Convert.ConvertValue<float>(value);
				case DbType.Double:
					return Common.Convert.ConvertValue<double>(value);
				case DbType.Decimal:
				case DbType.Currency:
					return Common.Convert.ConvertValue<decimal>(value);
				case DbType.Date:
				case DbType.Time:
				case DbType.DateTime:
				case DbType.DateTime2:
				case DbType.DateTimeOffset:
					return Common.Convert.ConvertValue<DateTime>(value);
				case DbType.AnsiString:
				case DbType.AnsiStringFixedLength:
				case DbType.String:
				case DbType.StringFixedLength:
					return Common.Convert.ConvertValue<string>(value);
				case DbType.Guid:
					return Common.Convert.ConvertValue<Guid>(value);
				case DbType.Binary:
					return Common.Convert.ConvertValue<byte[]>(value);
				default:
					return value;
			}
		}
		#endregion

		#region 嵌套子类
		protected struct Method : IEquatable<Method>
		{
			#region 公共字段
			/// <summary>方法的名称。</summary>
			public readonly string Name;

			/// <summary>对应的数据访问方法种类。</summary>
			public readonly DataAccessMethod Kind;

			/// <summary>获取一个值，指示该方法是否为批量写入操作。</summary>
			public readonly bool IsMultiple;
			#endregion

			#region 构造函数
			private Method(DataAccessMethod kind)
			{
				this.Kind = kind;
				this.Name = kind.ToString();
				this.IsMultiple = false;
			}

			private Method(string name, DataAccessMethod kind, bool isMultiple)
			{
				this.Name = name ?? kind.ToString();
				this.Kind = kind;
				this.IsMultiple = isMultiple;
			}
			#endregion

			#region 静态方法
			public static Method Get()
			{
				return new Method(nameof(Get), DataAccessMethod.Select, false);
			}

			public static Method Count()
			{
				return new Method(nameof(Count), DataAccessMethod.Aggregate, false);
			}

			public static Method Aggregate(DataAggregateFunction aggregate)
			{
				return new Method(aggregate.ToString(), DataAccessMethod.Aggregate, false);
			}

			public static Method Exists()
			{
				return new Method(DataAccessMethod.Exists);
			}

			public static Method Execute()
			{
				return new Method(DataAccessMethod.Execute);
			}

			public static Method Increment()
			{
				return new Method(nameof(Increment), DataAccessMethod.Increment, false);
			}

			public static Method Decrement()
			{
				return new Method(nameof(Decrement), DataAccessMethod.Increment, false);
			}

			public static Method Select(string name = null)
			{
				if(string.IsNullOrEmpty(name))
					return new Method(DataAccessMethod.Select);
				else
					return new Method(name, DataAccessMethod.Select, false);
			}

			public static Method Delete()
			{
				return new Method(DataAccessMethod.Delete);
			}

			public static Method Insert()
			{
				return new Method(DataAccessMethod.Insert);
			}

			public static Method InsertMany()
			{
				return new Method(nameof(InsertMany), DataAccessMethod.Insert, true);
			}

			public static Method Update()
			{
				return new Method(DataAccessMethod.Update);
			}

			public static Method UpdateMany()
			{
				return new Method(nameof(UpdateMany), DataAccessMethod.Update, true);
			}

			public static Method Upsert()
			{
				return new Method(DataAccessMethod.Upsert);
			}

			public static Method UpsertMany()
			{
				return new Method(nameof(UpsertMany), DataAccessMethod.Upsert, true);
			}
			#endregion

			#region 公共方法
			/// <summary>获取一个值，指示当前是否为删除方法，即 <see cref="Kind"/> 属性值是否等于 <see cref="DataAccessMethod.Delete"/>。</summary>
			public bool IsDelete { get => this.Kind == DataAccessMethod.Delete; }
			/// <summary>获取一个值，指示当前是否为新增方法，即 <see cref="Kind"/> 属性值是否等于 <see cref="DataAccessMethod.Insert"/>。</summary>
			public bool IsInsert { get => this.Kind == DataAccessMethod.Insert; }
			/// <summary>获取一个值，指示当前是否为更新方法，即 <see cref="Kind"/> 属性值是否等于 <see cref="DataAccessMethod.Update"/>。</summary>
			public bool IsUpdate { get => this.Kind == DataAccessMethod.Update; }
			/// <summary>获取一个值，指示当前是否为增改方法，即 <see cref="Kind"/> 属性值是否等于 <see cref="DataAccessMethod.Upsert"/>。</summary>
			public bool IsUpsert { get => this.Kind == DataAccessMethod.Upsert; }
			/// <summary>获取一个值，指示当前是否为查询方法，即 <see cref="Kind"/> 属性值是否等于 <see cref="DataAccessMethod.Select"/>。</summary>
			public bool IsSelect { get => this.Kind == DataAccessMethod.Select; }
			/// <summary>获取一个值，指示当前是否为获取方法，即 <see cref="Kind"/> 属性值是否等于 <see cref="DataAccessMethod.Select"/> 并且 <see cref="Name"/> 等于“Get”。</summary>
			public bool IsGet { get => this.Kind == DataAccessMethod.Select && this.Name == nameof(Get); }

			/// <summary>
			/// 获取一个值，指示当前方法是否为读取方法(Select/Exists/Aggregate)。
			/// </summary>
			public bool IsReading
			{
				get => this.Kind == DataAccessMethod.Select ||
				       this.Kind == DataAccessMethod.Exists ||
				       this.Kind == DataAccessMethod.Aggregate;
			}

			/// <summary>
			/// 获取一个值，指示当前方法是否为修改方法(Incremnet/Decrement/Delete/Insert/Update/Upsert)。
			/// </summary>
			public bool IsWriting
			{
				get => this.Kind == DataAccessMethod.Delete ||
				       this.Kind == DataAccessMethod.Insert ||
				       this.Kind == DataAccessMethod.Update ||
				       this.Kind == DataAccessMethod.Upsert ||
				       this.Kind == DataAccessMethod.Increment;
			}
			#endregion

			#region 重写方法
			public bool Equals(Method method)
			{
				return this.Kind == method.Kind && string.Equals(this.Name, method.Name);
			}

			public override bool Equals(object obj)
			{
				if(obj == null || obj.GetType() != typeof(Method))
					return false;

				return this.Equals((Method)obj);
			}

			public override int GetHashCode()
			{
				return HashCode.Combine(Name);
			}

			public override string ToString()
			{
				return this.Name;
			}
			#endregion
		}

		public sealed class Condition : Zongsoft.Data.Condition.Builder<TModel>
		{
			#region 私有构造
			private Condition() { }
			#endregion
		}
		#endregion
	}
}
