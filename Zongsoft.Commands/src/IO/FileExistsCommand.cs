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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Components;

namespace Zongsoft.IO.Commands;

public class FileExistsCommand : CommandBase<CommandContext>
{
	#region 构造函数
	public FileExistsCommand() : base("Exists") { }
	public FileExistsCommand(string name) : base(name) { }
	#endregion

	#region 重写方法
	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		if(context.Arguments.IsEmpty)
			throw new CommandException(Properties.Resources.Command_ArgumentsRequired_Message);

		if(context.Arguments.Count == 1)
			return await ExistsAsync(context.Arguments[0]);
		else
			return context.Arguments.Select(async path => await ExistsAsync(path)).ToArray();

		async ValueTask<bool> ExistsAsync(string path)
		{
			var existed = await FileSystem.File.ExistsAsync(path);

			if(existed)
				context.Output.WriteLine(CommandOutletColor.Green, string.Format(Properties.Resources.FileExisted, path));
			else
				context.Output.WriteLine(CommandOutletColor.Red, string.Format(Properties.Resources.FileNotExisted, path));

			return existed;
		}
	}
	#endregion
}
