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

namespace Zongsoft.Data.Transactions;

public interface IEnlistment
{
	/// <summary>异步执行事务登记回调。</summary>
	/// <param name="context">事务登记上下文。</param>
	/// <param name="cancellation">异步操作取消标记。</param>
	/// <returns>表示事务登记回调执行的异步任务。</returns>
	ValueTask OnEnlistAsync(EnlistmentContext context, CancellationToken cancellation = default);
}
