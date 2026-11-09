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
using System.Collections.Generic;

namespace Zongsoft.Messaging;

/// <summary>表示消息队列提供程序的接口。</summary>
public interface IMessageQueueProvider : IEnumerable<IMessageQueue>
{
	/// <summary>消息队列提供程序名称，譬如：<c>Kafka</c>、<c>RabbitMQ</c>、<c>Redis</c>、<c>Mqtt</c>、<c>Aliyun.MNS</c>等。</summary>
	string Name { get; }

	/// <summary>判断指定名称的消息队列是否存在。</summary>
	/// <param name="name">指定的消息队列名称。</param>
	/// <returns>如果指定名称的消息队列存在则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	bool Exists(string name);

	/// <summary>获取或构建的默认消息队列。</summary>
	/// <param name="settings">指定待构建的消息队列设置信息。</param>
	/// <returns>返回获取或构建的消息队列对象。</returns>
	IMessageQueue Queue(IEnumerable<KeyValuePair<string, string>> settings = null);

	/// <summary>获取或构建指定名称的消息队列。</summary>
	/// <param name="name">指定要获取或构建的队列名，如果为空或空字符串则表示默认队列。</param>
	/// <param name="settings">指定待构建的消息队列设置信息。</param>
	/// <returns>返回获取或构建的消息队列对象。</returns>
	IMessageQueue Queue(string name, IEnumerable<KeyValuePair<string, string>> settings = null);
}
