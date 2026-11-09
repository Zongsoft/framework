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
 * Copyright (c) Zongsoft Studio. All rights reserved.
 *
 * Licensed under the MIT License (see LICENSE).
 */

using System;
using System.Collections;

namespace Zongsoft.Data;

public class DataServiceContext<TModel> : IDataServiceContext<TModel>
{
	#region 成员字段
	private object _result;
	private readonly Func<DataServiceContext<TModel>, object> _resultGetter;
	private readonly Action<DataServiceContext<TModel>, object> _resultSetter;
	#endregion

	#region 构造函数
	public DataServiceContext(IDataService<TModel> service, DataServiceMethod method, object result, object[] arguments) : this(service, method, null, null, null, arguments) => _result = result;
	public DataServiceContext(IDataService<TModel> service, DataServiceMethod method, IDataAccessContextBase accessContext, object result, object[] arguments) : this(service, method, accessContext, null, null, arguments) => _result = result;

	public DataServiceContext(IDataService<TModel> service, DataServiceMethod method, Func<DataServiceContext<TModel>, object> resultGetter, Action<DataServiceContext<TModel>, object> resultSetter, object[] arguments) : this(service, method, null, resultGetter, resultSetter, arguments) { }
	public DataServiceContext(IDataService<TModel> service, DataServiceMethod method, IDataAccessContextBase accessContext, Func<DataServiceContext<TModel>, object> resultGetter, Action<DataServiceContext<TModel>, object> resultSetter, object[] arguments)
	{
		this.Service = service ?? throw new ArgumentNullException(nameof(service));
		this.Method = method;
		this.AccessContext = accessContext;
		this.Arguments = arguments;

		_resultGetter = resultGetter ?? GetResult;
		_resultSetter = resultSetter ?? SetResult;
	}
	#endregion

	#region 公共属性
	public IDataService<TModel> Service { get; }
	public DataServiceMethod Method { get; }
	public IDataAccessContextBase AccessContext { get; }
	public object Result { get => _resultGetter(this); set => _resultSetter(this, value); }
	public object[] Arguments { get; }
	#endregion

	#region 私有方法
	//注意：本方法内的代码不能使用 Result 属性（可能会引发栈溢出）。
	private static object GetResult(DataServiceContext<TModel> context)
	{
		if(context == null)
			return null;

		if(context._result != null)
			return context._result;

		return context.AccessContext switch
		{
			DataSelectContextBase selection => selection.Result,
			DataInsertContextBase insertion => insertion.Count,
			DataUpsertContextBase upsertion => upsertion.Count,
			DataUpdateContextBase updation => updation.Count,
			DataDeleteContextBase deletion => deletion.Count,
			DataImportContextBase importing => importing.Count,
			DataExistContextBase existing => existing.Result,
			DataExecuteContextBase execution => execution.Result,
			DataAggregateContextBase aggregate => aggregate.Result,
			_ => context._result,
		};
	}

	//注意：本方法内的代码不能使用 Result 属性（可能会引发栈溢出）。
	private static void SetResult(DataServiceContext<TModel> context, object value)
	{
		if(context == null)
			return;

		switch(context.AccessContext)
		{
			case DataExecuteContextBase execution:
				execution.Result = value;
				break;
			case DataExistContextBase existing:
				existing.Result = (bool)value;
				break;
			case DataAggregateContextBase aggregate:
				aggregate.Result = value;
				break;
			case DataImportContextBase importing:
				importing.Count = (int)value;
				break;
			case DataSelectContextBase selection:
				selection.Result = value;
				break;
			case DataInsertContextBase insertion:
				insertion.Count = (int)value;
				break;
			case DataUpsertContextBase upsertion:
				upsertion.Count = (int)value;
				break;
			case DataUpdateContextBase updation:
				updation.Count = (int)value;
				break;
			case DataDeleteContextBase deletion:
				deletion.Count = (int)value;
				break;
			default:
				context._result = value;
				break;
		}
	}
	#endregion
}
