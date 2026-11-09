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
using System.ComponentModel;
using System.Collections.Generic;

using Zongsoft.Components;

namespace Zongsoft.Plugins.Commands;

[DisplayName("Text.FindCommand.Name")]
[Description("Text.FindCommand.Description")]
[CommandOption("obtain", 'b', typeof(ObtainMode), DefaultValue = ObtainMode.Never, Description = "Text.FindCommand.Options.ObtainMode")]
[CommandOption("depth", 'd', typeof(int), DefaultValue = 3, Description = "Text.FindCommand.Options.Depth")]
public class FindCommand : CommandBase<CommandContext>
{
	#region 成员字段
	private readonly PluginTree _pluginTree;
	#endregion

	#region 构造函数
	public FindCommand(PluginTree pluginTree) : this("Find", pluginTree) { }
	public FindCommand(string name, PluginTree pluginTree) : base(name)
	{
		_pluginTree = pluginTree ?? throw new ArgumentNullException(nameof(pluginTree));
	}
	#endregion

	#region 重写方法
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		if(context.Arguments.IsEmpty)
			throw new CommandException(Properties.Resources.Text_Message_MissingCommandArguments);

		var result = new List<object>(context.Arguments.Count);

		for(int i = 0; i < context.Arguments.Count; i++)
		{
			var node = _pluginTree.Find(context.Arguments[i]);

			if(node == null)
				context.Output.WriteLine(CommandOutletColor.DarkRed, string.Format(Properties.Resources.Plugin_NodeNotFound_Message, context.Arguments[i]));
			else
			{
				var mode = context.Options.GetValue<ObtainMode>("obtain");
				var value = node.UnwrapValue(mode);

				if(value != null)
					result.Add(value);

				context.Output.Write(Utility.GetPluginNodeContent(node, mode, context.Options.GetValue<int>("depth")));
			}
		}

		return result.Count switch
		{
			0 => ValueTask.FromResult<object>(null),
			1 => ValueTask.FromResult<object>(result[0]),
			_ => ValueTask.FromResult<object>(result)
		};
	}
	#endregion
}
