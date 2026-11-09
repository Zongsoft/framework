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

namespace Zongsoft.Data.Transactions;

/// <summary>表示事务状态的枚举。</summary>
public enum TransactionStatus
{
	/// <summary>事务活动中。</summary>
	Active,

	/// <summary>事务已回滚。</summary>
	Aborted,

	/// <summary>事务已提交。</summary>
	Committed,

	/// <summary>事务的状态未知。</summary>
	Undetermined
}
