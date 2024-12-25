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

namespace Zongsoft.Communication;

/// <summary>
/// 表示请求对象的接口。
/// </summary>
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
