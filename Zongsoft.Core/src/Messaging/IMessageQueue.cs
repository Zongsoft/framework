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
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Messaging
{
	public interface IMessageQueue
	{
		#region 公共属性
		/// <summary>获取队列的名称，该名称应该为队列的唯一标识。</summary>
		string Name { get; }
		#endregion

		#region 长度方法
		/// <summary>
		/// 获取队列的元素数量。
		/// </summary>
		/// <returns>返回的队列元素数量。</returns>
		long GetCount();

		/// <summary>
		/// 获取队列的元素数量。
		/// </summary>
		/// <param name="cancellation">监视取消请求的令牌。</param>
		/// <returns>返回的队列元素数量。</returns>
		Task<long> GetCountAsync(CancellationToken cancellation = default);
		#endregion

		#region 清空方法
		/// <summary>
		/// 移除队列中的所有元素。
		/// </summary>
		void Clear();

		/// <summary>
		/// 异步移除队列中的所有元素。
		/// </summary>
		/// <param name="cancellation">监视取消请求的令牌。</param>
		/// <returns>返回表示异步操作的任务对象。</returns>
		Task ClearAsync(CancellationToken cancellation = default);
		#endregion

		#region 入队方法
		/// <summary>
		/// 将数据添加到队列的结尾处。
		/// </summary>
		/// <param name="data">要入队的数据。</param>
		/// <param name="settings">指定入队的一些选项参数，具体内容请参考特定实现者的规范。</param>
		void Enqueue(ReadOnlySpan<byte> data, MessageEnqueueSettings settings = null);

		/// <summary>
		/// 将数据添加到队列的结尾处。
		/// </summary>
		/// <param name="data">要入队的数据。</param>
		/// <param name="settings">指定入队的一些选项参数，具体内容请参考特定实现者的规范。</param>
		/// <param name="cancellation">监视取消请求的令牌。</param>
		/// <returns>返回表示异步操作的任务对象。</returns>
		Task EnqueueAsync(ReadOnlySpan<byte> data, MessageEnqueueSettings settings = null, CancellationToken cancellation = default);
		#endregion

		#region 出队方法
		/// <summary>
		/// 移除并返回位于队列开始处的数据。
		/// </summary>
		/// <param name="settings">指定出队的一些选项参数，具体内容请参考特定实现者的规范。</param>
		/// <returns>返回从队列的开头处移除的数据。</returns>
		byte[] Dequeue(MessageDequeueSettings settings = null);

		/// <summary>
		/// 移除并返回位于队列开始处的数据。
		/// </summary>
		/// <param name="settings">指定出队的一些选项参数，具体内容请参考特定实现者的规范。</param>
		/// <param name="cancellation">监视取消请求的令牌。</param>
		/// <returns>返回表示异步操作的任务数据。</returns>
		Task<byte[]> DequeueAsync(MessageDequeueSettings settings, CancellationToken cancellation = default);
		#endregion

		#region 获取方法
		/// <summary>
		/// 返回位于队列开始处的数据但不将其移除。
		/// </summary>
		/// <returns>返回位于队列开头处的数据。</returns>
		/// <remarks>
		/// 	<para>此方法类似于<seealso cref="Dequeue(MessageDequeueSettings)"/>出队方法，但本方法不修改<seealso cref="Zongsoft.Collections.Queue"/>队列。</para>
		/// </remarks>
		byte[] Peek();

		/// <summary>
		/// 返回位于队列开始处的数据但不将其移除。
		/// </summary>
		/// <param name="cancellation">监视取消请求的令牌。</param>
		/// <returns>返回表示异步操作的任务数据。</returns>
		/// <remarks>
		/// 	<para>此方法类似于<seealso cref="DequeueAsync(MessageDequeueSettings, CancellationToken)"/>出队方法，但本方法不修改<seealso cref="Zongsoft.Collections.Queue"/>队列。</para>
		/// </remarks>
		Task<byte[]> PeekAsync(CancellationToken cancellation = default);
		#endregion

		#region 默认实现
		Task EnqueueAsync(ReadOnlySpan<byte> data, CancellationToken cancellation) => this.EnqueueAsync(data, null, cancellation);
		Task<byte[]> DequeueAsync(CancellationToken cancellation) => this.DequeueAsync(null, cancellation);
		#endregion
	}
}
