﻿/*
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
using System.Data;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

using Zongsoft.Common;
using Zongsoft.Services;

namespace Zongsoft.Data
{
	[Service<IDataService>]
	[DefaultMember(nameof(Filters))]
	public abstract partial class DataServiceBase<TModel> : IDataService<TModel>, IMatchable, IMatchable<string>
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
		private Dictionary<Type, IDataService> _subservices;
		private readonly DataServiceAttribute _attribute;
		private readonly DataServiceMutability? _mutability;
		private readonly DataServiceFilterCollection<TModel> _filters;
		#endregion

		#region 构造函数
		protected DataServiceBase(IServiceProvider serviceProvider)
		{
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
			_attribute = (DataServiceAttribute)System.Attribute.GetCustomAttribute(this.GetType(), typeof(DataServiceAttribute), true);
			_filters = new DataServiceFilterCollection<TModel>(this);

			//创建数据搜索器
			_searcher = new DataSearcher<TModel>(this);

			//初始化嵌套子服务集
			this.InitializeSubservices();
		}

		protected DataServiceBase(IServiceProvider serviceProvider, IDataServiceValidator<TModel> validator, IDataServiceAuthorizer<TModel> authorizer = null) : this(serviceProvider)
		{
			this.Validator = validator;
			this.Authorizer = authorizer;
		}

		protected DataServiceBase(IServiceProvider serviceProvider, IDataServiceAuthorizer<TModel> authorizer, IDataServiceValidator<TModel> validator = null) : this(serviceProvider)
		{
			this.Authorizer = authorizer;
			this.Validator = validator;
		}

		protected DataServiceBase(IServiceProvider serviceProvider, DataServiceMutability? mutability)
		{
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
			_attribute = (DataServiceAttribute)System.Attribute.GetCustomAttribute(this.GetType(), typeof(DataServiceAttribute), true);
			_filters = _filters = new DataServiceFilterCollection<TModel>(this);
			_mutability = mutability;

			//创建数据搜索器
			_searcher = new DataSearcher<TModel>(this);

			//初始化嵌套子服务集
			this.InitializeSubservices();
		}

		protected DataServiceBase(IServiceProvider serviceProvider, DataServiceMutability? mutability, IDataServiceValidator<TModel> validator, IDataServiceAuthorizer<TModel> authorizer = null) : this(serviceProvider, mutability)
		{
			this.Validator = validator;
			this.Authorizer = authorizer;
		}

		protected DataServiceBase(IServiceProvider serviceProvider, DataServiceMutability? mutability, IDataServiceAuthorizer<TModel> authorizer, IDataServiceValidator<TModel> validator = null) : this(serviceProvider, mutability)
		{
			this.Authorizer = authorizer;
			this.Validator = validator;
		}

		protected DataServiceBase(string name, IServiceProvider serviceProvider)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			_name = name.Trim();
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
			_attribute = (DataServiceAttribute)System.Attribute.GetCustomAttribute(this.GetType(), typeof(DataServiceAttribute), true);
			_filters = _filters = new DataServiceFilterCollection<TModel>(this);

			//创建数据搜索器
			_searcher = new DataSearcher<TModel>(this);

			//初始化嵌套子服务集
			this.InitializeSubservices();
		}

		protected DataServiceBase(string name, IServiceProvider serviceProvider, IDataServiceValidator<TModel> validator, IDataServiceAuthorizer<TModel> authorizer = null) : this(name, serviceProvider)
		{
			this.Validator = validator;
			this.Authorizer = authorizer;
		}

		protected DataServiceBase(string name, IServiceProvider serviceProvider, IDataServiceAuthorizer<TModel> authorizer, IDataServiceValidator<TModel> validator = null) : this(name, serviceProvider)
		{
			this.Authorizer = authorizer;
			this.Validator = validator;
		}

		protected DataServiceBase(string name, IServiceProvider serviceProvider, DataServiceMutability? mutability)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			_name = name.Trim();
			_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
			_attribute = (DataServiceAttribute)System.Attribute.GetCustomAttribute(this.GetType(), typeof(DataServiceAttribute), true);
			_filters = _filters = new DataServiceFilterCollection<TModel>(this);
			_mutability = mutability;

			//创建数据搜索器
			_searcher = new DataSearcher<TModel>(this);

			//初始化嵌套子服务集
			this.InitializeSubservices();
		}

		protected DataServiceBase(string name, IServiceProvider serviceProvider, DataServiceMutability? mutability, IDataServiceValidator<TModel> validator, IDataServiceAuthorizer<TModel> authorizer = null) : this(name, serviceProvider, mutability)
		{
			this.Validator = validator;
			this.Authorizer = authorizer;
		}

		protected DataServiceBase(string name, IServiceProvider serviceProvider, DataServiceMutability? mutability, IDataServiceAuthorizer<TModel> authorizer, IDataServiceValidator<TModel> validator = null) : this(name, serviceProvider, mutability)
		{
			this.Authorizer = authorizer;
			this.Validator = validator;
		}

		protected DataServiceBase(IDataService service)
		{
			this.Service = service ?? throw new ArgumentNullException(nameof(service));
			_attribute = (DataServiceAttribute)System.Attribute.GetCustomAttribute(this.GetType(), typeof(DataServiceAttribute), true);
			_filters = _filters = new DataServiceFilterCollection<TModel>(this);
			_mutability = DataServiceMutability.All;

			//创建数据搜索器
			_searcher = new DataSearcher<TModel>(this);

			//初始化嵌套子服务集
			this.InitializeSubservices();
		}

		protected DataServiceBase(IDataService service, IDataServiceValidator<TModel> validator, IDataServiceAuthorizer<TModel> authorizer = null) : this(service)
		{
			this.Validator = validator;
			this.Authorizer = authorizer;
		}

		protected DataServiceBase(IDataService service, IDataServiceAuthorizer<TModel> authorizer, IDataServiceValidator<TModel> validator = null) : this(service)
		{
			this.Authorizer = authorizer;
			this.Validator = validator;
		}

		protected DataServiceBase(IDataService service, DataServiceMutability? mutability)
		{
			this.Service = service ?? throw new ArgumentNullException(nameof(service));
			_attribute = (DataServiceAttribute)System.Attribute.GetCustomAttribute(this.GetType(), typeof(DataServiceAttribute), true);
			_filters = _filters = new DataServiceFilterCollection<TModel>(this);
			_mutability = mutability;

			//创建数据搜索器
			_searcher = new DataSearcher<TModel>(this);

			//初始化嵌套子服务集
			this.InitializeSubservices();
		}

		protected DataServiceBase(IDataService service, DataServiceMutability? mutability, IDataServiceValidator<TModel> validator, IDataServiceAuthorizer<TModel> authorizer = null) : this(service, mutability)
		{
			this.Validator = validator;
			this.Authorizer = authorizer;
		}

		protected DataServiceBase(IDataService service, DataServiceMutability? mutability, IDataServiceAuthorizer<TModel> authorizer, IDataServiceValidator<TModel> validator = null) : this(service, mutability)
		{
			this.Authorizer = authorizer;
			this.Validator = validator;
		}

		protected DataServiceBase(string name, IDataService service)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			_name = name.Trim();
			this.Service = service ?? throw new ArgumentNullException(nameof(service));
			_attribute = (DataServiceAttribute)System.Attribute.GetCustomAttribute(this.GetType(), typeof(DataServiceAttribute), true);
			_filters = _filters = new DataServiceFilterCollection<TModel>(this);
			_mutability = DataServiceMutability.All;

			//创建数据搜索器
			_searcher = new DataSearcher<TModel>(this);

			//初始化嵌套子服务集
			this.InitializeSubservices();
		}

		protected DataServiceBase(string name, IDataService service, IDataServiceValidator<TModel> validator, IDataServiceAuthorizer<TModel> authorizer = null) : this(name, service)
		{
			this.Validator = validator;
			this.Authorizer = authorizer;
		}

		protected DataServiceBase(string name, IDataService service, IDataServiceAuthorizer<TModel> authorizer, IDataServiceValidator<TModel> validator = null) : this(name, service)
		{
			this.Authorizer = authorizer;
			this.Validator = validator;
		}

		protected DataServiceBase(string name, IDataService service, DataServiceMutability? mutability)
		{
			if(string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));

			_name = name.Trim();
			this.Service = service ?? throw new ArgumentNullException(nameof(service));
			_attribute = (DataServiceAttribute)System.Attribute.GetCustomAttribute(this.GetType(), typeof(DataServiceAttribute), true);
			_filters = _filters = new DataServiceFilterCollection<TModel>(this);
			_mutability = mutability;

			//创建数据搜索器
			_searcher = new DataSearcher<TModel>(this);

			//初始化嵌套子服务集
			this.InitializeSubservices();
		}

		protected DataServiceBase(string name, IDataService service, DataServiceMutability? mutability, IDataServiceValidator<TModel> validator, IDataServiceAuthorizer<TModel> authorizer = null) : this(name, service, mutability)
		{
			this.Validator = validator;
			this.Authorizer = authorizer;
		}

		protected DataServiceBase(string name, IDataService service, DataServiceMutability? mutability, IDataServiceAuthorizer<TModel> authorizer, IDataServiceValidator<TModel> validator = null) : this(name, service, mutability)
		{
			this.Authorizer = authorizer;
			this.Validator = validator;
		}
		#endregion

		#region 公共属性
		public string Name
		{
			get
			{
				if(string.IsNullOrEmpty(_name))
					_name = Model.Naming.Get<TModel>();

				return _name;
			}
			protected set
			{
				if(string.IsNullOrWhiteSpace(value))
					throw new ArgumentNullException();

				_name = value.Trim();
			}
		}

		public DataServiceAttribute Attribute => _attribute;
		public virtual bool CanInsert => _mutability.HasValue ? _mutability.Value.Insertable : (this.Service?.CanInsert ?? true);
		public virtual bool CanUpdate => _mutability.HasValue ? _mutability.Value.Updatable : (this.Service?.CanUpdate ?? true);
		public virtual bool CanUpsert => _mutability.HasValue ? _mutability.Value.Upsertable : this.CanInsert && this.CanUpdate;
		public virtual bool CanDelete => _mutability.HasValue ? _mutability.Value.Deletable : this.Service != null && this.Service.IsMutable; //在未定义Mutability时，只要依赖的主服务可写则该子服务(即Service属性不为空)可删

		public IDataAccess DataAccess
		{
			get
			{
				if(_dataAccess == null)
				{
					if(this.Service != null)
						return this.Service.DataAccess;

					var provider = _serviceProvider?.Resolve<IDataAccessProvider>();
					if(provider != null)
						_dataAccess = provider.GetAccessor(ApplicationModuleAttribute.Find(this.GetType())?.Name);
				}

				return _dataAccess;
			}
			set => _dataAccess = value ?? throw new ArgumentNullException();
		}

		public IDataSearcher<TModel> Searcher
		{
			get => _searcher;
			set => _searcher = value ?? throw new ArgumentNullException();
		}

		public IDataService Service { get; }
		public virtual System.Security.Claims.ClaimsPrincipal Principal => ApplicationContext.Current?.Principal;
		public IDataServiceAuthorizer<TModel> Authorizer { get; protected set; }
		public IDataServiceValidator<TModel> Validator { get; protected set; }
		IDataServiceValidator IDataService.Validator => this.Validator;
		public ICollection<IDataServiceFilter<TModel>> Filters => _filters;

		public IServiceProvider ServiceProvider
		{
			get => _serviceProvider ?? this.Service?.ServiceProvider;
			set => _serviceProvider = value ?? throw new ArgumentNullException();
		}
		#endregion

		#region 保护属性
		protected DataServiceMutability? Mutability => _mutability;
		#endregion

		#region 获取服务
		private void InitializeSubservices()
		{
			static IEnumerable<Type> GetNestedTypes(Type type)
			{
				while(type != null && !type.IsGenericType && type != typeof(object))
				{
					var nestedTypes = type.GetNestedTypes().Where(type => type.IsNestedPublic && type.IsClass && !type.IsAbstract && typeof(IDataService).IsAssignableFrom(type));

					foreach(var nestedType in nestedTypes)
						yield return nestedType;

					type = type.BaseType;
				}
			}

			var nestedTypes = GetNestedTypes(this.GetType());
			var serviceDescriptors = nestedTypes.GroupBy(
				type =>
				{
					object[] args = null;

					foreach(var constructor in type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
					{
						var parameters = constructor.GetParameters();

						switch(parameters.Length)
						{
							case 0:
								continue;
							case 1:
								if(parameters[0].ParameterType.IsAssignableFrom(this.GetType()))
									args = new[] { this };
								break;
							default:
								if(parameters[0].ParameterType.IsAssignableFrom(this.GetType()))
								{
									args = new object[parameters.Length];
									args[0] = this;

									for(int i = 1; i < parameters.Length; i++)
									{
										args[i] = parameters[i].HasDefaultValue ?
											parameters[i].DefaultValue :
											parameters[i].ParameterType.GetDefaultValue();
									}
								}

								break;
						}

						if(args != null && args.Length > 0)
							break;
					}

					return (IDataService)Activator.CreateInstance(type, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, args, null);
				},
				type =>
				{
					var contracts = new List<Type>() { type };
					var baseType = type.BaseType;

					while(baseType != null && !baseType.IsGenericType && baseType != typeof(object))
					{
						contracts.Add(baseType);
						baseType = baseType.BaseType;
					}

					return contracts;
				}
			).ToArray();

			if(serviceDescriptors.Length > 0)
			{
				_subservices = new Dictionary<Type, IDataService>();

				for(int i = 0; i < serviceDescriptors.Length; i++)
				{
					var subservice = serviceDescriptors[i].Key;
					if(subservice == null)
						continue;

					foreach(var serviceDescriptor in serviceDescriptors[i])
					{
						foreach(var contract in serviceDescriptor)
							_subservices.TryAdd(contract, subservice);
					}
				}
			}
		}

		public IDataService GetSubservice(Type type) => type != null && _subservices != null && _subservices.TryGetValue(type, out var result) ? result : null;
		public TService GetSubservice<TService>() where TService : class, IDataService => _subservices != null && _subservices.TryGetValue(typeof(TService), out var result) ? (TService)result : default;
		#endregion

		#region 授权验证
		protected virtual void Authorize(DataServiceMethod method, IDataOptions options)
		{
			var authorizer = this.Authorizer;

			if(authorizer != null)
			{
				authorizer.Authorize(this, method, options);
			}
			else
			{
				if(Security.ClaimsPrincipalExtension.IsAnonymous(this.Principal))
					throw new Security.Membership.AuthorizationException();
			}
		}
		#endregion

		#region 执行方法
		public IEnumerable<T> Execute<T>(string name, IDictionary<string, object> inParameters, DataExecuteOptions options = null) => this.Execute<T>(name, inParameters, out _, options);
		public IEnumerable<T> Execute<T>(string name, IDictionary<string, object> inParameters, out IDictionary<string, object> outParameters, DataExecuteOptions options = null)
		{
			//构建数据操作的选项对象
			if(options == null)
				options = new DataExecuteOptions();

			//进行授权验证
			this.Authorize(DataServiceMethod.Execute(), options);

			return this.OnExecute<T>(name, inParameters, out outParameters, options);
		}

		protected virtual IEnumerable<T> OnExecute<T>(string name, IDictionary<string, object> inParameters, out IDictionary<string, object> outParameters, DataExecuteOptions options)
		{
			return this.DataAccess.Execute<T>(name, inParameters, out outParameters, options, ctx => this.OnExecuting(ctx), ctx => this.OnExecuted(ctx));
		}

		public object ExecuteScalar(string name, IDictionary<string, object> inParameters, DataExecuteOptions options = null) => this.ExecuteScalar(name, inParameters, out _, options);
		public object ExecuteScalar(string name, IDictionary<string, object> inParameters, out IDictionary<string, object> outParameters, DataExecuteOptions options = null)
		{
			//构建数据操作的选项对象
			if(options == null)
				options = new DataExecuteOptions();

			//进行授权验证
			this.Authorize(DataServiceMethod.Execute(), options);

			return this.OnExecuteScalar(name, inParameters, out outParameters, options);
		}

		protected virtual object OnExecuteScalar(string name, IDictionary<string, object> inParameters, out IDictionary<string, object> outParameters, DataExecuteOptions options)
		{
			return this.DataAccess.ExecuteScalar(name, inParameters, out outParameters, options, ctx => this.OnExecuting(ctx), ctx => this.OnExecuted(ctx));
		}
		#endregion

		#region 存在方法
		public bool Exists(string key, DataExistsOptions options = null) => this.Exists(this.ConvertKey(DataServiceMethod.Exists(), key, options, out _), options);
		public bool Exists<TKey1>(TKey1 key1, DataExistsOptions options = null)
			where TKey1 : IEquatable<TKey1> => this.Exists(this.ConvertKey(DataServiceMethod.Exists(), key1, options, out _), options);
		public bool Exists<TKey1, TKey2>(TKey1 key1, TKey2 key2, DataExistsOptions options = null)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2> => this.Exists(this.ConvertKey(DataServiceMethod.Exists(), key1, key2, options, out _), options);
		public bool Exists<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, DataExistsOptions options = null)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3> => this.Exists(this.ConvertKey(DataServiceMethod.Exists(), key1, key2, key3, options, out _), options);
		public bool Exists<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataExistsOptions options = null)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4> => this.Exists(this.ConvertKey(DataServiceMethod.Exists(), key1, key2, key3, key4, options, out _), options);
		public bool Exists<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataExistsOptions options = null)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TKey5 : IEquatable<TKey5> => this.Exists(this.ConvertKey(DataServiceMethod.Exists(), key1, key2, key3, key4, key5, options, out _), options);

		public bool Exists(ICondition criteria, DataExistsOptions options = null)
		{
			//构建数据操作的选项对象
			if(options == null)
				options = new DataExistsOptions();

			//进行授权验证
			this.Authorize(DataServiceMethod.Exists(), options);

			//修整查询条件
			criteria = this.OnValidate(DataServiceMethod.Exists(), criteria, options);

			//执行存在操作
			return this.OnExists(criteria, options);
		}

		public bool Exists(Data.Condition criteria, DataExistsOptions options = null) => this.Exists((ICondition)criteria, options);
		public bool Exists(ConditionCollection criteria, DataExistsOptions options = null) => this.Exists((ICondition)criteria, options);

		protected virtual bool OnExists(ICondition criteria, DataExistsOptions options)
		{
			return this.DataAccess.Exists(this.Name, criteria, options, ctx => this.OnExisting(ctx), ctx => this.OnExisted(ctx));
		}
		#endregion

		#region 聚合方法
		public int Count(string key, string member = null, DataAggregateOptions options = null) => this.Count(this.ConvertKey(DataServiceMethod.Count(), key, options, out _), member, options);
		public int Count<TKey1>(TKey1 key1, string member = null, DataAggregateOptions options = null)
			where TKey1 : IEquatable<TKey1> => this.Count(this.ConvertKey(DataServiceMethod.Count(), key1, options, out _), member, options);
		public int Count<TKey1, TKey2>(TKey1 key1, TKey2 key2, string member = null, DataAggregateOptions options = null)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2> => this.Count(this.ConvertKey(DataServiceMethod.Count(), key1, key2, options, out _), member, options);
		public int Count<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string member = null, DataAggregateOptions options = null)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3> => this.Count(this.ConvertKey(DataServiceMethod.Count(), key1, key2, key3, options, out _), member, options);
		public int Count<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string member = null, DataAggregateOptions options = null)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4> => this.Count(this.ConvertKey(DataServiceMethod.Count(), key1, key2, key3, key4, options, out _), member, options);
		public int Count<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string member = null, DataAggregateOptions options = null)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TKey5 : IEquatable<TKey5> => this.Count(this.ConvertKey(DataServiceMethod.Count(), key1, key2, key3, key4, key5, options, out _), member, options);

		public int Count(ICondition criteria = null, string member = null, DataAggregateOptions options = null)
		{
			//构建数据操作的选项对象
			if(options == null)
				options = new DataAggregateOptions();

			//进行授权验证
			this.Authorize(DataServiceMethod.Count(), options);

			//修整查询条件
			criteria = this.OnValidate(DataServiceMethod.Count(), criteria, options);

			//执行聚合操作
			return this.OnAggregate<int>(new DataAggregate(DataAggregateFunction.Count, member), criteria, options) ?? 0;
		}

		public int Count(Data.Condition criteria = null, string member = null, DataAggregateOptions options = null) => this.Count((ICondition)criteria, member, options);
		public int Count(ConditionCollection criteria = null, string member = null, DataAggregateOptions options = null) => this.Count((ICondition)criteria, member, options);

		public TValue? Aggregate<TValue>(DataAggregateFunction function, string member, string key, DataAggregateOptions options = null)
			where TValue : struct, IEquatable<TValue> => this.Aggregate<TValue>(function, member, this.ConvertKey(DataServiceMethod.Aggregate(function), key, options, out _), options);
		public TValue? Aggregate<TKey1, TValue>(DataAggregateFunction function, string member, TKey1 key1, DataAggregateOptions options = null)
			where TKey1 : IEquatable<TKey1>
			where TValue : struct, IEquatable<TValue> => this.Aggregate<TValue>(function, member, this.ConvertKey(DataServiceMethod.Aggregate(function), key1, options, out _), options);
		public TValue? Aggregate<TKey1, TKey2, TValue>(DataAggregateFunction function, string member, TKey1 key1, TKey2 key2, DataAggregateOptions options = null)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TValue : struct, IEquatable<TValue> => this.Aggregate<TValue>(function, member, this.ConvertKey(DataServiceMethod.Aggregate(function), key1, key2, options, out _), options);
		public TValue? Aggregate<TKey1, TKey2, TKey3, TValue>(DataAggregateFunction function, string member, TKey1 key1, TKey2 key2, TKey3 key3, DataAggregateOptions options = null)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TValue : struct, IEquatable<TValue> => this.Aggregate<TValue>(function, member, this.ConvertKey(DataServiceMethod.Aggregate(function), key1, key2, key3, options, out _), options);
		public TValue? Aggregate<TKey1, TKey2, TKey3, TKey4, TValue>(DataAggregateFunction function, string member, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataAggregateOptions options = null)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TValue : struct, IEquatable<TValue> => this.Aggregate<TValue>(function, member, this.ConvertKey(DataServiceMethod.Aggregate(function), key1, key2, key3, key4, options, out _), options);
		public TValue? Aggregate<TKey1, TKey2, TKey3, TKey4, TKey5, TValue>(DataAggregateFunction function, string member, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataAggregateOptions options = null)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TKey5 : IEquatable<TKey5>
			where TValue : struct, IEquatable<TValue> => this.Aggregate<TValue>(function, member, this.ConvertKey(DataServiceMethod.Aggregate(function), key1, key2, key3, key4, key5, options, out _), options);

		public TValue? Aggregate<TValue>(DataAggregateFunction function, string member, ICondition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue>
		{
			//构建数据操作的选项对象
			if(options == null)
				options = new DataAggregateOptions();

			//进行授权验证
			this.Authorize(DataServiceMethod.Aggregate(function), options);

			//修整查询条件
			criteria = this.OnValidate(DataServiceMethod.Aggregate(function), criteria, options);

			//执行聚合操作
			return this.OnAggregate<TValue>(new DataAggregate(function, member), criteria, options);
		}

		public TValue? Aggregate<TValue>(DataAggregateFunction function, string member, Data.Condition criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => this.Aggregate<TValue>(function, member, (ICondition)criteria, options);
		public TValue? Aggregate<TValue>(DataAggregateFunction function, string member, ConditionCollection criteria = null, DataAggregateOptions options = null) where TValue : struct, IEquatable<TValue> => this.Aggregate<TValue>(function, member, (ICondition)criteria, options);

		protected virtual TValue? OnAggregate<TValue>(DataAggregate aggregate, ICondition criteria, DataAggregateOptions options) where TValue : struct, IEquatable<TValue>
		{
			return this.DataAccess.Aggregate<TValue>(this.Name, aggregate, criteria, options, ctx => this.OnAggregating(ctx), ctx => this.OnAggregated(ctx));
		}
		#endregion

		#region 删除方法
		public int Delete(string key, DataDeleteOptions options = null) => this.Delete(key, null, options);
		public int Delete(string key, string schema, DataDeleteOptions options = null) => this.Delete(this.ConvertKey(DataServiceMethod.Delete(), key, options, out _), schema, options);

		public int Delete<TKey1>(TKey1 key1, DataDeleteOptions options = null)
			where TKey1 : IEquatable<TKey1> => this.Delete(key1, null, options);
		public int Delete<TKey1>(TKey1 key1, string schema, DataDeleteOptions options = null)
			where TKey1 : IEquatable<TKey1> => this.Delete(this.ConvertKey(DataServiceMethod.Delete(), key1, options, out _), schema, options);

		public int Delete<TKey1, TKey2>(TKey1 key1, TKey2 key2, DataDeleteOptions options = null)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2> => this.Delete(key1, key2, null, options);
		public int Delete<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, DataDeleteOptions options = null)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2> => this.Delete(this.ConvertKey(DataServiceMethod.Delete(), key1, key2, options, out _), schema, options);

		public int Delete<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, DataDeleteOptions options = null)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3> => this.Delete(key1, key2, key3, null, options);
		public int Delete<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, DataDeleteOptions options = null)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3> => this.Delete(this.ConvertKey(DataServiceMethod.Delete(), key1, key2, key3, options, out _), schema, options);

		public int Delete<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataDeleteOptions options = null)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4> => this.Delete(key1, key2, key3, key4, null, options);
		public int Delete<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, DataDeleteOptions options = null)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4> => this.Delete(this.ConvertKey(DataServiceMethod.Delete(), key1, key2, key3, key4, options, out _), schema, options);

		public int Delete<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataDeleteOptions options = null)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TKey5 : IEquatable<TKey5> => this.Delete(key1, key2, key3, key4, key5, null, options);
		public int Delete<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, DataDeleteOptions options = null)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TKey5 : IEquatable<TKey5> => this.Delete(this.ConvertKey(DataServiceMethod.Delete(), key1, key2, key3, key4, key5, options, out _), schema, options);

		public int Delete(ICondition criteria, DataDeleteOptions options = null) => this.Delete(criteria, null, options);
		public int Delete(ICondition criteria, string schema, DataDeleteOptions options = null)
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
			return this.OnDelete(criteria, this.GetSchema(schema), options);
		}

		public int Delete(Data.Condition criteria, DataDeleteOptions options = null) => this.Delete((ICondition)criteria, options);
		public int Delete(Data.Condition criteria, string schema, DataDeleteOptions options = null) => this.Delete((ICondition)criteria, schema, options);
		public int Delete(ConditionCollection criteria, DataDeleteOptions options = null) => this.Delete((ICondition)criteria, options);
		public int Delete(ConditionCollection criteria, string schema, DataDeleteOptions options = null) => this.Delete((ICondition)criteria, schema, options);

		protected virtual int OnDelete(ICondition criteria, ISchema schema, DataDeleteOptions options)
		{
			if(criteria == null)
				throw new NotSupportedException("The criteria cann't is null on delete operation.");

			return this.DataAccess.Delete(this.Name, criteria, schema, options, ctx => this.OnDeleting(ctx), ctx => this.OnDeleted(ctx));
		}
		#endregion

		#region 插入方法
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
			var schematic = this.GetSchema(schema, TypeExtension.GetElementType(items.GetType()));

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
			var schematic = this.GetSchema(schema, TypeExtension.GetElementType(items.GetType()));

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
		#endregion

		#region 增改方法
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
			var schematic = this.GetSchema(schema, TypeExtension.GetElementType(items.GetType()));

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
			var schematic = this.GetSchema(schema, TypeExtension.GetElementType(items.GetType()));

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
		#endregion

		#region 更新方法
		public int Update(string key, object data, DataUpdateOptions options = null) => this.Update(key, data, null, options);
		public int Update(string key, object data, string schema, DataUpdateOptions options = null) => this.Update(data, this.ConvertKey(DataServiceMethod.Update(), key, options, out _), schema, options);

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

		public int UpdateMany(IEnumerable items, DataUpdateOptions options = null) => this.UpdateMany(items, string.Empty, options);
		public int UpdateMany(IEnumerable items, string schema, DataUpdateOptions options = null)
		{
			//确认是否可以执行该操作
			this.EnsureUpdate(options);

			if(items == null)
				return 0;

			//构建数据操作的选项对象
			if(options == null)
				options = new DataUpdateOptions();

			//进行授权验证
			this.Authorize(DataServiceMethod.UpdateMany(), options);

			//解析数据模式表达式
			var schematic = this.GetSchema(schema, TypeExtension.GetElementType(items.GetType()));

			//将当前更新数据集合对象转换成数据字典集合
			var dictionaries = DataDictionary.GetDictionaries<TModel>(items, dictionary =>
			{
				//验证待更新的数据
				this.OnValidate(DataServiceMethod.UpdateMany(), schematic, dictionary, options);
			});

			return this.OnUpdateMany(dictionaries, schematic, options);
		}

		public int UpdateMany(string key, IEnumerable items, DataUpdateOptions options = null) => this.UpdateMany(key, items, null, options);
		public int UpdateMany(string key, IEnumerable items, string schema, DataUpdateOptions options = null)
		{
			//确认是否可以执行该操作
			this.EnsureUpdate(options);

			if(items == null)
				return 0;

			//构建数据操作的选项对象
			if(options == null)
				options = new DataUpdateOptions();

			//进行授权验证
			this.Authorize(DataServiceMethod.UpdateMany(), options);

			//解析数据模式表达式
			var schematic = this.GetSchema(schema, TypeExtension.GetElementType(items.GetType()));

			//将当前更新数据集合对象转换成数据字典集合
			var dictionaries = DataDictionary.GetDictionaries<TModel>(items, dictionary =>
			{
				//处理数据模型
				this.OnModel(key, dictionary, options);

				//验证待更新的数据
				this.OnValidate(DataServiceMethod.UpdateMany(), schematic, dictionary, options);
			});

			return this.OnUpdateMany(dictionaries, schematic, options);
		}

		protected virtual int OnUpdateMany(IEnumerable<IDataDictionary<TModel>> items, ISchema schema, DataUpdateOptions options)
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
			return this.Get(key, string.Empty, Paging.Disabled, null, sortings);
		}

		public object Get(string key, DataSelectOptions options, params Sorting[] sortings)
		{
			return this.Get(key, string.Empty, Paging.Disabled, options, sortings);
		}

		public object Get(string key, Paging paging, params Sorting[] sortings)
		{
			return this.Get(key, string.Empty, paging, null, sortings);
		}

		public object Get(string key, Paging paging, DataSelectOptions options, params Sorting[] sortings)
		{
			return this.Get(key, string.Empty, paging, options, sortings);
		}

		public object Get(string key, string schema, params Sorting[] sortings)
		{
			return this.Get(key, schema, Paging.Disabled, null, sortings);
		}

		public object Get(string key, string schema, DataSelectOptions options, params Sorting[] sortings)
		{
			return this.Get(key, schema, Paging.Disabled, options, sortings);
		}

		public object Get(string key, string schema, Paging paging, params Sorting[] sortings)
		{
			return this.Get(key, schema, paging, null, sortings);
		}

		public object Get(string key, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings)
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
				return this.OnGet(criteria, this.GetSchema(schema), options);
			}

			return this.Select(criteria, schema, paging, options, sortings);
		}

		protected virtual TModel OnGet(ICondition criteria, ISchema schema, DataSelectOptions options)
		{
			return this.DataAccess.Select<TModel>(this.Name, criteria, schema, null, options, null, ctx => this.OnGetting(ctx), ctx => this.OnGetted(ctx)).FirstOrDefault();
		}
		#endregion

		#region 单键查询
		public object Get<TKey1>(TKey1 key1, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
		{
			return this.Get<TKey1>(key1, null, null, null, sortings);
		}

		public object Get<TKey1>(TKey1 key1, DataSelectOptions options, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
		{
			return this.Get<TKey1>(key1, null, null, options, sortings);
		}

		public object Get<TKey1>(TKey1 key1, Paging paging, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
		{
			return this.Get<TKey1>(key1, null, paging, null, sortings);
		}

		public object Get<TKey1>(TKey1 key1, Paging paging, DataSelectOptions options, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
		{
			return this.Get<TKey1>(key1, null, paging, options, sortings);
		}

		public object Get<TKey1>(TKey1 key1, string schema, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
		{
			return this.Get<TKey1>(key1, schema, null, null, sortings);
		}

		public object Get<TKey1>(TKey1 key1, string schema, DataSelectOptions options, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
		{
			return this.Get<TKey1>(key1, schema, null, options, sortings);
		}

		public object Get<TKey1>(TKey1 key1, string schema, Paging paging, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
		{
			return this.Get<TKey1>(key1, schema, paging, null, sortings);
		}

		public object Get<TKey1>(TKey1 key1, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings)
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
				return this.OnGet(criteria, this.GetSchema(schema), options);
			}

			return this.Select(criteria, schema, paging, options, sortings);
		}
		#endregion

		#region 双键查询
		public object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
		{
			return this.Get<TKey1, TKey2>(key1, key2, null, null, null, sortings);
		}

		public object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, DataSelectOptions options, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
		{
			return this.Get<TKey1, TKey2>(key1, key2, null, null, options, sortings);
		}

		public object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, Paging paging, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
		{
			return this.Get<TKey1, TKey2>(key1, key2, null, paging, null, sortings);
		}

		public object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, Paging paging, DataSelectOptions options, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
		{
			return this.Get<TKey1, TKey2>(key1, key2, null, paging, options, sortings);
		}

		public object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
		{
			return this.Get<TKey1, TKey2>(key1, key2, schema, null, null, sortings);
		}

		public object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, DataSelectOptions options, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
		{
			return this.Get<TKey1, TKey2>(key1, key2, schema, null, options, sortings);
		}

		public object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Paging paging, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
		{
			return this.Get<TKey1, TKey2>(key1, key2, schema, paging, null, sortings);
		}

		public object Get<TKey1, TKey2>(TKey1 key1, TKey2 key2, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings)
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
				return this.OnGet(criteria, this.GetSchema(schema), options);
			}

			return this.Select(criteria, schema, paging, options, sortings);
		}
		#endregion

		#region 三键查询
		public object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
		{
			return this.Get<TKey1, TKey2, TKey3>(key1, key2, key3, null, null, null, sortings);
		}

		public object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, DataSelectOptions options, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
		{
			return this.Get<TKey1, TKey2, TKey3>(key1, key2, key3, null, null, options, sortings);
		}

		public object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, Paging paging, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
		{
			return this.Get<TKey1, TKey2, TKey3>(key1, key2, key3, null, paging, null, sortings);
		}

		public object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, Paging paging, DataSelectOptions options, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
		{
			return this.Get<TKey1, TKey2, TKey3>(key1, key2, key3, null, paging, options, sortings);
		}

		public object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
		{
			return this.Get<TKey1, TKey2, TKey3>(key1, key2, key3, schema, null, null, sortings);
		}

		public object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, DataSelectOptions options, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
		{
			return this.Get<TKey1, TKey2, TKey3>(key1, key2, key3, schema, null, options, sortings);
		}

		public object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Paging paging, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
		{
			return this.Get<TKey1, TKey2, TKey3>(key1, key2, key3, schema, paging, null, sortings);
		}

		public object Get<TKey1, TKey2, TKey3>(TKey1 key1, TKey2 key2, TKey3 key3, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings)
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
				return this.OnGet(criteria, this.GetSchema(schema), options);
			}

			return this.Select(criteria, schema, paging, options, sortings);
		}
		#endregion

		#region 四键查询
		public object Get<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
		{
			return this.Get<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, null, null, null, sortings);
		}

		public object Get<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, DataSelectOptions options, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
		{
			return this.Get<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, null, null, options, sortings);
		}

		public object Get<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, Paging paging, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
		{
			return this.Get<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, null, paging, null, sortings);
		}

		public object Get<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, Paging paging, DataSelectOptions options, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
		{
			return this.Get<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, null, paging, options, sortings);
		}

		public object Get<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
		{
			return this.Get<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, schema, null, null, sortings);
		}

		public object Get<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, DataSelectOptions options, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
		{
			return this.Get<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, schema, null, options, sortings);
		}

		public object Get<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, Paging paging, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
		{
			return this.Get<TKey1, TKey2, TKey3, TKey4>(key1, key2, key3, key4, schema, paging, null, sortings);
		}

		public object Get<TKey1, TKey2, TKey3, TKey4>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings)
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
				return this.OnGet(criteria, this.GetSchema(schema), options);
			}

			return this.Select(criteria, schema, paging, options, sortings);
		}
		#endregion

		#region 五键查询
		public object Get<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TKey5 : IEquatable<TKey5>
		{
			return this.Get<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, null, null, null, sortings);
		}

		public object Get<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, DataSelectOptions options, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TKey5 : IEquatable<TKey5>
		{
			return this.Get<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, null, null, options, sortings);
		}

		public object Get<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, Paging paging, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TKey5 : IEquatable<TKey5>
		{
			return this.Get<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, null, paging, null, sortings);
		}

		public object Get<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, Paging paging, DataSelectOptions options, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TKey5 : IEquatable<TKey5>
		{
			return this.Get<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, null, paging, options, sortings);
		}

		public object Get<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TKey5 : IEquatable<TKey5>
		{
			return this.Get<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, schema, null, null, sortings);
		}

		public object Get<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, DataSelectOptions options, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TKey5 : IEquatable<TKey5>
		{
			return this.Get<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, schema, null, options, sortings);
		}

		public object Get<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, Paging paging, params Sorting[] sortings)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TKey5 : IEquatable<TKey5>
		{
			return this.Get<TKey1, TKey2, TKey3, TKey4, TKey5>(key1, key2, key3, key4, key5, schema, paging, null, sortings);
		}

		public object Get<TKey1, TKey2, TKey3, TKey4, TKey5>(TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings)
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
				return this.OnGet(criteria, this.GetSchema(schema), options);
			}

			return this.Select(criteria, schema, paging, options, sortings);
		}
		#endregion

		#region 常规查询
		public IEnumerable<TModel> Select(DataSelectOptions options = null, params Sorting[] sortings)
		{
			return this.Select(null, string.Empty, null, options, sortings);
		}

		public IEnumerable<TModel> Select(ICondition criteria, params Sorting[] sortings)
		{
			return this.Select(criteria, string.Empty, null, null, sortings);
		}

		public IEnumerable<TModel> Select(ICondition criteria, DataSelectOptions options, params Sorting[] sortings)
		{
			return this.Select(criteria, string.Empty, null, options, sortings);
		}

		public IEnumerable<TModel> Select(ICondition criteria, Paging paging, params Sorting[] sortings)
		{
			return this.Select(criteria, string.Empty, paging, null, sortings);
		}

		public IEnumerable<TModel> Select(ICondition criteria, Paging paging, DataSelectOptions options, params Sorting[] sortings)
		{
			return this.Select(criteria, string.Empty, paging, options, sortings);
		}

		public IEnumerable<TModel> Select(ICondition criteria, string schema, params Sorting[] sortings)
		{
			return this.Select(criteria, schema, null, null, sortings);
		}

		public IEnumerable<TModel> Select(ICondition criteria, string schema, DataSelectOptions options, params Sorting[] sortings)
		{
			return this.Select(criteria, schema, null, options, sortings);
		}

		public IEnumerable<TModel> Select(ICondition criteria, string schema, Paging paging, params Sorting[] sortings)
		{
			return this.Select(criteria, schema, paging, null, sortings);
		}

		public IEnumerable<TModel> Select(ICondition criteria, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings)
		{
			//构建数据操作的选项对象
			if(options == null)
				options = new DataSelectOptions();

			//进行授权验证
			this.Authorize(DataServiceMethod.Select(), options);

			//修整查询条件
			criteria = this.OnValidate(DataServiceMethod.Select(), criteria, options);

			//执行查询方法
			return this.OnSelect(criteria, this.GetSchema(schema, typeof(TModel)), paging, sortings, options);
		}

		protected virtual IEnumerable<TModel> OnSelect(ICondition criteria, ISchema schema, Paging paging, Sorting[] sortings, DataSelectOptions options)
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

		public IEnumerable<T> Select<T>(Grouping grouping, DataSelectOptions options, params Sorting[] sortings)
		{
			return this.Select<T>(grouping, null, string.Empty, null, options, sortings);
		}

		public IEnumerable<T> Select<T>(Grouping grouping, Paging paging, params Sorting[] sortings)
		{
			return this.Select<T>(grouping, null, string.Empty, paging, null, sortings);
		}

		public IEnumerable<T> Select<T>(Grouping grouping, Paging paging, DataSelectOptions options, params Sorting[] sortings)
		{
			return this.Select<T>(grouping, null, string.Empty, paging, options, sortings);
		}

		public IEnumerable<T> Select<T>(Grouping grouping, string schema, params Sorting[] sortings)
		{
			return this.Select<T>(grouping, null, schema, null, null, sortings);
		}

		public IEnumerable<T> Select<T>(Grouping grouping, string schema, DataSelectOptions options, params Sorting[] sortings)
		{
			return this.Select<T>(grouping, null, schema, null, options, sortings);
		}

		public IEnumerable<T> Select<T>(Grouping grouping, string schema, Paging paging, params Sorting[] sortings)
		{
			return this.Select<T>(grouping, null, schema, paging, null, sortings);
		}

		public IEnumerable<T> Select<T>(Grouping grouping, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings)
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

		public IEnumerable<T> Select<T>(Grouping grouping, ICondition criteria, string schema, DataSelectOptions options, params Sorting[] sortings)
		{
			return this.Select<T>(grouping, criteria, schema, null, options, sortings);
		}

		public IEnumerable<T> Select<T>(Grouping grouping, ICondition criteria, string schema, Paging paging, params Sorting[] sortings)
		{
			return this.Select<T>(grouping, criteria, schema, paging, null, sortings);
		}

		public IEnumerable<T> Select<T>(Grouping grouping, ICondition criteria, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings)
		{
			//构建数据操作的选项对象
			if(options == null)
				options = new DataSelectOptions();

			//进行授权验证
			this.Authorize(DataServiceMethod.Select(), options);

			//修整查询条件
			criteria = this.OnValidate(DataServiceMethod.Select(), criteria, options);

			//执行查询方法
			return this.OnSelect<T>(grouping, criteria, string.IsNullOrWhiteSpace(schema) ? null : this.GetSchema(schema, typeof(TModel)), paging, sortings, options);
		}

		protected virtual IEnumerable<T> OnSelect<T>(Grouping grouping, ICondition criteria, ISchema schema, Paging paging, Sorting[] sortings, DataSelectOptions options)
		{
			return this.DataAccess.Select<T>(this.Name, grouping, criteria, schema, paging, options, sortings, ctx => this.OnSelecting(ctx), ctx => this.OnSelected(ctx));
		}
		#endregion

		#region 显式实现
		IEnumerable IDataService.Select(DataSelectOptions options, params Sorting[] sortings) => this.Select(options, sortings);
		IEnumerable IDataService.Select(ICondition criteria, params Sorting[] sortings) => this.Select(criteria, sortings);
		IEnumerable IDataService.Select(ICondition criteria, DataSelectOptions options, params Sorting[] sortings) => this.Select(criteria, options, sortings);
		IEnumerable IDataService.Select(ICondition criteria, string schema, params Sorting[] sortings) => this.Select(criteria, schema, sortings);
		IEnumerable IDataService.Select(ICondition criteria, string schema, DataSelectOptions options, params Sorting[] sortings) => this.Select(criteria, schema, options, sortings);
		IEnumerable IDataService.Select(ICondition criteria, string schema, Paging paging, params Sorting[] sortings) => this.Select(criteria, schema, paging, sortings);
		IEnumerable IDataService.Select(ICondition criteria, string schema, Paging paging, DataSelectOptions options, params Sorting[] sortings) => this.Select(criteria, schema, paging, options, sortings);
		IEnumerable IDataService.Select(ICondition criteria, Paging paging, params Sorting[] sortings) => this.Select(criteria, paging, sortings);
		IEnumerable IDataService.Select(ICondition criteria, Paging paging, DataSelectOptions options, params Sorting[] sortings) => this.Select(criteria, paging, options, sortings);
		#endregion

		#endregion

		#region 默认排序
		private Sorting[] _defaultSortings;
		private Sorting[] GetDefaultSortings() => _defaultSortings ??= this.GetSortings() ?? Array.Empty<Sorting>();
		protected virtual Sorting[] GetSortings()
		{
			var sortings = _attribute?.GetSortings();

			//如果没有定义 DataServiceAttribute 且是主服务，则默认主键倒序
			if((sortings == null || sortings.Length == 0) && this.Service == null)
			{
				var keys = Mapping.Entities[this.Name].Key;

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
		/// <summary>条件验证方法，适用于<c>Select</c>，<c>Update</c>，<c>Upsert</c>，<c>Delete</c>等方法。</summary>
		/// <param name="method">方法的类型。</param>
		/// <param name="criteria">查询条件。</param>
		/// <param name="options">执行方法的可选项。</param>
		/// <returns>处理后的条件。</returns>
		protected virtual ICondition OnValidate(DataServiceMethod method, ICondition criteria, IDataOptions options)
		{
			return this.Validator?.Validate(this, method, criteria, options) ?? criteria;
		}

		/// <summary>数据验证方法，适用于<c>Insert</c>，<c>Update</c>，<c>Upsert</c>等方法。</summary>
		/// <param name="method">方法的类型。</param>
		/// <param name="schema">数据模式。</param>
		/// <param name="data">待验证的数据。</param>
		/// <param name="options">执行方法的可选项。</param>
		protected virtual void OnValidate(DataServiceMethod method, ISchema schema, IDataDictionary<TModel> data, IDataMutateOptions options)
		{
			this.Validator?.Validate(this, method, schema, data, options);
		}
		#endregion

		#region 触发过滤
		protected void OnFiltered(DataServiceMethod method, object result, params object[] arguments) => this.OnFiltered(method, null, result, arguments);
		protected void OnFiltered(DataServiceMethod method, IDataAccessContextBase context, params object[] arguments) => this.OnFiltered(method, context, null, arguments);
		protected void OnFiltered(DataServiceMethod method, IDataAccessContextBase context, object result, params object[] arguments) => _filters.OnFiltered(method, context, result, arguments);

		protected bool OnFiltering(DataServiceMethod method, params object[] arguments) => this.OnFiltering(method, null, arguments);
		protected bool OnFiltering(DataServiceMethod method, IDataAccessContextBase context, params object[] arguments) => _filters.OnFiltering(method, context, null, arguments);
		#endregion

		#region 激发事件
		protected virtual void OnGetted(DataSelectContextBase context)
		{
			this.Getted?.Invoke(this, new DataGettedEventArgs<TModel>(context));
			this.OnFiltered(DataServiceMethod.Get(), context);
		}

		protected virtual bool OnGetting(DataSelectContextBase context)
		{
			var e = this.Getting;

			if(e != null)
			{
				var args = new DataGettingEventArgs<TModel>(context);
				e.Invoke(this, args);

				if(args.Cancel)
					return true;
			}

			return this.OnFiltering(DataServiceMethod.Get(), context);
		}

		protected virtual void OnExecuted(DataExecuteContextBase context)
		{
			this.Executed?.Invoke(this, new DataExecutedEventArgs(context));
			this.OnFiltered(DataServiceMethod.Execute(), context);
		}

		protected virtual bool OnExecuting(DataExecuteContextBase context)
		{
			var e = this.Executing;

			if(e != null)
			{
				var args = new DataExecutingEventArgs(context);
				e.Invoke(this, args);

				if(args.Cancel)
					return true;
			}

			return this.OnFiltering(DataServiceMethod.Execute(), context);
		}

		protected virtual void OnExisted(DataExistContextBase context)
		{
			this.Existed?.Invoke(this, new DataExistedEventArgs(context));
			this.OnFiltered(DataServiceMethod.Exists(), context);
		}

		protected virtual bool OnExisting(DataExistContextBase context)
		{
			var e = this.Existing;

			if(e != null)
			{
				var args = new DataExistingEventArgs(context);
				e.Invoke(this, args);

				if(args.Cancel)
					return true;
			}

			return this.OnFiltering(DataServiceMethod.Exists(), context);
		}

		protected virtual void OnAggregated(DataAggregateContextBase context)
		{
			this.Aggregated?.Invoke(this, new DataAggregatedEventArgs(context));
			this.OnFiltered(DataServiceMethod.Aggregate(context.Aggregate.Function), context);
		}

		protected virtual bool OnAggregating(DataAggregateContextBase context)
		{
			var e = this.Aggregating;

			if(e != null)
			{
				var args = new DataAggregatingEventArgs(context);
				e.Invoke(this, args);

				if(args.Cancel)
					return true;
			}

			return this.OnFiltering(DataServiceMethod.Aggregate(context.Aggregate.Function), context);
		}

		protected virtual void OnDeleted(DataDeleteContextBase context)
		{
			this.Deleted?.Invoke(this, new DataDeletedEventArgs(context));
			this.OnFiltered(DataServiceMethod.Delete(), context);
		}

		protected virtual bool OnDeleting(DataDeleteContextBase context)
		{
			var e = this.Deleting;

			if(e != null)
			{
				var args = new DataDeletingEventArgs(context);
				e.Invoke(this, args);

				if(args.Cancel)
					return true;
			}

			return this.OnFiltering(DataServiceMethod.Delete(), context);
		}

		protected virtual void OnInserted(DataInsertContextBase context)
		{
			this.Inserted?.Invoke(this, new DataInsertedEventArgs(context));
			this.OnFiltered(context.IsMultiple ? DataServiceMethod.InsertMany() : DataServiceMethod.Insert(), context);
		}

		protected virtual bool OnInserting(DataInsertContextBase context)
		{
			var e = this.Inserting;

			if(e != null)
			{
				var args = new DataInsertingEventArgs(context);
				e.Invoke(this, args);

				if(args.Cancel)
					return true;
			}

			return this.OnFiltering(context.IsMultiple ? DataServiceMethod.InsertMany() : DataServiceMethod.Insert(), context);
		}

		protected virtual void OnUpserted(DataUpsertContextBase context)
		{
			this.Upserted?.Invoke(this, new DataUpsertedEventArgs(context));
			this.OnFiltered(context.IsMultiple ? DataServiceMethod.UpsertMany() : DataServiceMethod.Upsert(), context);
		}

		protected virtual bool OnUpserting(DataUpsertContextBase context)
		{
			var e = this.Upserting;

			if(e != null)
			{
				var args = new DataUpsertingEventArgs(context);
				e.Invoke(this, args);

				if(args.Cancel)
					return true;
			}

			return this.OnFiltering(context.IsMultiple ? DataServiceMethod.UpsertMany() : DataServiceMethod.Upsert(), context);
		}

		protected virtual void OnUpdated(DataUpdateContextBase context)
		{
			this.Updated?.Invoke(this, new DataUpdatedEventArgs(context));
			this.OnFiltered(context.IsMultiple ? DataServiceMethod.UpdateMany() : DataServiceMethod.Update(), context);
		}

		protected virtual bool OnUpdating(DataUpdateContextBase context)
		{
			var e = this.Updating;

			if(e != null)
			{
				var args = new DataUpdatingEventArgs(context);
				e.Invoke(this, args);

				if(args.Cancel)
					return true;
			}

			return this.OnFiltering(context.IsMultiple ? DataServiceMethod.UpdateMany() : DataServiceMethod.Update(), context);
		}

		protected virtual void OnSelected(DataSelectContextBase context)
		{
			this.Selected?.Invoke(this, new DataSelectedEventArgs(context));
			this.OnFiltered(DataServiceMethod.Select(), context);
		}

		protected virtual bool OnSelecting(DataSelectContextBase context)
		{
			var e = this.Selecting;

			if(e != null)
			{
				var args = new DataSelectingEventArgs(context);
				e.Invoke(this, args);

				if(args.Cancel)
					return true;
			}

			return this.OnFiltering(DataServiceMethod.Select(), context);
		}
		#endregion

		#region 模型转换
		protected virtual void OnModel(string key, IDataDictionary<TModel> dictionary, IDataMutateOptions options) => this.OnModel(string.IsNullOrEmpty(key) ? null : key.Split('-', StringSplitOptions.TrimEntries), dictionary, options);
		protected virtual void OnModel(object[] values, IDataDictionary<TModel> dictionary, IDataMutateOptions options)
		{
			if(values == null || values.Length == 0 || dictionary == null)
				return;

			var entity = this.Service == null ? this.GetEntity() : this.Service.GetEntity();
			if(entity == null || entity.Key == null || entity.Key.Length < values.Length)
				return;

			for(int i = 0; i < values.Length; i++)
				dictionary.TrySetValue(entity.Key[i].Name, ConvertValue(values[i], entity.Key[i].Type));
		}
		#endregion

		#region 条件转换
		/// <summary>将指定的键值文本转换为操作条件。</summary>
		/// <param name="method">指定的操作方法。</param>
		/// <param name="key">指定的键值文本。</param>
		/// <param name="options">指定的数据操作选项。</param>
		/// <param name="singular">输出一个值，指示转换后的操作条件作用结果是否为必定为单个对象。</param>
		/// <returns>返回对应的操作条件。</returns>
		protected virtual ICondition OnCondition(DataServiceMethod method, string key, IDataOptions options, out bool singular)
		{
			singular = false;

			if(string.IsNullOrWhiteSpace(key))
				return null;

			if(_attribute != null && _attribute.Criteria != null && CriteriaParser.TryParse(key, out var members))
				return Criteria.Transform(_attribute.Criteria, members, true);

			return this.OnCondition(method, StringExtension.Slice(key, '-').ToArray(), options, out singular);
		}

		/// <summary>将指定的键值数组转换为操作条件。</summary>
		/// <param name="method">指定的操作方法。</param>
		/// <param name="values">指定的键值数组。</param>
		/// <param name="options">指定的数据操作选项。</param>
		/// <param name="singular">输出一个值，指示转换后的操作条件作用结果是否为必定为单个对象。</param>
		/// <returns>返回对应的操作条件。</returns>
		protected virtual ICondition OnCondition(DataServiceMethod method, object[] values, IDataOptions options, out bool singular)
		{
			//设置输出参数默认值
			singular = false;

			//获取当前数据服务对应的主键
			var primaryKey = Mapping.Entities[this.Name].Key;

			//如果主键获取失败或主键未定义或主键项数量小于传入的数组元素个数则返回空
			if(primaryKey == null || primaryKey.Length == 0 || primaryKey.Length < values.Length)
				return null;

			//只有传入参数值数量与主键数匹配其结果才为单个项
			singular = primaryKey.Length == values.Length;

			if(primaryKey.Length == 1)
			{
				if(values[0] is string text && text != null && text.Length > 0)
				{
					var parts = StringExtension.Slice(text, ',').ToArray();

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
		private ICondition ConvertKey(DataServiceMethod method, string key, IDataOptions options, out bool singular)
		{
			if(string.IsNullOrWhiteSpace(key))
			{
				singular = false;
				return null;
			}

			return this.OnCondition(method, key, options, out singular);
		}

		private ICondition ConvertKey<TKey1>(DataServiceMethod method, TKey1 key1, IDataOptions options, out bool singular)
			where TKey1 : IEquatable<TKey1> => this.OnCondition(method, new object[] { key1 }, options, out singular);
		private ICondition ConvertKey<TKey1, TKey2>(DataServiceMethod method, TKey1 key1, TKey2 key2, IDataOptions options, out bool singular)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2> => this.OnCondition(method, new object[] { key1, key2 }, options, out singular);
		private ICondition ConvertKey<TKey1, TKey2, TKey3>(DataServiceMethod method, TKey1 key1, TKey2 key2, TKey3 key3, IDataOptions options, out bool singular)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3> => this.OnCondition(method, new object[] { key1, key2, key3 }, options, out singular);
		private ICondition ConvertKey<TKey1, TKey2, TKey3, TKey4>(DataServiceMethod method, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, IDataOptions options, out bool singular)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4> => this.OnCondition(method, new object[] { key1, key2, key3, key4 }, options, out singular);
		private ICondition ConvertKey<TKey1, TKey2, TKey3, TKey4, TKey5>(DataServiceMethod method, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, IDataOptions options, out bool singular)
			where TKey1 : IEquatable<TKey1>
			where TKey2 : IEquatable<TKey2>
			where TKey3 : IEquatable<TKey3>
			where TKey4 : IEquatable<TKey4>
			where TKey5 : IEquatable<TKey5> => this.OnCondition(method, new object[] { key1, key2, key3, key4, key5 }, options, out singular);

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private ISchema GetSchema(string expression, Type type = null, bool immutable = false)
		{
			var schema = this.DataAccess.Schema.Parse(this.Name, expression, type ?? typeof(TModel));

			if(schema != null)
				schema.IsReadOnly = immutable;

			return schema;
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private ICondition GetUpdateKey(IDataDictionary dictionary)
		{
			var keys = Mapping.Entities[this.Name].Key;

			if(keys == null || keys.Length == 0)
				return null;

			if(keys.Length == 1)
			{
				if(dictionary.TryGetValue(keys[0].Name, out var value) && value != null)
					return Condition.Equal(keys[0].Name, value);

				return null;
			}

			var criteria = ConditionCollection.And();

			for(int i = 0; i < keys.Length; i++)
			{
				if(dictionary.TryGetValue(keys[i].Name, out var value) && value != null)
					criteria.Add(Condition.Equal(keys[i].Name, value));
				else
					return null;
			}

			return criteria;
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private void EnsureDelete(IDataDeleteOptions options)
		{
			if(!this.CanDelete && !Options.Allowed(options))
				throw new InvalidOperationException("The delete operation is not allowed.");
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private void EnsureInsert(IDataInsertOptions options)
		{
			if(!this.CanInsert && !Options.Allowed(options))
				throw new InvalidOperationException("The insert operation is not allowed.");
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private void EnsureUpdate(IDataUpdateOptions options)
		{
			if(!this.CanUpdate && !Options.Allowed(options))
				throw new InvalidOperationException("The update operation is not allowed.");
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private void EnsureUpsert(IDataUpsertOptions options)
		{
			if(!this.CanUpsert && !Options.Allowed(options))
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
					return Common.Convert.ConvertValue<DateTime>(value);
				case DbType.DateTimeOffset:
					return Common.Convert.ConvertValue<DateTimeOffset>(value);
				case DbType.Xml:
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

		#region 静态方法
		/// <summary>创建一个匿名用户的数据服务授权验证器。</summary>
		/// <param name="isReadOnly">指定一个值，指示是否只允许读取操作。</param>
		/// <returns>返回创建的数据服务授权验证器。</returns>
		public static IDataServiceAuthorizer<TModel> Anonymous(bool isReadOnly) =>
			isReadOnly ? AnonymousAuthorizer.ReadOnly : AnonymousAuthorizer.Default;

		/// <summary>创建一个匿名用户的数据服务授权验证器。</summary>
		/// <param name="authorize">指定数据服务操作的授权验证函数；该验证函数返回真(True)表示验证通过。</param>
		/// <returns>返回创建的数据服务授权验证器。</returns>
		public static IDataServiceAuthorizer<TModel> Anonymous(Func<IDataService<TModel>, DataServiceMethod, IDataOptions, bool> authorize) =>
			authorize == null ? AnonymousAuthorizer.Default : new AnonymousAuthorizer(authorize);

		/// <summary>创建一个非匿名用户的数据服务授权验证器。</summary>
		/// <param name="isReadOnly">指定一个值，指示是否只允许读取操作。</param>
		/// <returns>返回创建的数据服务授权验证器。</returns>
		public static IDataServiceAuthorizer<TModel> Nonanonymous(bool isReadOnly) =>
			isReadOnly ? NonanonymousAuthorizer.ReadOnly : NonanonymousAuthorizer.Default;

		/// <summary>创建一个非匿名用户的数据服务授权验证器。</summary>
		/// <param name="authorize">指定数据服务操作的授权验证函数；该验证函数返回真(True)表示验证通过。</param>
		/// <returns>返回创建的数据服务授权验证器。</returns>
		public static IDataServiceAuthorizer<TModel> Nonanonymous(Func<IDataService<TModel>, DataServiceMethod, IDataOptions, bool> authorize) =>
			authorize == null ? NonanonymousAuthorizer.Default : new NonanonymousAuthorizer(authorize);
		#endregion

		#region 服务匹配
		protected virtual bool OnMatch(object argument) => argument is string name && this.OnMatch(name);
		protected virtual bool OnMatch(string name) => string.Equals(this.Name, name, StringComparison.OrdinalIgnoreCase);
		bool IMatchable.Match(object argument) => this.OnMatch(argument);
		bool IMatchable<string>.Match(string argument) => this.OnMatch(argument);
		#endregion

		#region 嵌套子类
		public sealed class Condition : Zongsoft.Data.Condition.Builder<TModel>
		{
			private Condition() { }
		}

		private class AnonymousAuthorizer(Func<IDataService<TModel>, DataServiceMethod, IDataOptions, bool> authorize) : IDataServiceAuthorizer<TModel>
		{
			public static readonly AnonymousAuthorizer Default = new AnonymousAuthorizer(null);
			public static readonly AnonymousAuthorizer ReadOnly = new AnonymousAuthorizer((service, method, options) => method.IsReading);

			private readonly Func<IDataService<TModel>, DataServiceMethod, IDataOptions, bool> _authorize = authorize;

			public void Authorize(IDataService<TModel> service, DataServiceMethod method, IDataOptions options)
			{
				if(_authorize != null && !_authorize(service, method, options))
					throw new Security.Membership.AuthorizationException();
			}
		}

		private class NonanonymousAuthorizer(Func<IDataService<TModel>, DataServiceMethod, IDataOptions, bool> authorize) : IDataServiceAuthorizer<TModel>
		{
			public static readonly NonanonymousAuthorizer Default = new NonanonymousAuthorizer(null);
			public static readonly NonanonymousAuthorizer ReadOnly = new NonanonymousAuthorizer((service, method, options) => method.IsReading);

			private readonly Func<IDataService<TModel>, DataServiceMethod, IDataOptions, bool> _authorize = authorize;

			public void Authorize(IDataService<TModel> service, DataServiceMethod method, IDataOptions options)
			{
				if(Zongsoft.Security.ClaimsPrincipalExtension.IsAnonymous(service.Principal) || (_authorize != null && !_authorize(service, method, options)))
					throw new Security.Membership.AuthorizationException();
			}
		}
		#endregion
	}
}
