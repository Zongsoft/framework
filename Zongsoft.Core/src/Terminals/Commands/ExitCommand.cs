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
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;

using Zongsoft.Components;

namespace Zongsoft.Terminals.Commands;

[DisplayName("ExitCommand.Name")]
[Description("ExitCommand.Description")]
[CommandOption(YES_OPTION, 'y', typeof(bool), false, Description = "ExitCommand.Options.Confirm")]
public class ExitCommand : CommandBase<CommandContext>
{
	#region 常量定义
	private const string YES_OPTION = "yes";
	#endregion

	#region 构造函数
	public ExitCommand() : base("Exit") { }
	public ExitCommand(string name) : base(name) { }
	#endregion

	#region 重写方法
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		var terminal = context.GetTerminal() ??
			throw new NotSupportedException($"The `{this.Name}` command is only supported running in a terminal executor.");

		if(context.Options.Switch(YES_OPTION))
			throw new Terminal.ExitException();

		terminal.Write(Properties.Resources.ExitCommand_Confirm);

		if(string.Equals(terminal.Input.ReadLine().Trim(), "yes", StringComparison.OrdinalIgnoreCase))
			throw new Terminal.ExitException();

		return ValueTask.FromResult<object>(null);
	}
	#endregion
}
