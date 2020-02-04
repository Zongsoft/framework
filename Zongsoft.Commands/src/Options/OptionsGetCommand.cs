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
using System.ComponentModel;

using Zongsoft.Services;

namespace Zongsoft.Options.Commands
{
	/// <summary>
	/// 该命令名为“get”，本命令获取当前选项提供程序中的指定选项路径的配置信息。
	/// </summary>
	/// <remarks>
	///		<para>该命令的用法如下：</para>
	///		<code>[options.]get path1 path2 ... path#n</code>
	///		<para>通过 arguments 来指定要查找的选项路径。</para>
	/// </remarks>
	[DisplayName("Text.OptionsGetCommand.Name")]
	[Description("Text.OptionsGetCommand.Description")]
	public class OptionsGetCommand : CommandBase<CommandContext>
	{
		#region 构造函数
		public OptionsGetCommand() : base("Get")
		{
		}

		public OptionsGetCommand(string name) : base(name)
		{
		}
		#endregion

		#region 重写方法
		protected override object OnExecute(CommandContext context)
		{
			var optionProvider = OptionsCommand.GetOptionProvider(context.CommandNode);

			if(optionProvider == null)
				throw new CommandException(string.Format(Properties.Resources.Text_CannotObtainCommandTarget, "OptionProvider"));

			object result = null;

			if(context.Expression.Arguments.Length == 0)
				throw new CommandException(Properties.Resources.Text_Command_MissingArguments);

			if(context.Expression.Arguments.Length == 1)
				result = optionProvider.GetOptionValue(context.Expression.Arguments[0]);
			else
			{
				result = new object[context.Expression.Arguments.Length];

				for(int i = 0; i < context.Expression.Arguments.Length; i++)
					((object[])result)[i] = optionProvider.GetOptionValue(context.Expression.Arguments[i]);
			}

			//打印获取的结果信息
			context.Output.WriteLine(Zongsoft.Runtime.Serialization.Serializer.Json.Serialize(result));

			return result;
		}
		#endregion
	}
}
