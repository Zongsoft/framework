﻿/*
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
using System.ComponentModel;

using Zongsoft.Services;

namespace Zongsoft.Security.Commands
{
	/// <summary>
	/// 提供生成验证码的命令类。
	/// </summary>
	/// <example>
	///		<code>secret.generate -name:'user.phone.change:100' -pattern:#4 13800000001 13800000002 13800000003</code>
	/// </example>
	/// <remarks>
	///		<para>命令‘pattern’选项即可以表示一个固定的验证码值，如果未指定或为空则生成6位数字的验证码；也可以表示生成验证码的规则，则大致如下所示：</para>
	///		<list type="bullet">
	///			<item>guid|uuid，表示生成一个GUID值</item>
	///			<item>#{number}，表示生成{number}个的数字字符，譬如：#4</item>
	///			<item>?{number}，表示生成{number}个的含有字母或数字的字符，譬如：?8</item>
	///			<item>*{number}，完全等同于?{number}。</item>
	///		</list>
	/// </remarks>
	[DisplayName("Text.SecretGenerateCommand.Name")]
	[Description("Text.SecretGenerateCommand.Description")]
	[CommandOption(KEY_NAME_OPTION, typeof(string), null, true, "Text.SecretGenerateCommand.Options.Name")]
	[CommandOption(KEY_PATTERN_OPTION, typeof(string), null, false, "Text.SecretGenerateCommand.Options.Pattern")]
	public class SecretGenerateCommand : CommandBase<CommandContext>
	{
		#region 常量定义
		private const string KEY_NAME_OPTION = "name";
		private const string KEY_PATTERN_OPTION = "pattern";
		private const string KEY_TIMEOUT_OPTION = "timeout";
		#endregion

		#region 构造函数
		public SecretGenerateCommand() : base("Generate")
		{
		}

		public SecretGenerateCommand(string name) : base(name)
		{
		}
		#endregion

		#region 重写方法
		protected override object OnExecute(CommandContext context)
		{
			//从环境中查找秘密提供程序
			var secretor = context.CommandNode.Find<SecretCommand>(true)?.Secretor;

			if(secretor == null)
				throw new CommandException("Missing required secretor for the command.");

			var name = context.Expression.Options.GetValue<string>(KEY_NAME_OPTION);
			var pattern = context.Expression.Options.GetValue<string>(KEY_PATTERN_OPTION);

			switch(context.Expression.Arguments.Length)
			{
				case 0:
					return secretor.Generate(name, pattern, null);
				case 1:
					return secretor.Generate(name, pattern, context.Expression.Arguments[0]);
			}

			//定义返回验证码的数组
			var results = new string[context.Expression.Arguments.Length];

			for(int i = 0; i < context.Expression.Arguments.Length; i++)
			{
				results[i] = secretor.Generate(name, pattern, context.Expression.Arguments[i]);
			}

			return results;
		}
		#endregion
	}
}
