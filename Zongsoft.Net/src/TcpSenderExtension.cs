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
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Communication;

namespace Zongsoft.Net;

/// <summary>提供二进制发送的便捷入口，始终通过发送者的分帧契约编码。</summary>
public static class TcpSenderExtension
{
	/// <summary>发送一段借用内存；调用完成前应保持内存有效且内容不变。</summary>
	/// <param name="sender">二进制通道、客户端或广播发送者。</param>
	/// <param name="data">待发送的报文内容。</param>
	/// <param name="cancellation">取消令牌。</param>
	/// <returns>本地提交操作。</returns>
	public static ValueTask SendAsync(this ISender<ReadOnlySequence<byte>> sender, ReadOnlyMemory<byte> data, CancellationToken cancellation = default)
	{
		ArgumentNullException.ThrowIfNull(sender);
		return sender.SendAsync(new ReadOnlySequence<byte>(data), cancellation);
	}
}
