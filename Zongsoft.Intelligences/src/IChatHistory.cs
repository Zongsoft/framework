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
 * Copyright (C) 2025-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Intelligences library.
 *
 * The Zongsoft.Intelligences is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Intelligences is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Intelligences library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;

using Microsoft.Extensions.AI;

namespace Zongsoft.Intelligences;

/// <summary>
/// 表示聊天历史记录的接口。
/// </summary>
public interface IChatHistory : IEnumerable<ChatMessage>
{
	/// <summary>获取记录数量。</summary>
	int Count { get; }

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