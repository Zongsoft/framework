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

/// <summary>提供数据服务授权验证功能的接口。</summary>
public interface IDataServiceAuthorizer<TModel>
{
	/// <summary>对数据服务方法进行授权验证。</summary>
	/// <param name="service">指定的数据服务对象。</param>
	/// <param name="method">待验证的数据服务方法。</param>
	/// <param name="options">待验证方法的选项参数。</param>
	/// <param name="cancellation">取消标记。</param>
	/// <returns>表示异步操作的任务。</returns>
	ValueTask AuthorizeAsync(IDataService<TModel> service, DataServiceMethod method, IDataOptions options, CancellationToken cancellation = default);
}
