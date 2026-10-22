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

/// <summary>表示数据服务的接口。</summary>
public partial interface IDataService
{
	#region 事件定义
	event Func<object, DataExistedEventArgs, CancellationToken, ValueTask> Existed;
	event Func<object, DataExistingEventArgs, CancellationToken, ValueTask> Existing;
	event Func<object, DataAggregatedEventArgs, CancellationToken, ValueTask> Aggregated;
	event Func<object, DataAggregatingEventArgs, CancellationToken, ValueTask> Aggregating;
	event Func<object, DataDeletedEventArgs, CancellationToken, ValueTask> Deleted;
	event Func<object, DataDeletingEventArgs, CancellationToken, ValueTask> Deleting;
	event Func<object, DataInsertedEventArgs, CancellationToken, ValueTask> Inserted;
	event Func<object, DataInsertingEventArgs, CancellationToken, ValueTask> Inserting;
	event Func<object, DataUpsertedEventArgs, CancellationToken, ValueTask> Upserted;
	event Func<object, DataUpsertingEventArgs, CancellationToken, ValueTask> Upserting;
	event Func<object, DataUpdatedEventArgs, CancellationToken, ValueTask> Updated;
	event Func<object, DataUpdatingEventArgs, CancellationToken, ValueTask> Updating;
	event Func<object, DataSelectedEventArgs, CancellationToken, ValueTask> Selected;
	event Func<object, DataSelectingEventArgs, CancellationToken, ValueTask> Selecting;
	#endregion

	#region 属性定义
	/// <summary>获取数据服务的名称，该名称亦为数据访问接口的调用名。</summary>
	string Name { get; }

	/// <summary>获取一个值，指示是否支持删除操作。</summary>
	bool CanDelete { get; }

	/// <summary>获取一个值，指示是否支持新增操作。</summary>
	bool CanInsert { get; }

	/// <summary>获取一个值，指示是否支持修改操作。</summary>
	bool CanUpdate { get; }

	/// <summary>获取一个值，指示是否支持增改操作。</summary>
	bool CanUpsert { get; }

	/// <summary>获取当前服务的安全主体。</summary>
	System.Security.Claims.ClaimsPrincipal Principal { get; }

	/// <summary>获取数据服务注解。</summary>
	DataServiceAttribute Attribute { get; }

	/// <summary>获取父数据服务。</summary>
	IDataService Service { get; }

	/// <summary>获取数据访问接口。</summary>
	IDataAccess DataAccess { get; }

	/// <summary>获取数据服务验证器。</summary>
	IDataServiceValidator Validator { get; }

	/// <summary>获取服务提供程序。</summary>
	IServiceProvider ServiceProvider { get; }

	/// <summary>获取一个值，指示数据服务是否可写（“可删”或“可增”或“可改”）。</summary>
	bool IsMutable => this.CanDelete || this.CanInsert || this.CanUpdate;
	/// <summary>获取一个值，指示数据服务是否不可写（“不可删”且“不可增”且“不可改”）。</summary>
	bool IsImmutable => !(this.CanDelete || this.CanInsert || this.CanUpdate);
	#endregion

	#region 获取服务
	/// <summary>获取指定类型的嵌套子服务。</summary>
	/// <typeparam name="TService">指定要获取的嵌套子服务类型。</typeparam>
	/// <returns>如果查找成功则返回指定类型的子服务，否则返回空(<c>null</c>)。</returns>
	TService GetSubservice<TService>() where TService : class, IDataService;

	/// <summary>获取指定类型的嵌套子服务。</summary>
	/// <param name="type">指定要获取的嵌套子服务类型。</param>
	/// <returns>如果查找成功则返回指定类型的子服务，否则返回空(<c>null</c>)。</returns>
	IDataService GetSubservice(Type type);
	#endregion
}

/*
 * 本接口是为了处理相关方法的泛型条件重载版本的条件参数类型匹配错误的问题。
 */
public partial interface IDataService
{
	#region 排序重载
	IAsyncEnumerable<object> SelectAsync(Sorting[] sortings, CancellationToken cancellation) => this.SelectAsync(default(DataSelectOptions), sortings, cancellation);
	IAsyncEnumerable<T> SelectAsync<T>(Grouping grouping, ICondition criteria, Sorting[] sortings, CancellationToken cancellation) => this.SelectAsync<T>(grouping, criteria, default(string), sortings, cancellation);
	#endregion
}
