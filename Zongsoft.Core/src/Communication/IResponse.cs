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

/// <summary>表示请求响应的接口。</summary>
public interface IResponse
{
	/// <summary>获取响应的地址。</summary>
	string Url { get; }

	/// <summary>获取响应关联的请求对象。</summary>
	IRequest Request { get; }

	/// <summary>获取响应的数据。</summary>
	ReadOnlyMemory<byte> Data { get; }
}
