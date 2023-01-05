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
 * Copyright (C) 2010-2023 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Messaging
{
	/// <summary>
	/// 表示消息消费者的接口。
	/// </summary>
	public interface IMessageConsumer : IDisposable
	{
		#region 属性定义
		/// <summary>获取订阅的消息主题。</summary>
		string[] Topics { get; }

		/// <summary>获取订阅的过滤标签。</summary>
		string[] Tags { get; }

		/// <summary>获取一个值，指示消费者是否已订阅完成。</summary>
		bool IsSubscribed { get; }

		/// <summary>获取消息处理器。</summary>
		IMessageHandler Handler { get; }

		/// <summary>获取订阅选项设置。</summary>
		MessageSubscribeOptions Options { get; }
		#endregion

		#region 订阅方法
		/// <summary>订阅消息主题。</summary>
		/// <param name="topics">指定要订阅的消息主题，多个主题之间以分号分隔。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>返回的订阅异步操作任务。</returns>
		ValueTask SubscribeAsync(string topics, CancellationToken cancellation = default);

		/// <summary>订阅消息主题。</summary>
		/// <param name="topics">指定要订阅的消息主题，多个主题之间以分号分隔。</param>
		/// <param name="tags">指定要过滤的消息标签，多个标签之间以分号分隔。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>返回的订阅异步操作任务。</returns>
		ValueTask SubscribeAsync(string topics, string tags, CancellationToken cancellation = default);

		/// <summary>订阅消息主题。</summary>
		/// <param name="topics">指定要订阅的消息主题，多个主题之间以分号分隔。</param>
		/// <param name="options">指定的主题订阅操作的设置。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>返回的订阅异步操作任务。</returns>
		ValueTask SubscribeAsync(string topics, MessageSubscribeOptions options, CancellationToken cancellation = default);

		/// <summary>订阅消息主题。</summary>
		/// <param name="topics">指定要订阅的消息主题，多个主题之间以分号分隔。</param>
		/// <param name="tags">指定要过滤的消息标签，多个标签之间以分号分隔。</param>
		/// <param name="options">指定的主题订阅操作的设置。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>返回的订阅异步操作任务。</returns>
		ValueTask SubscribeAsync(string topics, string tags, MessageSubscribeOptions options, CancellationToken cancellation = default);

		/// <summary>订阅消息主题。</summary>
		/// <param name="topics">指定要订阅的消息主题集。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>返回的订阅异步操作任务。</returns>
		ValueTask SubscribeAsync(IEnumerable<string> topics, CancellationToken cancellation = default);

		/// <summary>订阅消息主题。</summary>
		/// <param name="topics">指定要订阅的消息主题集。</param>
		/// <param name="tags">指定要过滤的消息标签，多个标签之间以分号分隔。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>返回的订阅异步操作任务。</returns>
		ValueTask SubscribeAsync(IEnumerable<string> topics, string tags, CancellationToken cancellation = default);

		/// <summary>订阅消息主题。</summary>
		/// <param name="topics">指定要订阅的消息主题集。</param>
		/// <param name="options">指定的主题订阅操作的设置。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>返回的订阅异步操作任务。</returns>
		ValueTask SubscribeAsync(IEnumerable<string> topics, MessageSubscribeOptions options, CancellationToken cancellation = default);

		/// <summary>订阅消息主题。</summary>
		/// <param name="topics">指定要订阅的消息主题集。</param>
		/// <param name="tags">指定要过滤的消息标签，多个标签之间以分号分隔。</param>
		/// <param name="options">指定的主题订阅操作的设置。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>返回的订阅异步操作任务。</returns>
		ValueTask SubscribeAsync(IEnumerable<string> topics, string tags, MessageSubscribeOptions options, CancellationToken cancellation = default);

		/// <summary>取消所有订阅。</summary>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>返回的取消订阅异步操作任务。</returns>
		ValueTask UnsubscribeAsync(CancellationToken cancellation = default);

		/// <summary>取消订阅消息主题。</summary>
		/// <param name="topics">指定要取消订阅的消息主题（多个主题之间以分号分隔），如果为空则表示取消所有订阅。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>返回的取消订阅异步操作任务。</returns>
		ValueTask UnsubscribeAsync(string topics, CancellationToken cancellation = default);

		/// <summary>取消订阅消息主题。</summary>
		/// <param name="topics">指定要取消订阅的消息主题集，如果为空则表示取消所有订阅。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>返回的取消订阅异步操作任务。</returns>
		ValueTask UnsubscribeAsync(IEnumerable<string> topics, CancellationToken cancellation = default);
		#endregion
	}
}