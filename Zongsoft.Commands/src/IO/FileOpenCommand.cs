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
using System.Collections.Generic;

using Zongsoft.Components;

namespace Zongsoft.IO.Commands;

[CommandOption(KEY_MODE_OPTION, typeof(FileMode), FileMode.Open, "FileCommand.Options.Mode")]
[CommandOption(KEY_SHARE_OPTION, typeof(FileShare), FileShare.Read, "FileCommand.Options.Share")]
[CommandOption(KEY_ACCESS_OPTION, typeof(FileAccess), FileAccess.Read, "FileCommand.Options.Access")]
public class FileOpenCommand : CommandBase<CommandContext>, ICommandCompletion
{
	#region 常量定义
	private const string KEY_MODE_OPTION = "mode";
	private const string KEY_SHARE_OPTION = "share";
	private const string KEY_ACCESS_OPTION = "access";
	#endregion

	#region 构造函数
	public FileOpenCommand() : base("Open") { }
	public FileOpenCommand(string name) : base(name) { }
	#endregion

	#region 执行方法
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		return FileUtility.OpenFileAsync(context,
			context.Options.GetValue<FileMode>(KEY_MODE_OPTION),
			context.Options.GetValue<FileAccess>(KEY_ACCESS_OPTION),
			context.Options.GetValue<FileShare>(KEY_SHARE_OPTION), cancellation);
	}
	#endregion

	#region 完成回调
	public ValueTask OnCompletedAsync(CommandCompletionContext context) => FileUtility.DisposeAsync(context.Result);
	#endregion
}
