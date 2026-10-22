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
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Common;
using Zongsoft.Services;

namespace Zongsoft.Data;

[Service<IDataService>]
[DefaultMember(nameof(Filters))]
public abstract partial class DataServiceBase<TModel> : IDataService<TModel>, IMatchable, IMatchable<string>
{
	#region 事件定义
	public event Func<object, DataGettedEventArgs<TModel>, CancellationToken, ValueTask> Getted;
	public event Func<object, DataGettingEventArgs<TModel>, CancellationToken, ValueTask> Getting;
	public event Func<object, DataExistedEventArgs, CancellationToken, ValueTask> Existed;
	public event Func<object, DataExistingEventArgs, CancellationToken, ValueTask> Existing;
	public event Func<object, DataAggregatedEventArgs, CancellationToken, ValueTask> Aggregated;
	public event Func<object, DataAggregatingEventArgs, CancellationToken, ValueTask> Aggregating;
	public event Func<object, DataDeletedEventArgs, CancellationToken, ValueTask> Deleted;
	public event Func<object, DataDeletingEventArgs, CancellationToken, ValueTask> Deleting;
	public event Func<object, DataInsertedEventArgs, CancellationToken, ValueTask> Inserted;
	public event Func<object, DataInsertingEventArgs, CancellationToken, ValueTask> Inserting;
	public event Func<object, DataUpsertedEventArgs, CancellationToken, ValueTask> Upserted;
	public event Func<object, DataUpsertingEventArgs, CancellationToken, ValueTask> Upserting;
	public event Func<object, DataUpdatedEventArgs, CancellationToken, ValueTask> Updated;
	public event Func<object, DataUpdatingEventArgs, CancellationToken, ValueTask> Updating;
	public event Func<object, DataSelectedEventArgs, CancellationToken, ValueTask> Selected;
	public event Func<object, DataSelectingEventArgs, CancellationToken, ValueTask> Selecting;
	#endregion

	#region 成员字段
	private string _name;
	private IDataAccess _dataAccess;
	private IDataSearcher<TModel> _searcher;
	private IServiceProvider _serviceProvider;
	private DataServiceDescriptor<TModel> _descriptor;
	private Dictionary<Type, IDataService> _subservices;
	private readonly DataServiceAttribute _attribute;
	private readonly DataServiceMutability? _mutability;
	private readonly DataServiceFilterCollection<TModel> _filters;
	#endregion

	#region 构造函数
	protected DataServiceBase(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		_attribute = GetAttribute(this.GetType());
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
		_attribute = GetAttribute(this.GetType());
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
		_attribute = GetAttribute(this.GetType());
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
		_attribute = GetAttribute(this.GetType());
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
		_attribute = GetAttribute(this.GetType());
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
		_attribute = GetAttribute(this.GetType());
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
		_attribute = GetAttribute(this.GetType());
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
		_attribute = GetAttribute(this.GetType());
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
	public DataServiceDescriptor<TModel> Descriptor => _descriptor ??= this.GetDescriptor();
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

				while(baseType != null && baseType != typeof(object))
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

		static IEnumerable<Type> GetNestedTypes(Type type)
		{
			while(type != null && type != typeof(object))
			{
				foreach(var nestedType in type.GetNestedTypes())
				{
					var serviceType = nestedType;
					if(serviceType.ContainsGenericParameters)
					{
						var arguments = type.GenericTypeArguments;
						if(!serviceType.IsGenericTypeDefinition || arguments.Length != serviceType.GetGenericArguments().Length)
							continue;

						serviceType = serviceType.MakeGenericType(arguments);
					}

					if(serviceType.IsNestedPublic && serviceType.IsClass && !serviceType.IsAbstract && typeof(IDataService).IsAssignableFrom(serviceType))
						yield return serviceType;
				}

				type = type.BaseType;
			}
		}
	}

	public IDataService GetSubservice(Type type) => type != null && _subservices != null && _subservices.TryGetValue(type, out var result) ? result : null;
	public TService GetSubservice<TService>() where TService : class, IDataService => _subservices != null && _subservices.TryGetValue(typeof(TService), out var result) ? (TService)result : default;
	#endregion

