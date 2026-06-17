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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
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
