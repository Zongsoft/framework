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
using System.Collections.Generic;

namespace Zongsoft.Components;

/// <summary>表示命令执行器的上下文（命令执行会话）类。</summary>
public class CommandExecutorContext : ICommandContext
{
	#region 构造函数
	public CommandExecutorContext(ICommandExecutor executor, IEnumerable<CommandLine.Cmdlet> cmdlets, object value)
	{
		this.Executor = executor ?? throw new ArgumentNullException(nameof(executor));
		this.Cmdlets = cmdlets ?? throw new ArgumentNullException(nameof(cmdlets));
		this.Value = value;
		this.Parameters = new();
	}

	internal CommandExecutorContext(CommandExecutorContext context, object result = null)
	{
		if(context == null)
			throw new ArgumentNullException(nameof(context));

		this.Executor = context.Executor;
		this.Cmdlets = context.Cmdlets;
		this.Value = context.Value;
		this.Result = result ?? context.Result;
		this.Parameters = context.Parameters;
	}
	#endregion

	#region 公共属性
	/// <summary>获取当前命令执行器对象。</summary>
	public ICommandExecutor Executor { get; }

	/// <summary>获取待执行的多个命令串集。</summary>
	public IEnumerable<CommandLine.Cmdlet> Cmdlets { get; }

	/// <summary>获取或设置从命令执行器传入的值。</summary>
	public object Value { get; set; }

	/// <summary>获取或设置命令执行器的最终结果。</summary>
	public object Result { get; set; }

	/// <summary>获取当前命令执行器的标准输出器。</summary>
	public ICommandOutlet Output => this.Executor.Output;

	/// <summary>获取当前命令执行器的错误输出器。</summary>
	public TextWriter Error => this.Executor.Error;

	/// <summary>获取当前命令会话的共享参数集。</summary>
	public Collections.Parameters Parameters { get; }
	#endregion
}
