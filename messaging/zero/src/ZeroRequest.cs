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

internal sealed partial class ZeroRequest : IRequest
{
	public ZeroRequest(string url, ReadOnlyMemory<byte> data, string identifier = null)
	{
		this.Url = url;
		this.Data = data;
		this.Identifier = string.IsNullOrEmpty(identifier) ? Randomizer.GenerateString(12) : identifier;
	}

	public string Url { get; }
	public string Identifier { get; }
	public ReadOnlyMemory<byte> Data { get; }

	public ZeroResponse Response(ReadOnlyMemory<byte> data) => new(this, data);
	public ZeroResponse Response(string url, ReadOnlyMemory<byte> data) => new(this, url, data);
	IResponse IRequest.Response(string url, ReadOnlyMemory<byte> data) => this.Response(url, data);

	public override string ToString() => $"[{this.Identifier}] {this.Url}";
}

partial class ZeroRequest
{
	public ReadOnlyMemory<byte> Pack()
	{
		var length = Encoding.UTF8.GetByteCount(this.Identifier);
		var buffer = new byte[length + 1 + this.Data.Length];
		buffer[length] = (byte)'\n';
		Encoding.UTF8.GetBytes(this.Identifier.AsSpan(), buffer);
		this.Data.CopyTo(buffer.AsMemory(length + 1));
		return buffer.AsMemory();
	}

	public static ZeroRequest Unpack(string url, ReadOnlyMemory<byte> data)
	{
		var length = data.Span.IndexOf((byte)'\n');
		if(length < 0)
			return null;

		var identifier = Encoding.UTF8.GetString(data[..length].Span);
		return new ZeroRequest(url, data[(length + 1)..], identifier);
	}
}