/*
 *   _____                                ______
 *  /_   /  ____  ____  ____  _________  / __/ /_
 *    / /  / __ \/ __ \/ __ \/ ___/ __ \/ /_/ __/
 *   / /__/ /_/ / / / / /_/ /\_ \/ /_/ / __/ /_
 *  /____/\____/_/ /_/\__  /____/\____/_/  \__/
 *                   /____/
 *
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2020 Zongsoft Studio <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.Plugins library.
 *
 * The Zongsoft.Plugins is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Plugins is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Plugins library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.ComponentModel;

using Zongsoft.Services;

namespace Zongsoft.Plugins.Commands
{
	[DisplayName("Text.TreeCommand.Name")]
	[Description("Text.TreeCommand.Description")]
	[CommandOption("depth", Type = typeof(int), DefaultValue = 3, Description = "Text.TreeCommand.Options.Depth")]
	[CommandOption("path", Description = "Text.TreeCommand.Options.Path")]
	public class TreeCommand : CommandBase<CommandContext>
	{
		#region 成员字段
		private PluginContext _pluginContext;
		#endregion

		#region 构造函数
		public TreeCommand(PluginContext pluginContext) : this("Tree", pluginContext)
		{
		}

		public TreeCommand(string name, PluginContext pluginContext) : base(name)
		{
			_pluginContext = pluginContext ?? throw new ArgumentNullException(nameof(pluginContext));
		}
		#endregion

		#region 重写方法
		protected override object OnExecute(CommandContext context)
		{
			var node = context.Parameter as PluginTreeNode;

			if(node == null)
			{
				if(context.Parameter != null)
					throw new CommandException(string.Format(Properties.Resources.Text_Message_InvalidCommandParameter, context.CommandNode.FullPath));

				if(context.Expression.Arguments.Length == 0)
					throw new CommandException(Properties.Resources.Text_Message_MissingCommandArguments);

				if(context.Expression.Arguments.Length > 1)
					throw new CommandException(Properties.Resources.Text_Message_CommandArgumentsTooMany);

				node = _pluginContext.PluginTree.Find(context.Expression.Arguments[0]);

				if(node == null)
				{
					context.Output.WriteLine(CommandOutletColor.DarkRed, string.Format(Properties.Resources.Text_Message_PluginNodeNotFound, context.Expression.Arguments[0]));
					return null;
				}
			}

			this.WritePluginTree(context.Output, node, context.Expression.Options.GetValue<int>("maxDepth"), 0, 0, context.Expression.Options.Contains("fullPath"));
			return node;
		}
		#endregion

		public void WritePluginTree(ICommandOutlet output, PluginTreeNode node, int maxDepth, int depth, int index, bool isFullPath)
		{
			if(node == null)
				return;

			var indent = depth > 0 ? new string('\t', depth) : string.Empty;

			if(depth > 0)
			{
				output.Write(CommandOutletColor.DarkMagenta, indent + "[{0}.{1}] ", depth, index);
			}

			output.Write(isFullPath ? node.FullPath : node.Name);
			output.Write(CommandOutletColor.DarkGray, " [{0}]", node.NodeType);

			if(node.Plugin == null)
				output.WriteLine();
			else
			{
				output.Write(CommandOutletColor.DarkGreen, "@");
				output.WriteLine(CommandOutletColor.DarkGray, node.Plugin.Name);
			}

			var target = node.UnwrapValue(ObtainMode.Never);
			if(target != null)
				output.WriteLine(CommandOutletColor.DarkYellow, "{0}{1}", indent, target.GetType().FullName);

			if(maxDepth > 0 && depth >= maxDepth)
				return;

			for(int i = 0; i < node.Children.Count; i++)
			{
				WritePluginTree(output, node.Children[i], maxDepth, depth + 1, i + 1, isFullPath);
			}
		}
	}
}
