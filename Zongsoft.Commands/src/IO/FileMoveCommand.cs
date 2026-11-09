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
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Components;

namespace Zongsoft.IO.Commands;

public class FileMoveCommand : CommandBase<CommandContext>
{
	#region 构造函数
	public FileMoveCommand() : base("Move") { }
	public FileMoveCommand(string name) : base(name) { }
	#endregion

	#region 执行方法
	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		if(context.Arguments.Count != 2)
			throw new CommandException(string.Format(Properties.Resources.Command_RequiresCountOfArguments, "2"));

		await FileSystem.File.MoveAsync(context.Arguments[0], context.Arguments[1]);
		return null;
	}
	#endregion
}
