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

using OllamaSharp;
using Microsoft.Extensions.AI;

using Zongsoft.Services;
using Zongsoft.Components;

namespace Zongsoft.Intelligences.Commands;

[CommandOption("model", Type = typeof(string))]
public class OllamaCommand : CommandBase<CommandContext>, IServiceAccessor<IChatSession>
{
	#region 成员字段
	private readonly ChatSession _session = new();
	#endregion

	#region 构造函数
	public OllamaCommand() : base("Ollama") { }
	public OllamaCommand(string name) : base(name) { }
	#endregion

	#region 公共属性
	public IOllamaApiClient Client
	{
		get => _session.Client;
		set => _session.Client = value;
	}

	public IChatSession Session => _session;
	IChatSession IServiceAccessor<IChatSession>.Value => this.Session;
	#endregion

	#region 重写方法
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		if(context.Value is IOllamaApiClient client)
			this.Client = client;

		if(context.Expression.Arguments.Count > 0)
		{
			var model = context.Expression.Arguments.Count > 1 ? context.Expression.Arguments[1] : string.Empty;

			if(Uri.TryCreate(context.Expression.Arguments[0], UriKind.Absolute, out var url))
				this.Client = new OllamaApiClient(url, model);
			else
				this.Client = new OllamaApiClient($"http://{context.Expression.Arguments[0]}:11434", model);
		}

		if(this.Client != null && context.Expression.Options.TryGetValue<string>("model", out var modelName))
			this.Client.SelectedModel = modelName;

		return ValueTask.FromResult<object>(this.Client);
	}
	#endregion

	#region 嵌套子类
	private sealed class ChatSession : IChatSession
	{
		IChatClient IChatSession.Client => this.Client as IChatClient;
		public IOllamaApiClient Client { get; set; }
		public IList<ChatMessage> History { get; }

		public ChatSession(IList<ChatMessage> history = null) : this(null, history) { }
		public ChatSession(IOllamaApiClient client, IList<ChatMessage> history = null)
		{
			this.Client = client;
			this.History = history ?? new List<ChatMessage>(64);
		}
	}
	#endregion
}
