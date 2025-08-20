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
using System.Collections;
using System.Collections.Generic;

using Zongsoft.Common;
using Zongsoft.Caching;
using Zongsoft.Services;

namespace Zongsoft.Intelligences;

public class ChatSessionManager(IChatService service) : IChatSessionManager
{
	#region 成员字段
	private readonly MemoryCache _cache = new();
	private readonly IChatService _service = service;
	#endregion

	#region 公共属性
	public IChatSession Current { get; set; }
	#endregion

	#region 公共方法
	public IChatSession Create(ChatSessionOptions options = null)
	{
		var session = new ChatSession(_service, options);
		_cache.SetValue(session.Identifier, session, session.Options.Expiration);
		this.Current ??= session;
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

		return _cache.Remove(identifier, out var value) ? value as IChatSession : null;
	}

	public IChatSession Activate(string identifier)
	{
		if(string.IsNullOrEmpty(identifier))
			return this.Current;

		if(_cache.TryGetValue(identifier, out var value))
			return this.Current = value as IChatSession;

		return null;
	}
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

	#region 嵌套子类
	private class ChatSession : IChatSession, IEquatable<ChatSession>
	{
		#region 成员字段
		private IChatService _service;
		#endregion

		#region 构造函数
		public ChatSession(IChatService service, ChatSessionOptions options = null)
		{
			_service = service ?? throw new ArgumentNullException(nameof(service));
			this.Identifier = Randomizer.GenerateString(10);
			this.Creation = DateTimeOffset.UtcNow;
			this.Options = options ?? new ChatSessionOptions(service.Settings.Driver.Name, TimeSpan.FromHours(24));

			if(this.Options.Parameters.TryGetValue("history", out var value))
				this.History = GetHistory(value);

			this.History ??= GetHistory(service.Settings["history"]) ?? new ChatHistory.Memory();

			static IChatHistory GetHistory(object target)
			{
				if(target is IChatHistory history)
					return history;
				if(target is string text)
					return ApplicationContext.Current?.Services.Resolve<IChatHistory>(text);

				return null;
			}
		}
		#endregion

		#region 公共属性
		public string Identifier { get; }
		public DateTimeOffset Creation { get; }
		public IChatHistory History { get; }
		public ChatSessionOptions Options { get; }
		#endregion

		#region 重写方法
		public override string ToString() => $"#{this.Identifier}({this.Creation.ToLocalTime():yyyy-MM-dd HH:mm:ss})";
		public bool Equals(ChatSession other) => other is not null && string.Equals(this.Identifier, other.Identifier);
		public override bool Equals(object obj) => base.Equals(obj);
		public override int GetHashCode() => this.Identifier.GetHashCode();
		#endregion

		#region 释放处置
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			var service = Interlocked.Exchange(ref _service, null);
			if(service != null)
				_service.Dispose();

			this.History?.Clear();
		}

		public async ValueTask DisposeAsync()
		{
			await this.DisposeAsync(true);
			GC.SuppressFinalize(this);
		}

		protected virtual ValueTask DisposeAsync(bool disposing)
		{
			var service = Interlocked.Exchange(ref _service, null);
			if(service != null)
				_service.Dispose();

			this.History?.Clear();
			return ValueTask.CompletedTask;
		}
		#endregion
	}
	#endregion
}
