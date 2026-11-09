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

namespace Zongsoft.Common;

/// <summary>表示条件判断的接口。</summary>
public interface IPredication<in T> : IPredication
{
	/// <summary>确定指定对象是否符合某种条件。</summary>
	/// <param name="argument">指定的条件参数对象。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>如果符合某种条件则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	ValueTask<bool> PredicateAsync(T argument, CancellationToken cancellation = default);

	/// <summary>确定指定对象是否符合某种条件。</summary>
	/// <param name="argument">指定的条件参数对象。</param>
	/// <param name="parameters">指定的附加参数集。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>如果符合某种条件则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	ValueTask<bool> PredicateAsync(T argument, Collections.Parameters parameters, CancellationToken cancellation = default);
}
