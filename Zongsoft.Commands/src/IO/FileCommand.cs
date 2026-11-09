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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Zongsoft.Components;

namespace Zongsoft.IO.Commands;

[CommandOption(KEY_MODE_OPTION, typeof(FileMode), FileMode.Open, "FileCommand.Options.Mode")]
[CommandOption(KEY_SHARE_OPTION, typeof(FileShare), FileShare.Read, "FileCommand.Options.Share")]
[CommandOption(KEY_ACCESS_OPTION, typeof(FileAccess), FileAccess.Read, "FileCommand.Options.Access")]
[CommandOption(KEY_ENCODING_OPTION, typeof(Encoding), null, "FileCommand.Options.Encoding")]
public class FileCommand : CommandBase<CommandContext>, ICommandCompletion
{
	#region 常量定义
	private const string KEY_MODE_OPTION = "mode";
	private const string KEY_SHARE_OPTION = "share";
	private const string KEY_ACCESS_OPTION = "access";
	private const string KEY_ENCODING_OPTION = "encoding";
	#endregion

	#region 构造函数
	public FileCommand() : base("File") { }
	public FileCommand(string name) : base(name) { }
	#endregion

	#region 执行方法
	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		bool isSaving = context.Value != null;

		if(!context.Options.TryGetValue<FileMode>(KEY_MODE_OPTION, out var mode))
			mode = isSaving ? FileMode.Create : FileMode.Open;

		if(!context.Options.TryGetValue<FileAccess>(KEY_ACCESS_OPTION, out var access))
			access = isSaving ? FileAccess.ReadWrite : FileAccess.Read;

		//打开一个或多个文件流
		var result = await FileUtility.OpenFileAsync(context, mode, access, context.Options.GetValue<FileShare>(KEY_SHARE_OPTION), cancellation).ConfigureAwait(false);

		//如果是写入操作则执行保存方法
		if(isSaving && result != null)
			await FileUtility.SaveAsync(result, context.Value, context.Options.GetValue<Encoding>(KEY_ENCODING_OPTION), cancellation);

		return result;
	}
	#endregion

	#region 执行完成
	public ValueTask OnCompletedAsync(CommandCompletionContext context) => FileUtility.DisposeAsync(context.Result);
	#endregion
}
