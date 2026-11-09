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

namespace Zongsoft.Upgrading.Commands;

[CommandOption(RECURSIVE_OPTION, 'r', typeof(bool), true)]
public class DeleteCommand : CommandBase<CommandContext>
{
	#region 单例字段
	public static readonly DeleteCommand Instance = new();
	#endregion

	#region 常量定义
	private const string RECURSIVE_OPTION = "recursive";
	#endregion

	#region 构造函数
	public DeleteCommand() : base("Delete") { }
	public DeleteCommand(string name) : base(name) { }
	#endregion

	#region 重写方法
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		for(int i = 0; i < context.Arguments.Count; i++)
		{
			if(Path.EndsInDirectorySeparator(context.Arguments[i]))
				Directory.Delete(context.Arguments[i], context.Options.Switch(RECURSIVE_OPTION));
			else
				File.Delete(context.Arguments[i]);
		}

		return default;
	}
	#endregion
}
