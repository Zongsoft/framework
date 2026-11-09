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
	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		var terminal = context.GetTerminal() ??
			throw new NotSupportedException(string.Format(Properties.Resources.Terminal_CommandRequiresTerminal_Message, this.Name));

		if(context.Options.Switch(YES_OPTION))
			throw new Terminal.ExitException();

		terminal.Write(Properties.Resources.ExitCommand_Confirm);

		var answer = await terminal.Input.ReadLineAsync(cancellation).ConfigureAwait(false);

		if(string.Equals(answer?.Trim(), "yes", StringComparison.OrdinalIgnoreCase))
			throw new Terminal.ExitException();

		return null;
	}
	#endregion
}
