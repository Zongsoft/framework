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
using System.Threading.Tasks;

using Microsoft.Extensions.AI;

namespace Zongsoft.Intelligences;

public abstract class ChatSessionBase<TClient> : IChatSession where TClient : class, IChatClient
{
	#region 构造函数
	protected ChatSessionBase() : this(null, null) { }
	protected ChatSessionBase(TClient client) : this(null, client) { }
	protected ChatSessionBase(string identifier, TClient client)
	{
		this.Client = client;
		this.Identifier = identifier ?? Common.Randomizer.GenerateString(10);
		this.History = ChatHistory.Memory;
	}
	#endregion

	#region 公共属性
	public string Identifier { get; }
	public IChatHistory History { get; }
	public TClient Client { get; protected set; }
	IChatClient IChatSession.Client => this.Client;
	#endregion

	#region 释放处置
	public void Dispose()
	{
		this.Dispose(true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if(this.Client is IDisposable disposable)
			disposable.Dispose();

		this.Client = null;
		this.History?.Clear();
	}

	public async ValueTask DisposeAsync()
	{
		await this.DisposeAsync(true);
		GC.SuppressFinalize(this);
	}

	protected virtual async ValueTask DisposeAsync(bool disposing)
	{
		if(this.Client is IAsyncDisposable asyncDisposable)
		{
			await asyncDisposable.DisposeAsync();
			this.Client = null;
			this.History?.Clear();
		}
		else
		{
			this.Dispose(disposing);
		}
	}
	#endregion
}
