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
		private DataServiceAttribute _attribute;
		private IDataSearcher<TModel> _searcher;
		private IServiceProvider _serviceProvider;
		#endregion

		#region 构造函数
		protected DataServiceBase(IServiceProvider serviceProvider, IDataSearcher<TModel> searcher = null)
		{
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
			_dataAccess = (IDataAccess)serviceProvider.GetService(typeof(IDataAccess)) ?? ((IDataAccessProvider)serviceProvider.GetService(typeof(IDataAccessProvider)))?.GetAccessor(null);
			_attribute = (DataServiceAttribute)System.Attribute.GetCustomAttribute(this.GetType(), typeof(DataServiceAttribute), true);

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
			_attribute = (DataServiceAttribute)System.Attribute.GetCustomAttribute(this.GetType(), typeof(DataServiceAttribute), true);

			//创建数据搜索器
			_searcher = searcher ?? new DataSearcher<TModel>(this);
		}

		protected DataServiceBase(IDataService service, IDataSearcher<TModel> searcher = null)
		{
			this.Service = service ?? throw new ArgumentNullException(nameof(service));
			_serviceProvider = service.ServiceProvider;
			_dataAccess = (IDataAccess)_serviceProvider.GetService(typeof(IDataAccess)) ?? ((IDataAccessProvider)_serviceProvider.GetService(typeof(IDataAccessProvider)))?.GetAccessor(null);
			_attribute = (DataServiceAttribute)System.Attribute.GetCustomAttribute(this.GetType(), typeof(DataServiceAttribute), true);

			//创建数据搜索器
			_searcher = searcher ?? new DataSearcher<TModel>(this);
		}

		protected DataServiceBase(string name, IDataService service, IDataSearcher<TModel> searcher = null)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			_name = name.Trim();
			this.Service = service ?? throw new ArgumentNullException(nameof(service));
			_serviceProvider = service.ServiceProvider;
			_dataAccess = (IDataAccess)_serviceProvider.GetService(typeof(IDataAccess)) ?? ((IDataAccessProvider)_serviceProvider.GetService(typeof(IDataAccessProvider)))?.GetAccessor(null);
			_attribute = (DataServiceAttribute)System.Attribute.GetCustomAttribute(this.GetType(), typeof(DataServiceAttribute), true);

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

		public virtual bool CanDelete { get => this.Service?.CanDelete ?? true; }

		public virtual bool CanInsert { get => this.Service?.CanInsert ?? true; }

		public virtual bool CanUpdate { get => this.Service?.CanUpdate ?? true; }

		public virtual bool CanUpsert { get => this.Service?.CanUpsert ?? true; }

		public DataServiceAttribute Attribute { get => _attribute; }

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

		public IDataServiceValidator<TModel> Validator { get; protected set; }

		IDataServiceValidator IDataService.Validator { get => this.Validator; }

		public IServiceProvider ServiceProvider
		{
			get => _serviceProvider;
			set => _serviceProvider = value ?? throw new ArgumentNullException();
		}
		#endregion

		#region 保护属性
		protected IDataService Service { get; }
		protected virtual System.Security.Claims.ClaimsPrincipal Principal
		{
			get => Services.ApplicationContext.Current?.Principal;
		}
		#endregion

		#region 授权验证
		protected virtual void Authorize(DataServiceMethod method, IDataOptions options)
		{
			var validator = this.Validator;

			if(validator != null)
			{
				validator.Authorize(method, options);
				return;
			}

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
			this.Authorize(DataServiceMethod.Execute(), options);

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
			this.Authorize(DataServiceMethod.Execute(), options);

			return this.OnExecuteScalar(name, inParameters, out outParameters, options);
		}

		protected virtual object OnExecuteScalar(string name, IDictionary<string, object> inParameters, out IDictionary<string, object> outParameters, IDataExecuteOptions options)
		{
			return this.DataAccess.ExecuteScalar(name, inParameters, out outParameters, options, ctx => this.OnExecuting(ctx), ctx => this.OnExecuted(ctx));
		}
		#endregion

		#region 存在方法
		public bool Exists(string key, IDataExistsOptions options = null)
		{
			return this.Exists(this.ConvertKey(DataServiceMethod.Exists(), key, out _), options);
		}

		public bool Exists<TKey1>(TKey1 key1, IDataExistsOptions options = null) where TKey1 : struct, IEquatable<TKey1>
		{
			return this.Exists(this.ConvertKey(DataServiceMethod.Exists(), key1, out _), options);
		}

		public bool Exists<TKey1, TKey2>(TKey1 key1, TKey2 key2, IDataExistsOptions options = null)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
		{
			return this.Exists(this.ConvertKey(DataServiceMethod.Exists(), key1, key2, out _), options);
		}

		public bool Exists<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, IDataExistsOptions options = null)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
		{
			return this.Exists(this.ConvertKey(DataServiceMethod.Exists(), key1, key2, key3, out _), options);
		}

		public bool Exists<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, IDataExistsOptions options = null)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
			where TKey4 : struct, IEquatable<TKey4>
		{
			return this.Exists(this.ConvertKey(DataServiceMethod.Exists(), key1, key2, key3, key4, out _), options);
		}

		public bool Exists<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, IDataExistsOptions options = null)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
			where TKey4 : struct, IEquatable<TKey4>
			where TKey5 : struct, IEquatable<TKey5>
		{
			return this.Exists(this.ConvertKey(DataServiceMethod.Exists(), key1, key2, key3, key4, key5, out _), options);
		}

		public bool Exists(ICondition criteria, IDataExistsOptions options = null)
		{
			//构建数据操作的选项对象
			if(options == null)
				options = new DataExistsOptions();

			//进行授权验证
			this.Authorize(DataServiceMethod.Exists(), options);

			//修整查询条件
			criteria = this.OnValidate(DataServiceMethod.Exists(), criteria, options.Filter, options);

			//执行存在操作
			return this.OnExists(criteria, options);
		}

		protected virtual bool OnExists(ICondition criteria, IDataExistsOptions options)
		{
			return this.DataAccess.Exists(this.Name, criteria, options, ctx => this.OnExisting(ctx), ctx => this.OnExisted(ctx));
		}
		#endregion

		#region 聚合方法
		public int Count(string key, string member = null, IDataAggregateOptions options = null)
		{
			return this.Count(this.ConvertKey(DataServiceMethod.Count(), key, out _), member, options);
		}

		public int Count<TKey1>(TKey1 key1, string member = null, IDataAggregateOptions options = null) where TKey1 : struct, IEquatable<TKey1>
		{
			return this.Count(this.ConvertKey(DataServiceMethod.Count(), key1, out _), member, options);
		}

		public int Count<TKey1, TKey2>(TKey1 key1, TKey2 key2, string member = null, IDataAggregateOptions options = null)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
		{
			return this.Count(this.ConvertKey(DataServiceMethod.Count(), key1, key2, out _), member, options);
		}

		public int Count<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string member = null, IDataAggregateOptions options = null)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
		{
			return this.Count(this.ConvertKey(DataServiceMethod.Count(), key1, key2, key3, out _), member, options);
		}

		public int Count<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string member = null, IDataAggregateOptions options = null)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
			where TKey4 : struct, IEquatable<TKey4>
		{
			return this.Count(this.ConvertKey(DataServiceMethod.Count(), key1, key2, key3, key4, out _), member, options);
		}

		public int Count<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string member = null, IDataAggregateOptions options = null)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
			where TKey4 : struct, IEquatable<TKey4>
			where TKey5 : struct, IEquatable<TKey5>
		{
			return this.Count(this.ConvertKey(DataServiceMethod.Count(), key1, key2, key3, key4, key5, out _), member, options);
		}

		public int Count(ICondition criteria = null, string member = null, IDataAggregateOptions options = null)
		{
			//构建数据操作的选项对象
			if(options == null)
				options = new DataAggregateOptions();

			//进行授权验证
			this.Authorize(DataServiceMethod.Count(), options);

			//修整查询条件
			criteria = this.OnValidate(DataServiceMethod.Count(), criteria, options.Filter, options);

			//执行聚合操作
			return (int)(this.OnAggregate(new DataAggregate(DataAggregateFunction.Count, member), criteria, options) ?? 0d);
		}

		public double? Aggregate(DataAggregateFunction function, string member, string key, IDataAggregateOptions options = null)
		{
			return this.Aggregate(function, member, this.ConvertKey(DataServiceMethod.Aggregate(function), key, out _), options);
		}

		public double? Aggregate<TKey1>(DataAggregateFunction function, string member, TKey1 key1, IDataAggregateOptions options = null)
			where TKey1 : struct, IEquatable<TKey1>
		{
			return this.Aggregate(function, member, this.ConvertKey(DataServiceMethod.Aggregate(function), key1, out _), options);
		}

		public double? Aggregate<TKey1, TKey2>(DataAggregateFunction function, string member, TKey1 key1, TKey2 key2, IDataAggregateOptions options = null)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
		{
			return this.Aggregate(function, member, this.ConvertKey(DataServiceMethod.Aggregate(function), key1, key2, out _), options);
		}

		public double? Aggregate<TKey1, TKey2, TKey3>(DataAggregateFunction function, string member, TKey1 key1, TKey2 key2, TKey3 key3, IDataAggregateOptions options = null)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
		{
			return this.Aggregate(function, member, this.ConvertKey(DataServiceMethod.Aggregate(function), key1, key2, key3, out _), options);
		}

		public double? Aggregate<TKey1, TKey2, TKey3, TKey4>(DataAggregateFunction function, string member, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, IDataAggregateOptions options = null)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
			where TKey4 : struct, IEquatable<TKey4>
		{
			return this.Aggregate(function, member, this.ConvertKey(DataServiceMethod.Aggregate(function), key1, key2, key3, key4, out _), options);
		}

		public double? Aggregate<TKey1, TKey2, TKey3, TKey4, TKey5>(DataAggregateFunction function, string member, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, IDataAggregateOptions options = null)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
			where TKey4 : struct, IEquatable<TKey4>
			where TKey5 : struct, IEquatable<TKey5>
		{
			return this.Aggregate(function, member, this.ConvertKey(DataServiceMethod.Aggregate(function), key1, key2, key3, key4, key5, out _), options);
		}

		public double? Aggregate(DataAggregateFunction function, string member, ICondition criteria = null, IDataAggregateOptions options = null)
		{
			//构建数据操作的选项对象
			if(options == null)
				options = new DataAggregateOptions();

			//进行授权验证
			this.Authorize(DataServiceMethod.Aggregate(function), options);

			//修整查询条件
			criteria = this.OnValidate(DataServiceMethod.Aggregate(function), criteria, options.Filter, options);

			//执行聚合操作
			return this.OnAggregate(new DataAggregate(function, member), criteria, options);
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
			this.Authorize(DataServiceMethod.Increment(), options);

			//修整查询条件
			criteria = this.OnValidate(DataServiceMethod.Increment(), criteria, options.Filter, options);

			//执行递增操作
			return this.OnIncrement(member, criteria, interval, options);
		}

		protected virtual long OnIncrement(string member, ICondition criteria, int interval, IDataIncrementOptions options)
		{
			return this.DataAccess.Increment(this.Name, member, criteria, interval, options, ctx => this.OnIncrementing(ctx), ctx => this.OnIncremented(ctx));
		}
		#endregion

		#region 删除方法
		public int Delete(string key, IDataDeleteOptions options = null)
		{
			return this.Delete(key, null, options);
		}

		public int Delete(string key, string schema, IDataDeleteOptions options = null)
		{
			return this.Delete(this.ConvertKey(DataServiceMethod.Delete(), key, out _), schema, options);
		}

		public int Delete<TKey1>(TKey1 key1, IDataDeleteOptions options = null)
			where TKey1 : struct, IEquatable<TKey1>
		{
			return this.Delete(key1, null, options);
		}

		public int Delete<TKey1>(TKey1 key1, string schema, IDataDeleteOptions options = null)
			where TKey1 : struct, IEquatable<TKey1>
		{
			return this.Delete(this.ConvertKey(DataServiceMethod.Delete(), key1, out _), schema, options);
		}

		public int Delete<TKey1, TKey2>(TKey1 key1, TKey2 key2, IDataDeleteOptions options = null)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
		{
			return this.Delete(key1, key2, null, options);
		}

		public int Delete<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, IDataDeleteOptions options = null)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
		{
			return this.Delete(this.ConvertKey(DataServiceMethod.Delete(), key1, key2, out _), schema, options);
		}

		public int Delete<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, IDataDeleteOptions options = null)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
		{
			return this.Delete(key1, key2, key3, null, options);
		}

		public int Delete<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, IDataDeleteOptions options = null)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
		{
			return this.Delete(this.ConvertKey(DataServiceMethod.Delete(), key1, key2, key3, out _), schema, options);
		}

		public int Delete<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, IDataDeleteOptions options = null)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
			where TKey4 : struct, IEquatable<TKey4>
		{
			return this.Delete(key1, key2, key3, key4, null, options);
		}

		public int Delete<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, IDataDeleteOptions options = null)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
			where TKey4 : struct, IEquatable<TKey4>
		{
			return this.Delete(this.ConvertKey(DataServiceMethod.Delete(), key1, key2, key3, key4, out _), schema, options);
		}

		public int Delete<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, IDataDeleteOptions options = null)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
			where TKey4 : struct, IEquatable<TKey4>
			where TKey5 : struct, IEquatable<TKey5>
		{
			return this.Delete(key1, key2, key3, key4, key5, null, options);
		}

		public int Delete<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, IDataDeleteOptions options = null)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
			where TKey4 : struct, IEquatable<TKey4>
			where TKey5 : struct, IEquatable<TKey5>
		{
			return this.Delete(this.ConvertKey(DataServiceMethod.Delete(), key1, key2, key3, key4, key5, out _), schema, options);
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
			this.Authorize(DataServiceMethod.Delete(), options);

			//修整删除条件
			criteria = this.OnValidate(DataServiceMethod.Delete(), criteria, options.Filter, options);

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
			this.Authorize(DataServiceMethod.Insert(), options);

			//将当前插入数据对象转换成数据字典
			var dictionary = DataDictionary.GetDictionary<TModel>(data);

			//解析数据模式表达式
			var schematic = this.GetSchema(schema, data.GetType());

			//验证待新增的数据
			this.OnValidate(DataServiceMethod.Insert(), schematic, dictionary, options);

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
			this.Authorize(DataServiceMethod.Upsert(), options);

			//将当前复写数据对象转换成数据字典
			var dictionary = DataDictionary.GetDictionary<TModel>(data);

			//解析数据模式表达式
			var schematic = this.GetSchema(schema, data.GetType());

			//验证待复写的数据
			this.OnValidate(DataServiceMethod.Upsert(), schematic, dictionary, options);

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
		public int Update(string key, object data, IDataUpdateOptions options = null)
		{
			return this.Update(key, data, null, options);
		}

		public int Update(string key, object data, string schema, IDataUpdateOptions options = null)
		{
			return this.Update(data, this.ConvertKey(DataServiceMethod.Update(), key, out _), schema, options);
		}

		public int Update<TKey1>(TKey1 key1, object data, IDataUpdateOptions options = null)
			where TKey1 : struct, IEquatable<TKey1>
		{
			return this.Update(key1, null, data, options);
		}

		public int Update<TKey1>(TKey1 key1, string schema, object data, IDataUpdateOptions options = null)
			where TKey1 : struct, IEquatable<TKey1>
		{
			return this.Update(data, this.ConvertKey(DataServiceMethod.Update(), key1, out _), schema, options);
		}

		public int Update<TKey1, TKey2>(TKey1 key1, TKey2 key2, object data, IDataUpdateOptions options = null)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
		{
			return this.Update(key1, key2, null, data, options);
		}

		public int Update<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, object data, IDataUpdateOptions options = null)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
		{
			return this.Update(data, this.ConvertKey(DataServiceMethod.Update(), key1, key2, out _), schema, options);
		}

		public int Update<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, object data, IDataUpdateOptions options = null)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
		{
			return this.Update(key1, key2, key3, null, data, options);
		}

		public int Update<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, object data, IDataUpdateOptions options = null)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
		{
			return this.Update(data, this.ConvertKey(DataServiceMethod.Update(), key1, key2, key3, out _), schema, options);
		}

		public int Update<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, object data, IDataUpdateOptions options = null)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
			where TKey4 : struct, IEquatable<TKey4>
		{
			return this.Update(key1, key2, key3, key4, null, data, options);
		}

		public int Update<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, object data, IDataUpdateOptions options = null)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
			where TKey4 : struct, IEquatable<TKey4>
		{
			return this.Update(data, this.ConvertKey(DataServiceMethod.Update(), key1, key2, key3, key4, out _), schema, options);
		}

		public int Update<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, object data, IDataUpdateOptions options = null)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
			where TKey4 : struct, IEquatable<TKey4>
			where TKey5 : struct, IEquatable<TKey5>
		{
			return this.Update(key1, key2, key3, key4, key5, null, data, options);
		}

		public int Update<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, object data, IDataUpdateOptions options = null)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
			where TKey4 : struct, IEquatable<TKey4>
			where TKey5 : struct, IEquatable<TKey5>
		{
			return this.Update(data, this.ConvertKey(DataServiceMethod.Update(), key1, key2, key3, key4, key5, out _), schema, options);
		}

		public int Update(object data, IDataUpdateOptions options = null)
		{
			return this.Update(data, (ICondition)null, null, options);
		}

		public int Update(object data, string schema, IDataUpdateOptions options = null)
		{
			return this.Update(data, (ICondition)null, schema, options);
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
			criteria = this.OnValidate(DataServiceMethod.Update(), criteria ?? this.EnsureUpdateCondition(dictionary), options.Filter, options);

			//解析数据模式表达式
			var schematic = this.GetSchema(schema, data.GetType());

			//验证待更新的数据
			this.OnValidate(DataServiceMethod.Update(), schematic, dictionary, options);

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

		#region 键值查询
		public object Get(string key, params Sorting[] sortings)
		{
			return this.Get(key, string.Empty, null, null, sortings);
		}

		public object Get(string key, IDataSelectOptions options, params Sorting[] sortings)
		{
			return this.Get(key, string.Empty, null, options, sortings);
		}

		public object Get(string key, Paging paging, params Sorting[] sortings)
		{
			return this.Get(key, string.Empty, paging, null, sortings);
		}

		public object Get(string key, Paging paging, IDataSelectOptions options, params Sorting[] sortings)
		{
			return this.Get(key, string.Empty, paging, options, sortings);
		}

		public object Get(string key, string schema, params Sorting[] sortings)
		{
			return this.Get(key, schema, null, null, sortings);
		}

		public object Get(string key, string schema, IDataSelectOptions options, params Sorting[] sortings)
		{
			return this.Get(key, schema, null, options, sortings);
		}

		public object Get(string key, string schema, Paging paging, params Sorting[] sortings)
		{
			return this.Get(key, schema, paging, null, sortings);
		}

		public object Get(string key, string schema, Paging paging, IDataSelectOptions options, params Sorting[] sortings)
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
				return this.OnGet(criteria, this.GetSchema(schema), options);
			}

			return this.Select(criteria, schema, paging, options, sortings);
		}

		protected virtual TModel OnGet(ICondition criteria, ISchema schema, IDataSelectOptions options)
		{
			return this.DataAccess.Select<TModel>(this.Name, criteria, schema, null, options, null, ctx => this.OnGetting(ctx), ctx => this.OnGetted(ctx)).FirstOrDefault();
		}
		#endregion

		#region 单键查询
		public object Get<TKey1>(TKey1 key1, params Sorting[] sortings)
			where TKey1 : struct, IEquatable<TKey1>
		{
			return this.Get<TKey1>(key1, null, null, null, sortings);
		}

		public object Get<TKey1>(TKey1 key1, IDataSelectOptions options, params Sorting[] sortings)
			where TKey1 : struct, IEquatable<TKey1>
		{
			return this.Get<TKey1>(key1, null, null, options, sortings);
		}

		public object Get<TKey1>(TKey1 key1, Paging paging, params Sorting[] sortings)
			where TKey1 : struct, IEquatable<TKey1>
		{
			return this.Get<TKey1>(key1, null, paging, null, sortings);
		}

		public object Get<TKey1>(TKey1 key1, Paging paging, IDataSelectOptions options, params Sorting[] sortings)
			where TKey1 : struct, IEquatable<TKey1>
		{
			return this.Get<TKey1>(key1, null, paging, options, sortings);
		}

		public object Get<TKey1>(TKey1 key1, string schema, params Sorting[] sortings)
			where TKey1 : struct, IEquatable<TKey1>
		{
			return this.Get<TKey1>(key1, schema, null, null, sortings);
		}

		public object Get<TKey1>(TKey1 key1, string schema, IDataSelectOptions options, params Sorting[] sortings)
			where TKey1 : struct, IEquatable<TKey1>
		{
			return this.Get<TKey1>(key1, schema, null, options, sortings);
		}

		public object Get<TKey1>(TKey1 key1, string schema, Paging paging, params Sorting[] sortings)
			where TKey1 : struct, IEquatable<TKey1>
		{
			return this.Get<TKey1>(key1, schema, paging, null, sortings);
		}

		public object Get<TKey1>(TKey1 key1, string schema, Paging paging, IDataSelectOptions options, params Sorting[] sortings)
			where TKey1 : struct, IEquatable<TKey1>
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
				return this.OnGet(criteria, this.GetSchema(schema), options);
			}

			return this.Select(criteria, schema, paging, options, sortings);
		}
		#endregion

		#region 双键查询
		public object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, params Sorting[] sortings)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
		{
			return this.Get<TKey1, TKey2>(key1, key2, null, null, null, sortings);
		}

		public object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, IDataSelectOptions options, params Sorting[] sortings)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
		{
			return this.Get<TKey1, TKey2>(key1, key2, null, null, options, sortings);
		}

		public object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, Paging paging, params Sorting[] sortings)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
		{
			return this.Get<TKey1, TKey2>(key1, key2, null, paging, null, sortings);
		}

		public object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, Paging paging, IDataSelectOptions options, params Sorting[] sortings)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
		{
			return this.Get<TKey1, TKey2>(key1, key2, null, paging, options, sortings);
		}

		public object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, params Sorting[] sortings)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
		{
			return this.Get<TKey1, TKey2>(key1, key2, schema, null, null, sortings);
		}

		public object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, IDataSelectOptions options, params Sorting[] sortings)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
		{
			return this.Get<TKey1, TKey2>(key1, key2, schema, null, options, sortings);
		}

		public object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Paging paging, params Sorting[] sortings)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
		{
			return this.Get<TKey1, TKey2>(key1, key2, schema, paging, null, sortings);
		}

		public object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Paging paging, IDataSelectOptions options, params Sorting[] sortings)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
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
				return this.OnGet(criteria, this.GetSchema(schema), options);
			}

			return this.Select(criteria, schema, paging, options, sortings);
		}
		#endregion

		#region 三键查询
		public object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, params Sorting[] sortings)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
		{
			return this.Get<TKey1, TKey2, TKey3>(key1, key2, key3, null, null, null, sortings);
		}

		public object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, IDataSelectOptions options, params Sorting[] sortings)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
		{
			return this.Get<TKey1, TKey2, TKey3>(key1, key2, key3, null, null, options, sortings);
		}

		public object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, Paging paging, params Sorting[] sortings)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
		{
			return this.Get<TKey1, TKey2, TKey3>(key1, key2, key3, null, paging, null, sortings);
		}

		public object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, Paging paging, IDataSelectOptions options, params Sorting[] sortings)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
		{
			return this.Get<TKey1, TKey2, TKey3>(key1, key2, key3, null, paging, options, sortings);
		}

		public object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, params Sorting[] sortings)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
		{
			return this.Get<TKey1, TKey2, TKey3>(key1, key2, key3, schema, null, null, sortings);
		}

		public object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, IDataSelectOptions options, params Sorting[] sortings)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
		{
			return this.Get<TKey1, TKey2, TKey3>(key1, key2, key3, schema, null, options, sortings);
		}

		public object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Paging paging, params Sorting[] sortings)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
		{
			return this.Get<TKey1, TKey2, TKey3>(key1, key2, key3, schema, paging, null, sortings);
		}

		public object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Paging paging, IDataSelectOptions options, params Sorting[] sortings)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
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
				return this.OnGet(criteria, this.GetSchema(schema), options);
			}

			return this.Select(criteria, schema, paging, options, sortings);
		}
		#endregion

		#region 四键查询
		public object Get<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, params Sorting[] sortings)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
			where TKey4 : struct, IEquatable<TKey4>
		{
			return this.Get<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, null, null, null, sortings);
		}

		public object Get<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, IDataSelectOptions options, params Sorting[] sortings)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
			where TKey4 : struct, IEquatable<TKey4>
		{
			return this.Get<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, null, null, options, sortings);
		}

		public object Get<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, Paging paging, params Sorting[] sortings)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
			where TKey4 : struct, IEquatable<TKey4>
		{
			return this.Get<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, null, paging, null, sortings);
		}

		public object Get<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, Paging paging, IDataSelectOptions options, params Sorting[] sortings)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
			where TKey4 : struct, IEquatable<TKey4>
		{
			return this.Get<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, null, paging, options, sortings);
		}

		public object Get<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, params Sorting[] sortings)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
			where TKey4 : struct, IEquatable<TKey4>
		{
			return this.Get<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, schema, null, null, sortings);
		}

		public object Get<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, IDataSelectOptions options, params Sorting[] sortings)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
			where TKey4 : struct, IEquatable<TKey4>
		{
			return this.Get<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, schema, null, options, sortings);
		}

		public object Get<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, Paging paging, params Sorting[] sortings)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
			where TKey4 : struct, IEquatable<TKey4>
		{
			return this.Get<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, schema, paging, null, sortings);
		}

		public object Get<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, Paging paging, IDataSelectOptions options, params Sorting[] sortings)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
			where TKey4 : struct, IEquatable<TKey4>
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
				return this.OnGet(criteria, this.GetSchema(schema), options);
			}

			return this.Select(criteria, schema, paging, options, sortings);
		}
		#endregion

		#region 五键查询
		public object Get<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, params Sorting[] sortings)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
			where TKey4 : struct, IEquatable<TKey4>
			where TKey5 : struct, IEquatable<TKey5>
		{
			return this.Get<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, null, null, null, sortings);
		}

		public object Get<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, IDataSelectOptions options, params Sorting[] sortings)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
			where TKey4 : struct, IEquatable<TKey4>
			where TKey5 : struct, IEquatable<TKey5>
		{
			return this.Get<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, null, null, options, sortings);
		}

		public object Get<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, Paging paging, params Sorting[] sortings)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
			where TKey4 : struct, IEquatable<TKey4>
			where TKey5 : struct, IEquatable<TKey5>
		{
			return this.Get<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, null, paging, null, sortings);
		}

		public object Get<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, Paging paging, IDataSelectOptions options, params Sorting[] sortings)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
			where TKey4 : struct, IEquatable<TKey4>
			where TKey5 : struct, IEquatable<TKey5>
		{
			return this.Get<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, null, paging, options, sortings);
		}

		public object Get<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, params Sorting[] sortings)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
			where TKey4 : struct, IEquatable<TKey4>
			where TKey5 : struct, IEquatable<TKey5>
		{
			return this.Get<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, schema, null, null, sortings);
		}

		public object Get<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, IDataSelectOptions options, params Sorting[] sortings)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
			where TKey4 : struct, IEquatable<TKey4>
			where TKey5 : struct, IEquatable<TKey5>
		{
			return this.Get<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, schema, null, options, sortings);
		}

		public object Get<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, Paging paging, params Sorting[] sortings)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
			where TKey4 : struct, IEquatable<TKey4>
			where TKey5 : struct, IEquatable<TKey5>
		{
			return this.Get<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, schema, paging, null, sortings);
		}

		public object Get<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, Paging paging, IDataSelectOptions options, params Sorting[] sortings)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
			where TKey4 : struct, IEquatable<TKey4>
			where TKey5 : struct, IEquatable<TKey5>
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
				return this.OnGet(criteria, this.GetSchema(schema), options);
			}

			return this.Select(criteria, schema, paging, options, sortings);
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
			this.Authorize(DataServiceMethod.Select(), options);

			//修整查询条件
			criteria = this.OnValidate(DataServiceMethod.Select(), criteria, options.Filter, options);

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

		public IEnumerable<T> Select<T>(Grouping grouping, Paging paging, params Sorting[] sortings)
		{
			return this.Select<T>(grouping, null, string.Empty, paging, null, sortings);
		}

		public IEnumerable<T> Select<T>(Grouping grouping, Paging paging, IDataSelectOptions options, params Sorting[] sortings)
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

		public IEnumerable<T> Select<T>(Grouping grouping, string schema, Paging paging, params Sorting[] sortings)
		{
			return this.Select<T>(grouping, null, schema, paging, null, sortings);
		}

		public IEnumerable<T> Select<T>(Grouping grouping, string schema, Paging paging, IDataSelectOptions options, params Sorting[] sortings)
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

		public IEnumerable<T> Select<T>(Grouping grouping, ICondition criteria, string schema, Paging paging, params Sorting[] sortings)
		{
			return this.Select<T>(grouping, criteria, schema, paging, null, sortings);
		}

		public IEnumerable<T> Select<T>(Grouping grouping, ICondition criteria, string schema, Paging paging, IDataSelectOptions options, params Sorting[] sortings)
		{
			//构建数据操作的选项对象
			if(options == null)
				options = new DataSelectOptions();

			//进行授权验证
			this.Authorize(DataServiceMethod.Select(), options);

			//修整查询条件
			criteria = this.OnValidate(DataServiceMethod.Select(), criteria, options.Filter, options);

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
			var sortings = _attribute?.GetSortings();

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
		protected virtual ICondition OnValidate(DataServiceMethod method, ICondition criteria, string filter, IDataOptions options)
		{
			var validator = this.Validator;
			return validator == null ? criteria : validator.Validate(method, criteria, filter, options);
		}

		protected virtual void OnValidate(DataServiceMethod method, ISchema schema, IDataDictionary<TModel> data, IDataMutateOptions options)
		{
			this.Validator?.Validate(method, schema, data, options);
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
		/// 将指定的键值文本转换为操作条件。
		/// </summary>
		/// <param name="method">指定的操作方法。</param>
		/// <param name="key">指定的键值文本。</param>
		/// <param name="singular">输出一个值，指示转换后的操作条件作用结果是否为必定为单个对象。</param>
		/// <returns>返回对应的操作条件。</returns>
		protected virtual ICondition OnCondition(DataServiceMethod method, string key, out bool singular)
		{
			singular = false;

			if(string.IsNullOrWhiteSpace(key))
				return null;

			if(_attribute != null && _attribute.Criteria != null && CriteriaParser.TryParse(key, out var members))
				return Criteria.Transform(_attribute.Criteria, members, true);

			return this.OnCondition(method, Common.StringExtension.Slice(key, '-').ToArray(), out singular);
		}

		/// <summary>
		/// 将指定的键值数组转换为操作条件。
		/// </summary>
		/// <param name="method">指定的操作方法。</param>
		/// <param name="values">指定的键值数组。</param>
		/// <param name="singular">输出一个值，指示转换后的操作条件作用结果是否为必定为单个对象。</param>
		/// <returns>返回对应的操作条件。</returns>
		protected virtual ICondition OnCondition(DataServiceMethod method, object[] values, out bool singular)
		{
			//设置输出参数默认值
			singular = false;

			//获取当前数据服务对应的主键
			var primaryKey = this.DataAccess.Metadata.Entities.Get(this.Name).Key;

			//如果主键获取失败或主键未定义或主键项数量小于传入的数组元素个数则返回空
			if(primaryKey == null || primaryKey.Length == 0 || primaryKey.Length < values.Length)
				return null;

			//只有传入参数值数量与主键数匹配其结果才为单个项
			singular = primaryKey.Length == values.Length;

			if(primaryKey.Length == 1)
			{
				if(values[0] is string text && text != null && text.Length > 0)
				{
					var parts = Zongsoft.Common.StringExtension.Slice(text, ',').ToArray();

					if(parts.Length > 1)
					{
						singular = false;
						return Condition.In(primaryKey[0].Name, parts);
					}
				}

				return Condition.Equal(primaryKey[0].Name, ConvertValue(values[0], primaryKey[0].Type));
			}

			var conditions = ConditionCollection.And();

			for(int i = 0; i < values.Length; i++)
			{
				conditions.Add(Data.Condition.Equal(primaryKey[i].Name, ConvertValue(values[i], primaryKey[i].Type)));
			}

			return conditions;
		}
		#endregion

		#region 私有方法
		private ICondition ConvertKey(DataServiceMethod method, string key, out bool singular)
		{
			if(string.IsNullOrWhiteSpace(key))
			{
				singular = false;
				return null;
			}

			return this.OnCondition(method, key, out singular);
		}

		private ICondition ConvertKey<TKey1>(DataServiceMethod method, TKey1 key1, out bool singular)
			where TKey1 : struct, IEquatable<TKey1>
		{
			return this.OnCondition(method, new object[] { key1 }, out singular);
		}

		private ICondition ConvertKey<TKey1, TKey2>(DataServiceMethod method, TKey1 key1, TKey2 key2, out bool singular)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
		{
			return this.OnCondition(method, new object[] { key1, key2 }, out singular);
		}

		private ICondition ConvertKey<TKey1, TKey2, TKey3>(DataServiceMethod method, TKey1 key1, TKey2 key2, TKey3 key3, out bool singular)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
		{
			return this.OnCondition(method, new object[] { key1, key2, key3 }, out singular);
		}

		private ICondition ConvertKey<TKey1, TKey2, TKey3, TKey4>(DataServiceMethod method, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, out bool singular)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
			where TKey4 : struct, IEquatable<TKey4>
		{
			return this.OnCondition(method, new object[] { key1, key2, key3, key4 }, out singular);
		}

		private ICondition ConvertKey<TKey1, TKey2, TKey3, TKey4, TKey5>(DataServiceMethod method, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, out bool singular)
			where TKey1 : struct, IEquatable<TKey1>
			where TKey2 : struct, IEquatable<TKey2>
			where TKey3 : struct, IEquatable<TKey3>
			where TKey4 : struct, IEquatable<TKey4>
			where TKey5 : struct, IEquatable<TKey5>
		{
			return this.OnCondition(method, new object[] { key1, key2, key3, key4, key5 }, out singular);
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
		public sealed class Condition : Zongsoft.Data.Condition.Builder<TModel>
		{
			private Condition() { }
		}
		#endregion
	}
}
