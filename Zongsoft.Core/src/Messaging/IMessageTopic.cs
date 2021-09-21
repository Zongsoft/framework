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
using System.Collections.Generic;

namespace Zongsoft.Messaging
{
	/// <summary>
	/// 提供消息主题基础功能的接口。
	/// </summary>
	public interface IMessageTopic
	{
		#region 属性定义
		/// <summary>获取消息队列的名称。</summary>
		string Name { get; }

		/// <summary>获取或设置消息主题的参数设置。</summary>
		IMessageTopicOptions Options { get; set; }
		#endregion

		#region 方法定义
		bool Subscribe(string topic, MessageTopicSubscriptionOptions options = null) => this.Subscribe(topic, null, options);
		bool Subscribe(string topic, string tags, MessageTopicSubscriptionOptions options = null);
		Task<bool> SubscribeAsync(string topic, MessageTopicSubscriptionOptions options = null) => this.SubscribeAsync(topic, null, options);
		Task<bool> SubscribeAsync(string topic, string tags, MessageTopicSubscriptionOptions options = null);

		void Publish(ReadOnlySpan<byte> data, string topic, MessageTopicPublishOptions options = null) => this.Publish(data, topic, null, options);
		void Publish(ReadOnlySpan<byte> data, string topic, string tags, MessageTopicPublishOptions options = null);
		Task PublishAsync(ReadOnlySpan<byte> data, string topic, MessageTopicPublishOptions options = null, CancellationToken cancellation = default) => this.PublishAsync(data, topic, null, options, cancellation);
		Task PublishAsync(ReadOnlySpan<byte> data, string topic, string tags, MessageTopicPublishOptions options = null, CancellationToken cancellation = default);
		#endregion
	}

	/// <summary>
	/// 提供消息主题相关功能的接口。
	/// </summary>
	public interface IMessageTopic<TMessage> : IMessageTopic
	{
		#region 方法定义
		/// <summary>
		/// 处理订阅的消息。
		/// </summary>
		/// <param name="message">待处理的消息。</param>
		bool Handle(ref TMessage message);

		/// <summary>
		/// 处理订阅的消息。
		/// </summary>
		/// <param name="message">待处理的消息。</param>
		/// <param name="cancellation">指定的异步取消标记。</param>
		Task<bool> HandleAsync(ref TMessage message, CancellationToken cancellation = default);
		#endregion
	}
}
