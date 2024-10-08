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
 * This file is part of Zongsoft.Externals.Aliyun library.
 *
 * The Zongsoft.Externals.Aliyun is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3.0 of the License,
 * or (at your option) any later version.
 *
 * The Zongsoft.Externals.Aliyun is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with the Zongsoft.Externals.Aliyun library. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Threading;
using System.Threading.Tasks;

using Zongsoft.Services;
using Zongsoft.Serialization;

namespace Zongsoft.Externals.Aliyun.Telecom
{
	[CommandOption(KEY_TEMPLATE_OPTION, typeof(string), null, true, "Text.PhoneCallCommand.Options.Template")]
	[CommandOption(KEY_PARAMETERS_OPTION, typeof(string), null, false, "Text.PhoneCallCommand.Options.Parameters")]
	[CommandOption(KEY_EXTRA_OPTION, typeof(string), null, false, "Text.PhoneCallCommand.Options.Extra")]
	[CommandOption(KEY_INTERACTIVE_OPTION, Description = "Text.PhoneCallCommand.Options.Interactive")]
	public class PhoneCallCommand : CommandBase<CommandContext>
	{
		#region 常量定义
		private const string KEY_TEMPLATE_OPTION = "template";
		private const string KEY_PARAMETERS_OPTION = "parameters";
		private const string KEY_EXTRA_OPTION = "extra";
		private const string KEY_INTERACTIVE_OPTION = "interactive";
		#endregion

		#region 成员字段
		private readonly Phone _phone;
		#endregion

		#region 构造函数
		public PhoneCallCommand(Phone phone) : base("Call")
		{
			_phone = phone;
		}
		#endregion

		#region 执行方法
		protected override object OnExecute(CommandContext context)
		{
			if(context.Expression.Arguments == null || context.Expression.Arguments.Length == 0)
				throw new CommandException("Missing arguments.");

			return Utility.ExecuteTask(() =>
				this.CallAsync(
					context.Expression.Options.GetValue<string>(KEY_TEMPLATE_OPTION),
					context.Expression.Arguments,
					context.Parameter ?? Utility.GetDictionary(context.Expression.Options.GetValue<string>(KEY_PARAMETERS_OPTION)),
					context.Expression.Options.GetValue<string>(KEY_EXTRA_OPTION),
					context.Expression.Options.Contains(KEY_INTERACTIVE_OPTION))
			);
		}

		private async Task<Phone.Result[]> CallAsync(string templateCode, string[] phoneNumbers, object parameter, string extra, bool interactive = false)
		{
			var results = new Phone.Result[phoneNumbers.Length];

			for(int i = 0; i < phoneNumbers.Length; i++)
			{
				if(interactive)
					results[i] = await _phone.CallAsync(templateCode, phoneNumbers[i], string.IsNullOrEmpty(extra) ? null : new Phone.InteractionArgument(extra), CancellationToken.None);
				else
					results[i] = await _phone.CallAsync(templateCode, phoneNumbers[i], parameter, extra, CancellationToken.None);
			}

			return results;
		}
		#endregion
	}
}
