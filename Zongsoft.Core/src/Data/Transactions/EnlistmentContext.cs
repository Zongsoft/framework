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

public class EnlistmentContext
{
	#region 成员字段
	private EnlistmentPhase _phase;
	private Transaction _transaction;
	#endregion

	#region 构造函数
	internal EnlistmentContext(Transaction transaction, EnlistmentPhase phase)
	{
		if(transaction == null)
			throw new ArgumentNullException(nameof(transaction));

		_transaction = transaction;
		_phase = phase;
	}
	#endregion

	#region 公共属性
	/// <summary>获取当前事务阶段。</summary>
	public EnlistmentPhase Phase => _phase;
	/// <summary>获取当前的事务对象。</summary>
	public Transaction Transaction => _transaction;
	#endregion
}
