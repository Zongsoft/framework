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

using Zongsoft.Caching;

namespace Zongsoft.Intelligences;

public class ChatSessionManager(IChatService service) : IChatSessionManager
{
	#region 事件声明
	public event EventHandler<ChatSessionEventArgs> Created;
	public event EventHandler<ChatSessionEventArgs> Activated;
	public event EventHandler<ChatSessionEventArgs> Abandoned;
	#endregion

	#region 成员字段
	private readonly MemoryCache _cache = new();
	private readonly IChatService _service = service;
	#endregion

	#region 公共属性
	public IChatSession Current { get; set; }
	public IChatSession this[string identifier] => this.Get(identifier) ?? throw new KeyNotFoundException();
	#endregion

	#region 公共方法
	public IChatSession Create(ChatSessionOptions options = null)
	{
		var session = new ChatSession(_service, options);

		//设置会话的开场白（即聊天会话的系统提示）
		if(!string.IsNullOrWhiteSpace(session.Options.Prelude))
			session.History.Append(new Microsoft.Extensions.AI.ChatMessage(Microsoft.Extensions.AI.ChatRole.System, session.Options.Prelude));

		//将新建会话保存到缓存中
		_cache.SetValue(session.Identifier, session, session.Options.Expiration);

		//更新当前会话
		this.Current = session;

		//激发“Created”事件
		this.OnCreated(new(_service, session));

		return session;
	}

	public IChatSession Get(string identifier)
	{
		if(string.IsNullOrEmpty(identifier))
			return this.Current;

		return _cache.TryGetValue(identifier, out var value) ? value as IChatSession : null;
	}

	public IChatSession Abandon(string identifier)
	{
		if(string.IsNullOrEmpty(identifier))
			return null;

		if(_cache.Remove(identifier, out var value) && value is IChatSession session)
		{
			//处置移除的会话
			session.Dispose();

			//激发“Abandoned”事件
			this.OnAbandoned(new(_service, session));

			return session;
		}

		return null;
	}

	public IChatSession Activate(string identifier)
	{
		if(string.IsNullOrEmpty(identifier))
			return this.Current;

		if(_cache.TryGetValue(identifier, out var value) && !object.Equals(this.Current, value))
		{
			//设置当前会话
			var current = this.Current = value as IChatSession;

			//激发“Activated”事件
			this.OnActivated(new(_service, current));

			return current;
		}

		return null;
	}
	#endregion

	#region 事件激发
	protected virtual void OnCreated(ChatSessionEventArgs args) => this.Created?.Invoke(this, args);
	protected virtual void OnActivated(ChatSessionEventArgs args) => this.Activated?.Invoke(this, args);
	protected virtual void OnAbandoned(ChatSessionEventArgs args) => this.Abandoned?.Invoke(this, args);
	#endregion

	#region 枚举遍历
	IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
	public IEnumerator<IChatSession> GetEnumerator()
	{
		#if NET9_0_OR_GREATER
		foreach(var key in _cache.Keys)
		{
			if(_cache.TryGetValue(key, out var value) && value is IChatSession session)
				yield return session;
		}
		#else
		throw new NotSupportedException();
		#endif
	}
	#endregion
}
