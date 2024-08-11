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

using Zongsoft.Components;

namespace Zongsoft.Messaging
{
	/// <summary>
	/// 表示消息队列的接口。
	/// </summary>
	public interface IMessageQueue : IMessageProducer, IDisposable
	{
		#region 属性定义
		/// <summary>获取消息队列的名称。</summary>
		string Name { get; }

		/// <summary>获取或设置消息队列的连接设置。</summary>
		Configuration.IConnectionSetting ConnectionSetting { get; set; }
		#endregion

		#region 订阅方法
		/// <summary>订阅指定的消息主题。</summary>
		/// <param name="handler">指定的消息接收处理函数。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>返回订阅成功的消息消费者任务。</returns>
		ValueTask<IMessageConsumer> SubscribeAsync(Action<Message> handler, CancellationToken cancellation = default);

		/// <summary>订阅指定的消息主题。</summary>
		/// <param name="handler">指定的消息接收处理函数。</param>
		/// <param name="options">指定的订阅消费者的设置。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>返回订阅成功的消息消费者任务。</returns>
		ValueTask<IMessageConsumer> SubscribeAsync(Action<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation = default);

		/// <summary>订阅指定的消息主题。</summary>
		/// <param name="handler">指定的消息接收处理器对象。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>返回订阅成功的消息消费者任务。</returns>
		ValueTask<IMessageConsumer> SubscribeAsync(IHandler<Message> handler, CancellationToken cancellation = default);

		/// <summary>订阅指定的消息主题。</summary>
		/// <param name="handler">指定的消息接收处理器对象。</param>
		/// <param name="options">指定的订阅消费者的设置。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>返回订阅成功的消息消费者任务。</returns>
		ValueTask<IMessageConsumer> SubscribeAsync(IHandler<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation = default);

		/// <summary>订阅指定的消息主题。</summary>
		/// <param name="topics">指定要订阅的消息主题，多个主题之间以分号分隔。</param>
		/// <param name="handler">指定的消息接收处理函数。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>返回订阅成功的消息消费者任务。</returns>
		ValueTask<IMessageConsumer> SubscribeAsync(string topics, Action<Message> handler, CancellationToken cancellation = default);

		/// <summary>订阅指定的消息主题。</summary>
		/// <param name="topics">指定要订阅的消息主题，多个主题之间以分号分隔。</param>
		/// <param name="handler">指定的消息接收处理函数。</param>
		/// <param name="options">指定的订阅消费者的设置。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>返回订阅成功的消息消费者任务。</returns>
		ValueTask<IMessageConsumer> SubscribeAsync(string topics, Action<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation = default);

		/// <summary>订阅指定的消息主题。</summary>
		/// <param name="topics">指定要订阅的消息主题，多个主题之间以分号分隔。</param>
		/// <param name="handler">指定的消息接收处理器对象。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>返回订阅成功的消息消费者任务。</returns>
		ValueTask<IMessageConsumer> SubscribeAsync(string topics, IHandler<Message> handler, CancellationToken cancellation = default);

		/// <summary>订阅指定的消息主题。</summary>
		/// <param name="topics">指定要订阅的消息主题，多个主题之间以分号分隔。</param>
		/// <param name="handler">指定的消息接收处理器对象。</param>
		/// <param name="options">指定的订阅消费者的设置。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>返回订阅成功的消息消费者任务。</returns>
		ValueTask<IMessageConsumer> SubscribeAsync(string topics, IHandler<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation = default);

		/// <summary>订阅指定的消息主题。</summary>
		/// <param name="topics">指定要订阅的消息主题，多个主题之间以分号分隔。</param>
		/// <param name="tags">指定要订阅的消息标签，多个标签之间以分号分隔。</param>
		/// <param name="handler">指定的消息接收处理函数。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>返回订阅成功的消息消费者任务。</returns>
		ValueTask<IMessageConsumer> SubscribeAsync(string topics, string tags, Action<Message> handler, CancellationToken cancellation = default);

		/// <summary>订阅指定的消息主题。</summary>
		/// <param name="topics">指定要订阅的消息主题，多个主题之间以分号分隔。</param>
		/// <param name="tags">指定要订阅的消息标签，多个标签之间以分号分隔。</param>
		/// <param name="handler">指定的消息接收处理函数。</param>
		/// <param name="options">指定的订阅消费者的设置。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>返回订阅成功的消息消费者任务。</returns>
		ValueTask<IMessageConsumer> SubscribeAsync(string topics, string tags, Action<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation = default);

		/// <summary>订阅指定的消息主题。</summary>
		/// <param name="topics">指定要订阅的消息主题，多个主题之间以分号分隔。</param>
		/// <param name="tags">指定要订阅的消息标签，多个标签之间以分号分隔。</param>
		/// <param name="handler">指定的消息接收处理器对象。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>返回订阅成功的消息消费者任务。</returns>
		ValueTask<IMessageConsumer> SubscribeAsync(string topics, string tags, IHandler<Message> handler, CancellationToken cancellation = default);

		/// <summary>订阅指定的消息主题。</summary>
		/// <param name="topics">指定要订阅的消息主题，多个主题之间以分号分隔。</param>
		/// <param name="tags">指定要订阅的消息标签，多个标签之间以分号分隔。</param>
		/// <param name="handler">指定的消息接收处理器对象。</param>
		/// <param name="options">指定的订阅消费者的设置。</param>
		/// <param name="cancellation">指定的异步操作取消标记。</param>
		/// <returns>返回订阅成功的消息消费者任务。</returns>
		ValueTask<IMessageConsumer> SubscribeAsync(string topics, string tags, IHandler<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation = default);
		#endregion
	}
}
