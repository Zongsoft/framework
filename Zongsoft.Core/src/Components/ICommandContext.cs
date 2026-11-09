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

namespace Zongsoft.Components;

/// <summary>表示命令执行的上下文的接口。</summary>
public interface ICommandContext
{
	/// <summary>获取当前命令执行器对象。</summary>
	ICommandExecutor Executor { get; }

	/// <summary>获取或设置传入的值。</summary>
	object Value { get; set; }

	/// <summary>获取或设置执行结果。</summary>
	object Result { get; set; }

	/// <summary>获取当前命令执行器的标准输出器。</summary>
	ICommandOutlet Output { get; }

	/// <summary>获取当前命令执行器的错误输出器。</summary>
	TextWriter Error { get; }

	/// <summary>获取当前命令会话的共享参数集。</summary>
	Collections.Parameters Parameters { get; }
}
