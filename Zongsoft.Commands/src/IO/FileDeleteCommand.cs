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

using Zongsoft.Components;

namespace Zongsoft.IO.Commands;

public class FileDeleteCommand : CommandBase<CommandContext>
{
	#region 构造函数
	public FileDeleteCommand() : base("Delete") { }
	public FileDeleteCommand(string name) : base(name) { }
	#endregion

	#region 重写方法
	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		if(context.Arguments.IsEmpty)
			throw new CommandException(Properties.Resources.Command_ArgumentsRequired_Message);

		for(int i = 0; i < context.Arguments.Count; i++)
		{
			var filePath = context.Arguments[i];
			var succeed = await FileSystem.File.DeleteAsync(filePath);
			var message = succeed ?
				Properties.Resources.FileDeleteSucceed_Message :
				Properties.Resources.FileDeleteFailed_Message;

			context.Output.WriteLine((succeed ? CommandOutletColor.Green : CommandOutletColor.Red), $"[{i + 1}] `{filePath}` {message}");
		}

		return null;
	}
	#endregion
}
