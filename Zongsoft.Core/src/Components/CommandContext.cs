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
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
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
	#region 构造函数
	public CommandContext(ICommandExecutor executor, CommandExpression expression, ICommand command, object value) : base(executor, expression, value)
	{
		this.Command = command ?? throw new ArgumentNullException(nameof(command));
	}

	public CommandContext(ICommandExecutor executor, CommandExpression expression, CommandNode commandNode, object value) : base(executor, expression, value)
	{
		if(commandNode == null)
			throw new ArgumentNullException(nameof(commandNode));

		if(commandNode.Command == null)
			throw new ArgumentException($"The Command property of '{commandNode.FullPath}' command-node is null.");

		this.CommandNode = commandNode;
		this.Command = commandNode.Command;
	}

	internal protected CommandContext(CommandContext context) : base(context) { }

	internal protected CommandContext(CommandContextBase context, CommandExpression expression, ICommand command, object value) : base(context, expression, value)
	{
		this.Command = command;
		this.CommandNode = null;
	}

	internal protected CommandContext(CommandContextBase context, CommandExpression expression, CommandNode commandNode, object value) : base(context, expression, value)
	{
		if(commandNode == null)
			throw new ArgumentNullException(nameof(commandNode));

		if(commandNode.Command == null)
			throw new ArgumentException($"The Command property of '{commandNode.FullPath}' command-node is null.");

		this.CommandNode = commandNode;
		this.Command = commandNode.Command;
	}
	#endregion

	#region 公共属性
	/// <summary>获取执行的命令对象。</summary>
	public ICommand Command { get; }

	/// <summary>获取执行的命令所在节点。</summary>
	public CommandNode CommandNode { get; }
	#endregion
}
