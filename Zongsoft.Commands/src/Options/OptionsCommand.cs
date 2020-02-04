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
 * This file is part of Zongsoft.Commands library.
 *
 * The Zongsoft.Commands is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Commands is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Commands library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;

using Zongsoft.Services;

namespace Zongsoft.Options.Commands
{
	public class OptionsCommand : CommandBase<CommandContext>
	{
		#region 成员字段
		private IOptionProvider _options;
		#endregion

		#region 构造函数
		public OptionsCommand() : base("Options")
		{
			_options = Zongsoft.Services.ApplicationContext.Current.Options;
		}

		public OptionsCommand(string name) : base(name)
		{
			_options = Zongsoft.Services.ApplicationContext.Current.Options;
		}
		#endregion

		#region 公共属性
		public IOptionProvider Options
		{
			get
			{
				return _options;
			}
			set
			{
				_options = value ?? throw new ArgumentNullException();
			}
		}
		#endregion

		#region 执行方法
		protected override object OnExecute(CommandContext context)
		{
			if(_options is OptionManager manager)
			{
				foreach(var node in manager.Nodes)
				{
					this.Print(context.Output, node, 0);
				}
			}

			return _options;
		}
		#endregion

		#region 私有方法
		private void Print(ICommandOutlet output, OptionNode node, int depth)
		{
			if(node == null)
				return;

			output.Write(CommandOutletColor.DarkYellow, (depth > 0 ? new string('\t', depth) : string.Empty) + node.Name);

			if(string.IsNullOrWhiteSpace(node.Title))
				output.WriteLine();
			else
				output.WriteLine(CommandOutletColor.DarkGray, " [{0}]", node.Title);

			if(node.Option != null && node.Option.OptionObject != null)
			{
				output.WriteLine(Zongsoft.Runtime.Serialization.Serializer.Json.Serialize(node.Option.OptionObject));
			}

			foreach(var child in node.Children)
			{
				Print(output, child, depth + 1);
			}
		}
		#endregion

		#region 静态方法
		internal static IOptionProvider GetOptionProvider(CommandTreeNode node)
		{
			if(node == null)
				return null;

			if(node.Command is OptionsCommand command)
				return command.Options;

			return GetOptionProvider(node.Parent);
		}
		#endregion
	}
}