	#region 授权验证
	protected virtual async ValueTask AuthorizeAsync(DataServiceMethod method, IDataOptions options, CancellationToken cancellation)
	{
		var authorizer = this.Authorizer;

		if(authorizer != null)
		{
			await authorizer.AuthorizeAsync(this, method, options, cancellation);
		}
		else
		{
			if(Security.ClaimsPrincipalExtension.IsAnonymous(this.Principal))
				throw new Security.Privileges.AuthorizationException();
		}
	}
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
	/// <summary>条件验证方法，适用于 <c>Select</c>、<c>Update</c>、<c>Upsert</c>、<c>Delete</c> 等方法。</summary>
	/// <param name="method">方法的类型。</param>
	/// <param name="criteria">查询条件。</param>
	/// <param name="options">执行方法的可选项。</param>
	/// <returns>处理后的条件。</returns>
	/// <param name="cancellation">取消标记。</param>
	protected virtual async ValueTask<ICondition> OnValidateAsync(DataServiceMethod method, ICondition criteria, IDataOptions options, CancellationToken cancellation)
	{
		var validator = this.Validator;
		return validator == null ? criteria : await validator.ValidateAsync(this, method, criteria, options, cancellation) ?? criteria;
	}

	/// <summary>数据验证方法，适用于 <c>Insert</c>、<c>Update</c>、<c>Upsert</c> 等方法。</summary>
	/// <param name="method">方法的类型。</param>
	/// <param name="schema">数据模式。</param>
	/// <param name="data">待验证的数据。</param>
	/// <param name="options">执行方法的可选项。</param>
	/// <param name="cancellation">取消标记。</param>
	/// <returns>表示异步操作的任务。</returns>
	protected virtual ValueTask OnValidateAsync(DataServiceMethod method, ISchema schema, IDataDictionary<TModel> data, IDataMutateOptions options, CancellationToken cancellation) => this.Validator?.ValidateAsync(this, method, schema, data, options, cancellation) ?? ValueTask.CompletedTask;
	#endregion

	#region 触发过滤
	protected ValueTask OnFilteredAsync(DataServiceMethod method, object result, params object[] arguments) => this.OnFilteredAsync(method, null, result, arguments, CancellationToken.None);
	protected ValueTask OnFilteredAsync(DataServiceMethod method, object result, object[] arguments, CancellationToken cancellation) => this.OnFilteredAsync(method, null, result, arguments, cancellation);
	protected ValueTask OnFilteredAsync(DataServiceMethod method, IDataAccessContextBase context, params object[] arguments) => this.OnFilteredAsync(method, context, null, arguments, CancellationToken.None);
	protected ValueTask OnFilteredAsync(DataServiceMethod method, IDataAccessContextBase context, CancellationToken cancellation) => this.OnFilteredAsync(method, context, null, [], cancellation);
	protected ValueTask OnFilteredAsync(DataServiceMethod method, IDataAccessContextBase context, object[] arguments, CancellationToken cancellation) => this.OnFilteredAsync(method, context, null, arguments, cancellation);
	protected ValueTask OnFilteredAsync(DataServiceMethod method, IDataAccessContextBase context, object result, params object[] arguments) => this.OnFilteredAsync(method, context, result, arguments, CancellationToken.None);
	protected ValueTask OnFilteredAsync(DataServiceMethod method, IDataAccessContextBase context, object result, object[] arguments, CancellationToken cancellation) => _filters.OnFilteredAsync(method, context, result, arguments, cancellation);

