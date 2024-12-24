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
using System.Text;

using Zongsoft.Common;
using Zongsoft.Components;
using Zongsoft.Communication;

namespace Zongsoft.Messaging.ZeroMQ;

internal sealed partial class ZeroResponse : IResponse
{
	public ZeroResponse(ZeroRequest request, ReadOnlyMemory<byte> data) : this(request, null, data) { }
	public ZeroResponse(ZeroRequest request, string url, ReadOnlyMemory<byte> data)
	{
		this.Request = request ?? throw new ArgumentNullException(nameof(request));
		this.Url = string.IsNullOrWhiteSpace(url) ? $"{request.Url}/reply" : url;
		this.Data = data;
	}

	public string Url { get; }
	public ReadOnlyMemory<byte> Data { get; }
	public ZeroRequest Request { get; }
	IRequest IResponse.Request => this.Request;

	public override string ToString() => $"[{this.Request.Identifier}] {this.Url}";
}

partial class ZeroResponse
{
	public static ReadOnlyMemory<byte> Pack(IResponse response)
	{
		if(response == null)
			throw new ArgumentNullException(nameof(response));

		if(response is ZeroResponse resp)
			return Pack(resp.Request.Identifier, resp.Data);
		else
			throw new ArgumentException($"The '{response.GetType().FullName}' type of response is not supported.");
	}

	public static ReadOnlyMemory<byte> Pack(string identifier, ReadOnlyMemory<byte> data)
	{
		var length = Encoding.UTF8.GetByteCount(identifier);
		var buffer = new byte[length + 1 + data.Length];
		buffer[length] = (byte)'\n';
		Encoding.UTF8.GetBytes(identifier, buffer);
		data.CopyTo(buffer.AsMemory(length + 1));
		return buffer.AsMemory();
	}

	public static (string identifier, ReadOnlyMemory<byte> data) Unpack(ReadOnlyMemory<byte> data)
	{
		var length = data.Span.IndexOf((byte)'\n');
		if(length < 0)
			return default;

		return (Encoding.UTF8.GetString(data[..length].Span), data[(length + 1)..]);
	}
}