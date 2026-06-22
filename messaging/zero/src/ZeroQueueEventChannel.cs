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
 * Copyright (C) 2010-2024 Zongsoft Studio <http://www.zongsoft.com>
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
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Components;

namespace Zongsoft.Messaging.ZeroMQ;

public class ZeroQueueEventChannel : IEventChannel
{
	public event EventHandler Closed;

	private ZeroQueue _queue;
	private IEventChannel _channel;

	[TypeConverter(typeof(MessageQueueConverter))]
	public IMessageQueue Queue
	{
		get => _queue;
		set
		{
			if(value is ZeroQueue queue)
			{
				_queue = queue;
				_channel = queue.Channel;

				if(queue.Channel != null)
					queue.Channel.Closed += this.Channel_Closed;
			}
		}
	}

	public bool IsClosed => this.Channel.IsClosed;
	public bool IsDisposed => this.Channel.IsDisposed;

	private IEventChannel Channel => _channel ?? throw new InvalidOperationException($"The {nameof(this.Queue)} property is not configured.");

	public ValueTask CloseAsync(CancellationToken cancellation = default) => this.Channel.CloseAsync(cancellation);
	public ValueTask DisposeAsync() => this.Channel.DisposeAsync();
	public ValueTask OpenAsync(EventExchanger exchanger, CancellationToken cancellation = default) => this.Channel.OpenAsync(exchanger, cancellation);
	public ValueTask SendAsync(EventContext data, CancellationToken cancellation = default) => this.Channel.SendAsync(data, cancellation);

	private void Channel_Closed(object sender, EventArgs e) => this.Closed?.Invoke(this, e);
}
