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
using System.IO;

namespace Zongsoft.Net;

/// <summary>表示由 TCP 通讯库识别的报文、连接状态或容量错误。</summary>
/// <remarks>通过 <see cref="ErrorCode" /> 区分原因；不统一包装取消、超时、Socket 或业务异常。服务停止汇总的受理故障通过 <see cref="Exception.InnerException" /> 保留。</remarks>
public class TcpException : IOException
{
	/// <summary>初始化指定错误码的 TCP 异常。</summary>
	/// <param name="errorCode">通讯错误码。</param>
	public TcpException(TcpError errorCode) : this(errorCode, null, null) { }

	/// <summary>初始化指定错误码和消息的 TCP 异常。</summary>
	/// <param name="errorCode">通讯错误码。</param>
	/// <param name="message">错误消息；为 <see langword="null" /> 时使用错误码对应的本地化消息。</param>
	public TcpException(TcpError errorCode, string message) : this(errorCode, message, null) { }

	/// <summary>初始化指定错误码、消息和内部异常的 TCP 异常。</summary>
	/// <param name="errorCode">通讯错误码。</param>
	/// <param name="message">错误消息；为 <see langword="null" /> 时使用错误码对应的本地化消息。</param>
	/// <param name="innerException">导致当前错误的异常，允许 <see langword="null" />。</param>
	public TcpException(TcpError errorCode, string message, Exception innerException) : base(message ?? errorCode switch
	{
		TcpError.SendRejected => Properties.Resources.Network_SendCapacity_Message,
		TcpError.InvalidFrame => Properties.Resources.Network_FrameInvalid_Message,
		TcpError.IncompleteFrame => Properties.Resources.Network_FrameIncomplete_Message,
		TcpError.ConnectionClosed => Properties.Resources.Network_ConnectionClosed_Message,
		_ => throw new ArgumentOutOfRangeException(nameof(errorCode)),
	}, innerException)
	{
		if(!Enum.IsDefined(errorCode))
			throw new ArgumentOutOfRangeException(nameof(errorCode));

		this.ErrorCode = errorCode;
	}

	/// <summary>获取可供调用方判断及筛选的通讯错误码。</summary>
	public TcpError ErrorCode { get; }
}
