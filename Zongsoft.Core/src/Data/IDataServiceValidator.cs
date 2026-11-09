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
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Data;

/// <summary>提供数据服务的条件和数据进行验证功能的接口。</summary>
public interface IDataServiceValidator
{
	/// <summary>验证指定数据服务方法的过滤条件。</summary>
	/// <param name="service">指定的数据服务对象。</param>
	/// <param name="method">待验证数据服务方法。</param>
	/// <param name="criteria">待验证的数据过滤条件。</param>
	/// <param name="options">待验证方法的选项参数。</param>
	/// <param name="cancellation">取消标记。</param>
	/// <returns>返回验证后的过滤条件。</returns>
	ValueTask<ICondition> ValidateAsync(IDataService service, DataServiceMethod method, ICondition criteria, IDataOptions options, CancellationToken cancellation = default);

	/// <summary>验证指定数据服务方法的写入数据。</summary>
	/// <param name="service">指定的数据服务对象。</param>
	/// <param name="method">待验证的数据服务方法。</param>
	/// <param name="schema">待验证的数据模式。</param>
	/// <param name="data">待验证的写入数据。</param>
	/// <param name="options">待验证方法的选项参数。</param>
	/// <param name="cancellation">取消标记。</param>
	/// <returns>表示异步操作的任务。</returns>
	ValueTask ValidateAsync(IDataService service, DataServiceMethod method, ISchema schema, IDataDictionary data, IDataMutateOptions options, CancellationToken cancellation = default);
}
