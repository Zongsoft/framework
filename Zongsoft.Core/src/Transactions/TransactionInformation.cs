/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Collections.Concurrent;

namespace Zongsoft.Transactions
{
	public class TransactionInformation
	{
		#region 成员字段
		private Guid _transactionId;
		private Transaction _transaction;
		private ConcurrentDictionary<string, object> _parameters;
		#endregion

		#region 构造函数
		public TransactionInformation(Transaction transaction)
		{
			if(transaction == null)
				throw new ArgumentNullException("transaction");

			_transaction = transaction;
			_transactionId = Guid.NewGuid();
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取当前事务的唯一编号。
		/// </summary>
		public Guid TransactionId
		{
			get
			{
				return _transactionId;
			}
		}

		/// <summary>
		/// 获取当前的事务对象。
		/// </summary>
		public Transaction Transaction
		{
			get
			{
				return _transaction;
			}
		}

		/// <summary>
		/// 获取当前事务对象的父事务，如果当前事务是根事务则返回空(null)。
		/// </summary>
		public Transaction Parent
		{
			get
			{
				return _transaction.Parent;
			}
		}

		/// <summary>
		/// 获取当前事务的行为特性。
		/// </summary>
		public TransactionBehavior Behavior
		{
			get
			{
				return _transaction.Behavior;
			}
		}

		/// <summary>
		/// 获取当前事务的状态。
		/// </summary>
		public TransactionStatus Status
		{
			get
			{
				return _transaction.Status;
			}
		}

		/// <summary>
		/// 获取当前事务的环境参数。
		/// </summary>
		public ConcurrentDictionary<string, object> Parameters
		{
			get
			{
				if(_parameters == null)
					System.Threading.Interlocked.CompareExchange(ref _parameters, new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase), null);

				return _parameters;
			}
		}
		#endregion
	}
}
