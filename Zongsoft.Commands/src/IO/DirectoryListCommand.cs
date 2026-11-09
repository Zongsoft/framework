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
using System.Collections.Generic;

using Zongsoft.Components;

namespace Zongsoft.IO.Commands;

[CommandOption(FULLY_OPTION, 'f')]
[CommandOption(RECURSIVE_OPTION, 'r')]
public class DirectoryListCommand : CommandBase<CommandContext>
{
	#region 常量定义
	private const string RECURSIVE_OPTION = "recursive";
	private const string FULLY_OPTION = "fully";
	#endregion

	#region 构造函数
	public DirectoryListCommand() : base("List") { }
	public DirectoryListCommand(string name) : base(name) { }
	#endregion

	#region 重写方法
	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		if(context.Arguments.Count > 1)
			throw new CommandException(Properties.Resources.Command_TooManyArguments);

		var result = new List<PathInfo>();
		var fully = context.Options.Switch(FULLY_OPTION);
		var path = context.Find<DirectoryCommand>(true)?.Current ?? "/";
		var pattern = context.Arguments.IsEmpty ? null : context.Arguments[0];
		var children = FileSystem.Directory.GetChildrenAsync(path, pattern, context.Options.Switch(RECURSIVE_OPTION), cancellation);

		await foreach(var child in children)
		{
			if(child is DirectoryInfo directory)
				DumpPath(context.Output, directory, fully);
			if(child is FileInfo file)
				DumpFile(context.Output, file, fully);

			result.Add(child);
		}

		return result;
	}
	#endregion

	private static void DumpPath(ICommandOutlet output, PathInfo info, bool fully)
	{
		var content = CommandOutletContent.Create()
			.Append(CommandOutletColor.Blue, fully ? info.Path : info.Name)
			.Append(" | ")
			.Append(CommandOutletColor.DarkCyan, info.CreatedTime)
			.Append(" | ")
			.Append(CommandOutletColor.DarkCyan, info.ModifiedTime)
			.Append(" | ")
			.Append(info.Url);

		output.WriteLine(content);
	}

	private static void DumpFile(ICommandOutlet output, FileInfo info, bool fully)
	{
		var content = CommandOutletContent.Create()
			.Append(CommandOutletColor.DarkGreen, fully ? info.Path : info.Name)
			.Append(" | ")
			.Append(CommandOutletColor.DarkYellow, info.Type)
			.Append(" | ")
			.Append(CommandOutletColor.DarkMagenta, info.Size)
			.Append(" | ")
			.Append(CommandOutletColor.DarkCyan, info.CreatedTime)
			.Append(" | ")
			.Append(CommandOutletColor.DarkCyan, info.ModifiedTime)
			.Append(" | ")
			.Append(info.Url);

		output.WriteLine(content);
	}
}
