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
 * Copyright (C) 2020-2026 Zongsoft Studio <http://www.zongsoft.com>
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

using Zongsoft.Messaging;
using Zongsoft.Collections;

namespace Zongsoft.Components;

partial class Events
{
	private const string TOPIC = "Events";

	/// <summary>获取事件消息的逻辑主题。</summary>
	/// <param name="qualifiedName">事件的完整限定名；<see langword="null"/> 或空字符串表示事件主题根。</param>
	/// <returns>事件主题根 <c>Events</c> 或指定事件的 <c>Events/完整限定名</c> 主题。</returns>
	/// <remarks>不包含队列分组前缀或订阅通配符；具体订阅表达式由消息队列适配器决定。</remarks>
	public static string GetTopic(string qualifiedName = null) => string.IsNullOrEmpty(qualifiedName) ? TOPIC : $"{TOPIC}/{qualifiedName}";

	/// <summary>创建将队列消息还原为事件并交给指定接收器的处理器。</summary>
	/// <param name="receiver">接收还原事件的接收器。</param>
	/// <returns>绑定到 <paramref name="receiver"/> 的独立消息处理器。</returns>
	/// <exception cref="ArgumentNullException"><paramref name="receiver"/> 为 <see langword="null"/>。</exception>
	/// <remarks>忽略空消息及不以 <c>Events/</c> 开头或缺少事件名的逻辑主题。接收失败和取消向调用方传播，不自动确认消息。</remarks>
	public static IHandler<Message> GetHandler(IEventReceiver receiver)
	{
		ArgumentNullException.ThrowIfNull(receiver);
		return new MessageHandler(receiver);
	}

	private sealed class MessageHandler(IEventReceiver receiver) : HandlerBase<Message>()
	{
		private readonly IEventReceiver _receiver = receiver;

		protected override ValueTask OnHandleAsync(Message message, Parameters parameters, CancellationToken cancellation)
		{
			if(message.IsEmpty)
				return ValueTask.CompletedTask;

			var topic = message.Topic;
			if(topic == null || topic.Length <= TOPIC.Length + 1 || !topic.StartsWith(TOPIC + "/", StringComparison.Ordinal))
				return ValueTask.CompletedTask;

			var name = topic[(TOPIC.Length + 1)..];
			(var argumentType, var argument, parameters) = Marshaler.Unmarshal(name, message.Data);
			return _receiver.ReceiveAsync(name, argumentType, argument, parameters, cancellation);
		}
	}
}
