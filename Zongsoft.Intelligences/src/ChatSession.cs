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

using Microsoft.Extensions.AI;

using Zongsoft.Common;
using Zongsoft.Services;
using Zongsoft.Configuration;

namespace Zongsoft.Intelligences;

internal class ChatSession : IChatSession, IEquatable<ChatSession>
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
		this.Options = options ?? new ChatSessionOptions(
			service.Settings.Driver.Name,
			service.Settings.GetValue(nameof(ChatSessionOptions.Expiration), TimeSpan.FromHours(12)));

		if(this.Options.Parameters != null && this.Options.Parameters.TryGetValue("history", out var value))
			this.History = GetHistory(value);

		this.History ??= GetHistory(service.Settings["history"]) ?? new ChatHistory.Memory();

		static IChatHistory GetHistory(object target)
		{
			if(target is IChatHistory history)
				return history;
			if(target is string text && !string.IsNullOrEmpty(text))
				return ApplicationContext.Current?.Services.Find<IChatHistory>(text);

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

	#region 公共方法
	public IAsyncEnumerable<string> ChatAsync(string content, CancellationToken cancellation = default) => this.ChatAsync(content, null, cancellation);
	public IAsyncEnumerable<string> ChatAsync(string content, ChatOptions options, CancellationToken cancellation = default)
	{
		if(string.IsNullOrWhiteSpace(content))
			return Zongsoft.Collections.Enumerable.EnumerateAsync<string>(null);

		//将对话内容加入到历史记录中
		this.History.Append(content);

		//返回文本包装器的异步流
		return new Response<string>(this.History, _service.GetStreamingResponseAsync(this.History, options, cancellation), message => message.Text);
	}
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

	#region 私有方法
	private static bool Populate(ref ChatMessage message, ChatResponseUpdate entry)
	{
		if(entry == null || entry.Contents.Count == 0)
			return false;

		message ??= new ChatMessage
		{
			Role = entry.Role ?? ChatRole.Assistant,
			AuthorName = entry.AuthorName,
			MessageId = entry.MessageId,
			RawRepresentation = entry.RawRepresentation,
			AdditionalProperties = entry.AdditionalProperties,
		};

		if(entry.Contents != null && entry.Contents.Count > 0)
		{
			foreach(var content in entry.Contents)
				message.Contents.Add(content);
		}

		if(entry.AdditionalProperties != null && entry.AdditionalProperties.Count > 0)
		{
			message.AdditionalProperties ??= new();

			foreach(var property in entry.AdditionalProperties)
				message.AdditionalProperties.Add(property.Key, property.Value);
		}

		return true;
	}
	#endregion

	#region 嵌套子类
	internal sealed class Response(IChatHistory history, IAsyncEnumerable<ChatResponseUpdate> response) : IAsyncEnumerable<ChatResponseUpdate>
	{
		private readonly IChatHistory _history = history;
		private readonly IAsyncEnumerable<ChatResponseUpdate> _response = response;
		public IAsyncEnumerator<ChatResponseUpdate> GetAsyncEnumerator(CancellationToken cancellation = default) => new AsyncEnumerator(_history, _response.GetAsyncEnumerator(cancellation));

		private sealed class AsyncEnumerator(IChatHistory history, IAsyncEnumerator<ChatResponseUpdate> response) : IAsyncEnumerator<ChatResponseUpdate>
		{
			private IChatHistory _history = history;
			private List<ChatResponseUpdate> _entries = new();
			private IAsyncEnumerator<ChatResponseUpdate> _response = response;

			public ChatResponseUpdate Current => _response.Current;
			public async ValueTask<bool> MoveNextAsync()
			{
				if(await _response.MoveNextAsync())
				{
					_entries.Add(_response.Current);
					return true;
				}

				return false;
			}

			public async ValueTask DisposeAsync()
			{
				await _response.DisposeAsync();

				if(_entries != null && _entries.Count > 0)
				{
					ChatMessage message = null;

					for(int i = 0; i < _entries.Count; i++)
						Populate(ref message, _entries[i]);

					if(message != null && message.Contents.Count > 0)
						_history.Append(message);
				}
			}
		}
	}

	internal sealed class Response<T>(IChatHistory history, IAsyncEnumerable<ChatResponseUpdate> response, Func<ChatResponseUpdate, T> converter) : IAsyncEnumerable<T>
	{
		private readonly IChatHistory _history = history;
		private readonly Func<ChatResponseUpdate, T> _converter = converter;
		private readonly IAsyncEnumerable<ChatResponseUpdate> _response = response;
		public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellation = default) => new AsyncEnumerator(_history, _response.GetAsyncEnumerator(cancellation), _converter);

		private sealed class AsyncEnumerator(IChatHistory history, IAsyncEnumerator<ChatResponseUpdate> response, Func<ChatResponseUpdate, T> converter) : IAsyncEnumerator<T>
		{
			private IChatHistory _history = history;
			private Func<ChatResponseUpdate, T> _converter = converter;
			private List<ChatResponseUpdate> _entries = new();
			private IAsyncEnumerator<ChatResponseUpdate> _response = response;

			public T Current => _converter(_response.Current);
			public async ValueTask<bool> MoveNextAsync()
			{
				if(await _response.MoveNextAsync())
				{
					_entries.Add(_response.Current);
					return true;
				}

				return false;
			}

			public async ValueTask DisposeAsync()
			{
				await _response.DisposeAsync();

				if(_entries != null && _entries.Count > 0)
				{
					ChatMessage message = null;

					for(int i = 0; i < _entries.Count; i++)
						Populate(ref message, _entries[i]);

					if(message != null && message.Contents.Count > 0)
						_history.Append(message);
				}
			}
		}
	}
	#endregion
}
