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

public class TransactionInformation
{
	#region 成员字段
	private Collections.SynchronizedDictionary<string, object> _parameters;
	#endregion

	#region 构造函数
	public TransactionInformation(Transaction transaction)
	{
		this.Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
		this.Identifier = $"{Guid.NewGuid():N}";
	}
	#endregion

	#region 公共属性
	/// <summary>获取当前事务的标识。</summary>
	public string Identifier { get; }

	/// <summary>获取当前的事务对象。</summary>
	public Transaction Transaction { get; }

	/// <summary>获取当前事务对象的父事务，如果返回空(<c>null</c>)则表示当前事务是根事务。</summary>
	public Transaction Parent => this.Transaction.Parent;

	/// <summary>获取当前事务的环境参数。</summary>
	public Collections.SynchronizedDictionary<string, object> Parameters
	{
		get
		{
			if(_parameters == null)
				System.Threading.Interlocked.CompareExchange(ref _parameters, new(StringComparer.OrdinalIgnoreCase), null);

			return _parameters;
		}
	}
	#endregion
}
