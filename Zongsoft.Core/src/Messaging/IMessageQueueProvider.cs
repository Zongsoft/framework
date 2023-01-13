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
using System.Collections.Generic;

namespace Zongsoft.Messaging
{
	/// <summary>
	/// 表示消息队列提供程序的接口。
	/// </summary>
	public interface IMessageQueueProvider : IEnumerable<IMessageQueue>
	{
		/// <summary>消息队列提供程序名称，譬如：<c>Kafka</c>、<c>RabbitMQ</c>、<c>RocketMQ</c>、<c>Mqtt</c>、<c>Aliyun.MNS</c>等。</summary>
		string Name { get; }

		/// <summary>判断指定名称的消息队列是否存在。</summary>
		/// <param name="name">指定的消息队列名称。</param>
		/// <returns>如果指定名称的消息队列存在则返回真(True)，否则返回假(False)。</returns>
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
}
