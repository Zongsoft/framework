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

using Microsoft.Extensions.AI;

namespace Zongsoft.Intelligences;

/// <summary>表示聊天历史记录的接口。</summary>
public interface IChatHistory : IEnumerable<ChatMessage>
{
	/// <summary>获取记录数量。</summary>
	int Count { get; }

	/// <summary>获取一个值，指示历史记录是否为空。</summary>
	bool IsEmpty { get; }

	/// <summary>获取指定序号的记录。</summary>
	/// <param name="index">指定的记录序号。</param>
	/// <returns>返回对应的记录。</returns>
	ChatMessage this[int index] { get; }

	/// <summary>清空历史记录。</summary>
	void Clear();

	/// <summary>追加聊天消息到历史记录。</summary>
	/// <param name="message">指定的历史聊天记录。</param>
	void Append(ChatMessage message);
}