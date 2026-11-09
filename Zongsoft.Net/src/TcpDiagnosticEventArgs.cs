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
using System.Diagnostics;

namespace Zongsoft.Net;

/// <summary>TCP 基础设施故障的不可变通知；由订阅者决定是否记录日志。</summary>
public sealed class TcpDiagnosticEventArgs : EventArgs
{
	internal TcpDiagnosticEventArgs(object source, string operation, Exception exception)
	{
		this.Source = source;
		this.Operation = operation;
		this.Exception = exception;
		this.Timestamp = DateTimeOffset.UtcNow;
		this.ActivityContext = Activity.Current?.Context ?? default;
	}

	/// <summary>获取故障所属客户端、服务端或通知所有者；共享监测故障为空。</summary>
	public object Source { get; }
	/// <summary>获取故障阶段：connect、accept、initialize、monitor 或 notification。</summary>
	public string Operation { get; }
	/// <summary>获取原始异常。</summary>
	public Exception Exception { get; }
	/// <summary>获取故障通知的 UTC 时间。</summary>
	public DateTimeOffset Timestamp { get; }
	/// <summary>获取通知发生时的跟踪上下文，供异步订阅者保留关联。</summary>
	public ActivityContext ActivityContext { get; }
}