	protected ValueTask<bool> OnFilteringAsync(DataServiceMethod method, params object[] arguments) => this.OnFilteringAsync(method, null, arguments, CancellationToken.None);
	protected ValueTask<bool> OnFilteringAsync(DataServiceMethod method, CancellationToken cancellation) => this.OnFilteringAsync(method, null, [], cancellation);
	protected ValueTask<bool> OnFilteringAsync(DataServiceMethod method, object[] arguments, CancellationToken cancellation) => this.OnFilteringAsync(method, null, arguments, cancellation);
	protected ValueTask<bool> OnFilteringAsync(DataServiceMethod method, IDataAccessContextBase context, params object[] arguments) => this.OnFilteringAsync(method, context, arguments, CancellationToken.None);
	protected ValueTask<bool> OnFilteringAsync(DataServiceMethod method, IDataAccessContextBase context, CancellationToken cancellation) => this.OnFilteringAsync(method, context, [], cancellation);
	protected ValueTask<bool> OnFilteringAsync(DataServiceMethod method, IDataAccessContextBase context, object[] arguments, CancellationToken cancellation) => _filters.OnFilteringAsync(method, context, [null, arguments], cancellation);
	#endregion

	#region 激发事件
	protected virtual async ValueTask OnGettedAsync(DataSelectContextBase context, TModel result, CancellationToken cancellation)
	{
		await this.RaiseAsync(this.Getted, new DataGettedEventArgs<TModel>(context, result), cancellation);
	}

	protected virtual async ValueTask<bool> OnGettingAsync(DataSelectContextBase context, CancellationToken cancellation)
	{
		var e = this.Getting;

		if(e != null)
		{
			var args = new DataGettingEventArgs<TModel>(context);
			await this.RaiseAsync(e, args, cancellation);

			if(args.Cancel)
				return true;
		}

		return await this.OnFilteringAsync(DataServiceMethod.Get(), context, cancellation);
	}

	protected virtual async ValueTask OnExistedAsync(DataExistContextBase context, CancellationToken cancellation)
	{
		await this.RaiseAsync(this.Existed, new DataExistedEventArgs(context), cancellation);
		await this.OnFilteredAsync(DataServiceMethod.Exists(), context, cancellation);
	}

	protected virtual async ValueTask<bool> OnExistingAsync(DataExistContextBase context, CancellationToken cancellation)
	{
		var e = this.Existing;

		if(e != null)
		{
			var args = new DataExistingEventArgs(context);
			await this.RaiseAsync(e, args, cancellation);

			if(args.Cancel)
				return true;
		}

		return await this.OnFilteringAsync(DataServiceMethod.Exists(), context, cancellation);
	}

	protected virtual async ValueTask OnAggregatedAsync(DataAggregateContextBase context, CancellationToken cancellation)
	{
		await this.RaiseAsync(this.Aggregated, new DataAggregatedEventArgs(context), cancellation);
		await this.OnFilteredAsync(DataServiceMethod.Aggregate(context.Aggregate.Function), context, cancellation);
	}

	protected virtual async ValueTask<bool> OnAggregatingAsync(DataAggregateContextBase context, CancellationToken cancellation)
	{
		var e = this.Aggregating;

		if(e != null)
		{
			var args = new DataAggregatingEventArgs(context);
			await this.RaiseAsync(e, args, cancellation);

			if(args.Cancel)
				return true;
		}

		return await this.OnFilteringAsync(DataServiceMethod.Aggregate(context.Aggregate.Function), context, cancellation);
	}

	protected virtual async ValueTask OnDeletedAsync(DataDeleteContextBase context, CancellationToken cancellation)
	{
		await this.RaiseAsync(this.Deleted, new DataDeletedEventArgs(context), cancellation);
		await this.OnFilteredAsync(DataServiceMethod.Delete(), context, cancellation);
	}

	protected virtual async ValueTask<bool> OnDeletingAsync(DataDeleteContextBase context, CancellationToken cancellation)
	{
		var e = this.Deleting;

		if(e != null)
		{
			var args = new DataDeletingEventArgs(context);
			await this.RaiseAsync(e, args, cancellation);

			if(args.Cancel)
				return true;
		}

		return await this.OnFilteringAsync(DataServiceMethod.Delete(), context, cancellation);
	}

	protected virtual async ValueTask OnInsertedAsync(DataInsertContextBase context, CancellationToken cancellation)
	{
		await this.RaiseAsync(this.Inserted, new DataInsertedEventArgs(context), cancellation);
		await this.OnFilteredAsync(context.IsMultiple ? DataServiceMethod.InsertMany() : DataServiceMethod.Insert(), context, cancellation);
	}

