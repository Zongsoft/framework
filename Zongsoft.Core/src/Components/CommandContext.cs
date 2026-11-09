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

/// <summary>表示命令执行的上下文类。</summary>
public class CommandContext : CommandContextBase
{
	#region 成员字段
	private readonly CommandDescriptor _descriptor;
	#endregion

	#region 构造函数
	public CommandContext(ICommandExecutor executor, CommandLine.Cmdlet cmdlet, ICommand command, object value) : base(executor, cmdlet, value)
	{
		this.Command = command ?? throw new ArgumentNullException(nameof(command));

		if(this.Command != null)
			_descriptor = CommandDescriptor.Describe(this.Command.GetType());
	}

	public CommandContext(ICommandExecutor executor, CommandLine.Cmdlet cmdlet, CommandNode node, object value) : base(executor, cmdlet, value)
	{
		if(node == null)
			throw new ArgumentNullException(nameof(node));

		this.CommandNode = node;
		this.Command = node.Command;

		if(this.Command != null)
			_descriptor = CommandDescriptor.Describe(this.Command.GetType());
	}

	internal protected CommandContext(ICommandContext context, CommandLine.Cmdlet cmdlet, ICommand command, object value) : base(context, cmdlet, value)
	{
		this.Command = command;
		this.CommandNode = null;

		if(this.Command != null)
			_descriptor = CommandDescriptor.Describe(this.Command.GetType());
	}

	internal protected CommandContext(ICommandContext context, CommandLine.Cmdlet cmdlet, CommandNode node, object value) : base(context, cmdlet, value)
	{
		if(node == null)
			throw new ArgumentNullException(nameof(node));

		this.CommandNode = node;
		this.Command = node.Command;

		if(this.Command != null)
			_descriptor = CommandDescriptor.Describe(this.Command.GetType());
	}
	#endregion

	#region 公共属性
	/// <summary>获取执行的命令对象。</summary>
	public ICommand Command { get; }

	/// <summary>获取执行的命令所在节点。</summary>
	public CommandNode CommandNode { get; }

	/// <inheritdoc />
	public override CommandDescriptor Descriptor => _descriptor;
	#endregion
}
