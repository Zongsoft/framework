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
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

using NetMQ;
using NetMQ.Sockets;

using Zongsoft.Common;
using Zongsoft.Components;
using Zongsoft.Communication;
using Zongsoft.Collections;
using System.Collections.Concurrent;

namespace Zongsoft.Messaging.ZeroMQ;

public class ZeroRequester<TRequest, TResponse> : IRequester
{
	private ZeroQueue _queue;

	private readonly ConcurrentDictionary<string, IRequesterResult> _pending;

	public ValueTask<IRequesterResult> RequestAsync(IRequest request, CancellationToken cancellation = default)
	{
		if(request == null)
			throw new ArgumentNullException(nameof(request));

		_queue.ProduceAsync(request.Url, request.Data, null, cancellation);
		_queue.SubscribeAsync(request.Url + "/ack", Handler.Instance, cancellation);

		return ValueTask.FromResult<IRequesterResult>(null);
	}

	public ValueTask OnRespondedAsync(IResponse response, CancellationToken cancellation)
	{
		return ValueTask.CompletedTask;
	}

	private sealed class Handler : HandlerBase<Message>
	{
		public static readonly Handler Instance = new();

		protected override ValueTask OnHandleAsync(Message argument, Parameters parameters, CancellationToken cancellation) => throw new NotImplementedException();
	}
}
