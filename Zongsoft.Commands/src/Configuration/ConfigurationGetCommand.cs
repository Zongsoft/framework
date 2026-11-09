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
using Zongsoft.Configuration;

namespace Zongsoft.Configuration.Commands;

/// <summary>该命令名为“get”，本命令获取当前选项提供程序中的指定选项路径的配置信息。</summary>
/// <remarks>
///		<para>该命令的用法如下：</para>
///		<code>[configuration.]get path1 path2 path3...</code>
///		<para>通过 arguments 来指定要查找的选项路径。</para>
/// </remarks>
[DisplayName("ConfigurationGetCommand.Name")]
[Description("ConfigurationGetCommand.Description")]
[CommandOption(SIMPLIFY_OPTION, 's', Description = "ConfigurationCommand.Options.Simplify")]
public class ConfigurationGetCommand : CommandBase<CommandContext>
{
	#region 常量定义
	private const string SIMPLIFY_OPTION = "simplify";
	#endregion

	#region 构造函数
	public ConfigurationGetCommand() : base("Get") { }
	public ConfigurationGetCommand(string name) : base(name) { }
	#endregion

	#region 重写方法
	protected override ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		var configuration = context.Find<ConfigurationCommand>(true)?.Configuration;

		if(configuration == null)
			throw new CommandException(string.Format(Properties.Resources.CannotObtainCommandTarget, "Configuration"));

		if(context.Arguments.IsEmpty)
			throw new CommandException(Properties.Resources.Command_ArgumentsRequired_Message);

		if(context.Arguments.Count == 1)
		{
			var section = configuration.GetSection(ConfigurationUtility.GetConfigurationPath(context.Arguments[0]));
			ConfigurationCommand.Print(section, context.Output, context.Options.Switch(SIMPLIFY_OPTION), 0);
			return ValueTask.FromResult<object>(section);
		}

		var sections = new Microsoft.Extensions.Configuration.IConfigurationSection[context.Arguments.Count];

		for(int i = 0; i < context.Arguments.Count; i++)
		{
			sections[i] = configuration.GetSection(ConfigurationUtility.GetConfigurationPath(context.Arguments[i]));
			ConfigurationCommand.Print(sections[i], context.Output, context.Options.Switch(SIMPLIFY_OPTION), 0);
		}

		return ValueTask.FromResult<object>(sections);
	}
	#endregion
}
