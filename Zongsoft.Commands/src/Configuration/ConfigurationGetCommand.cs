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
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;

using Zongsoft.Components;
using Zongsoft.Configuration;

namespace Zongsoft.Configuration.Commands;

/// <summary>
/// 该命令名为“get”，本命令获取当前选项提供程序中的指定选项路径的配置信息。
/// </summary>
/// <remarks>
///		<para>该命令的用法如下：</para>
///		<code>[configuration.]get path1 path2 path3...</code>
///		<para>通过 arguments 来指定要查找的选项路径。</para>
/// </remarks>
[DisplayName("Text.ConfigurationGetCommand.Name")]
[Description("Text.ConfigurationGetCommand.Description")]
[CommandOption(KEY_SIMPLIFY_OPTION, DefaultValue = false, Description = "Text.ConfigurationCommand.Options.Simplify")]
public class ConfigurationGetCommand : CommandBase<CommandContext>
{
	#region 常量定义
	private const string KEY_SIMPLIFY_OPTION = "simplify";
	#endregion

	#region 构造函数
	public ConfigurationGetCommand() : base("Get") { }
	public ConfigurationGetCommand(string name) : base(name) { }
	#endregion

	#region 重写方法
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		var configuration = context.CommandNode.Find<ConfigurationCommand>(true)?.Configuration;

		if(configuration == null)
			throw new CommandException(string.Format(Properties.Resources.Text_CannotObtainCommandTarget, "Configuration"));

		if(context.Expression.Arguments.Length == 0)
			throw new CommandException(Properties.Resources.Text_Command_MissingArguments);

		if(context.Expression.Arguments.Length == 1)
		{
			var section = configuration.GetSection(ConfigurationUtility.GetConfigurationPath(context.Expression.Arguments[0]));
			ConfigurationCommand.Print(section, context.Output, context.Expression.Options.Contains(KEY_SIMPLIFY_OPTION), 0);
			return ValueTask.FromResult<object>(section);
		}

		var sections = new Microsoft.Extensions.Configuration.IConfigurationSection[context.Expression.Arguments.Length];

		for(int i = 0; i < context.Expression.Arguments.Length; i++)
		{
			sections[i] = configuration.GetSection(ConfigurationUtility.GetConfigurationPath(context.Expression.Arguments[i]));
			ConfigurationCommand.Print(sections[i], context.Output, context.Expression.Options.Contains(KEY_SIMPLIFY_OPTION), 0);
		}

		return ValueTask.FromResult<object>(sections);
	}
	#endregion
}
