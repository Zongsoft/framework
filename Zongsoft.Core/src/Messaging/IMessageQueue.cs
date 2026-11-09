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
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Components;

namespace Zongsoft.Messaging;

/// <summary>表示消息队列的接口。</summary>
public interface IMessageQueue : IMessageProducer, IAsyncDisposable
{
	#region 属性定义
	/// <summary>获取消息队列的名称。</summary>
	string Name { get; }
	/// <summary>获取消息队列支持的功能集。</summary>
	MessageQueueFeatureCollection Features { get; }
	/// <summary>获取一个值，指示消息队列是否已被处置。</summary>
	bool IsDisposed { get; }
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
	/// <param name="topic">指定要订阅的消息主题。</param>
	/// <param name="handler">指定的消息接收处理函数。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回订阅成功的消息消费者任务。</returns>
	ValueTask<IMessageConsumer> SubscribeAsync(string topic, Action<Message> handler, CancellationToken cancellation = default);

	/// <summary>订阅指定的消息主题。</summary>
	/// <param name="topic">指定要订阅的消息主题。</param>
	/// <param name="handler">指定的消息接收处理函数。</param>
	/// <param name="options">指定的订阅消费者的设置。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回订阅成功的消息消费者任务。</returns>
	ValueTask<IMessageConsumer> SubscribeAsync(string topic, Action<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation = default);

	/// <summary>订阅指定的消息主题。</summary>
	/// <param name="topic">指定要订阅的消息主题。</param>
	/// <param name="handler">指定的消息接收处理器对象。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回订阅成功的消息消费者任务。</returns>
	ValueTask<IMessageConsumer> SubscribeAsync(string topic, IHandler<Message> handler, CancellationToken cancellation = default);

	/// <summary>订阅指定的消息主题。</summary>
	/// <param name="topic">指定要订阅的消息主题。</param>
	/// <param name="handler">指定的消息接收处理器对象。</param>
	/// <param name="options">指定的订阅消费者的设置。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回订阅成功的消息消费者任务。</returns>
	ValueTask<IMessageConsumer> SubscribeAsync(string topic, IHandler<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation = default);

	/// <summary>订阅指定的消息主题。</summary>
	/// <param name="topic">指定要订阅的消息主题。</param>
	/// <param name="tags">指定要订阅的消息标签，多个标签之间以分号分隔。</param>
	/// <param name="handler">指定的消息接收处理函数。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回订阅成功的消息消费者任务。</returns>
	ValueTask<IMessageConsumer> SubscribeAsync(string topic, string tags, Action<Message> handler, CancellationToken cancellation = default);

	/// <summary>订阅指定的消息主题。</summary>
	/// <param name="topic">指定要订阅的消息主题。</param>
	/// <param name="tags">指定要订阅的消息标签，多个标签之间以分号分隔。</param>
	/// <param name="handler">指定的消息接收处理函数。</param>
	/// <param name="options">指定的订阅消费者的设置。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回订阅成功的消息消费者任务。</returns>
	ValueTask<IMessageConsumer> SubscribeAsync(string topic, string tags, Action<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation = default);

	/// <summary>订阅指定的消息主题。</summary>
	/// <param name="topic">指定要订阅的消息主题。</param>
	/// <param name="tags">指定要订阅的消息标签，多个标签之间以分号分隔。</param>
	/// <param name="handler">指定的消息接收处理器对象。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回订阅成功的消息消费者任务。</returns>
	ValueTask<IMessageConsumer> SubscribeAsync(string topic, string tags, IHandler<Message> handler, CancellationToken cancellation = default);

	/// <summary>订阅指定的消息主题。</summary>
	/// <param name="topic">指定要订阅的消息主题。</param>
	/// <param name="tags">指定要订阅的消息标签，多个标签之间以分号分隔。</param>
	/// <param name="handler">指定的消息接收处理器对象。</param>
	/// <param name="options">指定的订阅消费者的设置。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回订阅成功的消息消费者任务。</returns>
	ValueTask<IMessageConsumer> SubscribeAsync(string topic, string tags, IHandler<Message> handler, MessageSubscribeOptions options, CancellationToken cancellation = default);
	#endregion
}
