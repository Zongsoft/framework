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
using System.Collections;
using System.Collections.Generic;

using Microsoft.Extensions.AI;

namespace Zongsoft.Intelligences;

public class ChatHistory
{
	#region 单例字段
	public static readonly IChatHistory Memory = new MemoryChatHistory();
	#endregion

	private sealed class MemoryChatHistory : IChatHistory, IEnumerable<ChatMessage>
	{
		#region 成员字段
		private readonly List<ChatMessage> _messages;
		#endregion

		#region 公共属性
		public int Count => _messages.Count;
		public ChatMessage this[int index] => _messages[index];
		#endregion

		#region 公共方法
		public void Clear() => _messages.Clear();
		public void Append(ChatMessage message) => _messages.Add(message ?? throw new ArgumentNullException(nameof(message)));
		#endregion

		#region 枚举遍历
		IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
		public IEnumerator<ChatMessage> GetEnumerator() => _messages.GetEnumerator();
		#endregion
	}
}