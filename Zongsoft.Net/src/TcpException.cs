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
