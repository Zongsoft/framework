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

namespace Zongsoft.Components;

/// <summary>表示命令会话执行完成的上下文类。</summary>
public class CommandCompletionContext : CommandExecutorContext
{
	#region 构造函数
	public CommandCompletionContext(CommandExecutorContext context, ICommandCompletion command, object result, Exception exception = null) : base(context, result)
	{
		this.Command = command;
		this.Exception = exception;
	}
	#endregion

	#region 公共属性
	/// <summary>获取当前的回调命令。</summary>
	public ICommandCompletion Command { get; }

	/// <summary>获取命令执行中发生的异常。</summary>
	public Exception Exception { get; internal set; }
	#endregion
}
