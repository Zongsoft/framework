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

namespace Zongsoft.Intelligences.Ollama;

[Services.Service<IChatClient>(Tags = "Ollama")]
[Services.Service<IChatService>(Tags = "Ollama")]
partial class OllamaClient : IChatService, IChatClient
{
	#region 公共属性
	public IChatSessionManager Sessions { get; }
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
