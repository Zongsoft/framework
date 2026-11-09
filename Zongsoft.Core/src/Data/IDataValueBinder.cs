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

/// <summary>提供数据实体属性写入值的校验和绑定的接口。</summary>
public interface IDataValueBinder
{
	/// <summary>绑定数据写入时指定属性的值。</summary>
	/// <param name="context">当前数据访问上下文对象。</param>
	/// <param name="data">待写入的数据项。</param>
	/// <param name="value">待写入的属性值。</param>
	/// <param name="cancellation">异步操作的取消令牌。</param>
	/// <returns>返回写入的数据值。</returns>
	ValueTask<object> BindAsync(IDataMutateContextBase context, object data, object value, CancellationToken cancellation = default);
}
