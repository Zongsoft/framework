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
using System.Buffers;

namespace Zongsoft.Communication;

/// <summary>提供通讯协议包有关打包与拆包功能的接口。</summary>
/// <typeparam name="TPackage">通讯协议包的类型。</typeparam>
public interface IPacketizer<TPackage>
{
	/// <summary>编码完整帧，不负责刷新缓存。</summary>
	/// <param name="writer">缓存写入器。</param>
	/// <param name="package">待打包的通讯包。</param>
	void Pack(IBufferWriter<byte> writer, in TPackage package);

	/// <summary>拆包，将字节流反序列化成通讯包对象。</summary>
	/// <param name="data">待拆包的字节流。</param>
	/// <param name="package">拆包成功的通讯包。</param>
	/// <returns>完整帧返回 <c>true</c> 且消费至少一个字节；数据不足返回 <c>false</c> 且不消费输入；非法数据抛出异常。</returns>
	bool Unpack(ref ReadOnlySequence<byte> data, out TPackage package);
}

/// <summary>提供请求/应答模式通讯协议包有关打包与拆包功能的接口。</summary>
/// <typeparam name="TRequest">请求包的类型。</typeparam>
/// <typeparam name="TResponse">响应包的类型。</typeparam>
public interface IPacketizer<in TRequest, TResponse>
{
	/// <summary>编码完整请求帧，不负责刷新缓存。</summary>
	/// <param name="writer">缓存写入器。</param>
	/// <param name="request">待打包的请求包。</param>
	void Pack(IBufferWriter<byte> writer, TRequest request);

	/// <summary>拆包，将字节流反序列化成响应包对象。</summary>
	/// <param name="data">待拆包的字节流。</param>
	/// <param name="response">拆包成功的响应包。</param>
	/// <returns>完整帧返回 <c>true</c> 且消费至少一个字节；数据不足返回 <c>false</c> 且不消费输入；非法数据抛出异常。</returns>
	bool Unpack(ref ReadOnlySequence<byte> data, out TResponse response);
}
