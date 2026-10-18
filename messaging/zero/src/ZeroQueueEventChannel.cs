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
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Messaging.ZeroMQ library.
 *
 * The Zongsoft.Messaging.ZeroMQ is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Messaging.ZeroMQ is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Messaging.ZeroMQ library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Components;

namespace Zongsoft.Messaging.ZeroMQ;

/// <summary>通过消息队列传输事件，并将收到的事件交给 <see cref="IEventReceiver"/>。</summary>
/// <remarks>拥有事件订阅，借用 <see cref="Queue"/>；关闭后可重新打开，释放后不可重用。</remarks>
[System.Reflection.DefaultMember(nameof(Filtering))]
[System.ComponentModel.DefaultProperty(nameof(Filtering))]
public class ZeroQueueEventChannel : IEventChannel
{
	#region 成员字段
	private IMessageQueue _queue;
	private IMessageConsumer _subscriber;
	private volatile bool _disposed;
	private readonly SemaphoreSlim _lifecycle = new(1, 1);
	#endregion

	#region 构造函数
	/// <summary>创建由插件或调用方设置消息队列的事件通道。</summary>
	/// <param name="options">消息入队选项；<see langword="null"/> 时启用阈值为 4096 字节的 Brotli 压缩。</param>
	public ZeroQueueEventChannel(MessageEnqueueOptions options = null) : this(null, options) { }
	/// <summary>创建借用指定消息队列的事件通道。</summary>
	/// <param name="queue">用于传输事件的消息队列；可在打开前通过 <see cref="Queue"/> 设置。</param>
	/// <param name="options">消息入队选项；<see langword="null"/> 时使用默认压缩设置。</param>
	public ZeroQueueEventChannel(IMessageQueue queue, MessageEnqueueOptions options = null)
	{
		_queue = queue;

		if(options == null)
		{
			options = new MessageEnqueueOptions() { Compression = new MessageCompression(IO.Compression.Compressor.Brotli, 4096) };
		}

		this.Options = options;
		this.Filtering = new();
	}
	#endregion

	#region 公共属性
	/// <summary>获取或设置用于传输事件的消息队列。</summary>
	/// <remarks>通道借用此队列，不负责释放；应在打开订阅前完成设置。</remarks>
	[System.ComponentModel.TypeConverter(typeof(MessageQueueConverter))]
	public IMessageQueue Queue
	{
		get => _queue;
		set => _queue = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>获取决定事件是否向远端发送的筛选条件。</summary>
	public EventFiltering Filtering { get; }
	/// <summary>获取或设置事件消息的入队选项。</summary>
	public MessageEnqueueOptions Options { get; set; }
	#endregion

	#region 公共方法
	/// <summary>建立事件主题订阅；已打开时保持现有订阅。</summary>
	/// <param name="receiver">处理远端事件的接收器。</param>
	/// <param name="cancellation">订阅操作的取消令牌。</param>
	/// <returns>订阅建立完成的任务。</returns>
	public async ValueTask OpenAsync(IEventReceiver receiver, CancellationToken cancellation = default)
	{
		ArgumentNullException.ThrowIfNull(receiver);

		await _lifecycle.WaitAsync(cancellation).ConfigureAwait(false);

		try
		{
			ObjectDisposedException.ThrowIf(_disposed, this);

			if(_subscriber != null)
				return;
			if(_queue == null)
				throw new InvalidOperationException(Properties.Resources.ZeroQueueEventChannel_QueueRequired_Message);

			_subscriber = await _queue.SubscribeAsync(Events.GetTopic(), Events.GetHandler(receiver), cancellation).ConfigureAwait(false);
		}
		finally
		{
			_lifecycle.Release();
		}
	}

	/// <summary>筛选并序列化事件，将符合条件的事件发送到消息队列。</summary>
	/// <param name="context">待发送的事件上下文。</param>
	/// <param name="cancellation">筛选及发送操作的取消令牌。</param>
	/// <returns>消息入队或筛选排除完成的任务。</returns>
	public async ValueTask SendAsync(EventContext context, CancellationToken cancellation = default)
	{
		ObjectDisposedException.ThrowIf(_disposed, this);

		if(_queue == null)
			throw new InvalidOperationException(Properties.Resources.ZeroQueueEventChannel_QueueRequired_Message);

		if(await this.Filtering.PredicateAsync(context, cancellation))
			await _queue.ProduceAsync(Events.GetTopic(context.QualifiedName), Events.Marshaler.Marshal(context), this.Options, cancellation);
	}
	#endregion

	#region 生命周期
	/// <summary>关闭并释放当前事件订阅，保留再次打开通道的能力。</summary>
	/// <param name="cancellation">取消进入关闭流程的等待；订阅清理开始后不受取消影响。</param>
	/// <returns>当前订阅释放完成的任务。</returns>
	public async ValueTask CloseAsync(CancellationToken cancellation = default)
	{
		await _lifecycle.WaitAsync(cancellation).ConfigureAwait(false);

		try
		{
			await this.CloseCoreAsync().ConfigureAwait(false);
		}
		finally
		{
			_lifecycle.Release();
		}
	}

	/// <summary>释放事件订阅，并永久关闭通道。</summary>
	/// <returns>通道拥有的订阅释放完成的任务。</returns>
	public async ValueTask DisposeAsync()
	{
		await _lifecycle.WaitAsync().ConfigureAwait(false);

		try
		{
			if(_disposed)
				return;

			await this.CloseCoreAsync().ConfigureAwait(false);
			_disposed = true;
		}
		finally { _lifecycle.Release(); }
	}

	/// <summary>在已取得生命周期互斥锁时释放当前订阅。</summary>
	/// <returns>订阅释放并清除引用的任务。</returns>
	private async ValueTask CloseCoreAsync()
	{
		if(_subscriber == null)
			return;

		await _subscriber.DisposeAsync().ConfigureAwait(false);
		_subscriber = null;
	}
	#endregion

	#region 重写方法
	/// <summary>获取包含通道类型与消息队列的描述。</summary>
	/// <returns>当前事件通道的文本表示。</returns>
	public override string ToString() => _queue == null ? $"{nameof(ZeroQueueEventChannel)}" : $"{nameof(ZeroQueueEventChannel)}:{_queue}";
	#endregion
}
