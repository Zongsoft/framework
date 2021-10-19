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
	[DisplayName("Text.FindCommand.Name")]
	[Description("Text.FindCommand.Description")]
	[CommandOption("obtain", Type = typeof(ObtainMode), DefaultValue = ObtainMode.Never, Description = "Text.FindCommand.Options.ObtainMode")]
	[CommandOption("depth", Type = typeof(int), DefaultValue = 3, Description = "Text.FindCommand.Options.Depth")]
	public class FindCommand : CommandBase<CommandContext>
	{
		#region 成员字段
		private readonly PluginTree _pluginTree;
		#endregion

		#region 构造函数
		public FindCommand(PluginTree pluginTree) : this("Find", pluginTree)
		{
		}

		public FindCommand(string name, PluginTree pluginTree) : base(name)
		{
			_pluginTree = pluginTree ?? throw new ArgumentNullException(nameof(pluginTree));
		}
		#endregion

		#region 重写方法
		protected override object OnExecute(CommandContext context)
		{
			if(context.Expression.Arguments.Length == 0)
				throw new CommandException(Properties.Resources.Text_Message_MissingCommandArguments);

			var result = new PluginTreeNode[context.Expression.Arguments.Length];

			for(int i = 0; i < context.Expression.Arguments.Length; i++)
			{
				result[i] = _pluginTree.Find(context.Expression.Arguments[i]);

				if(result[i] == null)
					context.Output.WriteLine(CommandOutletColor.DarkRed, string.Format(Properties.Resources.Text_Message_PluginNodeNotFound, context.Expression.Arguments[i]));

				Utility.PrintPluginNode(context.Output, result[i],
							context.Expression.Options.GetValue<ObtainMode>("obtain"),
							context.Expression.Options.GetValue<int>("depth"));
			}

			if(result.Length == 1)
				return result[0];

			return result;
		}
		#endregion
	}
}
