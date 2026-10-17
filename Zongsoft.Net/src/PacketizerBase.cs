/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Copyright (C) 2010-2026 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Net library.
 *
 * The Zongsoft.Net is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Net is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Net library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Buffers;

using Zongsoft.Communication;

namespace Zongsoft.Net;

/// <summary>提供 TCP 通讯报文的编解码、帧长度计算与解码资源释放能力。</summary>
/// <typeparam name="TPackage">通讯报文的类型。</typeparam>
/// <remarks>实例可由多个连接共享，必须支持并发调用；借用报文仅在当前接收处理完成前有效。</remarks>
public abstract class PacketizerBase<TPackage> : IPacketizer<TPackage>
{
	/// <summary>获取精确的线协议字节数，包括包头；不改变报文状态或创建完整编码副本。</summary>
	/// <param name="package">在发送完成前保持稳定的报文。</param>
	/// <returns>非负的完整帧长度。</returns>
	public abstract long GetLength(in TPackage package);

	/// <summary>编码完整帧，写入量必须等于 <see cref="GetLength" /> 的返回值；不负责刷新缓存。</summary>
	/// <param name="writer">缓存写入器。</param>
	/// <param name="package">待打包的通讯报文。</param>
	public abstract void Pack(IBufferWriter<byte> writer, in TPackage package);

	/// <summary>将字节流解码成通讯报文。</summary>
	/// <param name="data">待拆包的字节流。</param>
	/// <param name="package">拆包成功的通讯报文。</param>
	/// <returns>完整帧返回 <c>true</c> 且消费至少一个字节；数据不足返回 <c>false</c> 且不消费输入；非法数据抛出异常。</returns>
	public abstract bool Unpack(ref ReadOnlySequence<byte> data, out TPackage package);

	/// <summary>释放成功解码产生的报文资源，默认不执行操作。</summary>
	/// <param name="package">本次接收处理已经结束的解码报文。</param>
	/// <remarks>通道在处理完成或失败后调用一次；发送不转移报文所有权，不调用本方法。</remarks>
	public virtual void Release(in TPackage package) { }
}
