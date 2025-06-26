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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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
using System.Buffers;

namespace Zongsoft.Communication;

/// <summary>
/// 提供通讯协议包有关打包与拆包功能的接口。
/// </summary>
/// <typeparam name="TPackage">通讯协议包的类型。</typeparam>
public interface IPacketizer<TPackage>
{
	/// <summary>打包，将通讯包对象序列化到发送缓存。</summary>
	/// <param name="writer">缓存写入器。</param>
	/// <param name="package">待打包的通讯包。</param>
	void Pack(IBufferWriter<byte> writer, in TPackage package);

	/// <summary>拆包，将字节流反序列化成通讯包对象。</summary>
	/// <param name="data">待拆包的字节流。</param>
	/// <param name="package">拆包成功的通讯包。</param>
	/// <returns>如果拆包完成则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	bool Unpack(ref ReadOnlySequence<byte> data, out TPackage package);
}

/// <summary>
/// 提供请求/应答模式通讯协议包有关打包与拆包功能的接口。
/// </summary>
/// <typeparam name="TRequest">请求包的类型。</typeparam>
/// <typeparam name="TResponse">响应包的类型。</typeparam>
public interface IPacketizer<in TRequest, TResponse>
{
	/// <summary>打包，将请求包对象序列化到发送缓存。</summary>
	/// <param name="writer">缓存写入器。</param>
	/// <param name="request">待打包的请求包。</param>
	void Pack(IBufferWriter<byte> writer, TRequest request);

	/// <summary>拆包，将字节流反序列化成响应包对象。</summary>
	/// <param name="data">待拆包的字节流。</param>
	/// <param name="response">拆包成功的响应包。</param>
	/// <returns>如果拆包完成则返回真(<c>True</c>)，否则返回假(<c>False</c>)。</returns>
	bool Unpack(ref ReadOnlySequence<byte> data, out TResponse response);
}
