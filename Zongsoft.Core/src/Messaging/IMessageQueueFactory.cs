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

namespace Zongsoft.Messaging;

/// <summary>提供消息队列创建功能的接口。</summary>
public interface IMessageQueueFactory
{
	/// <summary>获取消息队列类型名称。</summary>
	string Name { get; }

	/// <summary>创建一个消息队列实例。</summary>
	/// <param name="settings">指定要创建的消息队列设置。</param>
	/// <returns>返回创建成功的消息队列对象。</returns>
	IMessageQueue Create(IMessageQueueSettings settings);

	/// <summary>创建一个消息队列实例。</summary>
	/// <param name="name">指定的消息队列名称。</param>
	/// <param name="connectionString">指定要创建的消息队列的连接字符串。</param>
	/// <returns>返回创建成功的消息队列对象。</returns>
	IMessageQueue Create(string name, string connectionString);
}
