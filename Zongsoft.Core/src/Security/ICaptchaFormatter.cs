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

namespace Zongsoft.Security;

/// <summary>提供人机识别签发结果的格式化功能。</summary>
/// <typeparam name="TContext">表示格式化上下文的类型。</typeparam>
public interface ICaptchaFormatter<TContext>
{
	/// <summary>格式化方法。</summary>
	/// <param name="context">指定的格式化上下文对象。</param>
	/// <param name="value">指定的待格式化的数据。</param>
	/// <param name="cancellation">异步操作的取消标记。</param>
	/// <returns>返回格式化后的结果。</returns>
	ValueTask<object> FormatAsync(TContext context, object value, CancellationToken cancellation = default);
}