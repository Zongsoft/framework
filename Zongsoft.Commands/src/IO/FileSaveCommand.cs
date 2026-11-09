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

[CommandOption(KEY_MODE_OPTION, typeof(FileMode), FileMode.OpenOrCreate, "FileCommand.Options.Mode")]
[CommandOption(KEY_SHARE_OPTION, typeof(FileShare), FileShare.Read, "FileCommand.Options.Share")]
[CommandOption(KEY_ACCESS_OPTION, typeof(FileAccess), FileAccess.ReadWrite, "FileCommand.Options.Access")]
[CommandOption(KEY_ENCODING_OPTION, typeof(Encoding), null, "FileCommand.Options.Encoding")]
public class FileSaveCommand : CommandBase<CommandContext>, ICommandCompletion
{
	#region 常量定义
	private const string KEY_MODE_OPTION = "mode";
	private const string KEY_SHARE_OPTION = "share";
	private const string KEY_ACCESS_OPTION = "access";
	private const string KEY_ENCODING_OPTION = "encoding";
	#endregion

	#region 构造函数
	public FileSaveCommand() : base("Save") { }
	public FileSaveCommand(string name) : base(name) { }
	#endregion

	#region 执行方法
	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		//打开一个或多个文件流
		var result = await FileUtility.OpenFileAsync(context,
			context.Options.GetValue<FileMode>(KEY_MODE_OPTION),
			context.Options.GetValue<FileAccess>(KEY_ACCESS_OPTION),
			context.Options.GetValue<FileShare>(KEY_SHARE_OPTION), cancellation).ConfigureAwait(false);

		if(result != null)
			await FileUtility.SaveAsync(result, context.Value, context.Options.GetValue<Encoding>(KEY_ENCODING_OPTION), cancellation);

		return result;
	}
	#endregion

	#region 完成回调
	public ValueTask OnCompletedAsync(CommandCompletionContext context) => FileUtility.DisposeAsync(context.Result);
	#endregion
}
