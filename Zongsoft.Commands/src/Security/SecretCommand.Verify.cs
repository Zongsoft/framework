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

namespace Zongsoft.Security.Commands;

partial class SecretCommand
{
	/// <summary>提供验证码校验的命令类。</summary>
	/// <example><code>secret.verify -name:'user.email:100' 123456</code></example>
	[DisplayName("SecretVerifyCommand.Name")]
	[Description("SecretVerifyCommand.Description")]
	[CommandOption(KEY_NAME_OPTION, typeof(string), null, true, "SecretVerifyCommand.Options.Name")]
	public class VerifyCommand : CommandBase<CommandContext>
	{
		#region 常量定义
		private const string KEY_NAME_OPTION = "name";
		#endregion

		#region 构造函数
		public VerifyCommand() : base("Verify") { }
		public VerifyCommand(string name) : base(name) { }
		#endregion

		#region 重写方法
		protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
		{
			if(context.Arguments.IsEmpty)
				throw new CommandException(Properties.Resources.Command_ArgumentsRequired_Message);

			//从环境中查找秘密提供程序
			var secretor = context.Find<SecretCommand>(true)?.Secretor ?? throw new CommandException(Properties.Resources.Secretor_Required_Message);
			(var succeed, var extra) = await secretor.VerifyAsync(context.Options.GetValue<string>(KEY_NAME_OPTION), context.Arguments[0], cancellation);

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
}