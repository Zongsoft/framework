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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Core library.
 *
 * The Zongsoft.Core is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Core is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Core library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

namespace Zongsoft.Components;

/// <summary>
/// 表示命令执行的上下文类。
/// </summary>
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

		if(node.Command == null)
			throw new ArgumentException($"The Command property of '{node.FullPath}' command-node is null.");

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

		if(node.Command == null)
			throw new ArgumentException($"The Command property of '{node.FullPath}' command-node is null.");

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
