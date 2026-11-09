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

namespace Zongsoft.Commands;

[DisplayName("EchoCommand.Name")]
[Description("EchoCommand.Description")]
[CommandOption(QUIET_OPTION, 'q', typeof(bool), false)]
public class EchoCommand : CommandBase<CommandContext>
{
	#region 常量定义
	const string QUIET_OPTION = "quiet";
	#endregion

	#region 重写方法
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		if(context.Arguments.IsEmpty)
		{
			if(!context.Options.Switch(QUIET_OPTION))
				context.Output.Write(CommandOutletDumper.Dump(context.Value));

			return ValueTask.FromResult(context.Value);
		}

		if(context.Arguments.Count == 1)
		{
			if(!context.Options.Switch(QUIET_OPTION))
				context.Output.Write(context.Arguments[0]);

			return ValueTask.FromResult<object>(context.Arguments[0]);
		}

		if(!context.Options.Switch(QUIET_OPTION))
		{
			foreach(var argument in context.Arguments)
				context.Output.WriteLine(argument);
		}

		return ValueTask.FromResult<object>(context.Arguments);
	}
	#endregion
}