	protected virtual async ValueTask<bool> OnInsertingAsync(DataInsertContextBase context, CancellationToken cancellation)
	{
		var e = this.Inserting;

		if(e != null)
		{
			var args = new DataInsertingEventArgs(context);
			await this.RaiseAsync(e, args, cancellation);

			if(args.Cancel)
				return true;
		}

		return await this.OnFilteringAsync(context.IsMultiple ? DataServiceMethod.InsertMany() : DataServiceMethod.Insert(), context, cancellation);
	}

	protected virtual async ValueTask OnUpsertedAsync(DataUpsertContextBase context, CancellationToken cancellation)
	{
		await this.RaiseAsync(this.Upserted, new DataUpsertedEventArgs(context), cancellation);
		await this.OnFilteredAsync(context.IsMultiple ? DataServiceMethod.UpsertMany() : DataServiceMethod.Upsert(), context, cancellation);
	}

	protected virtual async ValueTask<bool> OnUpsertingAsync(DataUpsertContextBase context, CancellationToken cancellation)
	{
		var e = this.Upserting;

		if(e != null)
		{
			var args = new DataUpsertingEventArgs(context);
			await this.RaiseAsync(e, args, cancellation);

			if(args.Cancel)
				return true;
		}

		return await this.OnFilteringAsync(context.IsMultiple ? DataServiceMethod.UpsertMany() : DataServiceMethod.Upsert(), context, cancellation);
	}

	protected virtual async ValueTask OnUpdatedAsync(DataUpdateContextBase context, CancellationToken cancellation)
	{
		await this.RaiseAsync(this.Updated, new DataUpdatedEventArgs(context), cancellation);
		await this.OnFilteredAsync(DataServiceMethod.Update(), context, cancellation);
	}

	protected virtual async ValueTask<bool> OnUpdatingAsync(DataUpdateContextBase context, CancellationToken cancellation)
	{
		var e = this.Updating;

		if(e != null)
		{
			var args = new DataUpdatingEventArgs(context);
			await this.RaiseAsync(e, args, cancellation);

			if(args.Cancel)
				return true;
		}

		return await this.OnFilteringAsync(DataServiceMethod.Update(), context, cancellation);
	}

	protected virtual async ValueTask OnSelectedAsync(DataSelectContextBase context, CancellationToken cancellation)
	{
		await this.RaiseAsync(this.Selected, new DataSelectedEventArgs(context), cancellation);
		await this.OnFilteredAsync(DataServiceMethod.Select(), context, cancellation);
	}

	protected virtual async ValueTask<bool> OnSelectingAsync(DataSelectContextBase context, CancellationToken cancellation)
	{
		var e = this.Selecting;

		if(e != null)
		{
			var args = new DataSelectingEventArgs(context);
			await this.RaiseAsync(e, args, cancellation);

			if(args.Cancel)
				return true;
		}

		return await this.OnFilteringAsync(DataServiceMethod.Select(), context, cancellation);
	}
	#endregion

