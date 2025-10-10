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
 * Copyright (C) 2010-2025 Zongsoft Studio <http://www.zongsoft.com>
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

namespace Zongsoft.Security.Commands;

/// <summary>
/// 提供验证码校验的命令类。
/// </summary>
/// <example>
///		<code>secret.verify -name:'user.email:100' 123456</code>
/// </example>
[DisplayName("Text.SecretVerifyCommand.Name")]
[Description("Text.SecretVerifyCommand.Description")]
[CommandOption(KEY_NAME_OPTION, typeof(string), null, true, "Text.SecretVerifyCommand.Options.Name")]
public class SecretVerifyCommand : CommandBase<CommandContext>
{
	#region 常量定义
	private const string KEY_NAME_OPTION = "name";
	#endregion

	#region 构造函数
	public SecretVerifyCommand() : base("Verify") { }
	public SecretVerifyCommand(string name) : base(name) { }
	#endregion

	#region 重写方法
	protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
	{
		if(context.Arguments.IsEmpty)
			throw new CommandException(Properties.Resources.Text_Command_MissingArguments);

		//从环境中查找秘密提供程序
		var secretor = context.Find<SecretCommand>(true)?.Secretor ?? throw new CommandException("Missing required secretor for the command.");
		(var succeed, var extra) = await secretor.VerifyAsync(context.GetOptions().GetValue<string>(KEY_NAME_OPTION), context.Arguments[0], cancellation);

		if(succeed)
		{
			if(extra != null && extra.Length > 0)
				context.Output.WriteLine(extra);

			return true;
		}

		return false;
	}
	#endregion
}
