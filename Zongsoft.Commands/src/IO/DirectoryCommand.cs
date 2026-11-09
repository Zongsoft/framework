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

public class DirectoryCommand : CommandBase<CommandContext>
{
	#region 构造函数
	public DirectoryCommand() : base("Directory") { }
	public DirectoryCommand(string name) : base(name) { }
	#endregion

	#region 公共属性
	public string Current { get => string.IsNullOrEmpty(field) ? "/" : field; set; }
	#endregion

	#region 执行方法
	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		if(context.Arguments.Count > 1)
			throw new CommandException(Properties.Resources.Command_TooManyArguments);

		if(context.Arguments.Count == 1)
			this.Current = context.Arguments[0];

		context.Output.WriteLine(this.Current);
		return await FileSystem.Directory.GetInfoAsync(this.Current, cancellation);
	}
	#endregion
}
