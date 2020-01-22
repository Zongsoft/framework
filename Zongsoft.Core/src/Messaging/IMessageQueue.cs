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
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Zongsoft.Messaging
{
	/// <summary>
	/// 表示消息队列的接口。
	/// </summary>
	public interface IMessageQueue : Zongsoft.Collections.IQueue<MessageBase>
	{
		#region 入队方法
		void Enqueue(object item, MessageEnqueueSettings settings = null);
		Task EnqueueAsync(object item, MessageEnqueueSettings settings = null);

		int EnqueueMany<TItem>(IEnumerable<TItem> items, MessageEnqueueSettings settings = null);
		Task<int> EnqueueManyAsync<TItem>(IEnumerable<TItem> items, MessageEnqueueSettings settings = null);
		#endregion

		#region 出队方法
		MessageBase Dequeue(MessageDequeueSettings settings = null);
		IEnumerable<MessageBase> Dequeue(int count, MessageDequeueSettings settings = null);

		Task<MessageBase> DequeueAsync(MessageDequeueSettings settings = null);
		Task<IEnumerable<MessageBase>> DequeueAsync(int count, MessageDequeueSettings settings = null);
		#endregion
	}
}
