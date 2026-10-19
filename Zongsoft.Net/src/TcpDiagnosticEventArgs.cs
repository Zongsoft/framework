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
