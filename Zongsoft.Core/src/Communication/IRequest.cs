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

namespace Zongsoft.Communication;

/// <summary>表示请求对象的接口。</summary>
public interface IRequest
{
	/// <summary>获取请求地址。</summary>
	string Url { get; }
	/// <summary>获取请求的唯一标识。</summary>
	string Identifier { get; }
	/// <summary>获取请求的数据。</summary>
	ReadOnlyMemory<byte> Data { get; }

	/// <summary>创建该请求关联的响应对象。</summary>
	/// <param name="data">指定的响应数据。</param>
	/// <returns>返回关联的响应对象。</returns>
	IResponse Response(ReadOnlyMemory<byte> data) => this.Response(null, data);

	/// <summary>创建该请求关联的响应对象。</summary>
	/// <param name="url">指定的响应地址。</param>
	/// <param name="data">指定的响应数据。</param>
	/// <returns>返回关联的响应对象。</returns>
	IResponse Response(string url, ReadOnlyMemory<byte> data);
}
