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

using Microsoft.Extensions.AI;

namespace Zongsoft.Intelligences;

/// <summary>表示聊天会话的接口。</summary>
public interface IChatSession : IChatClient, IEquatable<IChatSession>, IDisposable, IAsyncDisposable
{
	/// <summary>获取会话标识。</summary>
	string Identifier { get; }
	/// <summary>获取会话的创建时间。</summary>
	DateTimeOffset Creation { get; }
	/// <summary>获取聊天历史记录。</summary>
	IChatHistory History { get; }
	/// <summary>获取聊天会话的选项。</summary>
	ChatSessionOptions Options { get; }
	/// <summary>获取或设置会话的概述。</summary>
	string Summary { get; set; }
}
