﻿/*
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
 * Copyright (C) 2020-2024 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Threading;
using System.Threading.Tasks;

namespace Zongsoft.Communication;

public interface IRequester
{
	ValueTask<IRequesterResult> RequestAsync(ReadOnlyMemory<byte> request, CancellationToken cancellation = default) => this.RequestAsync(null, request, cancellation);
	ValueTask<IRequesterResult> RequestAsync(string identifier, ReadOnlyMemory<byte> request, CancellationToken cancellation = default);
	ValueTask OnRespondedAsync(ReadOnlyMemory<byte> response, CancellationToken cancellation);
}

public interface IRequester<in TRequest, TResponse>
	where TRequest : IRequest
	where TResponse : IResponse
{
	IPacketizer<TRequest, TResponse> Packetizer { get; }

	ValueTask<IRequesterResult> RequestAsync(TRequest request, CancellationToken cancellation = default);
	ValueTask OnRespondedAsync(TResponse response, CancellationToken cancellation);
}