	#region 服务描述
	protected virtual DataServiceDescriptor<TModel> GetDescriptor() => new(this);
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
	private static DataServiceAttribute GetAttribute(Type type)
	{
		for(; type != null; type = type.BaseType)
		{
			var attribute = type.GetCustomAttribute<DataServiceAttribute>(false);

			if(attribute != null)
				return attribute;
		}

		return null;
	}
	private async ValueTask RaiseAsync<TArgs>(Func<object, TArgs, CancellationToken, ValueTask> handlers, TArgs args, CancellationToken cancellation)
	{
		if(handlers == null)
			return;

		foreach(Func<object, TArgs, CancellationToken, ValueTask> handler in handlers.GetInvocationList())
			await handler(this, args, cancellation);
	}

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
		where TKey1 : IEquatable<TKey1> => this.OnCondition(method, [key1], options, out singular);
	private ICondition ConvertKey<TKey1, TKey2>(DataServiceMethod method, TKey1 key1, TKey2 key2, IDataOptions options, out bool singular)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2> => this.OnCondition(method, [key1, key2], options, out singular);
	private ICondition ConvertKey<TKey1, TKey2, TKey3>(DataServiceMethod method, TKey1 key1, TKey2 key2, TKey3 key3, IDataOptions options, out bool singular)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3> => this.OnCondition(method, [key1, key2, key3], options, out singular);
	private ICondition ConvertKey<TKey1, TKey2, TKey3, TKey4>(DataServiceMethod method, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, IDataOptions options, out bool singular)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4> => this.OnCondition(method, [key1, key2, key3, key4], options, out singular);
	private ICondition ConvertKey<TKey1, TKey2, TKey3, TKey4, TKey5>(DataServiceMethod method, TKey1 key1, TKey2 key2, TKey3 key3, TKey4 key4, TKey5 key5, IDataOptions options, out bool singular)
		where TKey1 : IEquatable<TKey1>
		where TKey2 : IEquatable<TKey2>
		where TKey3 : IEquatable<TKey3>
		where TKey4 : IEquatable<TKey4>
		where TKey5 : IEquatable<TKey5> => this.OnCondition(method, [key1, key2, key3, key4, key5], options, out singular);

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
			throw new InvalidOperationException(string.Format(System.Globalization.CultureInfo.CurrentCulture, Properties.Resources.DataService_OperationNotAllowed_Message, nameof(DataAccessMethod.Delete)));
	}

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private void EnsureInsert(IDataInsertOptions options)
	{
		if(!this.CanInsert && !Options.Allowed(options))
			throw new InvalidOperationException(string.Format(System.Globalization.CultureInfo.CurrentCulture, Properties.Resources.DataService_OperationNotAllowed_Message, nameof(DataAccessMethod.Insert)));
	}

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private void EnsureUpdate(IDataUpdateOptions options)
	{
		if(!this.CanUpdate && !Options.Allowed(options))
			throw new InvalidOperationException(string.Format(System.Globalization.CultureInfo.CurrentCulture, Properties.Resources.DataService_OperationNotAllowed_Message, nameof(DataAccessMethod.Update)));
	}

	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private void EnsureUpsert(IDataUpsertOptions options)
	{
		if(!this.CanUpsert && !Options.Allowed(options))
			throw new InvalidOperationException(string.Format(System.Globalization.CultureInfo.CurrentCulture, Properties.Resources.DataService_OperationNotAllowed_Message, nameof(DataAccessMethod.Upsert)));
	}

	private static object ConvertValue(object value, DbType dbType)
	{
		if(value == null)
			return null;

		return dbType switch
		{
			DbType.Byte => Common.Convert.ConvertValue<byte>(value),
			DbType.SByte => Common.Convert.ConvertValue<sbyte>(value),
			DbType.Boolean => Common.Convert.ConvertValue<bool>(value),
			DbType.Int16 => Common.Convert.ConvertValue<short>(value),
			DbType.Int32 => Common.Convert.ConvertValue<int>(value),
			DbType.Int64 => Common.Convert.ConvertValue<long>(value),
			DbType.UInt16 => Common.Convert.ConvertValue<ushort>(value),
			DbType.UInt32 => Common.Convert.ConvertValue<uint>(value),
			DbType.UInt64 => Common.Convert.ConvertValue<ulong>(value),
			DbType.Single => Common.Convert.ConvertValue<float>(value),
			DbType.Double => Common.Convert.ConvertValue<double>(value),
			DbType.Decimal or DbType.Currency => Common.Convert.ConvertValue<decimal>(value),
			DbType.Date or DbType.Time or DbType.DateTime or DbType.DateTime2 => Common.Convert.ConvertValue<DateTime>(value),
			DbType.DateTimeOffset => Common.Convert.ConvertValue<DateTimeOffset>(value),
			DbType.Xml or DbType.AnsiString or DbType.AnsiStringFixedLength or DbType.String or DbType.StringFixedLength => Common.Convert.ConvertValue<string>(value),
			DbType.Guid => Common.Convert.ConvertValue<Guid>(value),
			DbType.Binary => Common.Convert.ConvertValue<byte[]>(value),
			_ => value,
		};
	}
	#endregion

	#region 静态方法
	/// <summary>创建一个匿名用户的数据服务授权验证器。</summary>
	/// <param name="isReadOnly">指定一个值，指示是否只允许读取操作。</param>
	/// <returns>返回创建的数据服务授权验证器。</returns>
	public static IDataServiceAuthorizer<TModel> Anonymous(bool isReadOnly) =>
		isReadOnly ? AnonymousAuthorizer.ReadOnly : AnonymousAuthorizer.Default;

	/// <summary>创建一个匿名用户的数据服务授权验证器。</summary>
	/// <param name="authorize">指定数据服务操作的授权验证函数；该验证函数返回真(<c>True</c>)表示验证通过。</param>
	/// <returns>返回创建的数据服务授权验证器。</returns>
	public static IDataServiceAuthorizer<TModel> Anonymous(Func<IDataService<TModel>, DataServiceMethod, IDataOptions, CancellationToken, ValueTask<bool>> authorize) =>
		authorize == null ? AnonymousAuthorizer.Default : new AnonymousAuthorizer(authorize);

	/// <summary>创建一个非匿名用户的数据服务授权验证器。</summary>
	/// <param name="isReadOnly">指定一个值，指示是否只允许读取操作。</param>
	/// <returns>返回创建的数据服务授权验证器。</returns>
	public static IDataServiceAuthorizer<TModel> Nonanonymous(bool isReadOnly) =>
		isReadOnly ? NonanonymousAuthorizer.ReadOnly : NonanonymousAuthorizer.Default;

	/// <summary>创建一个非匿名用户的数据服务授权验证器。</summary>
	/// <param name="authorize">指定数据服务操作的授权验证函数；该验证函数返回真(<c>True</c>)表示验证通过。</param>
	/// <returns>返回创建的数据服务授权验证器。</returns>
	public static IDataServiceAuthorizer<TModel> Nonanonymous(Func<IDataService<TModel>, DataServiceMethod, IDataOptions, CancellationToken, ValueTask<bool>> authorize) =>
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

	private class AnonymousAuthorizer(Func<IDataService<TModel>, DataServiceMethod, IDataOptions, CancellationToken, ValueTask<bool>> authorize) : IDataServiceAuthorizer<TModel>
	{
		public static readonly AnonymousAuthorizer Default = new AnonymousAuthorizer(null);
		public static readonly AnonymousAuthorizer ReadOnly = new AnonymousAuthorizer((service, method, options, cancellation) => ValueTask.FromResult(method.IsReading));

		private readonly Func<IDataService<TModel>, DataServiceMethod, IDataOptions, CancellationToken, ValueTask<bool>> _authorize = authorize;

		public async ValueTask AuthorizeAsync(IDataService<TModel> service, DataServiceMethod method, IDataOptions options, CancellationToken cancellation)
		{
			if(_authorize != null && !await _authorize(service, method, options, cancellation))
				throw new Security.Privileges.AuthorizationException();
		}
	}

	private class NonanonymousAuthorizer(Func<IDataService<TModel>, DataServiceMethod, IDataOptions, CancellationToken, ValueTask<bool>> authorize) : IDataServiceAuthorizer<TModel>
	{
		public static readonly NonanonymousAuthorizer Default = new NonanonymousAuthorizer(null);
		public static readonly NonanonymousAuthorizer ReadOnly = new NonanonymousAuthorizer((service, method, options, cancellation) => ValueTask.FromResult(method.IsReading));

		private readonly Func<IDataService<TModel>, DataServiceMethod, IDataOptions, CancellationToken, ValueTask<bool>> _authorize = authorize;

		public async ValueTask AuthorizeAsync(IDataService<TModel> service, DataServiceMethod method, IDataOptions options, CancellationToken cancellation)
		{
			if(Zongsoft.Security.ClaimsPrincipalExtension.IsAnonymous(service.Principal) || (_authorize != null && !await _authorize(service, method, options, cancellation)))
				throw new Security.Privileges.AuthorizationException();
		}
	}
	#endregion
}
