/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2023 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Components;
using Zongsoft.Collections;

namespace Zongsoft.Messaging
{
	public abstract class MessageConsumerBase : IMessageConsumer
	{
		#region 成员字段
		private bool _subscribed;
		private string[] _topics;
		private string[] _tags;
		private IHandler<Message> _handler;
		private MessageSubscribeOptions _options;
		#endregion

		#region 构造函数
		protected MessageConsumerBase(IHandler<Message> handler, MessageSubscribeOptions options = null)
		{
			_handler = handler;
			_options = options;
		}

		protected MessageConsumerBase(string topics, string tags, IHandler<Message> handler = null) : this(topics, tags, null, handler) { }
		protected MessageConsumerBase(string topics, string tags, MessageSubscribeOptions options, IHandler<Message> handler = null)
		{
			_topics = Slice(topics);
			_tags = Slice(tags);
			_options = options;
			_handler = handler;
		}

		protected MessageConsumerBase(IEnumerable<string> topics, string tags, IHandler<Message> handler = null) : this(topics, tags, null, handler) { }
		protected MessageConsumerBase(IEnumerable<string> topics, string tags, MessageSubscribeOptions options, IHandler<Message> handler = null)
		{
			_topics = topics == null ? Array.Empty<string>() : topics.ToArray();
			_tags = tags.Split(new[] { ',', ';' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
			_options = options;
			_handler = handler;
		}
		#endregion

		#region 公共属性
		public string[] Topics
		{
			get => _topics;
			protected set
			{
				if(_subscribed)
					throw new InvalidOperationException();

				_topics = value;
			}
		}

		public string[] Tags
		{
			get => _tags;
			protected set
			{
				if(_subscribed)
					throw new InvalidOperationException();

				_tags = value;
			}
		}

		public IHandler<Message> Handler
		{
			get => _handler;
			protected set
			{
				if(_subscribed)
					throw new InvalidOperationException();

				_handler = value;
			}
		}

		public MessageSubscribeOptions Options
		{
			get => _options;
			protected set
			{
				if(_subscribed)
					throw new InvalidOperationException();

				_options = value;
			}
		}

		public bool IsSubscribed { get => _subscribed; }
		#endregion

		#region 订阅方法
		protected ValueTask SubscribeAsync(string topics, CancellationToken cancellation = default) => this.SubscribeAsync(Slice(topics), null, null, cancellation);
		protected ValueTask SubscribeAsync(string topics, string tags, CancellationToken cancellation = default) => this.SubscribeAsync(Slice(topics), tags, null, cancellation);
		protected ValueTask SubscribeAsync(string topics, MessageSubscribeOptions options, CancellationToken cancellation = default) => this.SubscribeAsync(Slice(topics), null, options, cancellation);
		protected ValueTask SubscribeAsync(string topics, string tags, MessageSubscribeOptions options, CancellationToken cancellation = default) => this.SubscribeAsync(Slice(topics), tags, options, cancellation);
		protected ValueTask SubscribeAsync(IEnumerable<string> topics, CancellationToken cancellation = default) => this.SubscribeAsync(topics, null, null, cancellation);
		protected ValueTask SubscribeAsync(IEnumerable<string> topics, string tags, CancellationToken cancellation = default) => this.SubscribeAsync(topics, tags, null, cancellation);
		protected ValueTask SubscribeAsync(IEnumerable<string> topics, MessageSubscribeOptions options, CancellationToken cancellation = default) => this.SubscribeAsync(topics, null, options, cancellation);
		protected async ValueTask SubscribeAsync(IEnumerable<string> topics, string tags, MessageSubscribeOptions options, CancellationToken cancellation = default)
		{
			if(topics == null || !topics.Any())
				topics = _topics;

			//尝试取消原有订阅
			await this.UnsubscribeAsync(topics, cancellation);

			//执行订阅操作
			await this.OnSubscribeAsync(topics, tags, options, cancellation);

			//更新当前的订阅主题
			_topics = topics == null ? Array.Empty<string>() : topics.ToArray();
			//更新当前的订阅设置
			_options = options;

			//更新订阅标记(已订阅)
			_subscribed = true;

			//通知订阅完成
			this.OnSubscribed();
		}

		public ValueTask UnsubscribeAsync(CancellationToken cancellation = default) => this.UnsubscribeAsync(this.Topics, cancellation);
		public ValueTask UnsubscribeAsync(string topics, CancellationToken cancellation = default) => this.UnsubscribeAsync(Slice(topics), cancellation);
		public ValueTask UnsubscribeAsync(IEnumerable<string> topics, CancellationToken cancellation = default)
		{
			if(!_subscribed)
				return ValueTask.CompletedTask;

			//取消所有订阅
			var task = this.OnUnsubscribeAsync(topics, cancellation);

			//更新订阅标记(未订阅)
			if(task.IsCompletedSuccessfully)
				this.OnUnsubscribed(topics);
			else
				task.AsTask().ContinueWith(t =>
				{
					if(t.IsCompletedSuccessfully)
						this.OnUnsubscribed(topics);
				}, cancellation);

			return task;
		}

		protected virtual void OnSubscribed() { }
		protected abstract ValueTask OnSubscribeAsync(IEnumerable<string> topics, string tags, MessageSubscribeOptions options, CancellationToken cancellation);

		protected virtual void OnUnsubscribed() { }
		protected abstract ValueTask OnUnsubscribeAsync(IEnumerable<string> topics, CancellationToken cancellation);

		private void OnUnsubscribed(IEnumerable<string> topics)
		{
			if(topics == null || !topics.Any() || object.ReferenceEquals(topics, _topics))
				_topics = null;
			else
				_topics = _topics?.Except(topics).ToArray();

			if(_topics == null || _topics.Length == 0)
			{
				_tags = null;
				_subscribed = false;

				//通知子类所有订阅已全部取消
				this.OnUnsubscribed();
			}
		}
		#endregion

		#region 私有方法
		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static string[] Slice(string text) => string.IsNullOrEmpty(text) ?
			Array.Empty<string>() :
			text.Split(new[] { ',', ';' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
		#endregion

		#region 资源释放
		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			var handler = Interlocked.Exchange(ref _handler, null);
			if(handler == null)
				return;

			if(disposing)
				this.UnsubscribeAsync().AsTask().Wait();
		}
		#endregion
	}
}
