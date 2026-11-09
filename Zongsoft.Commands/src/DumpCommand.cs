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

namespace Zongsoft.Commands;

[CommandOption(DEPTH_OPTION, 'd', typeof(int), 3)]
[CommandOption(INDENT_OPTION, 'i', typeof(string), "\t")]
[CommandOption(BINDING_OPTION, 'b', typeof(System.Reflection.BindingFlags), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)]
public class DumpCommand : CommandBase<CommandContext>
{
	#region 常量定义
	private const string DEPTH_OPTION = "depth";
	private const string INDENT_OPTION = "indent";
	private const string BINDING_OPTION = "binding";
	#endregion

	#region 重写方法
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		context.Output.Dump(context.Value, new CommandOutletDumperOptions()
		{
			MaximumDepth = context.Options.GetValue<int>(DEPTH_OPTION),
			IndentString = context.Options.GetValue<string>(INDENT_OPTION),
			Binding = context.Options.GetValue<System.Reflection.BindingFlags>(BINDING_OPTION),
		});

		return ValueTask.FromResult(context.Value);
	}
	#endregion
}
