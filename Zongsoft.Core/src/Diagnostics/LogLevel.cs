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

/// <summary>表示日志的级别。</summary>
public enum LogLevel : byte
{
	/// <summary>跟踪(0)</summary>
	Trace,

	/// <summary>调试(1)</summary>
	Debug,

	/// <summary>信息(2)</summary>
	Info,

	/// <summary>警告(3)</summary>
	Warn,

	/// <summary>错误(4)</summary>
	Error,

	/// <summary>崩溃(5)</summary>
	Fatal,
}
