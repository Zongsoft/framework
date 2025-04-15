﻿/*
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
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Collections;

/// <summary>
/// 队列，表示先进先出的数据容器。
/// </summary>
public interface IQueue : ICollection
{
	#region 事件定义
	/// <summary>当入队成功后激发的事件。</summary>
	event EventHandler<EnqueuedEventArgs> Enqueued;
	/// <summary>当出队成功后激发的事件。</summary>
	event EventHandler<DequeuedEventArgs> Dequeued;
	#endregion

	#region 属性定义
	/// <summary>获取队列的名称，该名称应该为队列的唯一标识。</summary>
	string Name { get; }

	/// <summary>获取队列容量值，即队列中已分配的可用元素数，该值在扩容时可能会增加。</summary>
	int Capacity { get; }
	#endregion

	#region 清空方法
	/// <summary>移除队列中的所有元素。</summary>
	void Clear();

	/// <summary>异步移除队列中的所有元素。</summary>
	/// <param name="cancellation">监视取消请求的令牌。</param>
	/// <returns>返回表示异步操作的任务对象。</returns>
	Task ClearAsync(CancellationToken cancellation = default);
	#endregion

	#region 入队方法
	/// <summary>将对象添加到队列的结尾处。</summary>
	/// <param name="item">要入队的对象，该值可以为空(null)。</param>
	/// <param name="settings">指定入队的一些选项参数，具体内容请参考特定实现者的规范。</param>
	void Enqueue(object item, object settings = null);

	/// <summary>将对象添加到队列的结尾处。</summary>
	/// <param name="item">要入队的对象，该值可以为空(null)。</param>
	/// <param name="settings">指定入队的一些选项参数，具体内容请参考特定实现者的规范。</param>
	/// <param name="cancellation">监视取消请求的令牌。</param>
	/// <returns>返回表示异步操作的任务对象。</returns>
	Task EnqueueAsync(object item, object settings = null, CancellationToken cancellation = default);

	/// <summary>将指定集合中的所有元素依次添加到队列的结尾处。</summary>
	/// <param name="items">要入队的集合。</param>
	/// <param name="settings">指定入队的一些选项参数，具体内容请参考特定实现者的规范。</param>
	void EnqueueMany<T>(IEnumerable<T> items, object settings = null);

	/// <summary>将指定集合中的所有元素依次添加到队列的结尾处。</summary>
	/// <param name="items">要入队的集合。</param>
	/// <param name="settings">指定入队的一些选项参数，具体内容请参考特定实现者的规范。</param>
	/// <param name="cancellation">监视取消请求的令牌。</param>
	/// <returns>返回表示异步操作的任务对象。</returns>
	Task EnqueueManyAsync<T>(IEnumerable<T> items, object settings = null, CancellationToken cancellation = default);
	#endregion

	#region 出队方法
	/// <summary>移除并返回位于队列开始处的对象。</summary>
	/// <param name="settings">指定出队的一些选项参数，具体内容请参考特定实现者的规范。</param>
	/// <returns>返回从队列的开头处移除的对象。</returns>
	object Dequeue(object settings = null);

	/// <summary>移除并返回位于队列开始处的对象。</summary>
	/// <param name="settings">指定出队的一些选项参数，具体内容请参考特定实现者的规范。</param>
	/// <param name="cancellation">监视取消请求的令牌。</param>
	/// <returns>返回表示异步操作的任务对象。</returns>
	Task<object> DequeueAsync(object settings = null, CancellationToken cancellation = default);

	/// <summary>移除并返回从开始处的由<paramref name="count"/>参数指定的连续多个对象。</summary>
	/// <param name="count">指定要连续移除的元素数。</param>
	/// <param name="settings">指定出队的一些选项参数，具体内容请参考特定实现者的规范。</param>
	/// <returns>从队列的开头处指定的连续对象集。</returns>
	/// <exception cref="System.InvalidOperationException">当队列为空，即<seealso cref="ICollection.Count"/>属性等于零。</exception>
	/// <exception cref="System.ArgumentOutOfRangeException"><paramref name="count"/>参数小于壹(1)。</exception>
	/// <remarks>如果<paramref name="count"/>参数指定的数值超出队列中可用的元素数，则忽略该参数值，而应用可用的元素数。</remarks>
	IEnumerable DequeueMany(int count, object settings = null);

	/// <summary>移除并返回从开始处的由<paramref name="count"/>参数指定的连续多个对象。</summary>
	/// <param name="count">指定要连续移除的元素数。</param>
	/// <param name="settings">指定出队的一些选项参数，具体内容请参考特定实现者的规范。</param>
	/// <param name="cancellation">监视取消请求的令牌。</param>
	/// <returns>从队列的开头处指定的连续对象集。</returns>
	/// <exception cref="System.ArgumentOutOfRangeException"><paramref name="count"/>参数小于壹(1)。</exception>
	/// <remarks>如果<paramref name="count"/>参数指定的数值超出队列中可用的元素数，则忽略该参数值，而应用可用的元素数。</remarks>
	IAsyncEnumerable<object> DequeueManyAsync(int count, object settings = null, CancellationToken cancellation = default);
	#endregion

	#region 获取方法
	/// <summary>返回位于队列开始处的对象但不将其移除。</summary>
	/// <returns>返回位于队列开头处的对象。</returns>
	/// <remarks>
	/// 	<para>此方法类似于<seealso cref="Dequeue(object)"/>出队方法，但本方法不修改<seealso cref="Zongsoft.Collections.Queue"/>队列。</para>
	/// </remarks>
	object Peek();

	/// <summary>返回位于队列开始处的对象但不将其移除。</summary>
	/// <param name="cancellation">监视取消请求的令牌。</param>
	/// <returns>返回表示异步操作的任务对象。</returns>
	/// <remarks>
	/// 	<para>此方法类似于<seealso cref="DequeueAsync(object, CancellationToken)"/>出队方法，但本方法不修改<seealso cref="Zongsoft.Collections.Queue"/>队列。</para>
	/// </remarks>
	Task<object> PeekAsync(CancellationToken cancellation = default);
	#endregion

	#region 默认实现
	Task EnqueueAsync(object item, CancellationToken cancellation) => this.EnqueueAsync(item, null, cancellation);
	Task EnqueueManyAsync<T>(IEnumerable<T> items, CancellationToken cancellation) => this.EnqueueManyAsync(items, null, cancellation);
	#endregion
}
