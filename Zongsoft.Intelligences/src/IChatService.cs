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
using System.Threading;
using System.Collections.Generic;

using Microsoft.Extensions.AI;

namespace Zongsoft.Intelligences;

public interface IChatService : IChatClient
{
	/// <summary>获取聊天会话管理器。</summary>
	IChatSessionManager Sessions { get; }
	/// <summary>获取聊天服务的设置信息。</summary>
	Configuration.IConnectionSettings Settings { get; }

	/// <summary>异步聊天。</summary>
	/// <param name="content">指定的聊天内容。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回响应文本的异步流。</returns>
	IAsyncEnumerable<string> ChatAsync(string content, CancellationToken cancellation = default) => this.ChatAsync(content, null, cancellation);

	/// <summary>异步聊天。</summary>
	/// <param name="content">指定的聊天内容。</param>
	/// <param name="options">指定的选项设置。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回响应文本的异步流。</returns>
	IAsyncEnumerable<string> ChatAsync(string content, ChatOptions options, CancellationToken cancellation = default);
}
