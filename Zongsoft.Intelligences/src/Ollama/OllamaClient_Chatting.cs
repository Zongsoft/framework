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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.Extensions.AI;

namespace Zongsoft.Intelligences.Ollama;

[Services.Service<IChatClient>(Tags = "Ollama")]
[Services.Service<IChatService>(Tags = "Ollama")]
partial class OllamaClient : IChatService, IChatClient
{
	#region 公共属性
	public ChatSessionManager Sessions { get; }
	#endregion

	#region 显式属性
	Configuration.IConnectionSettings IChatService.Settings => this.Settings;
	#endregion

	#region 公共方法
	public object GetService(Type serviceType, object serviceKey = null) => ((IChatClient)_client).GetService(serviceType, serviceKey);
	public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions options = null, CancellationToken cancellation = default) =>
		((IChatClient)_client).GetResponseAsync(messages, options, cancellation);
	public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions options = null, CancellationToken cancellation = default) =>
		((IChatClient)_client).GetStreamingResponseAsync(messages, options, cancellation);
	#endregion
}
