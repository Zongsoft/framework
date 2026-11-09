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

using Zongsoft.Components;

namespace Zongsoft.Plugins.Commands;

[DisplayName("Text.TreeCommand.Name")]
[Description("Text.TreeCommand.Description")]
[CommandOption("depth", 'd', typeof(int), DefaultValue = 3, Description = "Text.TreeCommand.Options.Depth")]
[CommandOption("path", 'p', typeof(string), Description = "Text.TreeCommand.Options.Path")]
public class TreeCommand : CommandBase<CommandContext>
{
	#region 成员字段
	private readonly PluginTree _pluginTree;
	#endregion

	#region 构造函数
	public TreeCommand(PluginTree pluginTree) : this("Tree", pluginTree) { }
	public TreeCommand(string name, PluginTree pluginTree) : base(name)
	{
		_pluginTree = pluginTree ?? throw new ArgumentNullException(nameof(pluginTree));
	}
	#endregion

	#region 重写方法
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		if(context.Value is not PluginTreeNode node)
		{
			if(context.Value != null)
				throw new CommandException(string.Format(Properties.Resources.Text_Message_InvalidCommandParameter, context.CommandNode.FullPath));

			if(context.Arguments.IsEmpty)
				throw new CommandException(Properties.Resources.Text_Message_MissingCommandArguments);

			if(context.Arguments.Count > 1)
				throw new CommandException(Properties.Resources.Text_Message_CommandArgumentsTooMany);

			node = _pluginTree.Find(context.Arguments[0]);

			if(node == null)
			{
				context.Output.WriteLine(CommandOutletColor.DarkRed, string.Format(Properties.Resources.Plugin_NodeNotFound_Message, context.Arguments[0]));
				return ValueTask.FromResult<object>(node);
			}
		}

		WritePluginTree(context.Output, node, context.Options.GetValue<int>("depth"), 0, 0, context.Options.Contains("qualified"));
		return ValueTask.FromResult<object>(node);
	}
	#endregion

	#region 打印信息
	private static void WritePluginTree(ICommandOutlet output, PluginTreeNode node, int maxDepth, int depth, int index, bool qualified)
	{
		if(node == null)
			return;

		var indent = depth > 0 ? new string('\t', depth) : string.Empty;

		if(depth > 0)
		{
			output.Write(CommandOutletColor.DarkMagenta, indent + $"[{depth}.{index}] ");
		}

		output.Write(qualified ? node.FullPath : node.Name);
		output.Write(CommandOutletColor.DarkCyan, $"({node.NodeType})");

		if(node.Plugin == null)
			output.WriteLine();
		else
		{
			output.Write(CommandOutletColor.DarkGreen, "@");
			output.WriteLine(CommandOutletColor.DarkGray, node.Plugin.Name);
		}

		var target = node.UnwrapValue(ObtainMode.Never);
		if(target != null)
			output.WriteLine(CommandOutletColor.DarkYellow, $"{indent}{Common.TypeAlias.GetAlias(target.GetType())}");

		if(maxDepth > 0 && depth >= maxDepth)
			return;

		for(int i = 0; i < node.Children.Count; i++)
		{
			WritePluginTree(output, node.Children[i], maxDepth, depth + 1, i + 1, qualified);
		}
	}
	#endregion
}
