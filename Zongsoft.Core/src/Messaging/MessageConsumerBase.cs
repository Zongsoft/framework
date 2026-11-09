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

using Zongsoft.Components;
using Zongsoft.Communication;

namespace Zongsoft.Messaging;

public abstract class MessageConsumerBase<TQueue> : ChannelBase, IMessageConsumer where TQueue : IMessageQueue
{
	#region 成员字段
	private IHandler<Message> _handler;
	#endregion

	#region 构造函数
	protected MessageConsumerBase(TQueue queue, IHandler<Message> handler, MessageSubscribeOptions options = null) : this(queue, null, [], handler, options) { }
	protected MessageConsumerBase(TQueue queue, string topic, IHandler<Message> handler, MessageSubscribeOptions options = null) : this(queue, topic, [], handler, options) { }
	protected MessageConsumerBase(TQueue queue, string topic, string tags, IHandler<Message> handler, MessageSubscribeOptions options = null) : this(queue, topic, Slice(tags), handler, options) { }
	protected MessageConsumerBase(TQueue queue, string topic, string[] tags, IHandler<Message> handler, MessageSubscribeOptions options = null)
	{
		this.Queue = queue ?? throw new ArgumentNullException(nameof(queue));
		this.Topic = topic;
		this.Tags = tags;
		this.Options = options;
		_handler = handler;
	}
	#endregion

	#region 公共属性
	public string Topic { get; }
	public string[] Tags { get; }
	public IHandler<Message> Handler => _handler;
	public MessageSubscribeOptions Options { get; }
	#endregion

	#region 保护属性
	protected TQueue Queue { get; }
	#endregion

	#region 取消订阅
	public ValueTask UnsubscribeAsync(CancellationToken cancellation = default) => this.CloseAsync(cancellation);
	#endregion

	#region 私有方法
	[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
	private static string[] Slice(string text) => string.IsNullOrEmpty(text) ? [] :
		text.Split([',', ';'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
	#endregion

	#region 资源释放
	protected override async ValueTask DisposeAsync(bool disposing)
	{
		Interlocked.Exchange(ref _handler, null);
		await base.DisposeAsync(disposing);
	}
	#endregion
}
