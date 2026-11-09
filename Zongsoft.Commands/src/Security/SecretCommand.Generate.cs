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
	/// <summary>提供生成验证码的命令类。</summary>
	/// <example><code>secret.generate -name:'user.phone.change:100' -pattern:#4 13800000001 13800000002 13800000003</code></example>
	/// <remarks>
	///		<para>命令‘pattern’选项即可以表示一个固定的验证码值，如果未指定或为空则生成6位数字的验证码；也可以表示生成验证码的规则，则大致如下所示：</para>
	///		<list type="bullet">
	///			<item>guid|uuid，表示生成一个GUID值</item>
	///			<item>#{number}，表示生成{number}个的数字字符，譬如：#4</item>
	///			<item>?{number}，表示生成{number}个的含有字母或数字的字符，譬如：?8</item>
	///			<item>*{number}，完全等同于?{number}。</item>
	///		</list>
	/// </remarks>
	[DisplayName("SecretGenerateCommand.Name")]
	[Description("SecretGenerateCommand.Description")]
	[CommandOption(KEY_NAME_OPTION, typeof(string), null, true, "SecretGenerateCommand.Options.Name")]
	[CommandOption(KEY_PATTERN_OPTION, typeof(string), null, false, "SecretGenerateCommand.Options.Pattern")]
	public class GenerateCommand : CommandBase<CommandContext>
	{
		#region 常量定义
		private const string KEY_NAME_OPTION = "name";
		private const string KEY_PATTERN_OPTION = "pattern";
		private const string KEY_TIMEOUT_OPTION = "timeout";
		#endregion

		#region 构造函数
		public GenerateCommand() : base("Generate") { }
		public GenerateCommand(string name) : base(name) { }
		#endregion

		#region 重写方法
		protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
		{
			//从环境中查找秘密提供程序
			var secretor = context.Find<SecretCommand>(true)?.Secretor ?? throw new CommandException(Properties.Resources.Secretor_Required_Message);
			var name = context.Options.GetValue<string>(KEY_NAME_OPTION);
			var pattern = context.Options.GetValue<string>(KEY_PATTERN_OPTION);

			switch(context.Arguments.Count)
			{
				case 0:
					return await secretor.GenerateAsync(name, pattern, null, cancellation);
				case 1:
					return await secretor.GenerateAsync(name, pattern, context.Arguments[0], cancellation);
			}

			//定义返回验证码的数组
			var results = new string[context.Arguments.Count];

			for(int i = 0; i < context.Arguments.Count; i++)
			{
				results[i] = await secretor.GenerateAsync(name, pattern, context.Arguments[i], cancellation);
			}

			return results;
		}
		#endregion
	}
}