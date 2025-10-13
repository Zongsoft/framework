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
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.Extensions.AI;

namespace Zongsoft.Intelligences;

public static class ChatClientUtility
{
	internal static ChatRole GetRole(string role) => string.IsNullOrWhiteSpace(role) ? ChatRole.User : new ChatRole(role);

	/// <summary>异步聊天。</summary>
	/// <param name="client">指定的聊天客户端。</param>
	/// <param name="content">指定的聊天内容。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回响应文本的异步流。</returns>
	public static IAsyncEnumerable<string> ChatAsync(this IChatClient client, string content, CancellationToken cancellation = default) => ChatAsync(client, ChatRole.User, content, null, cancellation);

	/// <summary>异步聊天。</summary>
	/// <param name="client">指定的聊天客户端。</param>
	/// <param name="content">指定的聊天内容。</param>
	/// <param name="options">指定的选项设置。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回响应文本的异步流。</returns>
	public static IAsyncEnumerable<string> ChatAsync(this IChatClient client, string content, ChatOptions options, CancellationToken cancellation = default) => ChatAsync(client, ChatRole.User, content, options, cancellation);

	/// <summary>异步聊天。</summary>
	/// <param name="client">指定的聊天客户端。</param>
	/// <param name="role">指定的聊天角色。</param>
	/// <param name="content">指定的聊天内容。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回响应文本的异步流。</returns>
	public static IAsyncEnumerable<string> ChatAsync(this IChatClient client, string role, string content, CancellationToken cancellation = default) => ChatAsync(client, GetRole(role), content, null, cancellation);

	/// <summary>异步聊天。</summary>
	/// <param name="client">指定的聊天客户端。</param>
	/// <param name="role">指定的聊天角色。</param>
	/// <param name="content">指定的聊天内容。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回响应文本的异步流。</returns>
	public static IAsyncEnumerable<string> ChatAsync(this IChatClient client, ChatRole role, string content, CancellationToken cancellation = default) => ChatAsync(client, role, content, null, cancellation);

	/// <summary>异步聊天。</summary>
	/// <param name="client">指定的聊天客户端。</param>
	/// <param name="role">指定的聊天角色。</param>
	/// <param name="content">指定的聊天内容。</param>
	/// <param name="options">指定的选项设置。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回响应文本的异步流。</returns>
	public static IAsyncEnumerable<string> ChatAsync(this IChatClient client, string role, string content, ChatOptions options, CancellationToken cancellation = default) => ChatAsync(client, GetRole(role), content, options, cancellation);

	/// <summary>异步聊天。</summary>
	/// <param name="client">指定的聊天客户端。</param>
	/// <param name="role">指定的聊天角色。</param>
	/// <param name="content">指定的聊天内容。</param>
	/// <param name="options">指定的选项设置。</param>
	/// <param name="cancellation">指定的异步操作取消标记。</param>
	/// <returns>返回响应文本的异步流。</returns>
	public static async IAsyncEnumerable<string> ChatAsync(this IChatClient client, ChatRole role, string content, ChatOptions options, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellation = default)
	{
		if(client == null)
			throw new ArgumentNullException(nameof(client));

		if(string.IsNullOrWhiteSpace(content))
			yield break;

		var response = client.GetStreamingResponseAsync(new ChatMessage(role, content), options, cancellation);

		await foreach(var message in response)
			yield return message.Text;
	}
}
