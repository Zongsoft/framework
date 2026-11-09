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

namespace Zongsoft.Messaging;

/// <summary>表示消息存储器工厂。</summary>
public interface IMessageStorageFactory
{
	/// <summary>创建一个独占的消息存储器。</summary>
	/// <param name="name">指定消息代理或消息存储器容器的名称。</param>
	/// <returns>返回创建的消息存储器，如果创建失败则会抛出异常。</returns>
	IMessageStorage Create(string name);
}
