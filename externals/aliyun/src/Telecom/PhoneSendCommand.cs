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

using Zongsoft.Components;

namespace Zongsoft.Externals.Aliyun.Telecom
{
	[CommandOption(KEY_TEMPLATE_OPTION, 't', typeof(string), null, true, "Text.PhoneSendCommand.Options.Template")]
	[CommandOption(KEY_PARAMETERS_OPTION, 'p', typeof(string), null, false, "Text.PhoneSendCommand.Options.Parameters")]
	[CommandOption(KEY_SCHEME_OPTION, 's', typeof(string), null, false, "Text.PhoneSendCommand.Options.Scheme")]
	[CommandOption(KEY_EXTRA_OPTION, 'e', typeof(string), null, false, "Text.PhoneSendCommand.Options.Extra")]
	public class PhoneSendCommand : CommandBase<CommandContext>
	{
		#region 常量定义
		private const string KEY_TEMPLATE_OPTION = "template";
		private const string KEY_PARAMETERS_OPTION = "parameters";
		private const string KEY_SCHEME_OPTION = "scheme";
		private const string KEY_EXTRA_OPTION = "extra";
		#endregion

		#region 成员字段
		private readonly Phone _phone;
		#endregion

		#region 构造函数
		public PhoneSendCommand(Phone phone) : base("Send")
		{
			_phone = phone;
		}
		#endregion

		#region 执行方法
		protected override async ValueTask<object> OnExecuteAsync(CommandContext context, CancellationToken cancellation)
		{
			if(context.Arguments == null || context.Arguments.IsEmpty)
				throw new CommandException("Missing arguments.");

			var result = await _phone.SendAsync(
				context.Options.GetValue<string>(KEY_TEMPLATE_OPTION),
				context.Arguments,
				context.Value ?? Utility.GetDictionary(context.Options.GetValue<string>(KEY_PARAMETERS_OPTION)),
				context.Options.GetValue<string>(KEY_SCHEME_OPTION),
				context.Options.GetValue<string>(KEY_EXTRA_OPTION), cancellation);

			return result;
		}
		#endregion
	}
}
