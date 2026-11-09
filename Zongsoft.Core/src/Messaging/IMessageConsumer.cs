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
using Zongsoft.Communication;

namespace Zongsoft.Messaging;

/// <summary>表示消息消费者的接口。</summary>
public interface IMessageConsumer : IChannel
{
	#region 属性定义
	/// <summary>获取订阅的消息主题。</summary>
	string Topic { get; }

	/// <summary>获取订阅的过滤标签。</summary>
	string[] Tags { get; }

	/// <summary>获取消息处理器。</summary>
	IHandler<Message> Handler { get; }

	/// <summary>获取订阅选项设置。</summary>
	MessageSubscribeOptions Options { get; }
	#endregion

	#region 订阅方法
	/// <summary>取消当前的订阅。</summary>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回的取消订阅异步操作任务。</returns>
	ValueTask UnsubscribeAsync(CancellationToken cancellation = default);
	#endregion
}