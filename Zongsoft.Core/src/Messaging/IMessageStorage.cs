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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Configuration;

namespace Zongsoft.Messaging;

/// <summary>表示提供消息存储功能的接口。</summary>
/// <remarks>
/// <para>消息存储独立于消息队列驱动，每个队列或队列服务器应使用独立的存储实例。</para>
/// <para>存储数据的隔离范围由 <see cref="Settings"/> 决定；当 <see cref="Disposable"/> 为真时，挂载该实例的消息队列服务器负责在自身释放时释放它。</para>
/// </remarks>
public interface IMessageStorage
{
	#region 属性定义
	/// <summary>获取存储器的实现名称，如 <c>redis</c> 或 <c>sqlite</c>。</summary>
	/// <value>返回非空的存储实现名称。</value>
	string Name { get; }

	/// <summary>指示当前存储器是否由托管者释放。</summary>
	/// <value>如果存储器由托管者释放则为真(<c>True</c>)，否则为假(<c>False</c>)。</value>
	bool Disposable { get; }

	/// <summary>获取或设置当前存储器的连接设置。</summary>
	/// <value>当前存储器独占的连接设置，不能为空。</value>
	/// <remarks>应在存储器挂载到队列或服务器之前完成设置，运行期间是否允许更换由具体实现决定。</remarks>
	IConnectionSettings Settings { get; set; }
	#endregion

	#region 方法定义
	/// <summary>清除当前存储器中的全部消息。</summary>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回表示异步清除操作的任务，其结果为实际删除的消息数量。</returns>
	/// <exception cref="OperationCanceledException">异步操作已由 <paramref name="cancellation"/> 取消。</exception>
	ValueTask<int> ClearAsync(CancellationToken cancellation = default);

	/// <summary>清除当前存储器中指定主题的全部消息。</summary>
	/// <param name="topic">指定要清除的消息主题；为 <see langword="null"/> 表示清除所有消息，空字符串表示默认主题。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回表示异步清除操作的任务，其结果为实际删除的消息数量。</returns>
	/// <exception cref="OperationCanceledException">异步操作已由 <paramref name="cancellation"/> 取消。</exception>
	/// <remarks>非空主题采用区分大小写的精确匹配；传入 <see langword="null"/> 与调用 <see cref="ClearAsync(CancellationToken)"/> 等效。</remarks>
	ValueTask<int> ClearAsync(string topic, CancellationToken cancellation = default);

	/// <summary>获取当前存储器中的消息。</summary>
	/// <param name="cancellation">指定的异步枚举取消标记。</param>
	/// <returns>返回当前存储器中尚未过期的消息异步序列。</returns>
	/// <exception cref="OperationCanceledException">异步枚举已由 <paramref name="cancellation"/> 取消。</exception>
	/// <remarks>返回的消息必须是独立快照且不得包含确认回调；枚举顺序由具体存储实现定义。</remarks>
	IAsyncEnumerable<Message> GetAsync(CancellationToken cancellation = default);

	/// <summary>获取当前存储器中指定主题的消息。</summary>
	/// <param name="topic">指定要获取的消息主题；为 <see langword="null"/> 表示不限主题，空字符串表示默认主题。</param>
	/// <param name="cancellation">指定的异步枚举取消标记。</param>
	/// <returns>返回指定主题中尚未过期的消息异步序列。</returns>
	/// <exception cref="OperationCanceledException">异步枚举已由 <paramref name="cancellation"/> 取消。</exception>
	/// <remarks>非空主题采用区分大小写的精确匹配；传入 <see langword="null"/> 与调用 <see cref="GetAsync(CancellationToken)"/> 等效。返回的消息必须是独立快照且不得包含确认回调，枚举顺序由具体存储实现定义。</remarks>
	IAsyncEnumerable<Message> GetAsync(string topic, CancellationToken cancellation = default);

	/// <summary>新增或更新指定的消息。</summary>
	/// <param name="message">指定要保存的消息。</param>
	/// <param name="expiry">指定消息的生存时长，小于或等于零表示永久保存。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回表示异步保存操作的任务。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="message"/> 没有有效标识。</exception>
	/// <exception cref="OperationCanceledException">异步操作已由 <paramref name="cancellation"/> 取消。</exception>
	/// <remarks>该方法完成时，存储实现必须已持有消息及其数据、标签等可变成员的快照，后续修改调用方传入的消息不得改变已保存内容；确认回调不属于存储内容。</remarks>
	ValueTask SetAsync(Message message, TimeSpan expiry = default, CancellationToken cancellation = default);

	/// <summary>删除指定的消息。</summary>
	/// <param name="identifier">指定要删除的消息标识。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回表示异步删除操作的任务；如果找到并删除了指定消息则其结果为真，否则为假。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="identifier"/> 为空或空白字符串。</exception>
	/// <exception cref="OperationCanceledException">异步操作已由 <paramref name="cancellation"/> 取消。</exception>
	ValueTask<bool> RemoveAsync(string identifier, CancellationToken cancellation = default);
	#endregion
}
