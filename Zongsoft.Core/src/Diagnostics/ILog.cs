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

namespace Zongsoft.Diagnostics;

/// <summary>表示日志记录的实体类。</summary>
public interface ILog
{
	/// <summary>获取日志级别。</summary>
	LogLevel Level { get; }
	/// <summary>获取日志来源。</summary>
	string Source { get; }
	/// <summary>获取日志消息。</summary>
	string Message { get; }
	/// <summary>获取日志时间。</summary>
	DateTime Timestamp { get; }
	/// <summary>获取异常对象。</summary>
	Exception Exception { get; }
}
