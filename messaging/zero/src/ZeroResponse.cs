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
			throw new ArgumentException(string.Format(Properties.Resources.ZeroResponse_TypeUnsupported_Message, response.GetType().FullName));
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
